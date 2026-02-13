/// <summary>
/// File: Authuicontroller.cs
/// Author: Jayden Wong
/// Description: Controls all menu UI panels including login, sign-up, handbook, settings, and end screen with Firebase integration.
/// </summary>
using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using BentoBoss.FirebaseManagers;
using UnityEngine.SceneManagement;

public class AuthUIController : MonoBehaviour
{
  public static AuthUIController Instance { get; private set; }

  private Canvas _canvas;

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

  [SerializeField] private TextMeshProUGUI handbookUsernameText;
  [SerializeField] private TextMeshProUGUI handbookEmailText;

  [SerializeField] private TextMeshProUGUI handbookTotalPlaysText;
  [SerializeField] private TextMeshProUGUI handbookBestScoreText;
  [SerializeField] private TextMeshProUGUI handbookRecentScoreText;
  [SerializeField] private TextMeshProUGUI handbookBestGradeText;
  [SerializeField] private TextMeshProUGUI handbookOrdersCompletedText;
  [SerializeField] private TextMeshProUGUI handbookOrderAccuracyText;
  [SerializeField] private TextMeshProUGUI handbookChangeAccuracyText;
  [SerializeField] private TextMeshProUGUI handbookHighestBalanceText;
  [SerializeField] private TextMeshProUGUI handbookPlayTimeText;

  [Header("Credits Panel")]
  [SerializeField] private GameObject creditPanel;
  [SerializeField] private Button creditBackButton;

  [Header("Settings Panel")]
  [SerializeField] private GameObject settingsPanel;
  [SerializeField] private Button settingsBackButton;
  [SerializeField] private Slider musicVolumeSlider;

  [Header("End Screen Panel")]
  [SerializeField] private GameObject endScreenPanel;
  [SerializeField] private TextMeshProUGUI dateText;
  [SerializeField] private TextMeshProUGUI gradeText;
  [SerializeField] private TextMeshProUGUI scoreText;
  [SerializeField] private TextMeshProUGUI earningsText;
  [SerializeField] private TextMeshProUGUI statsText;
  [SerializeField] private Button playAgainButton;
  [SerializeField] private Button mainMenuButtonEndScreen;

  [Header("Settings")]
  [SerializeField] private string gameSceneName = "GameScene";

  private bool _hasEndScreenData = false;
  private int _finalScore;
  private string _grade;
  private float _totalEarnings;
  private float _totalTimeSeconds;
  private int _totalOrders;
  private int _trashDisposed;
  private string _completedAt;

  void Awake()
  {
    if (Instance != null && Instance != this)
    {
      Destroy(gameObject);
      return;
    }
    Instance = this;
    DontDestroyOnLoad(gameObject);

    _canvas = GetComponentInChildren<Canvas>();

    if (loginPanel == null) Debug.LogError("[AuthUI] LoginPanel not assigned!");
    if (signUpPanel == null) Debug.LogError("[AuthUI] SignUpPanel not assigned!");
    if (forgotPasswordPanel == null) Debug.LogWarning("[AuthUI] ForgotPasswordPanel not assigned!");
    if (menuPanel == null) Debug.LogWarning("[AuthUI] MenuPanel not assigned!");
    if (guidePanel == null) Debug.LogWarning("[AuthUI] GuidePanel not assigned!");
    if (handbookPanel == null) Debug.LogWarning("[AuthUI] HandbookPanel not assigned!");
    if (settingsPanel == null) Debug.LogWarning("[AuthUI] SettingsPanel not assigned!");
    if (creditPanel == null) Debug.LogWarning("[AuthUI] CreditPanel not assigned!");
  }

  void OnEnable()
  {
    SceneManager.sceneLoaded += OnSceneLoaded;
  }

  void OnDisable()
  {
    SceneManager.sceneLoaded -= OnSceneLoaded;
  }

