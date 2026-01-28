using UnityEngine;

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

    void Start()
    {
        overlayPanel.SetActive(false);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (current == soundPanel || current == graphicsPanel || current == customizationPanel)
                ShowOptions();
            else if (current == optionsPanel)
                ShowPause();
            else if (current == pausePanel)
                Resume();
            else
                Pause();
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
        Time.timeScale = 1;
        pausePanel.Hide();
        overlayPanel.SetActive(false);
        current = null;
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
}
