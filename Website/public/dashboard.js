import { app } from "./firebase-config.js";
import {
  getAuth,
  signOut,
  onAuthStateChanged,
} from "https://www.gstatic.com/firebasejs/10.12.0/firebase-auth.js";
import {
  getDatabase,
  ref,
  onValue,
  remove,
  update,
  get,
} from "https://www.gstatic.com/firebasejs/10.12.0/firebase-database.js";
import {
  getStorage,
  ref as storageRef,
  getDownloadURL,
} from "https://www.gstatic.com/firebasejs/10.12.0/firebase-storage.js";

const auth = getAuth(app);
const db = getDatabase(app);
const storage = getStorage(app);

// ===== STATE =====
let currentUser = null;
let isAdmin = false;
let usersData = {}; // cached users snapshot
let sessionsData = {}; // cached sessions snapshot
let expandedPlayerUid = null; // which player row is expanded
let activeSessionKey = null; // which session tab is selected

// ===== AUTH GUARD =====
onAuthStateChanged(auth, async (user) => {
  if (!user) {
    window.location.href = "index.html";
    return;
  }
  currentUser = user;

  // Check admin status
  try {
    const snap = await get(ref(db, "users/" + user.uid + "/isAdmin"));
    isAdmin = snap.val() === true;
  } catch (e) {
    isAdmin = false;
  }

  // Show dashboard
  document.getElementById("loading-screen").classList.add("hidden");
  document.getElementById("dashboard-content").classList.remove("hidden");

  // Show admin UI elements
  if (isAdmin) {
    document.getElementById("admin-badge").classList.remove("hidden");
    document.getElementById("export-dropdown-wrap").classList.remove("hidden");
    document.getElementById("danger-zone").classList.remove("hidden");
    document
      .querySelectorAll(".admin-col")
      .forEach((el) => (el.style.display = ""));
  }

  // Set username in header
  try {
    const uSnap = await get(ref(db, "users/" + user.uid + "/username"));
    document.getElementById("header-username").textContent =
      uSnap.val() || user.email;
  } catch (e) {
    document.getElementById("header-username").textContent = user.email;
  }

  // Load logo
  loadHeaderLogo();

  // Start real-time listeners
  setupListeners();
});

// ===== LOGO =====
async function loadHeaderLogo() {
  try {
    const logoRef = storageRef(storage, "logo.png");
    const url = await getDownloadURL(logoRef);
    const img = document.getElementById("header-logo-img");
    const ph = document.getElementById("header-logo-placeholder");
    img.src = url;
    img.onload = () => {
      ph.classList.add("hidden");
      img.classList.remove("hidden");
    };
    img.onerror = () => {
      ph.classList.add("hidden");
    };
    // Set favicon
    document.getElementById("favicon").href = url;
  } catch (e) {
    document.getElementById("header-logo-placeholder").classList.add("hidden");
  }
}

// ===== REAL-TIME LISTENERS =====
function setupListeners() {
  // Listen to users node
  onValue(ref(db, "users"), (snapshot) => {
    usersData = snapshot.val() || {};
    rebuildDashboard();
  });
  // Listen to sessions node
  onValue(ref(db, "sessions"), (snapshot) => {
    sessionsData = snapshot.val() || {};
    rebuildDashboard();
  });
}

// ===== REBUILD ENTIRE DASHBOARD =====
function rebuildDashboard() {
  updateStats();
  renderPlayersTable();
  renderLeaderboard();
  // Re-expand detail panel if one was open
  if (expandedPlayerUid && usersData[expandedPlayerUid]) {
    renderSessionPanel(expandedPlayerUid);
  } else {
    expandedPlayerUid = null;
    document.getElementById("session-detail-container").innerHTML = "";
  }
}

// ===== STATS =====
function updateStats() {
  const userKeys = Object.keys(usersData).filter(
    (uid) => !usersData[uid].isAdmin,
  );
  const playerCount = userKeys.length;
  let totalSessions = 0;
  let totalBalance = 0;
  let totalTime = 0;
  let sessionCount = 0;

  for (const uid of Object.keys(sessionsData)) {
    if (usersData[uid]?.isAdmin) continue;
    const playerSessions = sessionsData[uid] || {};
    for (const sKey of Object.keys(playerSessions)) {
      totalSessions++;
      const summary = playerSessions[sKey]?.session_summary;
      if (summary) {
        sessionCount++;
        totalBalance += summary.final_balance || 0;
        totalTime += summary.total_time_seconds || 0;
      }
    }
  }

  document.getElementById("stat-players").textContent = playerCount;
  document.getElementById("stat-sessions").textContent = totalSessions;
  document.getElementById("stat-avg-balance").textContent =
    sessionCount > 0 ? (totalBalance / sessionCount).toFixed(2) : "—";
  document.getElementById("stat-avg-time").textContent =
    sessionCount > 0 ? formatTime(totalTime / sessionCount) : "—";
}

