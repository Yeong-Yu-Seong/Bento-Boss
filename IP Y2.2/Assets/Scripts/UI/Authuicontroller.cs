using UnityEngine;
using UnityEngine.UI;
using TMPro;
using BentoBoss.FirebaseManagers;
using UnityEngine.SceneManagement;

public class AuthUIController : MonoBehaviour
{
  [Header("Panels")]
  [SerializeField] private GameObject loginPanel;
  [SerializeField] private GameObject signUpPanel;
  [SerializeField] private GameObject forgotPasswordPanel;

  [Header("Login Panel")]
  [SerializeField] private TMP_InputField loginEmailInput;
  [SerializeField] private TMP_InputField loginPasswordInput;
  [SerializeField] private Button loginButton;
  [SerializeField] private TextMeshProUGUI loginErrorText;
  [SerializeField] private TextMeshProUGUI goToSignUpText;
  [SerializeField] private TextMeshProUGUI forgotPasswordText;

  [Header("Sign Up Panel")]
  [SerializeField] private TMP_InputField signUpUsernameInput;
  [SerializeField] private TMP_InputField signUpEmailInput;
  [SerializeField] private TMP_InputField signUpPasswordInput;
  [SerializeField] private Button signUpButton;
  [SerializeField] private TextMeshProUGUI signUpErrorText;
  [SerializeField] private TextMeshProUGUI goToLoginText;

  [Header("Forgot Password Panel")]
  [SerializeField] private TMP_InputField forgotPasswordEmailInput;
  [SerializeField] private Button sendResetButton;
  [SerializeField] private TextMeshProUGUI forgotPasswordErrorText;
  [SerializeField] private Button backToLoginButton;

  [Header("Menu Panel")]
  [SerializeField] private GameObject menuPanel;
  [SerializeField] private Button startGameButton;
  [SerializeField] private Button creditsButton;
  [SerializeField] private Button handbookButton;
  [SerializeField] private Button settingsButton;
  [SerializeField] private Button signOutButton;
  [SerializeField] private TextMeshProUGUI welcomeText;

  [Header("Guide Panel")]
  [SerializeField] private GameObject guidePanel;
  [SerializeField] private Button guideBackButton;

  [Header("Handbook Panel")]
  [SerializeField] private GameObject handbookPanel;
  [SerializeField] private Button handbookBackButton;

  [Header("Credits Panel")]
  [SerializeField] private GameObject creditPanel;
  [SerializeField] private Button creditBackButton;

  [Header("Settings Panel")]
  [SerializeField] private GameObject settingsPanel;
  [SerializeField] private Button settingsBackButton;

  [Header("Settings")]
  [SerializeField] private string gameSceneName = "GameScene";

  void Awake()
  {
    // Check critical assignments
    if (loginPanel == null) Debug.LogError("[AuthUI] LoginPanel not assigned!");
    if (signUpPanel == null) Debug.LogError("[AuthUI] SignUpPanel not assigned!");
    if (forgotPasswordPanel == null) Debug.LogWarning("[AuthUI] ForgotPasswordPanel not assigned!");
    if (menuPanel == null) Debug.LogWarning("[AuthUI] MenuPanel not assigned!");
    if (guidePanel == null) Debug.LogWarning("[AuthUI] GuidePanel not assigned!");
    if (handbookPanel == null) Debug.LogWarning("[AuthUI] HandbookPanel not assigned!");
    if (settingsPanel == null) Debug.LogWarning("[AuthUI] SettingsPanel not assigned!");
    if (creditPanel == null) Debug.LogWarning("[AuthUI] CreditPanel not assigned!");
  }

