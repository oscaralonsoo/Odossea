using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LoadPrefs : MonoBehaviour
{
    [Header("General Setting")]
    [SerializeField] private bool canUse= false;
    [SerializeField] private MenuController menuController = null;

    [Header("Volume Setting")]
    [SerializeField] private TMP_Text volumeTextValue = null;
    [SerializeField] private Slider volumeSlider = null;

    [Header("Brightness Setting")]
    [SerializeField] private Slider brightnessSlider = null;
    [SerializeField] private TMP_Text brightnessTextValue = null;

    [Header("Quality Level Setting")]
    [SerializeField] private TMP_Dropdown qualityDropdown = null;

    [Header("Fullscreen Setting")]
    [SerializeField] private Toggle fullScreenToggle = null;

    private void Awake()
    {
        if(canUse)
        {
            if(PlayerPrefs.HasKey("masterVolume"))
            {
                float localVolume = PlayerPrefs.GetFloat("masterVolume");

                volumeTextValue.text = localVolume.ToString("0");
                volumeSlider.value = localVolume;
                AudioListener.volume = localVolume;

            }
            else
            {
                menuController.ResetButton("Audio");
            }
            if(PlayerPrefs.HasKey("masterQuality"))
            {
                int localQuality = PlayerPrefs.GetInt("masterQuality");
                qualityDropdown.value = localQuality;
                QualitySettings.SetQualityLevel(localQuality);
            }
            if (PlayerPrefs.HasKey("masterFullScreen"))
            {
                int localFullScreen = PlayerPrefs.GetInt("masterFullScreen");
                if (localFullScreen == 1)
                {
                    fullScreenToggle.isOn = true;
                    Screen.fullScreen = true;
                }
                else
                {
                    fullScreenToggle.isOn = false;
                    Screen.fullScreen = false;
                }
            }
            if (PlayerPrefs.HasKey("masterBrightness"))
            {
                float localBrightness = PlayerPrefs.GetFloat("masterBrightness");
                brightnessTextValue.text = localBrightness.ToString("0");
                brightnessSlider.value = localBrightness;
            }
        }
    }
}
