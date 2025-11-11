using UnityEngine;
using System;

[Serializable]
public class SaveState
{
    public int cols;
    public int rows;
    public int score;
    public int highScore;
    public int combo;
    public double lastComboTime;
    public int[] cardOrder;
    public int[] matchedIndices;
    public int[] faceUpIndices;
}

public class ScoreManager : MonoBehaviour
{
    public int CurrentScore { get; private set; }
    public int BestScore { get; private set; }
    public int ComboCount { get; private set; }
    public float comboWindow = 3f;
    double lastMatchTime = 0;
    public int baseMatchPoints = 100;

    void Awake()
    {
        BestScore = PlayerPrefs.GetInt("best_score", 0);
        CurrentScore = 0;
        ComboCount = 0;
    }

    public void ResetScore()
    {
        CurrentScore = 0;
        ComboCount = 0;
    }

    public void OnMatch()
    {
        double now = Time.realtimeSinceStartupAsDouble;
        if (now - lastMatchTime <= comboWindow)
        {
            ComboCount++;
        }
        else
        {
            ComboCount = 1;
        }
        lastMatchTime = now;
        int earned = baseMatchPoints * ComboCount;
        CurrentScore += earned;
        if (CurrentScore > BestScore)
        {
            BestScore = CurrentScore;
            PlayerPrefs.SetInt("best_score", BestScore);
        }
    }

    public void OnMismatch()
    {
        ComboCount = 0;
        // Minus points for not matching
        CurrentScore = Mathf.Max(0, CurrentScore - baseMatchPoints / 4);
    }

    public void OnGameOver()
    {
        if (CurrentScore > BestScore)
        {
            BestScore = CurrentScore;
            PlayerPrefs.SetInt("best_score", BestScore);
        }
    }

    public void ClearSavedData()
    {
        PlayerPrefs.SetInt("best_score", 0);
    }

    public void LoadState(SaveState s)
    {
        CurrentScore = s.score;
        BestScore = s.highScore;
        ComboCount = s.combo;
        lastMatchTime = s.lastComboTime;
    }
}