  void Start()
  {
    // Setup main buttons
    loginButton.onClick.AddListener(OnLoginClicked);
    signUpButton.onClick.AddListener(OnSignUpClicked);
    if (sendResetButton != null) sendResetButton.onClick.AddListener(OnResetClicked);

    // Menu panel buttons
    if (startGameButton != null) startGameButton.onClick.AddListener(OnStartGameClicked);
    if (creditsButton != null) creditsButton.onClick.AddListener(ShowCreditPanel);
    if (handbookButton != null) handbookButton.onClick.AddListener(ShowHandbookPanel);
    if (settingsButton != null) settingsButton.onClick.AddListener(ShowSettingsPanel);
    if (signOutButton != null) signOutButton.onClick.AddListener(OnSignOutClicked);

    // Back buttons
    if (guideBackButton != null) guideBackButton.onClick.AddListener(ShowMenuPanel);
    if (handbookBackButton != null) handbookBackButton.onClick.AddListener(ShowMenuPanel);
    if (creditBackButton != null) creditBackButton.onClick.AddListener(ShowMenuPanel);
    if (settingsBackButton != null) settingsBackButton.onClick.AddListener(ShowMenuPanel);

    // Make text clickable by adding Button component automatically
    MakeTextClickableWithButton(goToSignUpText, ShowSignUpPanel);
    MakeTextClickableWithButton(goToLoginText, ShowLoginPanel);
    MakeTextClickableWithButton(forgotPasswordText, ShowForgotPasswordPanel);
    if (backToLoginButton != null) backToLoginButton.onClick.AddListener(ShowLoginPanel);

    // Force show login panel
    ShowLoginPanel();
    Debug.Log("[AuthUI] Started - Login panel active");
  }

  /// <summary>
  /// Makes text clickable by adding Button component instead of EventTrigger
  /// This is more reliable and works better with Unity's UI system
  /// </summary>
  void MakeTextClickableWithButton(TextMeshProUGUI text, UnityEngine.Events.UnityAction action)
  {
    if (text == null)
    {
      Debug.LogWarning("[AuthUI] Text is null, cannot make clickable");
      return;
    }

    // Get or add Button component
    Button button = text.GetComponent<Button>();
    if (button == null)
    {
      button = text.gameObject.AddComponent<Button>();
      Debug.Log($"[AuthUI] Added Button to '{text.gameObject.name}'");
    }

    // Clear old listeners and add new one
    button.onClick.RemoveAllListeners();
    button.onClick.AddListener(action);

    // Make button transparent (we only see the text)
    button.transition = Selectable.Transition.None;

    Debug.Log($"[AuthUI] Made '{text.gameObject.name}' clickable");
  }

  void HideAllPanels()
  {
    if (loginPanel != null) loginPanel.SetActive(false);
    if (signUpPanel != null) signUpPanel.SetActive(false);
    if (forgotPasswordPanel != null) forgotPasswordPanel.SetActive(false);
    if (menuPanel != null) menuPanel.SetActive(false);
    if (guidePanel != null) guidePanel.SetActive(false);
    if (handbookPanel != null) handbookPanel.SetActive(false);
    if (settingsPanel != null) settingsPanel.SetActive(false);
    if (creditPanel != null) creditPanel.SetActive(false);
  }

  void ShowLoginPanel()
  {
    Debug.Log("[AuthUI] Showing Login Panel");
    HideAllPanels();
    if (loginPanel != null) loginPanel.SetActive(true);
    ClearAll();
  }

  void ShowSignUpPanel()
  {
    Debug.Log("[AuthUI] Showing Sign Up Panel");
    HideAllPanels();
    if (signUpPanel != null) signUpPanel.SetActive(true);
    ClearAll();
  }

  void ShowForgotPasswordPanel()
  {
    Debug.Log("[AuthUI] Showing Forgot Password Panel");

    if (forgotPasswordPanel == null)
    {
      Debug.LogWarning("[AuthUI] Forgot Password Panel not assigned!");
      return;
    }

    HideAllPanels();
    forgotPasswordPanel.SetActive(true);
    ClearAll();
  }

  void ShowMenuPanel()
  {
    Debug.Log("[AuthUI] Showing Menu Panel");
    HideAllPanels();
    if (menuPanel != null) menuPanel.SetActive(true);
  }

  void ShowGuidePanel()
  {
    Debug.Log("[AuthUI] Showing Guide Panel");
    HideAllPanels();
    if (guidePanel != null) guidePanel.SetActive(true);
  }

