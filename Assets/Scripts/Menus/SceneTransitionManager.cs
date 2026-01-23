using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class SceneTransitionManager : MonoBehaviour
{
    public static SceneTransitionManager Instance;

    [SerializeField] private Animator animator; // Animator del SceneTransition
    private string sceneToLoad;

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject); // Esto hace que todo hijo persista
    }

    public void LoadScene(string sceneName)
    {
        sceneToLoad = sceneName;
        animator.SetTrigger("FadeOut");
    }

    // Evento Animation Event al final de FadeOut
    public void OnFadeComplete()
    {
        StartCoroutine(LoadSceneAndFadeIn());
    }

    private IEnumerator LoadSceneAndFadeIn()
    {
        // Carga la escena
        SceneManager.LoadScene(sceneToLoad);

        // Espera un frame para que Unity termine de destruir/crear
        yield return null;

        // Dispara FadeIn
        animator.SetTrigger("FadeIn");
    }
}
