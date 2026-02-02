public class SfxData
{
    public string Id { get; private set; }
    public float Volume { get; private set; }
    public bool UseRandomPitch { get; private set; }

    public SfxData(string id, float volume, bool useRandomPitch)
    {
        Id = id;
        Volume = volume;
        UseRandomPitch = useRandomPitch;
    }
}

public class BgmData
{
    public string Id { get; private set; }
    public float Volume { get; private set; }

    public BgmData(string id, float volume)
    {
        Id = id;
        Volume = volume;
    }
}