function formatTime(seconds) {
  const m = Math.floor(seconds / 60);
  const s = Math.floor(seconds % 60);
  return m.toString().padStart(2, "0") + ":" + s.toString().padStart(2, "0");
}

// ===== PLAYERS TABLE =====
function renderPlayersTable() {
  const tbody = document.getElementById("players-tbody");
  const emptyEl = document.getElementById("players-empty");
  const userKeys = Object.keys(usersData).filter(
    (uid) => !usersData[uid].isAdmin,
  );

  if (userKeys.length === 0) {
    tbody.innerHTML = "";
    emptyEl.classList.remove("hidden");
    return;
  }
  emptyEl.classList.add("hidden");

  let html = "";
  for (const uid of userKeys) {
    const u = usersData[uid];
    const playerSessions = sessionsData[uid] || {};
    const sessionKeys = Object.keys(playerSessions);
    const sessionCount = sessionKeys.length;

    // Best score
    let bestScore = 0;
    let lastPlayed = null;
    for (const sKey of sessionKeys) {
      const summary = playerSessions[sKey]?.session_summary;
      if (summary) {
        if ((summary.final_balance || 0) > bestScore)
          bestScore = summary.final_balance;
        const d = summary.completed_at ? new Date(summary.completed_at) : null;
        if (d && (!lastPlayed || d > lastPlayed)) lastPlayed = d;
      }
    }

    const isExpanded = expandedPlayerUid === uid;
    const rowBg = isExpanded ? "bg-sand/50" : "";
    html += `<tr class="${rowBg} hover:bg-sand/50 cursor-pointer transition-colors" onclick="togglePlayerDetail('${uid}')">
          <td class="py-4 px-5 text-sm md:text-base font-semibold">${escHtml(u.username || "—")}</td>
          <td class="py-4 px-5 text-sm md:text-base">${escHtml(u.email || "—")}</td>
          <td class="py-4 px-5 text-sm md:text-base">${sessionCount}</td>
          <td class="py-4 px-5 text-sm md:text-base font-semibold text-moss">${sessionCount > 0 ? bestScore.toFixed(2) : "—"}</td>
          <td class="py-4 px-5 text-sm md:text-base">${lastPlayed ? formatDate(lastPlayed) : "—"}</td>
          <td class="py-4 px-5 admin-col" style="${isAdmin ? "" : "display:none;"}">
            <button onclick="event.stopPropagation(); confirmDeletePlayer('${uid}', '${escAttr(u.username)}')" class="text-ember hover:bg-ember/10 rounded-full p-2 transition-all duration-300" title="Delete player">
              <svg class="w-4 h-4" fill="none" stroke="currentColor" stroke-width="2" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16"/></svg>
            </button>
          </td>
        </tr>`;
  }
  tbody.innerHTML = html;
}

function formatDate(d) {
  const day = d.getDate();
  const months = [
    "Jan",
    "Feb",
    "Mar",
    "Apr",
    "May",
    "Jun",
    "Jul",
    "Aug",
    "Sep",
    "Oct",
    "Nov",
    "Dec",
  ];
  const month = months[d.getMonth()];
  const year = d.getFullYear();
  let hours = d.getHours();
  const ampm = hours >= 12 ? "PM" : "AM";
  hours = hours % 12 || 12;
  const mins = d.getMinutes().toString().padStart(2, "0");
  return `${day} ${month} ${year}, ${hours}:${mins} ${ampm}`;
}

// ===== SESSION DETAIL PANEL =====
window.togglePlayerDetail = function (uid) {
  if (expandedPlayerUid === uid) {
    expandedPlayerUid = null;
    activeSessionKey = null;
    document.getElementById("session-detail-container").innerHTML = "";
    renderPlayersTable();
    return;
  }
  expandedPlayerUid = uid;
  activeSessionKey = null;
  renderPlayersTable();
  renderSessionPanel(uid);
};

