using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScoreKeeper : MonoBehaviour
{
    public static int Score { get; private set; }
    float lastEneyKilledTime;
    int streakCount;
    float streakExpiryTime = 1;

    void Start()
    {
        Score = 0;
        Enemy.OnDeathStatic += OnEnemyKilled;
        FindObjectOfType<Player>().OnDeath += OnPlayerDeath;
    }

    void OnEnemyKilled()
    {
        if (Time.time < lastEneyKilledTime + streakExpiryTime)
        {
            streakCount++;
        }
        else
        {
            streakCount = 0;
        }

        lastEneyKilledTime = Time.time;

        Score += 5 + (int)Mathf.Pow(2, streakCount);
    }

    void OnPlayerDeath()
    {
        Enemy.OnDeathStatic -= OnEnemyKilled; // With static events, there is a possibility os subscribing twice. Maybe not fooldproof, but this works
    }

}
