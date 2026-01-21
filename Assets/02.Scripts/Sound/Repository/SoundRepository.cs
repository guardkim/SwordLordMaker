using System.Collections.Generic;
using BansheeGz.BGDatabase;
using UnityEngine;

public class SoundRepository : ISoundRepository
{
    private const string SfxTableName = "SfxData";
    private const string BgmTableName = "BgmData";

    private const string VolumeField = "Volume";
    private const string UseRandomPitchField = "UseRandomPitch";

    private readonly BGMetaEntity _sfxMeta;
    private readonly BGMetaEntity _bgmMeta;

    private readonly Dictionary<string, SfxData> _sfxCache;
    private readonly Dictionary<string, BgmData> _bgmCache;

    public SoundRepository()
    {
        _sfxCache = new Dictionary<string, SfxData>();
        _bgmCache = new Dictionary<string, BgmData>();

        _sfxMeta = BGRepo.I[SfxTableName];
        _bgmMeta = BGRepo.I[BgmTableName];

        if (_sfxMeta == null)
        {
            Debug.LogWarning($"[SoundRepository] 테이블을 찾을 수 없습니다: {SfxTableName}");
        }

        if (_bgmMeta == null)
        {
            Debug.LogWarning($"[SoundRepository] 테이블을 찾을 수 없습니다: {BgmTableName}");
        }

        LoadAllSfxData();
        LoadAllBgmData();
    }

    public List<SfxData> LoadAllSfxData()
    {
        var result = new List<SfxData>();
        _sfxCache.Clear();

        if (_sfxMeta == null)
        {
            return result;
        }

        int count = _sfxMeta.CountEntities;
        for (int i = 0; i < count; i++)
        {
            BGEntity entity = _sfxMeta.GetEntity(i);
            SfxData data = CreateSfxDataFromEntity(entity);
            _sfxCache[data.Id] = data;
            result.Add(data);
        }

        return result;
    }

    public List<BgmData> LoadAllBgmData()
    {
        var result = new List<BgmData>();
        _bgmCache.Clear();

        if (_bgmMeta == null)
        {
            return result;
        }

        int count = _bgmMeta.CountEntities;
        for (int i = 0; i < count; i++)
        {
            BGEntity entity = _bgmMeta.GetEntity(i);
            BgmData data = CreateBgmDataFromEntity(entity);
            _bgmCache[data.Id] = data;
            result.Add(data);
        }

        return result;
    }

    public SfxData GetSfxData(string id)
    {
        if (_sfxCache.TryGetValue(id, out SfxData data))
        {
            return data;
        }

        return null;
    }

    public BgmData GetBgmData(string id)
    {
        if (_bgmCache.TryGetValue(id, out BgmData data))
        {
            return data;
        }

        return null;
    }

    private SfxData CreateSfxDataFromEntity(BGEntity entity)
    {
        return new SfxData(
            entity.Name,
            entity.Get<float>(VolumeField),
            entity.Get<bool>(UseRandomPitchField)
        );
    }

    private BgmData CreateBgmDataFromEntity(BGEntity entity)
    {
        return new BgmData(
            entity.Name,
            entity.Get<float>(VolumeField)
        );
    }
}