function renderSessionPanel(uid) {
  const container = document.getElementById("session-detail-container");
  const u = usersData[uid];
  const playerSessions = sessionsData[uid] || {};
  const sessionKeys = Object.keys(playerSessions).sort().reverse();

  if (sessionKeys.length === 0) {
    container.innerHTML = `<div class="bg-cream border border-timber rounded-[1.5rem] mt-2 p-6 text-center">
          <p class="text-base text-grass">No sessions recorded yet for this player.</p>
        </div>`;
    return;
  }

  // Default to first session if none selected
  if (!activeSessionKey || !playerSessions[activeSessionKey]) {
    activeSessionKey = sessionKeys[0];
  }

  const session = playerSessions[activeSessionKey];
  const summary = session?.session_summary || {};
  const inventory = session?.inventory_logs || {};
  const transactions = session?.transaction_history || {};

  // Build session tabs
  let tabsHtml = "";
  for (const sKey of sessionKeys) {
    const sData = playerSessions[sKey]?.session_summary;
    const dateLabel = sData?.completed_at
      ? formatDate(new Date(sData.completed_at))
      : sKey;
    const isActive = sKey === activeSessionKey;
    tabsHtml += `<button onclick="event.stopPropagation(); selectSession('${uid}', '${sKey}')"
          class="flex-shrink-0 flex items-center gap-1.5 text-sm font-semibold px-4 py-2 rounded-full transition-all duration-300 ${isActive ? "bg-moss text-white" : "bg-stone text-loam hover:bg-sand"}">
          ${escHtml(dateLabel)}
          ${
            isAdmin && !isActive
              ? `<span onclick="event.stopPropagation(); confirmDeleteSession('${uid}', '${sKey}', '${escAttr(dateLabel)}')" class="ml-1 text-ember hover:text-white" title="Delete session">
            <svg class="w-4 h-4" fill="none" stroke="currentColor" stroke-width="2" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16"/></svg>
          </span>`
              : ""
          }
        </button>`;
  }

  // Inventory chips
  const inventoryItems = [
    { key: "apple_count", label: "Apple" },
    { key: "banana_count", label: "Banana" },
    { key: "orange_count", label: "Orange" },
    { key: "strawberry_count", label: "Strawberry" },
    { key: "green_tea_count", label: "Green Tea" },
    { key: "blueberry_drink_count", label: "Blueberry Drink" },
    { key: "bento_set_1_count", label: "Bento Set 1" },
    { key: "bento_set_2_count", label: "Bento Set 2" },
  ];
  let invHtml = "";
  for (const item of inventoryItems) {
    const count = inventory[item.key] ?? 0;
    if (count > 0) {
      invHtml += `<span class="rounded-full px-4 py-2 text-sm font-semibold bg-sand text-loam">${item.label} &times; ${count}</span>`;
    } else {
      invHtml += `<span class="rounded-full px-4 py-2 text-sm font-semibold bg-stone/50 text-grass line-through opacity-60">${item.label} &times; 0</span>`;
    }
  }

  // Transaction table
  const orderKeys = Object.keys(transactions).sort();
  let ordersHtml = "";
  let correctItems = 0,
    correctChange = 0,
    totalOrders = orderKeys.length;
  for (let i = 0; i < orderKeys.length; i++) {
    const o = transactions[orderKeys[i]];
    const itemOk = o.is_correct_item === true;
    const changeOk = o.is_change_correct === true;
    if (itemOk) correctItems++;
    if (changeOk) correctChange++;
    const rowBg =
      !itemOk || !changeOk ? "bg-ember/10" : i % 2 === 1 ? "bg-stone/30" : "";
    ordersHtml += `<tr class="${rowBg}">
          <td class="py-3 px-4 text-sm">${i + 1}</td>
          <td class="py-3 px-4 text-sm">${escHtml(o.requested_food || "—")} &times; ${o.requested_food_qty || 0}</td>
          <td class="py-3 px-4 text-sm">${escHtml(o.requested_drink || "—")} &times; ${o.requested_drink_qty || 0}</td>
          <td class="py-3 px-4 text-sm">${(o.order_cost || 0).toFixed(2)}</td>
          <td class="py-3 px-4 text-sm">${(o.amount_paid || 0).toFixed(2)}</td>
          <td class="py-3 px-4 text-sm">${(o.change_given || 0).toFixed(2)}</td>
          <td class="py-3 px-4 text-sm text-center">${itemOk ? checkSvg : crossSvg}</td>
          <td class="py-3 px-4 text-sm text-center">${changeOk ? checkSvg : crossSvg}</td>
        </tr>`;
  }

  // Accuracy — use pre-computed counts from session_summary if available
  correctItems = summary.food_correct_count ?? correctItems;
  correctChange = summary.change_correct_count ?? correctChange;
  const foodWrong = summary.food_wrong_count ?? totalOrders - correctItems;
  const changeWrong = summary.change_wrong_count ?? totalOrders - correctChange;
  const itemAcc =
    totalOrders > 0 ? Math.round((correctItems / totalOrders) * 100) : 0;
  const changeAcc =
    totalOrders > 0 ? Math.round((correctChange / totalOrders) * 100) : 0;
  function accColor(pct) {
    return pct >= 80 ? "text-moss" : pct >= 50 ? "text-clay" : "text-ember";
  }

  // Username edit
  const editBtnHtml = isAdmin
    ? `<button onclick="event.stopPropagation(); startEditUsername('${uid}')" class="text-grass hover:text-moss p-1 transition-colors" title="Edit username">
        <svg class="w-4 h-4" fill="none" stroke="currentColor" stroke-width="2" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" d="M16.862 4.487l1.687-1.688a1.875 1.875 0 112.652 2.652L10.582 16.07a4.5 4.5 0 01-1.897 1.13L6 18l.8-2.685a4.5 4.5 0 011.13-1.897l8.932-8.931z"/></svg>
      </button>`
    : "";

  container.innerHTML = `<div class="bg-cream border border-timber rounded-[1.5rem] mt-2 p-6 md:p-8">
        <!-- Header -->
        <div class="flex items-center justify-between mb-4">
          <div class="flex items-center gap-2">
            <span id="detail-username-display" class="font-heading text-lg font-bold text-loam">${escHtml(u?.username || "—")}</span>
            ${editBtnHtml}
            <div id="detail-username-edit" class="hidden flex items-center gap-2">
              <input id="detail-username-input" type="text" value="${escAttr(u?.username || "")}" class="rounded-full h-9 px-4 text-sm border-2 border-timber focus:border-moss focus:ring-2 focus:ring-moss/20 focus:outline-none" />
              <button onclick="saveUsername('${uid}')" class="bg-moss text-white rounded-full px-4 h-9 text-sm font-bold hover:scale-105 active:scale-95 transition-all">Save</button>
              <button onclick="cancelEditUsername()" class="border border-timber text-loam rounded-full px-4 h-9 text-sm font-semibold hover:bg-stone transition-all">Cancel</button>
            </div>
          </div>
          <button onclick="togglePlayerDetail('${uid}')" class="text-grass hover:text-loam p-1 rounded-full hover:bg-stone transition-all" title="Close">
            <svg class="w-5 h-5" fill="none" stroke="currentColor" stroke-width="2" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" d="M6 18L18 6M6 6l12 12"/></svg>
          </button>
        </div>

        <!-- Session tabs -->
        ${sessionKeys.length > 1 ? `<div class="session-tabs flex gap-2 overflow-x-auto pb-2 mb-4">${tabsHtml}</div>` : ""}

        <!-- Session summary mini-cards -->
        <div class="grid grid-cols-2 md:grid-cols-3 gap-3 md:gap-4 mt-4">
          <div class="bg-rice rounded-xl p-4 border border-timber">
            <p class="text-sm text-grass">Score</p>
            <p class="font-heading text-xl md:text-2xl font-bold text-moss">${summary.final_score ?? "—"}</p>
          </div>
          <div class="bg-rice rounded-xl p-4 border border-timber">
            <p class="text-sm text-grass">Grade</p>
            <span class="inline-block text-lg font-heading font-bold px-3 py-1 rounded-full mt-1 ${summary.grade === "A" || summary.grade === "B" ? "bg-moss/15 text-moss" : summary.grade === "C" ? "bg-clay/15 text-clay" : "bg-ember/15 text-ember"}">${summary.grade ?? "—"}</span>
          </div>
          <div class="bg-rice rounded-xl p-4 border border-timber">
            <p class="text-sm text-grass">Final Balance</p>
            <p class="font-heading text-xl md:text-2xl font-bold text-moss">${(summary.final_balance ?? 0).toFixed(2)}</p>
          </div>
          <div class="bg-rice rounded-xl p-4 border border-timber">
            <p class="text-sm text-grass">Total Time</p>
            <p class="font-heading text-xl md:text-2xl font-bold text-loam">${formatTime(summary.total_time_seconds || 0)}</p>
          </div>
          <div class="bg-rice rounded-xl p-4 border border-timber">
            <p class="text-sm text-grass">Trash Disposed</p>
            <p class="font-heading text-xl md:text-2xl font-bold text-loam">${summary.trash_disposed ?? 0}</p>
          </div>
          <div class="bg-rice rounded-xl p-4 border border-timber">
            <p class="text-sm text-grass">Bento Unlocked</p>
            ${summary.is_bento_unlocked ? '<span class="inline-block bg-moss/15 text-moss text-sm font-semibold px-3 py-1 rounded-full mt-1">Yes</span>' : '<span class="inline-block bg-stone text-grass text-sm font-semibold px-3 py-1 rounded-full mt-1">No</span>'}
          </div>
        </div>

        <!-- Inventory -->
        <div class="mt-5">
          <h3 class="font-heading text-base md:text-lg font-bold text-loam mb-3">Inventory</h3>
          <div class="flex flex-wrap gap-2">${invHtml}</div>
        </div>

        <!-- Transaction history -->
        ${
          totalOrders > 0
            ? `<div class="mt-5">
          <h3 class="font-heading text-base md:text-lg font-bold text-loam mb-3">Orders</h3>
          <div class="overflow-x-auto rounded-xl border border-timber">
            <table class="w-full text-left">
              <thead>
                <tr class="bg-stone text-sm font-semibold">
                  <th class="py-2 px-4">#</th>
                  <th class="py-2 px-4">Food</th>
                  <th class="py-2 px-4">Drink</th>
                  <th class="py-2 px-4">Cost</th>
                  <th class="py-2 px-4">Paid</th>
                  <th class="py-2 px-4">Change</th>
                  <th class="py-2 px-4 text-center">Item</th>
                  <th class="py-2 px-4 text-center">Change</th>
                </tr>
              </thead>
              <tbody>${ordersHtml}</tbody>
            </table>
          </div>
          <!-- Accuracy summary -->
          <div class="bg-rice rounded-xl p-4 border border-timber inline-flex flex-wrap gap-6 items-center mt-3">
            <span class="font-heading text-base font-bold ${accColor(itemAcc)}">Food: ${correctItems} correct, ${foodWrong} wrong</span>
            <span class="font-heading text-base font-bold ${accColor(changeAcc)}">Change: ${correctChange} correct, ${changeWrong} wrong</span>
          </div>
        </div>`
            : '<p class="mt-5 text-sm text-grass">No orders in this session.</p>'
        }
      </div>`;
}

