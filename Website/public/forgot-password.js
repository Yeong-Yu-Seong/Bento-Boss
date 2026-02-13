import { app } from "./firebase-config.js";
import {
  getAuth,
  sendPasswordResetEmail,
  onAuthStateChanged,
} from "https://www.gstatic.com/firebasejs/10.12.0/firebase-auth.js";
import {
  getStorage,
  ref as storageRef,
  getDownloadURL,
} from "https://www.gstatic.com/firebasejs/10.12.0/firebase-storage.js";

const auth = getAuth(app);
const storage = getStorage(app);

onAuthStateChanged(auth, (user) => {
  if (user) {
    window.location.href = "dashboard.html";
  }
});

async function loadLogo() {
  try {
    const logoRef = storageRef(storage, "logo.png");
    const logoURL = await getDownloadURL(logoRef);
    const img = document.getElementById("logo-img");
    const placeholder = document.getElementById("logo-placeholder");
    img.src = logoURL;
    img.onload = () => {
      placeholder.classList.add("hidden");
      img.classList.remove("hidden");
    };
    img.onerror = () => {
      placeholder.classList.add("hidden");
    };
    document.getElementById("favicon").href = logoURL;
  } catch (e) {
    document.getElementById("logo-placeholder").classList.add("hidden");
  }
}
loadLogo();

const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

function showError(msg) {
  const banner = document.getElementById("error-banner");
  banner.textContent = msg;
  banner.classList.remove("hidden");
}
function hideError() {
  document.getElementById("error-banner").classList.add("hidden");
}
function showFieldError(id, msg) {
  const el = document.getElementById(id);
  el.textContent = msg;
  el.classList.remove("hidden");
}

window.handleReset = async function (e) {
  e.preventDefault();
  hideError();
  document.getElementById("reset-email-error").classList.add("hidden");

  const email = document.getElementById("reset-email").value.trim();

  if (!email) {
    showFieldError("reset-email-error", "Email is required.");
    return;
  }
  if (!emailRegex.test(email)) {
    showFieldError("reset-email-error", "Please enter a valid email.");
    return;
  }

  const btn = document.getElementById("reset-btn");
  btn.disabled = true;
  btn.textContent = "Sending...";
  btn.classList.add("opacity-70");

  try {
    await sendPasswordResetEmail(auth, email);
    document.getElementById("reset-form").classList.add("hidden");
    document.getElementById("error-banner").classList.add("hidden");
    document.getElementById("success-card").classList.remove("hidden");
  } catch (err) {
    const map = {
      "auth/user-not-found": "No account found with this email.",
      "auth/invalid-email": "Please enter a valid email.",
      "auth/too-many-requests": "Too many attempts. Please try again later.",
    };
    showError(map[err.code] || "Something went wrong. Please try again.");
    btn.disabled = false;
    btn.textContent = "Send Reset Link";
    btn.classList.remove("opacity-70");
  }
};
