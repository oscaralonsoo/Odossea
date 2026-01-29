using UnityEngine;
using UnityEngine.UI;

public class MainMenuManager : MonoBehaviour
{
    // PANELS
    public UIPanel mainPanel;
    public UIPanel optionsPanel;
    public UIPanel soundPanel;
    public UIPanel graphicsPanel;
    UIPanel current;

    [Header("Audio Settings")]
    [SerializeField] Slider musicSlider;
    [SerializeField] Slider sfxSlider;
    float pendingMusicVol, pendingSFXVol;

    [Header("Graphics Settings")]
    [SerializeField] Slider brightnessSlider;
    [SerializeField] Toggle fullscreenToggle;
    [SerializeField] CanvasGroup brightnessOverlay;
    float pendingBrightness;
    bool pendingFullscreen;

    [Header("Defaults")]
    [SerializeField] float defaultMusic = 1f;
    [SerializeField] float defaultSFX = 1f;
    [SerializeField] float defaultBrightness = 1f;
    void Start()
    {
        mainPanel.Show();
        current = mainPanel;

        LoadSettings();
    }

    void LoadSettings()
    {
        pendingMusicVol = PlayerPrefs.GetFloat("MusicVol", 1f);
        pendingSFXVol = PlayerPrefs.GetFloat("SFXVol", 1f);
        musicSlider.value = pendingMusicVol;
        sfxSlider.value = pendingSFXVol;

        pendingFullscreen = PlayerPrefs.GetInt("Fullscreen", 1) == 1;
        pendingBrightness = PlayerPrefs.GetFloat("Brightness", 1f);
        fullscreenToggle.isOn = pendingFullscreen;
        brightnessSlider.value = pendingBrightness;

        ApplyAudio();
        ApplyGraphics();
    }

    public void OnMusicSlider(float val) => pendingMusicVol = val;
    public void OnSFXSlider(float val) => pendingSFXVol = val;

    public void ApplyAudio()
    {
        PlayerPrefs.SetFloat("MusicVol", pendingMusicVol);
        PlayerPrefs.SetFloat("SFXVol", pendingSFXVol);

        if (MusicManager.Instance != null)
            MusicManager.Instance.SetMusicVolume(pendingMusicVol);

        SFXManager.SetSFXVolume(pendingSFXVol);
    }

    public void OnBrightnessSlider(float val)
    {
        pendingBrightness = val;
        UpdateBrightnessOverlay(val); 
    }

    public void OnFullscreenToggle(bool val) => pendingFullscreen = val;

    public void ApplyGraphics()
    {
        PlayerPrefs.SetFloat("Brightness", pendingBrightness);
        PlayerPrefs.SetInt("Fullscreen", pendingFullscreen ? 1 : 0);
        PlayerPrefs.Save();

        Screen.fullScreen = pendingFullscreen;
        UpdateBrightnessOverlay(pendingBrightness);
    }

    private void UpdateBrightnessOverlay(float value)
    {
        if (brightnessOverlay != null)
        {
            float targetAlpha = 1f - value;
            brightnessOverlay.alpha = Mathf.Clamp(targetAlpha, 0f, 0.8f);
        }
    }
    public void ResetSoundSettings()
    {
        pendingMusicVol = defaultMusic;
        pendingSFXVol = defaultSFX;

        musicSlider.value = defaultMusic;
        sfxSlider.value = defaultSFX;

        ApplyAudio();

        Debug.Log("Sonido reseteado a valores de fábrica.");
    }

    public void ResetGraphicsSettings()
    {
        pendingFullscreen = true;
        pendingBrightness = defaultBrightness;

        fullscreenToggle.isOn = true;
        brightnessSlider.value = defaultBrightness;

        ApplyGraphics();

        Debug.Log("Gráficos reseteados a valores de fábrica.");
    }
    public void Play(string scene) => SceneTransitionManager.Instance.LoadScene(scene);
    public void Quit() => Application.Quit();

    public void ShowOptions() => SwitchPanel(optionsPanel);
    public void BackToMain() => SwitchPanel(mainPanel);
    public void ShowSound() => SwitchPanel(soundPanel);
    public void ShowGraphics() => SwitchPanel(graphicsPanel);
    public void BackToOptions() => SwitchPanel(optionsPanel);

    private void SwitchPanel(UIPanel next)
    {
        if (current != null) current.Hide();
        next.Show();
        current = next;
    }
}