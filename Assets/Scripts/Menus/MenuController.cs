using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class MenuController : MonoBehaviour
{
    [Header("Menu Animators")]
    [SerializeField] private Animator mainMenuAnimator;
    [SerializeField] private Animator optionsAnimator;

    private bool isTransitioning = false;

    // Constantes de estado para el Animator
    private const int IDLE = 0;
    private const int POPUP = 1;
    private const int POPDOWN = 2;

    [Header("Volume Setting")]
    [SerializeField] private TMP_Text volumeTextValue = null;
    [SerializeField] private Slider volumeSlider = null;
    [SerializeField] private float defaultVolume = 0.5f;

    [Header("Graphics Setting")]
    [SerializeField] private Slider brightnessSlider = null;
    [SerializeField] private TMP_Text brightnessTextValue = null;
    [SerializeField] private float defaultBrightness = 5.0f;

    [Space(10)]
    [SerializeField] private TMP_Dropdown qualityDropdown = null;
    [SerializeField] private Toggle fullScreenToggle = null;

    private int qualityLevel;
    private bool isFullScreen;
    private float brightnessLevel;

    [Header("Levels To Load")]
    public string newGameLevel;
    public string levelToLoad;

    [Header("Resolution DropDown")]
    public TMP_Dropdown resolutionDropdown;
    private Resolution[] resolutions;

    [Header("UI Elements")]
    [SerializeField] private Button loadGameButton = null;
    private bool lastTransitioningState = false;

    private void Start()
    {
        SetupResolution();
        SetupLoadButton();
    }

    private void Update()
    {
        // Solo permitimos nuevas acciones si ambos animadores están en IDLE (0)
        if (isTransitioning)
        {
            if (mainMenuAnimator.GetInteger("State") == IDLE &&
                optionsAnimator.GetInteger("State") == IDLE)
            {
                isTransitioning = false;
            }
        }
    }

    private void SetupResolution()
    {
        resolutions = Screen.resolutions;
        resolutionDropdown.ClearOptions();
        List<string> options = new List<string>();
        int currentResolutionIndex = 0;

        for (int i = 0; i < resolutions.Length; i++)
        {
            string option = resolutions[i].width + " x " + resolutions[i].height;
            options.Add(option);

            if (resolutions[i].width == Screen.currentResolution.width &&
                resolutions[i].height == Screen.currentResolution.height)
            {
                currentResolutionIndex = i;
            }
        }

        resolutionDropdown.AddOptions(options);
        resolutionDropdown.value = currentResolutionIndex;
        resolutionDropdown.RefreshShownValue();
    }

    private void SetupLoadButton()
    {
        if (loadGameButton != null)
        {
            if (string.IsNullOrEmpty(levelToLoad) && PlayerPrefs.HasKey("SavedLevel"))
                levelToLoad = PlayerPrefs.GetString("SavedLevel");

            loadGameButton.interactable = !string.IsNullOrEmpty(levelToLoad);
        }
    }

    // ================= MENU FLOW =================

    public void OpenOptions()
    {
        if (isTransitioning) return;
        isTransitioning = true;

        optionsAnimator.SetInteger("State", POPUP);
        mainMenuAnimator.SetInteger("State", POPDOWN);
    }

    public void CloseOptions()
    {
        if (isTransitioning) return;
        isTransitioning = true;

        optionsAnimator.SetInteger("State", POPDOWN);
        mainMenuAnimator.SetInteger("State", POPUP);
    }

    // ================= ANIMATION EVENTS =================
    // Estos metodos deben llamarse al FINAL de cada clip de animación en Unity

    public void OnOptionsPopupFinished() => optionsAnimator.SetInteger("State", IDLE);

    public void OnOptionsPopdownFinished() => optionsAnimator.SetInteger("State", IDLE);

    public void OnMainMenuPopdownFinished() => mainMenuAnimator.SetInteger("State", IDLE);

    public void OnMainMenuPopupFinished() => mainMenuAnimator.SetInteger("State", IDLE);

    // ================= ACCIONES =================

    public void NewGameYes() => SceneTransitionManager.Instance.LoadScene(newGameLevel);

    public void LoadGameYes()
    {
        if (!string.IsNullOrEmpty(levelToLoad))
            SceneTransitionManager.Instance.LoadScene(levelToLoad);
    }

    public void ExitButton() => Application.Quit();

    // ================= AJUSTES =================

    public void SetVolume(float volume)
    {
        AudioListener.volume = volume;
        volumeTextValue.text = volume.ToString("0.0");
    }

    public void VolumeApply() => PlayerPrefs.SetFloat("masterVolume", AudioListener.volume);

    public void SetBrightness(float brightness)
    {
        brightnessLevel = brightness;
        brightnessTextValue.text = brightness.ToString("0.0");
    }

    public void SetFullScreen(bool fullscreen) => isFullScreen = fullscreen;

    public void SetQuality(int qualityIndex) => qualityLevel = qualityIndex;

    public void GraphicsApply()
    {
        PlayerPrefs.SetFloat("masterBrightness", brightnessLevel);
        PlayerPrefs.SetInt("masterQuality", qualityLevel);
        QualitySettings.SetQualityLevel(qualityLevel);
        PlayerPrefs.SetInt("masterFullScreen", isFullScreen ? 1 : 0);
        Screen.fullScreen = isFullScreen;
    }

    public void SetResolution(int resolutionIndex)
    {
        Resolution resolution = resolutions[resolutionIndex];
        Screen.SetResolution(resolution.width, resolution.height, Screen.fullScreen);
    }

    public void ResetButton(string MenuType)
    {
        if (MenuType == "Audio")
        {
            AudioListener.volume = defaultVolume;
            volumeSlider.value = defaultVolume;
            volumeTextValue.text = defaultVolume.ToString("0.0");
            VolumeApply();
        }
        else if (MenuType == "Graphics")
        {
            brightnessSlider.value = defaultBrightness;
            brightnessTextValue.text = defaultBrightness.ToString("0.0");
            qualityDropdown.value = 1;
            QualitySettings.SetQualityLevel(1);
            fullScreenToggle.isOn = false;
            Screen.fullScreen = false;
            GraphicsApply();
        }
    }
}