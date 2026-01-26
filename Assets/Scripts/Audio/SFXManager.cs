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
        // Multiple Sounds per Type & Random Selection
        AudioClip[] clips = instance.soundList[(int)sound].Sounds;
        AudioClip randomClip = clips[UnityEngine.Random.Range(0, clips.Length)];
        instance.audioSource.PlayOneShot(randomClip, volume);

        // Just One Sound per Type
        //switch (sound)
        //{
        //    //case SoundType.JUMP:
        //    //    instance.audioSource.PlayOneShot(instance.soundList[(int)sound].Sounds[0], volume);
        //    //    break;
        //    //case SoundType.SHOOT:
        //    //    instance.audioSource.PlayOneShot(instance.soundList[(int)sound].Sounds[0], volume);
        //    //    break;
        //    //case SoundType.DIE:
        //    //    instance.audioSource.PlayOneShot(instance.soundList[(int)sound].Sounds[0], volume);
        //    //    break;
        //    //case SoundType.ADD:
        //    //    instance.audioSource.PlayOneShot(instance.soundList[(int)sound].Sounds[0], volume);
        //    //    break;
        //    //case SoundType.MOVE:
        //    //    instance.audioSource.PlayOneShot(instance.soundList[(int)sound].Sounds[0], volume);
        //    //    break;
        //    //case SoundType.SHOOT_MISS:
        //    //    instance.audioSource.PlayOneShot(instance.soundList[(int)sound].Sounds[0], volume);
        //    //    break;
        //}
    }

#if UNITY_EDITOR
    private void OnEnable()
    {
        string[] names = Enum.GetNames(typeof(SoundType));
        Array.Resize<SoundList>(ref soundList, names.Length);

        // Asignamos el nombre de cada SoundList para identificarlo en el inspector
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
