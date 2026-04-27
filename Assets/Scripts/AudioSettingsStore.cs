using UnityEngine;

public static class AudioSettingsStore
{
    const string MusicPrefKey = "Music";
    const string SoundPrefKey = "Sound";

    public static bool IsMusicEnabled()
    {
        return PlayerPrefs.GetInt(MusicPrefKey, 1) == 1;
    }

    public static bool IsSoundEnabled()
    {
        return PlayerPrefs.GetInt(SoundPrefKey, 1) == 1;
    }

    public static void SetMusicEnabled(bool isOn)
    {
        PlayerPrefs.SetInt(MusicPrefKey, isOn ? 1 : 0);
        PlayerPrefs.Save();
    }

    public static void SetSoundEnabled(bool isOn)
    {
        PlayerPrefs.SetInt(SoundPrefKey, isOn ? 1 : 0);
        PlayerPrefs.Save();
    }
}
