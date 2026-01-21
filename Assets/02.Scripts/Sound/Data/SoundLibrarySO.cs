using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SoundLibrary", menuName = "Sound/Sound Library")]
public class SoundLibrarySO : ScriptableObject
{
    [Header("SFX")]
    [SerializeField] private List<SoundEntry> _sfxList = new();

    [Header("BGM")]
    [SerializeField] private List<SoundEntry> _bgmList = new();

    private Dictionary<string, AudioClip> _sfxCache;
    private Dictionary<string, AudioClip> _bgmCache;

    public void Initialize()
    {
        _sfxCache = new Dictionary<string, AudioClip>();
        _bgmCache = new Dictionary<string, AudioClip>();

        foreach (var entry in _sfxList)
        {
            if (entry.Clip != null && !string.IsNullOrEmpty(entry.Id))
            {
                _sfxCache[entry.Id] = entry.Clip;
            }
        }

        foreach (var entry in _bgmList)
        {
            if (entry.Clip != null && !string.IsNullOrEmpty(entry.Id))
            {
                _bgmCache[entry.Id] = entry.Clip;
            }
        }
    }

    public AudioClip GetSfxClip(string id)
    {
        if (_sfxCache == null)
        {
            Initialize();
        }

        if (_sfxCache.TryGetValue(id, out AudioClip clip))
        {
            return clip;
        }

        return null;
    }

    public AudioClip GetBgmClip(string id)
    {
        if (_bgmCache == null)
        {
            Initialize();
        }

        if (_bgmCache.TryGetValue(id, out AudioClip clip))
        {
            return clip;
        }

        return null;
    }

    [Serializable]
    public class SoundEntry
    {
        public string Id;
        public AudioClip Clip;
    }
}
