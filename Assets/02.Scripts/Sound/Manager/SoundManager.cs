using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public class SoundManager : DontDestroySingleton<SoundManager>
{
    [Header("Audio Mixer")]
    [SerializeField] private AudioMixer _audioMixer;

    [Header("Sound Library")]
    [SerializeField] private SoundLibrarySO _soundLibrary;

    [Header("Pool Settings")]
    [SerializeField] private int _poolSize = 40;

    [Header("BGM Settings")]
    [SerializeField] private float _crossFadeDuration = 1.0f;

    [Header("SFX Settings")]
    [SerializeField] private float _throttleInterval = 0.05f;
    [SerializeField] private float _minPitch = 0.9f;
    [SerializeField] private float _maxPitch = 1.1f;

    private const string MasterVolumeParam = "MasterVolume";
    private const string BgmVolumeParam = "MusicVolume";
    private const string SfxVolumeParam = "SfxVolume";

    private const string MasterVolumeKey = "SoundManager_MasterVolume";
    private const string BgmVolumeKey = "SoundManager_BGMVolume";
    private const string SfxVolumeKey = "SoundManager_SFXVolume";

    private AudioSource _bgmSourceA;
    private AudioSource _bgmSourceB;
    private bool _isUsingSourceA = true;

    private Queue<AudioSource> _sfxPool;
    private Transform _poolParent;

    private Dictionary<AudioClip, float> _lastPlayTimes;

    private Coroutine _crossFadeCoroutine;

    private ISoundRepository _repository;

    protected override void Initialize()
    {
        _repository = new SoundRepository();

        if (_soundLibrary != null)
        {
            _soundLibrary.Initialize();
        }

        InitializePool();
        InitializeBgmSources();
        InitializeThrottling();
        LoadVolumeSettings();
    }

    private void InitializePool()
    {
        _sfxPool = new Queue<AudioSource>();

        var poolObject = new GameObject("SFX_Pool");
        poolObject.transform.SetParent(transform);
        _poolParent = poolObject.transform;

        for (int i = 0; i < _poolSize; i++)
        {
            CreatePooledAudioSource();
        }
    }

    private void CreatePooledAudioSource()
    {
        var sourceObject = new GameObject($"SFX_Source_{_sfxPool.Count}");
        sourceObject.transform.SetParent(_poolParent);

        var audioSource = sourceObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;

        if (_audioMixer != null)
        {
            var sfxGroups = _audioMixer.FindMatchingGroups("SFX");
            if (sfxGroups.Length > 0)
            {
                audioSource.outputAudioMixerGroup = sfxGroups[0];
            }
        }

        sourceObject.SetActive(false);
        _sfxPool.Enqueue(audioSource);
    }

    private void InitializeBgmSources()
    {
        var bgmObject = new GameObject("BGM_Sources");
        bgmObject.transform.SetParent(transform);

        _bgmSourceA = bgmObject.AddComponent<AudioSource>();
        _bgmSourceB = bgmObject.AddComponent<AudioSource>();

        ConfigureBgmSource(_bgmSourceA);
        ConfigureBgmSource(_bgmSourceB);
    }

    private void ConfigureBgmSource(AudioSource source)
    {
        source.playOnAwake = false;
        source.loop = true;
        source.volume = 0f;

        if (_audioMixer != null)
        {
            var musicGroups = _audioMixer.FindMatchingGroups("Music");
            if (musicGroups.Length > 0)
            {
                source.outputAudioMixerGroup = musicGroups[0];
            }
        }
    }

    private void InitializeThrottling()
    {
        _lastPlayTimes = new Dictionary<AudioClip, float>();
    }

    public void PlaySFX(SfxId sfxId)
    {
        string key = sfxId.ToKey();
        AudioClip clip = _soundLibrary?.GetSfxClip(key);

        if (clip == null)
        {
            Debug.LogWarning($"[SoundManager] SFX 클립을 찾을 수 없습니다: {sfxId}");
            return;
        }

        SfxData data = _repository.GetSfxData(key);
        float volume = data?.Volume ?? 1f;
        bool useRandomPitch = data?.UseRandomPitch ?? true;

        PlaySFXInternal(clip, volume, useRandomPitch);
    }

    public void PlaySFX(SfxId sfxId, Vector3 position)
    {
        string key = sfxId.ToKey();
        AudioClip clip = _soundLibrary?.GetSfxClip(key);

        if (clip == null)
        {
            Debug.LogWarning($"[SoundManager] SFX 클립을 찾을 수 없습니다: {sfxId}");
            return;
        }

        SfxData data = _repository.GetSfxData(key);
        float volume = data?.Volume ?? 1f;
        bool useRandomPitch = data?.UseRandomPitch ?? true;

        PlaySFXInternal(clip, position, volume, useRandomPitch);
    }

    public void PlaySFX(AudioClip clip, float volume = 1.0f)
    {
        PlaySFXInternal(clip, volume, true);
    }

    public void PlaySFX(AudioClip clip, Vector3 position, float volume = 1.0f)
    {
        PlaySFXInternal(clip, position, volume, true);
    }

    private void PlaySFXInternal(AudioClip clip, float volume, bool useRandomPitch)
    {
        if (clip == null)
        {
            return;
        }

        if (!CanPlaySound(clip))
        {
            return;
        }

        AudioSource source = GetFromPool();
        if (source == null)
        {
            return;
        }

        source.clip = clip;
        source.volume = volume;
        source.pitch = useRandomPitch ? GetRandomPitch() : 1f;
        source.spatialBlend = 0f;
        source.gameObject.SetActive(true);
        source.Play();

        UpdateLastPlayTime(clip);
        StartCoroutine(ReturnToPoolAfterPlay(source, clip.length / source.pitch));
    }

    private void PlaySFXInternal(AudioClip clip, Vector3 position, float volume, bool useRandomPitch)
    {
        if (clip == null)
        {
            return;
        }

        if (!CanPlaySound(clip))
        {
            return;
        }

        AudioSource source = GetFromPool();
        if (source == null)
        {
            return;
        }

        source.transform.position = position;
        source.spatialBlend = 1.0f;
        source.clip = clip;
        source.volume = volume;
        source.pitch = useRandomPitch ? GetRandomPitch() : 1f;
        source.gameObject.SetActive(true);
        source.Play();

        Debug.LogWarning($"[SoundManager] SFX 클립: {clip.name}");
        
        UpdateLastPlayTime(clip);
        StartCoroutine(ReturnToPoolAfterPlay(source, clip.length / source.pitch));
    }

    private bool CanPlaySound(AudioClip clip)
    {
        if (!_lastPlayTimes.TryGetValue(clip, out float lastTime))
        {
            return true;
        }

        return Time.unscaledTime - lastTime >= _throttleInterval;
    }

    private void UpdateLastPlayTime(AudioClip clip)
    {
        _lastPlayTimes[clip] = Time.unscaledTime;
    }

    private float GetRandomPitch()
    {
        return Random.Range(_minPitch, _maxPitch);
    }

    private AudioSource GetFromPool()
    {
        if (_sfxPool.Count > 0)
        {
            return _sfxPool.Dequeue();
        }

        CreatePooledAudioSource();
        return _sfxPool.Dequeue();
    }

    private IEnumerator ReturnToPoolAfterPlay(AudioSource source, float duration)
    {
        yield return new WaitForSeconds(duration + 0.1f);

        source.Stop();
        source.clip = null;
        source.spatialBlend = 0f;
        source.pitch = 1f;
        source.gameObject.SetActive(false);
        _sfxPool.Enqueue(source);
    }

    public void PlayBGM(BgmId bgmId)
    {
        string key = bgmId.ToKey();
        AudioClip clip = _soundLibrary?.GetBgmClip(key);

        if (clip == null)
        {
            Debug.LogWarning($"[SoundManager] BGM 클립을 찾을 수 없습니다: {bgmId}");
            return;
        }

        BgmData data = _repository.GetBgmData(key);
        float volume = data?.Volume ?? 1f;

        PlayBGMInternal(clip, volume);
    }

    public void PlayBGM(AudioClip clip)
    {
        PlayBGMInternal(clip, 1f);
    }

    private void PlayBGMInternal(AudioClip clip, float targetVolume)
    {
        if (clip == null)
        {
            return;
        }

        AudioSource currentSource = _isUsingSourceA ? _bgmSourceA : _bgmSourceB;
        AudioSource nextSource = _isUsingSourceA ? _bgmSourceB : _bgmSourceA;

        if (currentSource.clip == clip && currentSource.isPlaying)
        {
            return;
        }

        if (_crossFadeCoroutine != null)
        {
            StopCoroutine(_crossFadeCoroutine);
        }

        _crossFadeCoroutine = StartCoroutine(CrossFadeCoroutine(currentSource, nextSource, clip, targetVolume));
        _isUsingSourceA = !_isUsingSourceA;
    }

    private IEnumerator CrossFadeCoroutine(AudioSource fadeOut, AudioSource fadeIn, AudioClip newClip, float targetVolume)
    {
        fadeIn.clip = newClip;
        fadeIn.volume = 0f;
        fadeIn.Play();

        float elapsed = 0f;
        float startVolumeOut = fadeOut.volume;

        while (elapsed < _crossFadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = elapsed / _crossFadeDuration;

            fadeOut.volume = Mathf.Lerp(startVolumeOut, 0f, progress);
            fadeIn.volume = Mathf.Lerp(0f, targetVolume, progress);

            yield return null;
        }

        fadeOut.Stop();
        fadeOut.volume = 0f;
        fadeIn.volume = targetVolume;

        _crossFadeCoroutine = null;
    }

    public void StopBGM()
    {
        if (_crossFadeCoroutine != null)
        {
            StopCoroutine(_crossFadeCoroutine);
            _crossFadeCoroutine = null;
        }

        StartCoroutine(FadeOutBGM());
    }

    private IEnumerator FadeOutBGM()
    {
        AudioSource currentSource = _isUsingSourceA ? _bgmSourceA : _bgmSourceB;

        float elapsed = 0f;
        float startVolume = currentSource.volume;

        while (elapsed < _crossFadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            currentSource.volume = Mathf.Lerp(startVolume, 0f, elapsed / _crossFadeDuration);
            yield return null;
        }

        currentSource.Stop();
        currentSource.volume = 0f;
    }

    public void SetMasterVolume(float normalizedVolume)
    {
        SetMixerVolume(MasterVolumeParam, normalizedVolume);
    }

    public void SetBGMVolume(float normalizedVolume)
    {
        SetMixerVolume(BgmVolumeParam, normalizedVolume);
    }

    public void SetSFXVolume(float normalizedVolume)
    {
        SetMixerVolume(SfxVolumeParam, normalizedVolume);
    }

    private void SetMixerVolume(string parameter, float normalizedVolume)
    {
        if (_audioMixer == null)
        {
            return;
        }

        float decibels = normalizedVolume > 0.0001f
            ? Mathf.Log10(normalizedVolume) * 20f
            : -80f;

        _audioMixer.SetFloat(parameter, decibels);
    }

    public float GetMasterVolume()
    {
        return GetMixerVolume(MasterVolumeParam);
    }

    public float GetBGMVolume()
    {
        return GetMixerVolume(BgmVolumeParam);
    }

    public float GetSFXVolume()
    {
        return GetMixerVolume(SfxVolumeParam);
    }

    private float GetMixerVolume(string parameter)
    {
        if (_audioMixer == null)
        {
            return 1f;
        }

        if (_audioMixer.GetFloat(parameter, out float decibels))
        {
            return Mathf.Pow(10f, decibels / 20f);
        }

        return 1f;
    }

    public void SaveVolumeSettings()
    {
        PlayerPrefs.SetFloat(MasterVolumeKey, GetMasterVolume());
        PlayerPrefs.SetFloat(BgmVolumeKey, GetBGMVolume());
        PlayerPrefs.SetFloat(SfxVolumeKey, GetSFXVolume());
        PlayerPrefs.Save();
    }

    public void LoadVolumeSettings()
    {
        if (PlayerPrefs.HasKey(MasterVolumeKey))
        {
            SetMasterVolume(PlayerPrefs.GetFloat(MasterVolumeKey));
        }

        if (PlayerPrefs.HasKey(BgmVolumeKey))
        {
            SetBGMVolume(PlayerPrefs.GetFloat(BgmVolumeKey));
        }

        if (PlayerPrefs.HasKey(SfxVolumeKey))
        {
            SetSFXVolume(PlayerPrefs.GetFloat(SfxVolumeKey));
        }
    }
}
