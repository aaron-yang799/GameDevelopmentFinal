using UnityEngine;

/// <summary>
/// Manages all game audio including sound effects and background music.
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("Audio Sources")]
    public AudioSource musicSource;
    public AudioSource sfxSource;

    [Header("Background Music")]
    public AudioClip backgroundMusic;

    [Header("Sound Effects")]
    public AudioClip pelletEatenSound;
    public AudioClip powerPelletEatenSound;
    public AudioClip ghostEatenSound;
    public AudioClip playerDeathSound;
    public AudioClip levelCompleteSound;
    public AudioClip gameOverSound;

    [Header("Volume Settings")]
    [Range(0f, 1f)]
    public float musicVolume = 0.5f;
    [Range(0f, 1f)]
    public float sfxVolume = 0.7f;

    void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    void Start()
    {
        // Set volumes
        if (musicSource != null)
        {
            musicSource.volume = musicVolume;
        }

        if (sfxSource != null)
        {
            sfxSource.volume = sfxVolume;
        }

        // Start background music
        PlayBackgroundMusic();
    }

    /// <summary>
    /// Plays background music on loop.
    /// </summary>
    public void PlayBackgroundMusic()
    {
        if (musicSource != null && backgroundMusic != null)
        {
            musicSource.clip = backgroundMusic;
            musicSource.loop = true;
            musicSource.Play();
        }
    }

    /// <summary>
    /// Stops background music.
    /// </summary>
    public void StopBackgroundMusic()
    {
        if (musicSource != null)
        {
            musicSource.Stop();
        }
    }

    /// <summary>
    /// Plays pellet eaten sound.
    /// </summary>
    public void PlayPelletEaten()
    {
        PlaySFX(pelletEatenSound);
    }

    /// <summary>
    /// Plays power pellet eaten sound.
    /// </summary>
    public void PlayPowerPelletEaten()
    {
        PlaySFX(powerPelletEatenSound);
    }

    /// <summary>
    /// Plays ghost eaten sound.
    /// </summary>
    public void PlayGhostEaten()
    {
        PlaySFX(ghostEatenSound);
    }

    /// <summary>
    /// Plays player death sound.
    /// </summary>
    public void PlayPlayerDeath()
    {
        PlaySFX(playerDeathSound);
    }

    /// <summary>
    /// Plays level complete sound.
    /// </summary>
    public void PlayLevelComplete()
    {
        PlaySFX(levelCompleteSound);
    }

    /// <summary>
    /// Plays game over sound.
    /// </summary>
    public void PlayGameOver()
    {
        PlaySFX(gameOverSound);
    }

    /// <summary>
    /// Plays a sound effect.
    /// </summary>
    void PlaySFX(AudioClip clip)
    {
        if (sfxSource != null && clip != null)
        {
            sfxSource.PlayOneShot(clip);
        }
    }

    /// <summary>
    /// Sets music volume.
    /// </summary>
    public void SetMusicVolume(float volume)
    {
        musicVolume = Mathf.Clamp01(volume);
        if (musicSource != null)
        {
            musicSource.volume = musicVolume;
        }
    }

    /// <summary>
    /// Sets sound effects volume.
    /// </summary>
    public void SetSFXVolume(float volume)
    {
        sfxVolume = Mathf.Clamp01(volume);
        if (sfxSource != null)
        {
            sfxSource.volume = sfxVolume;
        }
    }
}