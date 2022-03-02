using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameUI : MonoBehaviour
{
    public Image fadePlane;
    public GameObject gameOverUI;

    public RectTransform newWaveBanner;
    public Text newWaveTitle;
    public Text newWaveEnemyCount;
    public Text scoreUI;
    public Text gameOverScoreUI;
    public RectTransform healthBar;

    Spawner spawner;
    Player player;

    void Start()
    {
        player = FindObjectOfType<Player>();
        player.OnDeath += OnGameOver;
    }

    void Awake()
    {
        spawner = FindObjectOfType<Spawner>();
        spawner.OnNewWave += OnNewWave;
    }

    void Update()
    {
        scoreUI.text = ScoreKeeper.Score.ToString("D6"); // Format of six digits
        float healthPercent = 0;
        if (player != null)
        {
            healthPercent = player.health / player.startingHealth;
        }
        healthBar.localScale = new Vector3(healthPercent, 1, 1);
    }

    void OnNewWave(int waveNumber)
    {
        string[] numbers = { "One", "Two", "Three", "Four", "Five" };
        newWaveTitle.text = "- Wave " + numbers[waveNumber - 1] + " -";
        string enemyCountString = spawner.waves[waveNumber - 1].infinite ? "Infinite" : spawner.waves[waveNumber - 1].enemyCount + "";
        newWaveEnemyCount.text = "Enemies: " + enemyCountString;

        StopCoroutine(nameof(AnimateNewWaveBanner));
        StartCoroutine(nameof(AnimateNewWaveBanner));
    }

    void OnGameOver()
    {
        StartCoroutine(Fade(Color.clear, new Color(0, 0, 0, 0.95f), 1));
        gameOverScoreUI.text = scoreUI.text;
        scoreUI.gameObject.SetActive(false);
        healthBar.transform.parent.gameObject.SetActive(false);
        Cursor.visible = true;
        gameOverUI.SetActive(true);
    }

    IEnumerator AnimateNewWaveBanner()
    {
        float delayTime = 1.5f;
        float speed = 2.5f;
        float animationPercent = 0;
        int dir = 1;

        while (animationPercent >= 0)
        {
            animationPercent += Time.deltaTime * speed * dir;

            if (animationPercent >= 1)
            {
                animationPercent = 1;
                yield return new WaitForSeconds(delayTime);
                dir = -1;
            }

            newWaveBanner.anchoredPosition = Vector2.up * Mathf.Lerp(-170, 45, animationPercent);

            yield return null;
        }
    }

    IEnumerator Fade(Color fromColor, Color toColor, float time)
    {
        float speed = 1 / time;
        float percent = 0;

        while (percent < 1)
        {
            percent += Time.deltaTime * speed;
            fadePlane.color = Color.Lerp(fromColor, toColor, percent);
            yield return null;
        }
    }

    // Input
    public void StartNewGame()
    {
        SceneManager.LoadScene("Game");
    }

    public void ReturnToMainMenu()
    {
        SceneManager.LoadScene("Menu");
    }
}
