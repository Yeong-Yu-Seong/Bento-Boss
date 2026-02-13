/// <summary>
/// File: AuthManager.cs
/// Author: Jayden Wong
/// Description: Handles Firebase user registration, login, and sign-out with error parsing.
/// </summary>
using System;
using System.Threading.Tasks;
using UnityEngine;
using Firebase;
using Firebase.Auth;

namespace BentoBoss.FirebaseManagers
{
  public class AuthManager : MonoBehaviour
  {
    public static AuthManager Instance { get; private set; }

    private global::Firebase.Auth.FirebaseAuth _auth;
    public FirebaseUser CurrentUser => _auth?.CurrentUser;

    void Awake()
    {
      if (Instance != null && Instance != this)
      {
        Destroy(gameObject);
        return;
      }

      Instance = this;
      DontDestroyOnLoad(gameObject);

      FirebaseManager.Instance.OnFirebaseReady += () =>
      {
        _auth = global::Firebase.Auth.FirebaseAuth.DefaultInstance;
        Debug.Log("[Auth] Ready");
      };
    }

    /// <summary>
    /// Registers a new user with email and password (minimum 6 characters)
    /// </summary>
    public async Task<FirebaseResult<FirebaseUser>> Register(string email, string password)
    {
      if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
        return new FirebaseResult<FirebaseUser>(false, null, "Email/password cannot be empty");

      if (password.Length < 6)
        return new FirebaseResult<FirebaseUser>(false, null, "Password must be at least 6 characters");

      try
      {
        var result = await _auth.CreateUserWithEmailAndPasswordAsync(email, password);
        Debug.Log($"[Auth] User registered: {result.User.Email}");
        return new FirebaseResult<FirebaseUser>(true, result.User);
      }
      catch (FirebaseException ex)
      {
        string errorMessage = ParseRegisterError(ex);
        Debug.LogError($"[Auth] Register failed: {errorMessage}");
        return new FirebaseResult<FirebaseUser>(false, null, errorMessage);
      }
      catch (Exception ex)
      {
        Debug.LogError($"[Auth] Register failed: {ex.Message}");
        return new FirebaseResult<FirebaseUser>(false, null, "Error occurred");
      }
    }

    /// <summary>
    /// Signs in an existing user with email and password
    /// </summary>
    public async Task<FirebaseResult<FirebaseUser>> Login(string email, string password)
    {
      if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
        return new FirebaseResult<FirebaseUser>(false, null, "Email/password cannot be empty");

      try
      {
        var result = await _auth.SignInWithEmailAndPasswordAsync(email, password);
        Debug.Log($"[Auth] User logged in: {result.User.Email}");
        return new FirebaseResult<FirebaseUser>(true, result.User);
      }
      catch (FirebaseException ex)
      {
        string errorMessage = ParseAuthError(ex);
        Debug.LogError($"[Auth] Login failed: {errorMessage}");
        return new FirebaseResult<FirebaseUser>(false, null, errorMessage);
      }
      catch (Exception ex)
      {
        Debug.LogError($"[Auth] Login failed: {ex.Message}");
        return new FirebaseResult<FirebaseUser>(false, null, "Error occurred");
      }
    }

    /// <summary>
    /// Signs out the current user
    /// </summary>
    public void SignOut()
    {
      _auth?.SignOut();
      Debug.Log("[Auth] User signed out");
    }

    private string ParseAuthError(FirebaseException ex)
    {
      AuthError errorCode = (AuthError)ex.ErrorCode;

      switch (errorCode)
      {
        case AuthError.WrongPassword:
        case AuthError.UserNotFound:
        case AuthError.InvalidEmail:
          return "Invalid email/password";
        case AuthError.UserDisabled:
          return "Account has been disabled";
        case AuthError.TooManyRequests:
          return "Too many attempts. Please try again later";
        case AuthError.NetworkRequestFailed:
          return "Network error. Check your connection";
        default:
          return "Error occurred";
      }
    }

    private string ParseRegisterError(FirebaseException ex)
    {
      AuthError errorCode = (AuthError)ex.ErrorCode;

      switch (errorCode)
      {
        case AuthError.EmailAlreadyInUse:
          return "Email already registered";
        case AuthError.InvalidEmail:
          return "Invalid email format";
        case AuthError.WeakPassword:
          return "Password is too weak";
        case AuthError.NetworkRequestFailed:
          return "Network error. Check your connection";
        default:
          return "Error occurred";
      }
    }
  }
}
