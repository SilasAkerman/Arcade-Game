using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public enum AudioChannel { Master, Sfx, Music }

    public float masterVolumePercent { get; private set; } = .5f;
    public float sfxVolumePercent { get; private set; } = 1;
    public float musicVolumePercent { get; private set; } = 1;

    AudioSource sfx2DSource;
    AudioSource[] musicSources;
    int activeMusicSourceIndex;

    public static AudioManager instance; // wonderful singleton that everybody can access

    Transform audioListener;
    Transform playerT;

    SoundLibrary library;

    void Awake()
    {
        if (instance != null) // This instance is a duplicate, happens when returning to the game scene
        {
            Destroy(gameObject);
            return;
        }

        instance = this; // initializing the singleton
        DontDestroyOnLoad(gameObject);

        library = GetComponent<SoundLibrary>();

        musicSources = new AudioSource[2];
        for (int i = 0; i < musicSources.Length; i++)
        {
            GameObject newMusicSource = new GameObject("Music source " + (i + 1));
            musicSources[i] = newMusicSource.AddComponent<AudioSource>();
            //musicSources[i].loop = true;
            newMusicSource.transform.parent = transform; // Remember this for organising
        }
        GameObject newSfx2DSource = new GameObject("2D sfx source");
        sfx2DSource = newSfx2DSource.AddComponent<AudioSource>();
        newSfx2DSource.transform.parent = transform;

        audioListener = FindObjectOfType<AudioListener>().transform;
        if (FindObjectOfType<Player>() != null) playerT = FindObjectOfType<Player>().transform;

        masterVolumePercent = PlayerPrefs.GetFloat("master vol", masterVolumePercent);
        sfxVolumePercent = PlayerPrefs.GetFloat("sfx vol", sfxVolumePercent);
        musicVolumePercent = PlayerPrefs.GetFloat("music vol", musicVolumePercent);
    }

    void Update()
    {
        if (playerT == null) Invoke(nameof(TryToFindPlayer), 5);
        else audioListener.position = playerT.position;
    }

    void TryToFindPlayer()
    {
        if (FindObjectOfType<Player>() != null) playerT = FindObjectOfType<Player>().transform;
    }

    public void SetVolume(float volumePercent, AudioChannel channel)
    {
        switch (channel)
        {
            case AudioChannel.Master:
                masterVolumePercent = volumePercent;
                break;

            case AudioChannel.Sfx:
                sfxVolumePercent = volumePercent;
                break;

            case AudioChannel.Music:
                musicVolumePercent = volumePercent;
                break;
        }

        musicSources[0].volume = musicVolumePercent * masterVolumePercent;
        musicSources[1].volume = musicVolumePercent * masterVolumePercent;

        PlayerPrefs.SetFloat("master vol", masterVolumePercent);
        PlayerPrefs.SetFloat("sfx vol", musicVolumePercent);
        PlayerPrefs.SetFloat("music vol", sfxVolumePercent);
        PlayerPrefs.Save();
    }

    public void PlayMusic(AudioClip clip, float fadeDuration = 1)
    {
        activeMusicSourceIndex = 1 - activeMusicSourceIndex; // goes 0 -> 1 -> 0 -> 1 ... necause only two music sources
        musicSources[activeMusicSourceIndex].clip = clip;
        musicSources[activeMusicSourceIndex].Play();

        StartCoroutine(AnimateMusicCrossfade(fadeDuration));
    }

    public void PlaySound(AudioClip clip, Vector3 pos)
    {
        if (clip is null) return;
        AudioSource.PlayClipAtPoint(clip, pos, sfxVolumePercent * masterVolumePercent);
    }

    public void PlaySound(string soundName, Vector3 pos)
    {
        PlaySound(library.GetClipFromName(soundName), pos);
    }

    public void PlaySound2D(string soundName)
    {
        sfx2DSource.PlayOneShot(library.GetClipFromName(soundName), sfxVolumePercent * masterVolumePercent);
    }

    IEnumerator AnimateMusicCrossfade(float duration)
    {
        float percent = 0;

        while(percent < 1)
        {
            percent += Time.deltaTime * (1 / duration);
            musicSources[activeMusicSourceIndex].volume = Mathf.Lerp(0, musicVolumePercent * masterVolumePercent, percent);
            musicSources[1 - activeMusicSourceIndex].volume = Mathf.Lerp(musicVolumePercent * masterVolumePercent, 0, percent);
            yield return null;
        }
    }
}
