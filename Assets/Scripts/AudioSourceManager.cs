using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioSourceManager : MonoBehaviour
{
    public static AudioSourceManager instance { get; private set; }
    public SoundPlayer soundPlayer;

    private void Awake()
    {
        instance = this;
    }

    private void Start()
    {
        PoolManager.Instance.InitPool(soundPlayer, 20);
    }

    public void PlaySound(AudioClip clip, float pitchMin = 1, float pitchMax = 1)
    {
        PoolManager.Instance.GetInstance<SoundPlayer>(soundPlayer).PlayClip(clip, pitchMin, pitchMax);
    }
}