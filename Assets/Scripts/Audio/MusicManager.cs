using UnityEngine;

public class MusicManager : MonoBehaviour
{
    public static MusicManager Instance;

    [SerializeField] private AudioSource musicSource;

    [Header("Music Clips")]
    [SerializeField] private AudioClip menuMusic;
    [SerializeField] private AudioClip pauseMusic;
    [SerializeField] private AudioClip gameMusic;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else Destroy(gameObject);
    }

    // Reproducir canciones
    public void PlayMenu() => PlayMusic(menuMusic);
    public void PlayPauseMusic() => PlayMusic(pauseMusic);
    public void PlayGameMusic() => PlayMusic(gameMusic);

    private void PlayMusic(AudioClip clip)
    {
        if (musicSource.clip == clip) return;
        musicSource.clip = clip;
        musicSource.Play();
    }

    // Pausar / Reanudar música
    public void PauseMusic() => musicSource.Pause();
    public void ResumeMusic() => musicSource.UnPause();

    // Cambiar volumen en tiempo real
    public void SetMusicVolume(float value)
    {
            musicSource.volume = value;
    }
}
