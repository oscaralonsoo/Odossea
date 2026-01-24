using System.Collections;
using UnityEngine;
using UnityEngine.UI; 
using TMPro;
using System.Collections.Generic;
public class MenuController : MonoBehaviour
{

    [Header("Volume Setting")]
    [SerializeField] private TMP_Text volumeTextValue = null;
    [SerializeField] private Slider volumeSlider = null;
    [SerializeField] private float defaultVolume = 0.5f;

    [Header("Graphics Setting")]
    [SerializeField]private Slider brightnessSlider = null;
    [SerializeField] private TMP_Text brightnessTextValue = null;
    [SerializeField] private float defaultBrightness = 5.0f;

    [Space(10)]
    [SerializeField]private TMP_Dropdown qualityDropdown = null;
    [SerializeField] private Toggle fullScreenToggle = null;

    private int qualityLevel;
    private bool isFullScreen;
    private float brightnessLevel;

    [Header("Levels To Load")]
    public string newGameLevel;
    public string levelToLoad;

    [Header("Resolution DropDowm")]
    public TMP_Dropdown resolutionDropdown;
    private Resolution[] resolutions;

    [Header("UI Elements")]
    [SerializeField] private Button loadGameButton = null;


    private void Start()
    {
        resolutions = Screen.resolutions;
        resolutionDropdown.ClearOptions();

        List<string> options = new List<string>();

        int currtentResolutionIndex = 0;
        for (int i = 0; i < resolutions.Length; i++)
        {
            string option = resolutions[i].width + " x " + resolutions[i].height;
            options.Add(option);
            if (resolutions[i].width == Screen.currentResolution.width &&
                resolutions[i].height == Screen.currentResolution.height)
            {
                currtentResolutionIndex = i;
            }
        }
        resolutionDropdown.AddOptions(options);
        resolutionDropdown.value = currtentResolutionIndex;
        resolutionDropdown.RefreshShownValue();


        if (loadGameButton != null)
        {
          
            if (string.IsNullOrEmpty(levelToLoad) && PlayerPrefs.HasKey("SavedLevel"))
            {
                levelToLoad = PlayerPrefs.GetString("SavedLevel");
            }

            loadGameButton.interactable = !string.IsNullOrEmpty(levelToLoad);

            Debug.Log("levelToLoad: " + levelToLoad);
        }
    }

    public void SetResolution(int resolutionIndex)
    {
        Resolution resolution = resolutions[resolutionIndex];
        Screen.SetResolution(resolution.width, resolution.height, Screen.fullScreen);
    }

    public void NewGameYes()
    {
        SceneTransitionManager.Instance.LoadScene(newGameLevel);
    }

    public void LoadGameYes()
    {
        if (!string.IsNullOrEmpty(levelToLoad))
        {
            SceneTransitionManager.Instance.LoadScene(levelToLoad);
        }
        else
        {
            Debug.LogWarning("No level specified to load!");
        }
    }

    public void ExitButton()
    {
        Application.Quit();
    }

    public void SetVolume(float volume)
    {
        AudioListener.volume = volume;
        volumeTextValue.text = (volume * 1).ToString("0.0");
    }
    public void VolumeApply()
    {
        PlayerPrefs.SetFloat("masterVolume", AudioListener.volume);
        // Optional: Show Confirmation Prompt
    }

    public void SetBrightness(float brightness)
    {
        brightnessLevel = brightness;
        brightnessTextValue.text = brightness.ToString("0.0");
        // Implement brightness adjustment logic here
    }
    public void SetFullScreen(bool isFullscreen)
    {
        isFullScreen = isFullscreen;
    }

    public void SetQuality(int qualityIndex)
    {
        qualityLevel = qualityIndex;
    }

    public void GraphicsApply()
    {
        PlayerPrefs.SetFloat("masterBrightness", brightnessLevel);
        //Change brightness with post-processing or other methods

        PlayerPrefs.SetInt("masterQuality", qualityLevel);
        QualitySettings.SetQualityLevel(qualityLevel);

        PlayerPrefs.SetInt("masterFullScreen", (isFullScreen ? 1 : 0));
        Screen.fullScreen = isFullScreen;
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
        if (MenuType == "Graphics")
        {
            brightnessSlider.value = defaultBrightness;
            brightnessTextValue.text = defaultBrightness.ToString("0.0");

            qualityDropdown.value = 1;
            QualitySettings.SetQualityLevel(1);

            fullScreenToggle.isOn = false;
            Screen.fullScreen = false;

            Resolution currentResolution = Screen.currentResolution;
            Screen.SetResolution(currentResolution.width, currentResolution.height, Screen.fullScreen);
            resolutionDropdown.value = resolutions.Length;
            GraphicsApply();
        }
    }
}
