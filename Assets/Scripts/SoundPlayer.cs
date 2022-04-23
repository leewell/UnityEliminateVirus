using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundPlayer : MonoBehaviour
{
    private AudioSource audioSource;
    public AudioClip[] clips;
    [Header("最低音调")]
    public float currentPitchMin = 1;
    [Header("最高音调")]
    public float currentPitchMax = 1;
    public AudioClip clipSource;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (clipSource)
        {
            audioSource.clip = clipSource;
        }
    }

    /// <summary>
    /// 播放音频
    /// </summary>
    public void PlayClip(AudioClip clip, float pitchMin = 1, float pitchMax = 1)
    {
        audioSource.pitch = Random.Range(pitchMin, pitchMax);
        audioSource.PlayOneShot(clip);
    }

    /// <summary>
    /// 播放随机音频
    /// </summary>
    public void PlayRandomSound()
    {
        audioSource.pitch = Random.Range(currentPitchMin, currentPitchMax);
        audioSource.PlayOneShot(clips[Random.Range(0, clips.Length)]);
    }

    /// <summary>
    /// 外部调用持续播放
    /// </summary>
    public void PlaySound(bool loop)
    {
        if (clipSource != null)
        {
            if (!audioSource.isPlaying)
            {
                audioSource.loop = loop;
                audioSource.Play();
            }
        }
    }
}