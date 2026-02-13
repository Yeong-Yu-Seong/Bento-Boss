/// <summary>
/// File: ScoreCalculator.cs
/// Author: Jayden Wong
/// Description: Pure static score calculator using food accuracy, change accuracy, speed bonus, and waste penalty.
/// </summary>
using UnityEngine;

public static class ScoreCalculator
{
  private const float FOOD_MAX = 500f;
  private const float CHANGE_MAX = 500f;
  private const float SPEED_MAX = 300f;
  private const float SPEED_FULL_THRESHOLD = 420f;
  private const float SPEED_ZERO_THRESHOLD = 900f;
  private const float WASTE_PENALTY_PER_ITEM = 25f;

  public struct ScoreResult
  {
    public int totalScore;
    public int foodScore;
    public int changeScore;
    public int speedBonus;
    public int wastePenalty;
    public string grade;
  }

  /// <summary>
  /// Calculates the final score from accuracy, speed, and waste metrics and assigns a letter grade
  /// </summary>
  public static ScoreResult Calculate(int foodCorrect, int foodWrong,
      int changeCorrect, int changeWrong, float timeSeconds, int trashDisposed)
  {
    int totalOrders = foodCorrect + foodWrong;

    float foodAccuracy = totalOrders > 0 ? (float)foodCorrect / totalOrders : 0f;
    float rawFood = foodAccuracy * FOOD_MAX;

    float changeAccuracy = totalOrders > 0 ? (float)changeCorrect / totalOrders : 0f;
    float rawChange = changeAccuracy * CHANGE_MAX;

    // Full speed bonus at <=7min, linear falloff to 0 at 15min
    float rawSpeed;
    if (timeSeconds <= SPEED_FULL_THRESHOLD)
    {
      rawSpeed = SPEED_MAX;
    }
    else if (timeSeconds <= SPEED_ZERO_THRESHOLD)
    {
      rawSpeed = SPEED_MAX * (1f - (timeSeconds - SPEED_FULL_THRESHOLD) / (SPEED_ZERO_THRESHOLD - SPEED_FULL_THRESHOLD));
    }
    else
    {
      rawSpeed = 0f;
    }

    float rawWaste = trashDisposed * WASTE_PENALTY_PER_ITEM;

    int foodScore = Mathf.RoundToInt(rawFood);
    int changeScore = Mathf.RoundToInt(rawChange);
    int speedBonus = Mathf.RoundToInt(rawSpeed);
    int wastePenalty = Mathf.RoundToInt(rawWaste);
    int totalScore = Mathf.Max(0, foodScore + changeScore + speedBonus - wastePenalty);

    string grade = GetGrade(totalScore);

    Debug.Log($"[ScoreCalculator] Food: {foodScore} | Change: {changeScore} | Speed: {speedBonus} | Waste: -{wastePenalty} | Total: {totalScore} ({grade})");

    return new ScoreResult
    {
      totalScore = totalScore,
      foodScore = foodScore,
      changeScore = changeScore,
      speedBonus = speedBonus,
      wastePenalty = wastePenalty,
      grade = grade
    };
  }

  private static string GetGrade(int score)
  {
    if (score >= 1100) return "S";
    if (score >= 900) return "A";
    if (score >= 700) return "B";
    if (score >= 500) return "C";
    if (score >= 300) return "D";
    return "F";
  }
}
