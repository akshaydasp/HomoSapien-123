using System.IO;
using UnityEngine;
using System.Linq;
using System.Collections.Generic;

public static class SaveSystem
{
    public class GameSaveData
    {
        public int cols;
        public int rows;
        public int score;
        public int highScore;
        public int combo;
        public double lastComboTime;
        public List<int> cardOrder;
        public List<int> matchedIndices;
        public List<int> faceUpIndices;
    }

    static string PathFor(string filename)
    {
        return Path.Combine(Application.persistentDataPath, filename);
    }

    public static void Save(GameManager gm, string filename = "save.json")
    {
        var sd = new GameSaveData();
        sd.cols = gm.cols;
        sd.rows = gm.rows;
        sd.cardOrder = gm.GetCardOrder(); 
        sd.matchedIndices = gm.GetMatchedIndices();
        sd.faceUpIndices = gm.GetFaceUpIndices();

        sd.score = gm.scoreManager ? gm.scoreManager.CurrentScore : 0;
        sd.highScore = gm.scoreManager ? gm.scoreManager.BestScore : 0;
        sd.combo = gm.scoreManager ? gm.scoreManager.ComboCount : 0;
        sd.lastComboTime = 0; 

        string json = JsonUtility.ToJson(sd, true);
        File.WriteAllText(PathFor(filename), json);
        Debug.Log("Saved to " + PathFor(filename));
    }

    public static GameSaveData LoadRaw(string filename = "save.json")
    {
        var p = PathFor(filename);
        if (!File.Exists(p)) return null;
        var json = File.ReadAllText(p);
        return JsonUtility.FromJson<GameSaveData>(json);
    }

    public static SaveState Load(string filename = "save.json")
    {
        var raw = LoadRaw(filename);
        if (raw == null) return null;
        var state = new SaveState();
        state.cols = raw.cols;
        state.rows = raw.rows;
        state.score = raw.score;
        state.highScore = raw.highScore;
        state.combo = raw.combo;
        state.lastComboTime = raw.lastComboTime;
        state.cardOrder = raw.cardOrder.ToArray();
        state.matchedIndices = raw.matchedIndices.ToArray();
        state.faceUpIndices = raw.faceUpIndices.ToArray();
        return state;
    }
}
