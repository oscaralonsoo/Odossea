using System;
using UnityEngine;

public enum SoundType
{
    // Orden segun soundList en el inspector
    JUMP,
    SHOOT,
    DIE,
    ADD,
    MOVE,
    SHOOT_MISS
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
    }

    private void Start()
    {
        audioSource = GetComponent<AudioSource>();
    }

    public static void PlaySound(SoundType sound, float volume = 1f)
    {
        if (instance == null) return;

        AudioClip[] clips = instance.soundList[(int)sound].Sounds;
        if (clips.Length == 0) return;

        AudioClip randomClip = clips[UnityEngine.Random.Range(0, clips.Length)];
        instance.audioSource.PlayOneShot(randomClip, volume * MasterVolume);
    }

    public static void SetSFXVolume(float value)
    {
        MasterVolume = value;

        if (instance != null && instance.audioSource != null)
            instance.audioSource.volume = MasterVolume;
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
