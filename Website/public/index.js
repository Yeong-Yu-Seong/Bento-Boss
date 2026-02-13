import { app } from "./firebase-config.js";
import {
  getAuth,
  signInWithEmailAndPassword,
  createUserWithEmailAndPassword,
  onAuthStateChanged,
} from "https://www.gstatic.com/firebasejs/10.12.0/firebase-auth.js";
import {
  getDatabase,
  ref,
  set,
} from "https://www.gstatic.com/firebasejs/10.12.0/firebase-database.js";
import {
  getStorage,
  ref as storageRef,
  getDownloadURL,
} from "https://www.gstatic.com/firebasejs/10.12.0/firebase-storage.js";

const auth = getAuth(app);
const db = getDatabase(app);
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

function mapAuthError(code) {
  const map = {
    "auth/email-already-in-use": "This email is already registered.",
    "auth/wrong-password": "Incorrect password.",
    "auth/user-not-found": "No account found with this email.",
    "auth/weak-password": "Password must be at least 6 characters.",
    "auth/invalid-email": "Please enter a valid email.",
    "auth/too-many-requests": "Too many attempts. Please try again later.",
    "auth/invalid-credential": "Incorrect email or password.",
  };
  return map[code] || "Something went wrong. Please try again.";
}

function showError(msg) {
  const banner = document.getElementById("error-banner");
  banner.textContent = msg;
  banner.classList.remove("hidden");
}
function hideError() {
  document.getElementById("error-banner").classList.add("hidden");
}
function clearFieldErrors() {
  document.querySelectorAll('[id$="-error"]').forEach((el) => {
    if (el.id !== "error-banner") {
      el.classList.add("hidden");
      el.textContent = "";
    }
  });
}
function showFieldError(id, msg) {
  const el = document.getElementById(id);
  el.textContent = msg;
  el.classList.remove("hidden");
}

const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

window.handleLogin = async function (e) {
  e.preventDefault();
  hideError();
  clearFieldErrors();

  const email = document.getElementById("login-email").value.trim();
  const password = document.getElementById("login-password").value;
  let valid = true;

  if (!email) {
    showFieldError("login-email-error", "Email is required.");
    valid = false;
  } else if (!emailRegex.test(email)) {
    showFieldError("login-email-error", "Please enter a valid email.");
    valid = false;
  }
  if (!password) {
    showFieldError("login-password-error", "Password is required.");
    valid = false;
  }

  if (!valid) return;

  const btn = document.getElementById("login-btn");
  btn.disabled = true;
  btn.textContent = "Signing in...";
  btn.classList.add("opacity-70");

  try {
    await signInWithEmailAndPassword(auth, email, password);
  } catch (err) {
    showError(mapAuthError(err.code));
    btn.disabled = false;
    btn.textContent = "Sign In";
    btn.classList.remove("opacity-70");
  }
};

window.handleRegister = async function (e) {
  e.preventDefault();
  hideError();
  clearFieldErrors();

  const username = document.getElementById("reg-username").value.trim();
  const email = document.getElementById("reg-email").value.trim();
  const password = document.getElementById("reg-password").value;
  const confirm = document.getElementById("reg-confirm").value;
  let valid = true;

  if (!username) {
    showFieldError("reg-username-error", "Username is required.");
    valid = false;
  }
  if (!email) {
    showFieldError("reg-email-error", "Email is required.");
    valid = false;
  } else if (!emailRegex.test(email)) {
    showFieldError("reg-email-error", "Please enter a valid email.");
    valid = false;
  }
  if (!password) {
    showFieldError("reg-password-error", "Password is required.");
    valid = false;
  } else if (password.length < 6) {
    showFieldError(
      "reg-password-error",
      "Password must be at least 6 characters.",
    );
    valid = false;
  }
  if (!confirm) {
    showFieldError("reg-confirm-error", "Please confirm your password.");
    valid = false;
  } else if (password !== confirm) {
    showFieldError("reg-confirm-error", "Passwords do not match.");
    valid = false;
  }

  if (!valid) return;

  const btn = document.getElementById("register-btn");
  btn.disabled = true;
  btn.textContent = "Creating account...";
  btn.classList.add("opacity-70");

  try {
    const cred = await createUserWithEmailAndPassword(auth, email, password);
    await set(ref(db, "users/" + cred.user.uid), {
      email: email,
      username: username,
      isAdmin: false,
    });
  } catch (err) {
    showError(mapAuthError(err.code));
    btn.disabled = false;
    btn.textContent = "Create Account";
    btn.classList.remove("opacity-70");
  }
};

window.switchTab = function (tab) {
  hideError();
  clearFieldErrors();
  const loginForm = document.getElementById("login-form");
  const registerForm = document.getElementById("register-form");
  const tabLogin = document.getElementById("tab-login");
  const tabRegister = document.getElementById("tab-register");

  if (tab === "login") {
    loginForm.classList.remove("hidden");
    registerForm.classList.add("hidden");
    tabLogin.classList.add("bg-moss", "text-white");
    tabLogin.classList.remove("text-loam", "hover:bg-sand/50");
    tabRegister.classList.remove("bg-moss", "text-white");
    tabRegister.classList.add("text-loam", "hover:bg-sand/50");
  } else {
    loginForm.classList.add("hidden");
    registerForm.classList.remove("hidden");
    tabRegister.classList.add("bg-moss", "text-white");
    tabRegister.classList.remove("text-loam", "hover:bg-sand/50");
    tabLogin.classList.remove("bg-moss", "text-white");
    tabLogin.classList.add("text-loam", "hover:bg-sand/50");
  }
};
