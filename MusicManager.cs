using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MusicManager : MonoBehaviour
{
    public AudioClip mainTheme;
    public AudioClip menuTheme;

    string currentSceneName;

    void Start()
    {
        SceneManager.sceneLoaded += OnLevelWasLoadedFinish;
        OnLevelWasLoadedFinish(SceneManager.GetActiveScene(), LoadSceneMode.Single);
    }

    void OnLevelWasLoadedFinish(Scene loadedScene, LoadSceneMode loadSceneMode)
    {
        string newSceneName = loadedScene.name;
        if (newSceneName != currentSceneName)
        {
            currentSceneName = newSceneName;
            Invoke(nameof(PlayMusic), .2f); // Because onLevelWasLoaded comes before the AudioManager duplicate get's destroyed, a little delay ensures no duplicate music is playing
        }
    }

    void PlayMusic()
    {
        AudioClip clipToPlay = null;

        if (currentSceneName == "Menu")
        {
            clipToPlay = menuTheme;
        }
        else if (currentSceneName == "Game")
        {
            clipToPlay = mainTheme;
        }

        if (clipToPlay != null)
        {
            AudioManager.instance.PlayMusic(clipToPlay, 2);
            Invoke(nameof(PlayMusic), clipToPlay.length); // might change this
        }
    }
}
