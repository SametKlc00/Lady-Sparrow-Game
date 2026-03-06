using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// oyun verilerini saklamak icin kullanilir (oyuncu adi, bolum suresi gibi)
// genellikle lider tablosu icin kullanilir

[System.Serializable]
public class GameData
{
    public static int enemiesKilled;
    public static float timer;

    [System.Serializable]
    public class PlayerRecord {
        public string playerName;
        public float beatTime;
    }

    public List<PlayerRecord> leaderboard = new List<PlayerRecord>();

    // oyuncu kaydini lider tablosuna ekle
    public void AddPlayerRecord(string name, float time) {
        leaderboard.Add(new PlayerRecord { playerName = name, beatTime = time });
        SaveData();
    }

    // veriyi dosyaya kaydet
    public void SaveData() {
        string json = JsonUtility.ToJson(this, true);
        System.IO.File.WriteAllText(Application.persistentDataPath + "/GameData.json", json);
    }

    // veriyi dosyadan yukle
    public static GameData LoadData()
    {
        string filePath = Application.persistentDataPath + "/GameData.json";
        if (System.IO.File.Exists(filePath))
        {
            string json = System.IO.File.ReadAllText(filePath);
            return JsonUtility.FromJson<GameData>(json);
        }
        return new GameData(); // dosya yoksa yeni GameData dondur
    }
}