  void ShowHandbookPanel()
  {
    Debug.Log("[AuthUI] Showing Handbook Panel");
    HideAllPanels();
    if (handbookPanel != null) handbookPanel.SetActive(true);
  }

  void ShowSettingsPanel()
  {
    Debug.Log("[AuthUI] Showing Settings Panel");
    HideAllPanels();
    if (settingsPanel != null) settingsPanel.SetActive(true);
  }

  void ShowCreditPanel()
  {
    Debug.Log("[AuthUI] Showing Credit Panel");
    HideAllPanels();
    if (creditPanel != null) creditPanel.SetActive(true);
  }

  async void OnLoginClicked()
  {
    string email = loginEmailInput.text.Trim();
    string password = loginPasswordInput.text;

    // Check if fields are empty
    if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
    {
      loginErrorText.text = "Enter email and password";
      return;
    }

    // Basic email format check
    if (!email.Contains("@") || !email.Contains("."))
    {
      loginErrorText.text = "Invalid email format";
      return;
    }

    loginErrorText.text = "Logging in...";
    loginButton.interactable = false;

    var result = await AuthManager.Instance.Login(email, password);

    if (result.Success)
    {
      loginErrorText.text = "Success!";
      await System.Threading.Tasks.Task.Delay(150);

      string displayName = result.Data.Email;
      var userData = await DatabaseManager.Instance.GetUserData(result.Data.UserId);
      if (userData.Success && userData.Data != null && !string.IsNullOrEmpty(userData.Data.username))
        displayName = userData.Data.username;

      if (welcomeText != null) welcomeText.text = $"Welcome, {displayName}!";
      ShowMenuPanel();
    }
    else
    {
      loginErrorText.text = "Invalid email/password";
      loginButton.interactable = true;
    }
  }

  async void OnSignUpClicked()
  {
    string username = signUpUsernameInput != null ? signUpUsernameInput.text.Trim() : "";
    string email = signUpEmailInput.text.Trim();
    string password = signUpPasswordInput.text;

    // Validate username
    if (string.IsNullOrWhiteSpace(username))
    {
      signUpErrorText.text = "Enter username";
      return;
    }
    if (username.Length < 2)
    {
      signUpErrorText.text = "Username too short";
      return;
    }
    if (username.Length > 8)
    {
      signUpErrorText.text = "Username too long";
      return;
    }

    if (!ValidateInput(email, password, signUpErrorText)) return;

    signUpErrorText.text = "Creating account...";
    signUpButton.interactable = false;

    // Check username uniqueness
    var usernameCheck = await DatabaseManager.Instance.CheckUsernameExists(username);
    if (!usernameCheck.Success)
    {
      signUpErrorText.text = "Error occurred";
      signUpButton.interactable = true;
      return;
    }
    if (usernameCheck.Data)
    {
      signUpErrorText.text = "Username taken";
      signUpButton.interactable = true;
      return;
    }

    var result = await AuthManager.Instance.Register(email, password);

    if (result.Success)
    {
      signUpErrorText.text = "Saving...";
      await DatabaseManager.Instance.SaveUserData(result.Data.UserId, result.Data.Email, username);

      signUpErrorText.text = "Success!";
      await System.Threading.Tasks.Task.Delay(150);
      if (welcomeText != null) welcomeText.text = $"Welcome, {username}!";
      ShowMenuPanel();
    }
    else
    {
      signUpErrorText.text = SimplifyError(result.ErrorMessage);
      signUpButton.interactable = true;
    }
  }

  void OnStartGameClicked()
  {
    SceneManager.LoadScene(gameSceneName);
  }

  void OnSignOutClicked()
  {
    AuthManager.Instance.SignOut();
    ShowLoginPanel();
  }

