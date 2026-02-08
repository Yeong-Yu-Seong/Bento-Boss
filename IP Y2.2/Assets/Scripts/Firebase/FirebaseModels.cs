using System;
using System.Collections.Generic;

namespace BentoBoss.FirebaseManagers
{
  /// <summary>
  /// Simple result wrapper - tells if operation succeeded or failed
  /// </summary>
  public class FirebaseResult<T>
  {
    public bool Success;
    public string ErrorMessage;
    public T Data;

    public FirebaseResult(bool success, T data = default, string error = "")
    {
      Success = success;
      Data = data;
      ErrorMessage = error;
    }
  }

  /// <summary>
  /// User data stored at: users/{userId}
  /// </summary>
  [Serializable]
  public class UserData
  {
    public string email;
    public string username;

    public Dictionary<string, object> ToDictionary()
    {
      return new Dictionary<string, object>
            {
                { "email", email },
                { "username", username }
            };
    }
  }
}
