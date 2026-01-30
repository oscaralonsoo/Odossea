using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class SceneTransitionManager : MonoBehaviour
{
    public static SceneTransitionManager Instance;

    private Animator animator;
    private string sceneToLoad;

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject); 

        // Tomar referencia al Animator del mismo GameObject
        animator = GetComponent<Animator>();
        if (animator == null)
        {
            Debug.LogError("No se encontró un Animator en el mismo GameObject de SceneTransitionManager");
        }
    }

    public void LoadScene(string sceneName)
    {
        sceneToLoad = sceneName;
        animator.SetTrigger("FadeOut");
    }
    public void ReloadCurrentScene()
    {
        sceneToLoad = SceneManager.GetActiveScene().name;
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
        yield return null;

        // Dispara FadeIn
        animator.SetTrigger("FadeIn");
    }
}
