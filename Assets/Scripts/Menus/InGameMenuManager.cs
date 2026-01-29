using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;


public class InGameMenuManager : MonoBehaviour
{ 

    // PANELS
    public UIPanel pausePanel;
    public UIPanel optionsPanel;
    public UIPanel soundPanel;
    public UIPanel graphicsPanel;
    public UIPanel customizationPanel;

    public GameObject overlayPanel;
    public PlayerCustomizer playerCustomizer;
    UIPanel current;

    //MUSIC
    float pendingMusicVolume = 1f;
    float pendingSFXVolume = 1f;
    [SerializeField] float defaultMusic = 1f;
    [SerializeField] float defaultSFX = 1f;

    [SerializeField] UnityEngine.UI.Slider musicSlider;
    [SerializeField] UnityEngine.UI.Slider sfxSlider;

    //GRAPHICS
    [SerializeField] UnityEngine.UI.Toggle fullscreenToggle;
    [SerializeField] UnityEngine.UI.Slider brightnessSlider;
    bool pendingFullscreen;
    float pendingBrightness = 1f;

    [SerializeField] float defaultBrightness = 1f;
    [SerializeField] CanvasGroup brightnessOverlay;

    [Header("Resume Button")]
    [SerializeField] Button resumeButton;
    void Start()
    {
        //AUDIO
        overlayPanel.SetActive(false);
        pendingMusicVolume = PlayerPrefs.GetFloat("MusicVol", 1f);
        pendingSFXVolume = PlayerPrefs.GetFloat("SFXVol", 1f);

        musicSlider.value = pendingMusicVolume;
        sfxSlider.value = pendingSFXVolume;

        MusicManager.Instance.SetMusicVolume(1);
        SFXManager.SetSFXVolume(1);

        // GRAPHICS
        pendingFullscreen = PlayerPrefs.GetInt("Fullscreen", 1) == 1;
        pendingBrightness = PlayerPrefs.GetFloat("Brightness", defaultBrightness);

        fullscreenToggle.isOn = pendingFullscreen;

        brightnessSlider.value = pendingBrightness;

        ApplyGraphicsSettings();
    }

    public bool TogglePause()
    {
            if(Time.timeScale == 0)
            {
                Resume();
                return false;
            }
            else
            {
                Pause();
                return true;
            }
    }
    // PAUSE
    public void Pause()
    {
        Time.timeScale = 0;
        overlayPanel.SetActive(true);
        pausePanel.Show();
        current = pausePanel;

    }

    public void Resume()
    {
        resumeButton.Select();
        Time.timeScale = 1;
        pausePanel.Hide();
        optionsPanel.Hide();
        soundPanel.Hide();
        customizationPanel.Hide();
        overlayPanel.SetActive(false);
    }

    // OPTIONS
    public void ShowOptions()
    {
        if (current != null)
            current.Hide();

        optionsPanel.Show();
        current = optionsPanel;
    }

    public void ShowPause()
    {
        if (current != null)
            current.Hide();

        pausePanel.Show();
        current = pausePanel;
    }

    // SOUND / GRAPHICS
    public void ShowSound()
    {
        if (current != null)
            current.Hide();

        soundPanel.Show();
        current = soundPanel;
    }

    public void ShowGraphics()
    {
        if (current != null)
            current.Hide();

        graphicsPanel.Show();
        current = graphicsPanel;
    }

    public void ShowCustomitzation()
    {
        if (current != null)
            current.Hide();

        customizationPanel.Show();
        current = customizationPanel;
    }

    // SCENES
    public void LoadMainMenu(string scene)
    {
        Time.timeScale = 1;
        overlayPanel.SetActive(false);
        SceneTransitionManager.Instance.LoadScene(scene);
    }
    public void SelectMaterial(int index)
    {
        if (playerCustomizer != null)
        {
            playerCustomizer.SetMaterial(index);
        }
    }
    public void ApplySoundSettings()
    {
        MusicManager.Instance.SetMusicVolume(pendingMusicVolume);
        SFXManager.SetSFXVolume(pendingSFXVolume);

        PlayerPrefs.SetFloat("MusicVol", pendingMusicVolume);
        PlayerPrefs.SetFloat("SFXVol", pendingSFXVolume);
    }
    public void ResetSoundSettings()
    {
        pendingMusicVolume = defaultMusic;
        pendingSFXVolume = defaultSFX;

        musicSlider.value = defaultMusic;
        sfxSlider.value = defaultSFX;

        MusicManager.Instance.SetMusicVolume(defaultMusic);
        SFXManager.SetSFXVolume(defaultSFX);

        PlayerPrefs.SetFloat("MusicVol", defaultMusic);
        PlayerPrefs.SetFloat("SFXVol", defaultSFX);
    }
    public void OnMusicSlider(float value)
    {
        pendingMusicVolume = value;
        MusicManager.Instance.SetMusicVolume(pendingMusicVolume); // Cambia volumen al instante
    }

    public void OnSFXSlider(float value)
    {
        pendingSFXVolume = value;   
        SFXManager.SetSFXVolume(pendingSFXVolume);
    }


    public void OnFullscreenToggle(bool value)
    {
        pendingFullscreen = value;
    }

    public void OnBrightnessSlider(float value)
    {
        pendingBrightness = value;
        UpdateBrightnessOverlay(value); // Usamos una función común
    }

    public void ApplyGraphicsSettings()
    {
        PlayerPrefs.SetFloat("Brightness", pendingBrightness);
        PlayerPrefs.SetInt("Fullscreen", pendingFullscreen ? 1 : 0);
        PlayerPrefs.Save();

        Screen.fullScreen = pendingFullscreen;
        UpdateBrightnessOverlay(pendingBrightness);

        Debug.Log("Gráficos Aplicados. Brillo: " + pendingBrightness);
    }

    // Creamos esta función para que el cálculo sea SIEMPRE el mismo
    private void UpdateBrightnessOverlay(float value)
    {
        if (brightnessOverlay != null)
        {
            float targetAlpha = 1f - value;
            brightnessOverlay.alpha = Mathf.Clamp(targetAlpha, 0f, 0.8f);
        }
    }


    public void ResetGraphicsSettings()
    {
        pendingFullscreen = true;
        pendingBrightness = defaultBrightness;

        fullscreenToggle.isOn = true;
        brightnessSlider.value = defaultBrightness;

        ApplyGraphicsSettings();
    }

}
