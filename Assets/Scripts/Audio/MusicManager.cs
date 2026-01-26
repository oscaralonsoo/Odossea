using UnityEngine;
using System.Collections;

public class MusicManager : MonoBehaviour
{
    public static MusicManager Instance;

    [SerializeField] AudioSource musicSource;
    [SerializeField] float fadeDuration = 1.5f;

    [Header("Music Clips")]
    [SerializeField] AudioClip menuMusic;
    [SerializeField] AudioClip levelMusic;
    [SerializeField] AudioClip pauseMusic;

    Coroutine fadeRoutine;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else Destroy(gameObject);
    }

    public void PlayMenu() => PlayMusic(menuMusic);
    public void PlayLevel() => PlayMusic(levelMusic);
    public void PlayPause() => PlayMusic(pauseMusic);

    void PlayMusic(AudioClip clip)
    {
        if (musicSource.clip == clip) return;

        if (fadeRoutine != null)
            StopCoroutine(fadeRoutine);

        fadeRoutine = StartCoroutine(FadeTo(clip));
    }

    IEnumerator FadeTo(AudioClip newClip)
    {
        // Fade Out
        float startVolume = musicSource.volume;

        while (musicSource.volume > 0)
        {
            musicSource.volume -= Time.deltaTime / fadeDuration;
            yield return null;
        }

        musicSource.Stop();
        musicSource.clip = newClip;
        musicSource.Play();

        // Fade In
        while (musicSource.volume < startVolume)
        {
            musicSource.volume += Time.deltaTime / fadeDuration;
            yield return null;
        }

        musicSource.volume = startVolume;
    }

    public void PauseMusic()
    {
        if (fadeRoutine != null)
            StopCoroutine(fadeRoutine);

        fadeRoutine = StartCoroutine(FadeOutPause());
    }

    IEnumerator FadeOutPause()
    {
        while (musicSource.volume > 0)
        {
            musicSource.volume -= Time.unscaledDeltaTime / fadeDuration;
            yield return null;
        }

        musicSource.Pause();
    }

    public void ResumeMusic()
    {
        musicSource.UnPause();

        if (fadeRoutine != null)
            StopCoroutine(fadeRoutine);

        fadeRoutine = StartCoroutine(FadeIn());
    }

    IEnumerator FadeIn()
    {
        while (musicSource.volume < 1f)
        {
            musicSource.volume += Time.unscaledDeltaTime / fadeDuration;
            yield return null;
        }

        musicSource.volume = 1f;
    }
}
