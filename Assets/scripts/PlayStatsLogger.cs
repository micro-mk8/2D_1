using UnityEngine;
using System.IO;

// ゲーム全体のプレイ統計を保存・読み込みする係
public class PlayStatsLogger : MonoBehaviour
{
    [System.Serializable]
    private class SaveData
    {
        public int totalPlayCount;
        public int clearCount;
        public int gameOverCount;
    }

    public int TotalPlayCount { get; private set; }
    public int ClearCount      { get; private set; }
    public int GameOverCount   { get; private set; }

    string SavePath => Path.Combine(Application.persistentDataPath, "playstats.txt");

    void Awake()
    {
        LoadStats();
    }

    void LoadStats()
    {
        // 初回 (ファイルがまだない) 場合は 0,0,0 で開始
        if (!File.Exists(SavePath))
        {
            TotalPlayCount = 0;
            ClearCount = 0;
            GameOverCount = 0;
            return;
        }

        try
        {
            string json = File.ReadAllText(SavePath);
            var data = JsonUtility.FromJson<SaveData>(json);
            if (data != null)
            {
                TotalPlayCount = data.totalPlayCount;
                ClearCount     = data.clearCount;
                GameOverCount  = data.gameOverCount;
            }
        }
        catch
        {
            // ファイル壊れててもゲーム自体は止めたくないので、0スタートにする
            TotalPlayCount = 0;
            ClearCount = 0;
            GameOverCount = 0;
        }
    }

    void SaveStats()
    {
        var data = new SaveData()
        {
            totalPlayCount = TotalPlayCount,
            clearCount     = ClearCount,
            gameOverCount  = GameOverCount
        };

        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(SavePath, json);
    }

    // --- 外向けAPI ---

    public void RegisterGameStart()
    {
        TotalPlayCount++;
        SaveStats();
        Debug.Log("[Stats] GameStart total=" + TotalPlayCount);
    }

    public void RegisterClear()
    {
        ClearCount++;
        SaveStats();
        Debug.Log("[Stats] Clear total=" + ClearCount);
    }

    public void RegisterGameOver()
    {
        GameOverCount++;
        SaveStats();
        Debug.Log("[Stats] GameOver total=" + GameOverCount);
    }
}