  void Start()
  {
    loginButton.onClick.AddListener(OnLoginClicked);
    signUpButton.onClick.AddListener(OnSignUpClicked);
    if (sendResetButton != null) sendResetButton.onClick.AddListener(OnResetClicked);

    if (startGameButton != null) startGameButton.onClick.AddListener(OnStartGameClicked);
    if (creditsButton != null) creditsButton.onClick.AddListener(ShowCreditPanel);
    if (handbookButton != null) handbookButton.onClick.AddListener(ShowHandbookPanel);
    if (settingsButton != null) settingsButton.onClick.AddListener(ShowSettingsPanel);
    if (signOutButton != null) signOutButton.onClick.AddListener(OnSignOutClicked);

    if (guideBackButton != null) guideBackButton.onClick.AddListener(ShowMenuPanel);
    if (handbookBackButton != null) handbookBackButton.onClick.AddListener(ShowMenuPanel);
    if (creditBackButton != null) creditBackButton.onClick.AddListener(ShowMenuPanel);
    if (settingsBackButton != null) settingsBackButton.onClick.AddListener(ShowMenuPanel);

    MakeTextClickableWithButton(goToSignUpText, ShowSignUpPanel);
    MakeTextClickableWithButton(goToLoginText, ShowLoginPanel);
    MakeTextClickableWithButton(forgotPasswordText, ShowForgotPasswordPanel);
    if (backToLoginButton != null) backToLoginButton.onClick.AddListener(ShowLoginPanel);

    if (musicVolumeSlider != null && AudioManager.Instance != null)
      AudioManager.Instance.InitSlider(musicVolumeSlider);

    ShowLoginPanel();
    Debug.Log("[AuthUI] Started - Login panel active");
  }

  /// <summary>
  /// Adds a transparent Button component to a TextMeshProUGUI element to make it clickable
  /// </summary>
  void MakeTextClickableWithButton(TextMeshProUGUI text, UnityEngine.Events.UnityAction action)
  {
    if (text == null)
    {
      Debug.LogWarning("[AuthUI] Text is null, cannot make clickable");
      return;
    }

    Button button = text.GetComponent<Button>();
    if (button == null)
    {
      button = text.gameObject.AddComponent<Button>();
      Debug.Log($"[AuthUI] Added Button to '{text.gameObject.name}'");
    }

    button.onClick.RemoveAllListeners();
    button.onClick.AddListener(action);

    // Use no transition so only the text is visible, not a button background
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
    if (endScreenPanel != null) endScreenPanel.SetActive(false);
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
    if (_canvas != null) _canvas.enabled = true;
    HideAllPanels();
    if (menuPanel != null) menuPanel.SetActive(true);
  }

  void ShowGuidePanel()
  {
    Debug.Log("[AuthUI] Showing Guide Panel");
    HideAllPanels();
    if (guidePanel != null) guidePanel.SetActive(true);
  }

  async void ShowHandbookPanel()
  {
    Debug.Log("[AuthUI] Showing Handbook Panel");
    HideAllPanels();
    if (handbookPanel != null) handbookPanel.SetActive(true);

    await PopulateHandbookData();
  }

