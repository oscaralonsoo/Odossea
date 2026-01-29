using System;
using UnityEngine;

public enum SoundType
{
 
    SHOOT,
    SWITCH,
    JUMP,
    MOVE,
    BUTTON_PRESSED
}

[RequireComponent(typeof(AudioSource)), ExecuteInEditMode]
public class SFXManager : MonoBehaviour
{
    [SerializeField] private SoundList[] soundList; 
    private static SFXManager instance;
    private AudioSource audioSource;
    public static float MasterVolume = 1f;
    private void Awake()
    {
        instance = this;
        audioSource = GetComponent<AudioSource>();
    }

    private void Start()
    {
        audioSource = GetComponent<AudioSource>();
    }

    public static void PlaySound(SoundType sound, float volume = 1f)
    {
        if (instance == null) return;

        int index = (int)sound;
        if (index >= instance.soundList.Length)
        {
            Debug.LogWarning($"El sonido {sound} no existe en la lista del Inspector");
            return;
        }

        AudioClip[] clips = instance.soundList[index].Sounds;
        if (clips == null || clips.Length == 0)
        {
            Debug.LogWarning($"No hay clips asignados para: {sound}");
            return;
        }

        AudioClip randomClip = clips[UnityEngine.Random.Range(0, clips.Length)];
        instance.audioSource.PlayOneShot(randomClip, volume * MasterVolume);
    }

    public static void SetSFXVolume(float value)
    {
        MasterVolume = value;

        if (instance != null && instance.audioSource != null)
            instance.audioSource.volume = MasterVolume;
    }
    public static void PlaySpecificSound(SoundType sound, int index, float volume = 1f)
    {
        if (instance == null) return;

        AudioClip[] clips = instance.soundList[(int)sound].Sounds;

        // Verificamos que el índice exista para que no de error
        if (clips.Length > 0 && index < clips.Length)
        {
            instance.audioSource.PlayOneShot(clips[index], volume * MasterVolume);
        }
    }


#if UNITY_EDITOR
    private void OnEnable()
    {
        string[] names = Enum.GetNames(typeof(SoundType));
        Array.Resize<SoundList>(ref soundList, names.Length);

        for (int i = 0; i < soundList.Length; i++)
        {
            soundList[i].name = names[i];
        }
    }
#endif
}

[Serializable]
public struct SoundList
{
    [SerializeField] private AudioClip[] clips; 
    public AudioClip[] Sounds => clips;      
    [HideInInspector] public string name;   
}