const checkSvg =
  '<svg class="w-5 h-5 text-moss inline" fill="none" stroke="currentColor" stroke-width="2.5" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" d="M5 13l4 4L19 7"/></svg>';
const crossSvg =
  '<svg class="w-5 h-5 text-ember inline" fill="none" stroke="currentColor" stroke-width="2.5" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" d="M6 18L18 6M6 6l12 12"/></svg>';

window.selectSession = function (uid, sKey) {
  activeSessionKey = sKey;
  renderSessionPanel(uid);
};

// ===== LEADERBOARD =====
function renderLeaderboard() {
  const list = document.getElementById("leaderboard-list");
  const emptyEl = document.getElementById("leaderboard-empty");

  // Build scores: best final_balance per user
  const scores = [];
  for (const uid of Object.keys(usersData)) {
    if (usersData[uid].isAdmin) continue;
    const u = usersData[uid];
    const playerSessions = sessionsData[uid] || {};
    let bestScore = -1;
    let bestDate = null;
    for (const sKey of Object.keys(playerSessions)) {
      const summary = playerSessions[sKey]?.session_summary;
      if (summary && (summary.final_balance || 0) > bestScore) {
        bestScore = summary.final_balance;
        bestDate = summary.completed_at ? new Date(summary.completed_at) : null;
      }
    }
    if (bestScore >= 0) {
      scores.push({
        uid,
        username: u.username || u.email,
        bestScore,
        bestDate,
      });
    }
  }

  scores.sort((a, b) => b.bestScore - a.bestScore);

  if (scores.length === 0) {
    list.innerHTML = "";
    emptyEl.classList.remove("hidden");
    return;
  }
  emptyEl.classList.add("hidden");

  let html = "";
  for (let i = 0; i < scores.length; i++) {
    const s = scores[i];
    const topClass = i < 3 ? "border-l-4 border-moss pl-3" : "pl-4";
    html += `<div class="py-3 border-b border-timber/50 last:border-b-0 ${topClass} flex items-center gap-3">
          <span class="font-heading text-lg font-bold text-loam w-8">#${i + 1}</span>
          <div class="flex-1 min-w-0">
            <p class="text-sm font-semibold text-loam truncate">${escHtml(s.username)}</p>
            <p class="text-sm text-grass">${s.bestDate ? formatDate(s.bestDate) : "—"}</p>
          </div>
          <span class="font-heading text-base font-bold text-moss">${s.bestScore.toFixed(2)}</span>
        </div>`;
  }
  if (scores.length === 1) {
    html +=
      '<p class="text-sm text-grass text-center mt-4">Play more to climb the ranks!</p>';
  }
  list.innerHTML = html;
}

