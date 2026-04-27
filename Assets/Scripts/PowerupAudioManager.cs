using UnityEngine;

public class PowerupAudioManager : MonoBehaviour
{
    public static PowerupAudioManager Instance;

    [Header("Background Music")]
    public AudioClip backgroundMusic;
    [Range(0f, 1f)] public float musicVolume = 0.45f;
    public AudioSource musicSource;

    [Header("Powerup SFX")]
    public AudioClip spawnClip;
    [Range(0f, 1f)] public float spawnVolume = 0.9f;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
        EnsureMusicSource();
        ApplyAudioSettings();
    }

    void OnApplicationFocus(bool hasFocus)
    {
        if (hasFocus)
        {
            ApplyAudioSettings();
        }
    }

    void EnsureMusicSource()
    {
        if (musicSource == null)
        {
            musicSource = GetComponent<AudioSource>();
        }

        if (musicSource == null)
        {
            musicSource = gameObject.AddComponent<AudioSource>();
        }

        musicSource.playOnAwake = false;
        musicSource.loop = true;
        musicSource.spatialBlend = 0f;
        musicSource.volume = musicVolume;
        musicSource.clip = backgroundMusic;
    }

    public void ApplyAudioSettings()
    {
        ApplyMusic(AudioSettingsStore.IsMusicEnabled());
    }

    void ApplyMusic(bool isOn)
    {
        if (musicSource == null)
        {
            return;
        }

        musicSource.clip = backgroundMusic;
        musicSource.volume = musicVolume;
        musicSource.mute = !isOn || backgroundMusic == null;

        if (!isOn || backgroundMusic == null)
        {
            if (musicSource.isPlaying)
            {
                musicSource.Stop();
            }

            return;
        }

        if (!musicSource.isPlaying)
        {
            musicSource.Play();
        }
    }

    public void PlaySpawn(Vector3 position)
    {
        if (spawnClip == null || !AudioSettingsStore.IsSoundEnabled())
            return;

        GameObject soundObject = new GameObject("PowerupSpawnSfx");
        soundObject.transform.position = position;

        var source = soundObject.AddComponent<AudioSource>();
        source.clip = spawnClip;
        source.volume = spawnVolume;
        source.spatialBlend = 0f;
        source.playOnAwake = false;
        source.loop = false;
        source.Play();

        Destroy(soundObject, spawnClip.length + 0.1f);
    }
}