  /// <summary>
  /// Fetches user data and aggregate statistics from Firebase to populate the Handbook UI
  /// </summary>
  private async Task PopulateHandbookData()
  {
    string userId = AuthManager.Instance?.CurrentUser?.UserId;
    if (string.IsNullOrEmpty(userId))
    {
      Debug.LogWarning("[AuthUI] Cannot populate handbook - no user logged in");
      return;
    }

    var userDataResult = await DatabaseManager.Instance.GetUserData(userId);
    if (userDataResult.Success && userDataResult.Data != null)
    {
      if (handbookUsernameText != null)
        handbookUsernameText.text = $"Username: {userDataResult.Data.username}";

      if (handbookEmailText != null)
        handbookEmailText.text = $"Email: {userDataResult.Data.email}";
    }
    else
    {
      Debug.LogWarning($"[AuthUI] Failed to load user data: {userDataResult.ErrorMessage}");
    }

    var statsResult = await DatabaseManager.Instance.GetAggregateStats(userId);
    if (statsResult.Success && statsResult.Data != null)
    {
      var stats = statsResult.Data;

      if (handbookTotalPlaysText != null)
        handbookTotalPlaysText.text = $"Total Plays: {stats.totalSessions}";

      if (handbookBestScoreText != null)
        handbookBestScoreText.text = $"Best Score: {stats.bestScore}";

      if (handbookRecentScoreText != null)
        handbookRecentScoreText.text = $"Recent Score: {stats.recentScore}";

      if (handbookBestGradeText != null)
        handbookBestGradeText.text = $"Best Grade: {stats.bestGrade}";

      if (handbookOrdersCompletedText != null)
        handbookOrdersCompletedText.text = $"Orders Completed: {stats.totalOrdersCompleted}";

      if (handbookOrderAccuracyText != null)
        handbookOrderAccuracyText.text = $"Order Accuracy: {stats.foodAccuracyPercent:F1}%";

      if (handbookChangeAccuracyText != null)
        handbookChangeAccuracyText.text = $"Change Accuracy: {stats.changeAccuracyPercent:F1}%";

      if (handbookHighestBalanceText != null)
        handbookHighestBalanceText.text = $"Highest Balance: ${stats.highestBalance:F2}";

      if (handbookPlayTimeText != null)
        handbookPlayTimeText.text = $"Play Time: {stats.GetFormattedPlaytime()}";

      Debug.Log($"[AuthUI] Handbook populated with {stats.totalSessions} sessions");
    }
    else
    {
      Debug.LogWarning($"[AuthUI] Failed to load aggregate stats: {statsResult.ErrorMessage}");

      if (handbookTotalPlaysText != null) handbookTotalPlaysText.text = "Total Plays: 0";
      if (handbookBestScoreText != null) handbookBestScoreText.text = "Best Score: 0";
      if (handbookRecentScoreText != null) handbookRecentScoreText.text = "Recent Score: 0";
      if (handbookBestGradeText != null) handbookBestGradeText.text = "Best Grade: F";
      if (handbookOrdersCompletedText != null) handbookOrdersCompletedText.text = "Orders Completed: 0";
      if (handbookOrderAccuracyText != null) handbookOrderAccuracyText.text = "Order Accuracy: 0.0%";
      if (handbookChangeAccuracyText != null) handbookChangeAccuracyText.text = "Change Accuracy: 0.0%";
      if (handbookHighestBalanceText != null) handbookHighestBalanceText.text = "Highest Balance: $0.00";
      if (handbookPlayTimeText != null) handbookPlayTimeText.text = "Play Time: 0m";
    }
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

    if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
    {
      loginErrorText.text = "Enter email and password";
      return;
    }

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

  void OnSceneLoaded(Scene scene, LoadSceneMode mode)
  {
    Debug.Log($"[AuthUI] Scene loaded: {scene.name}, Has end data: {_hasEndScreenData}");

    if (scene.name == "MenuScene")
    {
      if (_canvas != null && !_canvas.enabled)
      {
        _canvas.enabled = true;
        Debug.Log("[AuthUI] Canvas re-enabled");
      }

      if (_hasEndScreenData)
      {
        Debug.Log("[AuthUI] Showing end screen");
        ShowEndScreen();
      }
    }
  }

  /// <summary>
  /// Stores end-of-session data that survives the scene transition back to MenuScene
  /// </summary>
  public void SetEndScreenData(int score, string grade, float earnings, float timeSeconds, int orders, int waste, string completedAt)
  {
    _finalScore = score;
    _grade = grade;
    _totalEarnings = earnings;
    _totalTimeSeconds = timeSeconds;
    _totalOrders = orders;
    _trashDisposed = waste;
    _completedAt = completedAt;
    _hasEndScreenData = true;
  }

  void ShowEndScreen()
  {
    if (_canvas != null) _canvas.enabled = true;

    HideAllPanels();
    if (endScreenPanel != null) endScreenPanel.SetActive(true);

    if (dateText != null)
    {
      if (DateTime.TryParse(_completedAt, out DateTime parsed))
        dateText.text = parsed.ToLocalTime().ToString("MMM dd, yyyy - h:mm tt");
      else
        dateText.text = _completedAt;
    }
    if (gradeText != null) gradeText.text = $"GRADE: {_grade}";
    if (scoreText != null) scoreText.text = $"{_finalScore} pts";
    if (earningsText != null) earningsText.text = $"${_totalEarnings:F2}";

    int minutes = Mathf.FloorToInt(_totalTimeSeconds / 60);
    int seconds = Mathf.FloorToInt(_totalTimeSeconds % 60);
    if (statsText != null)
    {
      statsText.text = $"Time: {minutes}m {seconds}s    Orders: {_totalOrders}\nWaste: {_trashDisposed}";
    }

    if (playAgainButton != null)
    {
      playAgainButton.onClick.RemoveAllListeners();
      playAgainButton.onClick.AddListener(OnPlayAgainClicked);
    }

    if (mainMenuButtonEndScreen != null)
    {
      mainMenuButtonEndScreen.onClick.RemoveAllListeners();
      mainMenuButtonEndScreen.onClick.AddListener(OnMainMenuFromEndScreenClicked);
    }

    Debug.Log($"[AuthUI] Showing End Screen — Score: {_finalScore} ({_grade})");
  }

  void OnPlayAgainClicked()
  {
    _hasEndScreenData = false;
    OnStartGameClicked();
  }

  void OnMainMenuFromEndScreenClicked()
  {
    _hasEndScreenData = false;
    ShowMenuPanel();
  }

  void OnStartGameClicked()
  {
    // Hide Canvas during gameplay so it doesn't render over GameScene
    if (_canvas != null) _canvas.enabled = false;
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
    if (playAgainButton != null) playAgainButton.onClick.RemoveAllListeners();
    if (mainMenuButtonEndScreen != null) mainMenuButtonEndScreen.onClick.RemoveAllListeners();
  }
}