// ===== ADMIN: EDIT USERNAME =====
window.startEditUsername = function (uid) {
  document.getElementById("detail-username-display").classList.add("hidden");
  document.getElementById("detail-username-edit").classList.remove("hidden");
  document.getElementById("detail-username-input").focus();
};
window.cancelEditUsername = function () {
  document.getElementById("detail-username-display").classList.remove("hidden");
  document.getElementById("detail-username-edit").classList.add("hidden");
};
window.saveUsername = async function (uid) {
  const newName = document.getElementById("detail-username-input").value.trim();
  if (!newName) {
    showToast("Username cannot be empty.", "error");
    return;
  }
  try {
    await update(ref(db, "users/" + uid), { username: newName });
    showToast("Username updated!", "success");
    cancelEditUsername();
    // Real-time listener will auto-update the UI
  } catch (e) {
    showToast("Failed to update username. " + e.message, "error");
  }
};

// ===== ADMIN: DELETE PLAYER =====
window.confirmDeletePlayer = function (uid, username) {
  showModal(`
        <h3 class="font-heading text-lg font-bold text-loam">Delete Player</h3>
        <p class="text-sm text-grass mt-2">Are you sure you want to delete <strong class="text-loam">${escHtml(username)}</strong>? This will remove their account data and all sessions. This cannot be undone.</p>
        <div class="flex gap-3 mt-6 justify-end">
          <button onclick="closeModal()" class="border-2 border-timber text-loam hover:bg-stone rounded-full px-6 h-10 text-sm font-semibold transition-all">Cancel</button>
          <button onclick="deletePlayer('${uid}')" class="bg-ember text-white rounded-full px-6 h-10 text-sm font-bold hover:bg-ember/90 active:scale-95 transition-all">Delete</button>
        </div>
      `);
};
window.deletePlayer = async function (uid) {
  try {
    await remove(ref(db, "users/" + uid));
    await remove(ref(db, "sessions/" + uid));
    if (expandedPlayerUid === uid) {
      expandedPlayerUid = null;
      document.getElementById("session-detail-container").innerHTML = "";
    }
    closeModal();
    showToast("Player deleted.", "success");
  } catch (e) {
    showToast("Failed to delete player. " + e.message, "error");
  }
};

