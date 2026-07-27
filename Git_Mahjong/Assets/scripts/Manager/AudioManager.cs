using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    public AudioSource BGM;
    public AudioSource SceneSingleMusic;
    public AudioSource SceneLoopMusic;
    public AudioSource btnMusic;

    public List<AudioClip> audioClips;
    private Dictionary<string, AudioClip> sceneMusics;

    private float bgmVolume = 1f;
    private bool isMusic = true;
    private bool isSound = true;
    private void Awake()
    {
        Instance = this;
    }
    private void Start()
    {
        sceneMusics = new Dictionary<string, AudioClip>();
        foreach (var audioClip in audioClips)
        {
            sceneMusics.Add(audioClip.name, audioClip);
        }
    }

    public void PlayBGM(string name)
    {
        AudioClip audioClip = GetAudioClip(name);
        if (audioClip == null) return;
        BGM.clip = audioClip;
        BGM.Play();
    }
    public void StopBGM()
    {
        BGM.Stop();
    }

    public void PlayBtnMusic()
    {
       // btnMusic.Play();
    }

    public void PlaySceneLoopMusic(string name)
    {
        AudioClip audioClip = GetAudioClip(name);
        if (audioClip == null) return;
        SceneLoopMusic.clip = audioClip;
        SceneLoopMusic.Play();
    }
    public void StopSceneLoopMusic()
    {
        SceneLoopMusic.Stop();
    }
    public void PlaySceneSingleMusic(string name)
    {
        AudioClip audioClip = GetAudioClip(name);
        if (audioClip == null) return;
        SceneSingleMusic.clip = audioClip;
        SceneSingleMusic.Play();
    }

    private AudioClip GetAudioClip(string name)
    {
        AudioClip audioClip = null;
        if (sceneMusics.TryGetValue(name, out audioClip))
        {
            audioClip = sceneMusics[name];
        }
        return audioClip;
    }

    public void MusicState(bool isOpen)
    {
        isMusic = isOpen;
        BGM.volume = isOpen ? bgmVolume : 0f;
    }

    public void SoundState(bool isOpen)
    {
        isSound = isOpen;
        float volume = isOpen ? 1f : 0f;
        SceneSingleMusic.volume = volume;
        SceneLoopMusic.volume = volume;
        btnMusic.volume = volume;
    }
    
    public void SetAudioSource(AudioSource audioSource, string name)
    {
        if (!isSound) return;
        AudioClip audioClip = GetAudioClip(name);
        if (audioClip == null) return;
        audioSource.Stop();
        audioSource.clip = audioClip;
        audioSource.Play();
    }

    public void BGMRecover()
    {
        float vS = isMusic ? bgmVolume : 0f;
        BGM.DOFade(vS, 0.5f);
    }
    public void BGMLitter()
    {
        float vS = isMusic ? 0.3f : 0f;
        BGM.volume = vS;
    }
}
