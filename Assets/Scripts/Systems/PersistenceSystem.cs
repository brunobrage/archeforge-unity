using System.Collections.Generic;
using UnityEngine;
using System.IO;

[System.Serializable]
public class GameState
{
    public Dictionary<string, int> Affinities { get; set; }
    public int CollectorCount { get; set; }
    public List<SavedItem> Inventory { get; set; }
    public List<int> EnemyHealth { get; set; }
    public float PlayerX { get; set; }
    public float PlayerY { get; set; }
}

[System.Serializable]
public class SavedItem
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string SpriteKey { get; set; }
    public int Quantity { get; set; }
    public int MaxStack { get; set; }
}

public class PersistenceSystem : MonoBehaviour
{
    private string savePath;
    private const string SAVE_FILENAME = "gamestate.json";

    void Awake()
    {
        savePath = Path.Combine(Application.persistentDataPath, SAVE_FILENAME);
        Debug.Log($"[PersistenceSystem] Save path: {savePath}");
    }

    public GameState Load()
    {
        if (File.Exists(savePath))
        {
            try
            {
                string json = File.ReadAllText(savePath);
                GameState state = JsonUtility.FromJson<GameState>(json);
                Debug.Log("[PersistenceSystem] Game state loaded successfully");
                return state;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[PersistenceSystem] Failed to load game state: {e.Message}");
                return null;
            }
        }

        Debug.Log("[PersistenceSystem] No save file found, starting fresh");
        return null;
    }

    public void Save(GameState state)
    {
        try
        {
            string json = JsonUtility.ToJson(state, true);
            File.WriteAllText(savePath, json);
            Debug.Log("[PersistenceSystem] Game state saved successfully");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[PersistenceSystem] Failed to save game state: {e.Message}");
        }
    }

    public void DeleteSave()
    {
        if (File.Exists(savePath))
        {
            File.Delete(savePath);
            Debug.Log("[PersistenceSystem] Save file deleted");
        }
    }
}