// ===== ADMIN: DELETE SESSION =====
window.confirmDeleteSession = function (uid, sKey, dateLabel) {
  showModal(`
        <h3 class="font-heading text-lg font-bold text-loam">Delete Session</h3>
        <p class="text-sm text-grass mt-2">Delete session from <strong class="text-loam">${escHtml(dateLabel)}</strong>?</p>
        <div class="flex gap-3 mt-6 justify-end">
          <button onclick="closeModal()" class="border-2 border-timber text-loam hover:bg-stone rounded-full px-6 h-10 text-sm font-semibold transition-all">Cancel</button>
          <button onclick="deleteSession('${uid}', '${sKey}')" class="bg-ember text-white rounded-full px-6 h-10 text-sm font-bold hover:bg-ember/90 active:scale-95 transition-all">Delete</button>
        </div>
      `);
};
window.deleteSession = async function (uid, sKey) {
  try {
    await remove(ref(db, "sessions/" + uid + "/" + sKey));
    if (activeSessionKey === sKey) activeSessionKey = null;
    closeModal();
    showToast("Session deleted.", "success");
  } catch (e) {
    showToast("Failed to delete session. " + e.message, "error");
  }
};

// ===== ADMIN: RESET ALL DATA =====
window.showResetModal = function () {
  showModal(`
        <h3 class="font-heading text-lg font-bold text-loam">Reset All Data</h3>
        <p class="text-sm text-grass mt-2">Are you sure? This will permanently delete <strong class="text-loam">ALL</strong> player data and sessions.</p>
        <div class="flex gap-3 mt-6 justify-end">
          <button onclick="closeModal()" class="border-2 border-timber text-loam hover:bg-stone rounded-full px-6 h-10 text-sm font-semibold transition-all">Cancel</button>
          <button onclick="showResetStep2()" class="bg-ember text-white rounded-full px-6 h-10 text-sm font-bold hover:bg-ember/90 active:scale-95 transition-all">Continue</button>
        </div>
      `);
};
window.showResetStep2 = function () {
  document.getElementById("modal-body").innerHTML = `
        <h3 class="font-heading text-lg font-bold text-loam">Confirm Reset</h3>
        <p class="text-sm text-grass mt-2">Type <strong class="text-loam">RESET</strong> to confirm.</p>
        <input id="reset-confirm-input" type="text" placeholder="Type RESET to confirm" class="mt-3 rounded-full h-11 px-6 text-base w-full border-2 border-timber bg-white/60 focus:border-ember focus:ring-2 focus:ring-ember/20 focus:outline-none" oninput="checkResetInput()" />
        <div class="flex gap-3 mt-6 justify-end">
          <button onclick="closeModal()" class="border-2 border-timber text-loam hover:bg-stone rounded-full px-6 h-10 text-sm font-semibold transition-all">Cancel</button>
          <button id="reset-confirm-btn" disabled class="bg-ember text-white rounded-full px-6 h-10 text-sm font-bold opacity-50 cursor-not-allowed transition-all" onclick="executeResetAll()">Reset All Data</button>
        </div>
      `;
  document.getElementById("reset-confirm-input").focus();
};
window.checkResetInput = function () {
  const val = document.getElementById("reset-confirm-input").value;
  const btn = document.getElementById("reset-confirm-btn");
  if (val === "RESET") {
    btn.disabled = false;
    btn.classList.remove("opacity-50", "cursor-not-allowed");
    btn.classList.add("hover:bg-ember/90", "active:scale-95");
  } else {
    btn.disabled = true;
    btn.classList.add("opacity-50", "cursor-not-allowed");
    btn.classList.remove("hover:bg-ember/90", "active:scale-95");
  }
};
window.executeResetAll = async function () {
  try {
    // Delete all sessions
    await remove(ref(db, "sessions"));
    // Delete all users except current admin
    for (const uid of Object.keys(usersData)) {
      if (uid !== currentUser.uid) {
        await remove(ref(db, "users/" + uid));
      }
    }
    expandedPlayerUid = null;
    activeSessionKey = null;
    document.getElementById("session-detail-container").innerHTML = "";
    closeModal();
    showToast("All data has been reset.", "success");
  } catch (e) {
    showToast("Failed to reset data. " + e.message, "error");
  }
};