  async void OnResetClicked()
  {
    if (forgotPasswordEmailInput == null)
    {
      Debug.LogError("[AuthUI] Forgot password email input not assigned!");
      return;
    }

    string email = forgotPasswordEmailInput.text.Trim();

    if (!ValidateEmail(email, forgotPasswordErrorText)) return;

    forgotPasswordErrorText.text = "Sending...";
    sendResetButton.interactable = false;

    try
    {
      await global::Firebase.Auth.FirebaseAuth.DefaultInstance.SendPasswordResetEmailAsync(email);
      forgotPasswordErrorText.text = "Email sent! Check inbox.";
      await System.Threading.Tasks.Task.Delay(2000);
      ShowLoginPanel();
    }
    catch (System.Exception ex)
    {
      forgotPasswordErrorText.text = SimplifyError(ex.Message);
    }
    finally
    {
      sendResetButton.interactable = true;
    }
  }

  bool ValidateInput(string email, string password, TextMeshProUGUI errorText)
  {
    if (!ValidateEmail(email, errorText)) return false;

    if (string.IsNullOrWhiteSpace(password))
    {
      errorText.text = "Enter password";
      return false;
    }

    if (password.Length < 6)
    {
      errorText.text = "Password too short (min 6)";
      return false;
    }

    return true;
  }

  bool ValidateEmail(string email, TextMeshProUGUI errorText)
  {
    if (string.IsNullOrWhiteSpace(email))
    {
      errorText.text = "Enter email";
      return false;
    }

    if (!email.Contains("@"))
    {
      errorText.text = "Invalid email";
      return false;
    }

    int atIndex = email.IndexOf("@");
    if (!email.Substring(atIndex).Contains("."))
    {
      errorText.text = "Invalid email";
      return false;
    }

    return true;
  }

  string SimplifyError(string error)
  {
    if (string.IsNullOrEmpty(error)) return "Error occurred";

    string e = error.ToLower();

    if (e.Contains("invalid") && (e.Contains("email") || e.Contains("password"))) return "Invalid email/password";
    if (e.Contains("email already registered")) return "Email already registered";
    if (e.Contains("too many attempts")) return "Too many attempts";
    if (e.Contains("network error")) return "Network error";
    if (e.Contains("password is too weak")) return "Password too weak";

    return "Error occurred";
  }

  void ClearAll()
  {
    if (loginEmailInput != null) loginEmailInput.text = "";
    if (loginPasswordInput != null) loginPasswordInput.text = "";
    if (signUpUsernameInput != null) signUpUsernameInput.text = "";
    if (signUpEmailInput != null) signUpEmailInput.text = "";
    if (signUpPasswordInput != null) signUpPasswordInput.text = "";
    if (forgotPasswordEmailInput != null) forgotPasswordEmailInput.text = "";

    if (loginErrorText != null) loginErrorText.text = "";
    if (signUpErrorText != null) signUpErrorText.text = "";
    if (forgotPasswordErrorText != null) forgotPasswordErrorText.text = "";

    if (loginButton != null) loginButton.interactable = true;
    if (signUpButton != null) signUpButton.interactable = true;
    if (sendResetButton != null) sendResetButton.interactable = true;
  }

  void OnDestroy()
  {
    if (loginButton != null) loginButton.onClick.RemoveAllListeners();
    if (signUpButton != null) signUpButton.onClick.RemoveAllListeners();
    if (sendResetButton != null) sendResetButton.onClick.RemoveAllListeners();
    if (startGameButton != null) startGameButton.onClick.RemoveAllListeners();
    if (creditsButton != null) creditsButton.onClick.RemoveAllListeners();
    if (handbookButton != null) handbookButton.onClick.RemoveAllListeners();
    if (settingsButton != null) settingsButton.onClick.RemoveAllListeners();
    if (signOutButton != null) signOutButton.onClick.RemoveAllListeners();
    if (backToLoginButton != null) backToLoginButton.onClick.RemoveAllListeners();
    if (guideBackButton != null) guideBackButton.onClick.RemoveAllListeners();
    if (handbookBackButton != null) handbookBackButton.onClick.RemoveAllListeners();
    if (creditBackButton != null) creditBackButton.onClick.RemoveAllListeners();
    if (settingsBackButton != null) settingsBackButton.onClick.RemoveAllListeners();
  }
}
