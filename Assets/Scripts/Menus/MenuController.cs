using System.Collections;
using UnityEngine;
using UnityEngine.UI; 
using TMPro;
public class MenuController : MonoBehaviour
{

    [Header("Volume Setting")]
    [SerializeField] private TMP_Text volumeTextValue = null;
    [SerializeField] private Slider volumeSlider = null;
    [SerializeField] private float defaultVolume = 0.5f;

    [Header("Levels To Load")]
    public string newGameLevel;
    public string levelToLoad;

    [Header("UI Elements")]
    [SerializeField] private Button loadGameButton = null; 

    private void Start()
    {
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

    public void ResetButton(string MenuType)
    {
        if (MenuType == "Audio")
        {
            AudioListener.volume = defaultVolume;
            volumeSlider.value = defaultVolume;
            volumeTextValue.text = defaultVolume.ToString("0.0");
            VolumeApply();
        }
    }
}