// ===== EXPORT =====
window.toggleExportDropdown = function () {
  document.getElementById("export-dropdown").classList.toggle("hidden");
};
// Close dropdown on outside click
document.addEventListener("click", (e) => {
  const wrap = document.getElementById("export-dropdown-wrap");
  if (wrap && !wrap.contains(e.target)) {
    document.getElementById("export-dropdown").classList.add("hidden");
  }
});

window.exportJSON = function () {
  const data = { users: usersData, sessions: sessionsData };
  const content = JSON.stringify(data, null, 2);
  downloadFile(
    content,
    `bentoboss_data_${todayStr()}.json`,
    "application/json",
  );
  document.getElementById("export-dropdown").classList.add("hidden");
  showToast("JSON exported!", "success");
};

window.exportCSV = function () {
  const rows = [
    [
      "Username",
      "Email",
      "Session Date",
      "Score",
      "Grade",
      "Final Balance",
      "Total Time (s)",
      "Total Orders",
      "Food Correct",
      "Food Wrong",
      "Change Correct",
      "Change Wrong",
      "Bento Unlocked",
    ],
  ];
  for (const uid of Object.keys(usersData)) {
    if (usersData[uid].isAdmin) continue;
    const u = usersData[uid];
    const playerSessions = sessionsData[uid] || {};
    for (const sKey of Object.keys(playerSessions)) {
      const s = playerSessions[sKey];
      const summary = s?.session_summary || {};
      const txn = s?.transaction_history || {};
      const totalOrders = Object.keys(txn).length;
      rows.push([
        u.username || "",
        u.email || "",
        summary.completed_at || sKey,
        summary.final_score ?? "",
        summary.grade ?? "",
        (summary.final_balance ?? 0).toFixed(2),
        (summary.total_time_seconds ?? 0).toFixed(1),
        totalOrders,
        summary.food_correct_count ?? "",
        summary.food_wrong_count ?? "",
        summary.change_correct_count ?? "",
        summary.change_wrong_count ?? "",
        summary.is_bento_unlocked ? "Yes" : "No",
      ]);
    }
  }
  const csv = rows
    .map((r) => r.map((c) => `"${String(c).replace(/"/g, '""')}"`).join(","))
    .join("\n");
  downloadFile(csv, `bentoboss_data_${todayStr()}.csv`, "text/csv");
  document.getElementById("export-dropdown").classList.add("hidden");
  showToast("CSV exported!", "success");
};

