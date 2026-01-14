using System;
using System.Collections.Generic;

[Serializable]
public class PlayerUpgradeLevels
{
    private Dictionary<string, int> _levels = new();

    public int GetLevel(string upgradeId)
    {
        return _levels.GetValueOrDefault(upgradeId, 0);
    }

    public void SetLevel(string upgradeId, int level)
    {
        _levels[upgradeId] = level;
    }

    public void IncrementLevel(string upgradeId)
    {
        int current = GetLevel(upgradeId);
        _levels[upgradeId] = current + 1;
    }

    public Dictionary<string, int> GetAll()
    {
        return new Dictionary<string, int>(_levels);
    }

    public string ToJson()
    {
        var wrapper = new SerializableWrapper { levels = new List<LevelEntry>() };
        foreach (var kvp in _levels)
        {
            wrapper.levels.Add(new LevelEntry { id = kvp.Key, level = kvp.Value });
        }
        return UnityEngine.JsonUtility.ToJson(wrapper);
    }

    public static PlayerUpgradeLevels FromJson(string json)
    {
        var result = new PlayerUpgradeLevels();

        if (string.IsNullOrEmpty(json))
        {
            return result;
        }

        try
        {
            var wrapper = UnityEngine.JsonUtility.FromJson<SerializableWrapper>(json);
            if (wrapper?.levels != null)
            {
                foreach (var entry in wrapper.levels)
                {
                    result._levels[entry.id] = entry.level;
                }
            }
        }
        catch (Exception e)
        {
            // JSON 파싱 실패 시 빈 레벨 반환
            UnityEngine.Debug.LogError($"[PlayerUpgradeLevels] JSON 파싱에 실패했습니다. 오류: {e.Message}");
        }

        return result;
    }

    [Serializable]
    private class SerializableWrapper
    {
        public List<LevelEntry> levels;
    }

    [Serializable]
    private class LevelEntry
    {
        public string id;
        public int level;
    }
}
