using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Spawner : MonoBehaviour
{
    public bool developerMode;

    public Wave[] waves;
    public Enemy enemy; // Let's have only one enemy type

    Player player;
    Transform playerT;

    Wave currentWave;
    int currentWaveNumber;

    int enemiesRemainingToSpawn;
    int enemiesRemainingAlive;
    float nextSpawnTime;

    MapGenerator map;

    float timeBetweenCampingChecks = 2.5f;
    float campThresholdDistance = 1.5f;
    float nextCampCheckTime;
    Vector3 campPositionOld;
    bool isCamping;

    bool isDisabled;

    public event System.Action<int> OnNewWave;

    void Start()
    {
        player = FindObjectOfType<Player>();
        playerT = player.transform;

        nextCampCheckTime = timeBetweenCampingChecks + Time.time;
        campPositionOld = playerT.position;
        player.OnDeath += OnPlayerDeath;

        map = FindObjectOfType<MapGenerator>();
        NextWave();
    }

    void Update()
    {
        if (isDisabled) return;

        if (Time.time > nextCampCheckTime)
        {
            nextCampCheckTime = Time.time + timeBetweenCampingChecks;
            isCamping = (Vector3.Distance(playerT.position, campPositionOld) < campThresholdDistance);
            campPositionOld = playerT.position;
        }

        if ((enemiesRemainingToSpawn > 0 || currentWave.infinite) && Time.time > nextSpawnTime)
        {
            enemiesRemainingToSpawn--;
            nextSpawnTime = Time.time + currentWave.timeBetweenSpawns;

            StartCoroutine(nameof(spawnEnemy));
        }

        if (developerMode)
        {
            if (Input.GetKeyDown(KeyCode.Return))
            {
                StopCoroutine(nameof(spawnEnemy));
                foreach(Enemy enemy in FindObjectsOfType<Enemy>())
                {
                    GameObject.Destroy(enemy.gameObject);
                }
                NextWave();
            }
        }

    }

    IEnumerator spawnEnemy()
    {
        float spawnDelay = 1; // How long the tile will flash
        float tileFlashSpeed = 4;

        Transform spawnTile = map.GetRandomOpenTile();
        if (isCamping) spawnTile = map.GetTileFromPosition(playerT.position);
        Material tileMat = spawnTile.GetComponent<Renderer>().material;
        Color initialColor = Color.white; // This could be bad if we want the color to change. Maybe set a global material?
        Color flashColor = Color.red;
        float spawnTimer = 0;

        while (spawnTimer < spawnDelay)
        {
            tileMat.color = Color.Lerp(initialColor, flashColor, Mathf.PingPong(spawnTimer * tileFlashSpeed, 1)); // PingPong will bounce the value back and forth when incrementing it

            spawnTimer += Time.deltaTime;
            yield return null;
        }
        tileMat.color = initialColor;

        Enemy spawnedEnemy = Instantiate(enemy, spawnTile.position + Vector3.up, Quaternion.identity);
        spawnedEnemy.OnDeath += OnEnemyDeath;
        spawnedEnemy.SetCharacteristics(currentWave.moveSpeed, currentWave.hitToKillPlayer, currentWave.enemyHealth, currentWave.skinColor);
    }

    void OnPlayerDeath()
    {
        isDisabled = true;
    }

    void OnEnemyDeath()
    {
        enemiesRemainingAlive--;

        if (enemiesRemainingAlive == 0)
        {
            NextWave();
        }
    }

    void ResetPlayerPosition()
    {
        playerT.position = map.GetTileFromPosition(Vector3.zero).position + Vector3.up * 3;
    }

    void NextWave()
    {
        if (currentWaveNumber > 0) AudioManager.instance.PlaySound2D("Level Completed");
        currentWaveNumber++;

        if (currentWaveNumber - 1 < waves.Length)
        {
            currentWave = waves[currentWaveNumber - 1];

            enemiesRemainingToSpawn = currentWave.enemyCount;
            enemiesRemainingAlive = enemiesRemainingToSpawn;
        }

        nextSpawnTime = Time.time + 0.5f;
        nextCampCheckTime = Time.time + nextCampCheckTime;

        OnNewWave?.Invoke(currentWaveNumber);
        ResetPlayerPosition();
    }


    /// <summary>
    /// Wave includes info for next enemies, spawnrates and such
    /// </summary>
    [System.Serializable] // This class can be set in the inspector
    public class Wave // Maybe this should be a struct?
    {
        public bool infinite;

        public int enemyCount;
        public float timeBetweenSpawns;

        public float moveSpeed;
        public int hitToKillPlayer; //Damage
        public float enemyHealth;
        public Color skinColor;
    }
}