function downloadFile(content, filename, mime) {
  const blob = new Blob([content], { type: mime });
  const url = URL.createObjectURL(blob);
  const a = document.createElement("a");
  a.href = url;
  a.download = filename;
  a.click();
  URL.revokeObjectURL(url);
}

function todayStr() {
  const d = new Date();
  return (
    d.getFullYear() +
    "-" +
    String(d.getMonth() + 1).padStart(2, "0") +
    "-" +
    String(d.getDate()).padStart(2, "0")
  );
}

// ===== LOGOUT =====
window.handleLogout = async function () {
  try {
    await signOut(auth);
    // onAuthStateChanged will redirect
  } catch (e) {
    showToast("Failed to log out.", "error");
  }
};

// ===== MODAL SYSTEM =====
function showModal(bodyHtml) {
  const backdrop = document.getElementById("modal-backdrop");
  const card = document.getElementById("modal-card");
  document.getElementById("modal-body").innerHTML = bodyHtml;
  backdrop.classList.remove("hidden");
  requestAnimationFrame(() => {
    card.classList.remove("scale-95", "opacity-0");
    card.classList.add("scale-100", "opacity-100");
  });
}
window.closeModal = function () {
  const backdrop = document.getElementById("modal-backdrop");
  const card = document.getElementById("modal-card");
  card.classList.add("scale-95", "opacity-0");
  card.classList.remove("scale-100", "opacity-100");
  setTimeout(() => backdrop.classList.add("hidden"), 200);
};
window.closeModalOnBackdrop = function (e) {
  if (e.target === document.getElementById("modal-backdrop")) closeModal();
};
// Close modal on Escape
document.addEventListener("keydown", (e) => {
  if (e.key === "Escape") closeModal();
});

// ===== TOAST SYSTEM =====
function showToast(msg, type) {
  const container = document.getElementById("toast-container");
  const toast = document.createElement("div");
  toast.className = `${type === "success" ? "bg-moss" : "bg-ember"} text-white rounded-xl shadow-lift px-5 py-3 text-sm font-semibold transition-all duration-300 opacity-0 translate-y-2`;
  toast.textContent = msg;
  container.appendChild(toast);
  requestAnimationFrame(() => {
    toast.classList.remove("opacity-0", "translate-y-2");
    toast.classList.add("opacity-100", "translate-y-0");
  });
  setTimeout(() => {
    toast.classList.add("opacity-0", "translate-y-2");
    toast.classList.remove("opacity-100", "translate-y-0");
    setTimeout(() => toast.remove(), 300);
  }, 3000);
}

// ===== UTILITIES =====
function escHtml(str) {
  const d = document.createElement("div");
  d.textContent = str;
  return d.innerHTML;
}
function escAttr(str) {
  return String(str || "")
    .replace(/'/g, "\\'")
    .replace(/"/g, "&quot;");
}
