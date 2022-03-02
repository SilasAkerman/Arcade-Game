using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapGenerator : MonoBehaviour
{
    public Map[] maps;
    public int mapIndex;

    public Transform tilePrefab;
    public Transform obstaclePrefab;
    public Transform mapFloor;
    public Transform navmeshFloor;
    public Transform navmeshMaskPrefab;

    public Vector2 maxMapSize;

    [Range(0, 1)]
    public float outlinePercent;

    public float tileSize;

    List<Coord> allTileCoords;
    Queue<Coord> shuffledTileCoords;
    Queue<Coord> shuffledOpenTileCoords;
    Transform[,] tileMap;

    Map currentMap;

    void Awake()
    {
        FindObjectOfType<Spawner>().OnNewWave += OnNewWay;
    }

    void OnNewWay(int waveNumber)
    {
        mapIndex = waveNumber - 1;
        GeneratMap();
    }

    public void GeneratMap()
    {
        currentMap = maps[mapIndex];
        tileMap = new Transform[currentMap.mapSize.x, currentMap.mapSize.y];
        System.Random prng = new System.Random(currentMap.seed);

        // Generating Coords
        allTileCoords = new List<Coord>();
        for (int x = 0; x < currentMap.mapSize.x; x++)
        {
            for (int y = 0; y < currentMap.mapSize.y; y++)
            {
                allTileCoords.Add(new Coord(x, y));
            }
        }
        shuffledTileCoords = new Queue<Coord>(Utility.ShuffleArray(allTileCoords.ToArray(), currentMap.seed));

        // Creating mapHold object
        string holderName = "Generated Map";
        if (transform.Find(holderName))
        {
            DestroyImmediate(transform.Find(holderName).gameObject);
        }
        Transform mapHolder = new GameObject(holderName).transform;
        mapHolder.parent = transform;


        // Spawning tiles
        for (int x = 0; x < currentMap.mapSize.x; x++){
            for (int y = 0; y < currentMap.mapSize.y; y++)
            {
                Vector3 tilePosition = CoordToPosition(x, y);
                Transform newTile = Instantiate(tilePrefab, tilePosition, Quaternion.Euler(Vector3.right * 90)); // Rotate 90 on x-axis, required this rotation overrides the prefab rotation due to setting position and rotation
                newTile.localScale = Vector3.one * (1 - outlinePercent) * tileSize;
                newTile.parent = mapHolder;
                tileMap[x, y] = newTile;
            }
        }


        // Spawning obstacles
        bool[,] obstacleMap = new bool[(int)currentMap.mapSize.x, (int)currentMap.mapSize.y];

        int obstacleCount = (int)(currentMap.mapSize.x * currentMap.mapSize.y * currentMap.obstaclePercent);
        int currentObstacleCount = 0;
        List<Coord> allOpenCoords = new List<Coord>(allTileCoords);

        for (int i = 0; i < obstacleCount; i++)
        {
            Coord randomCoord = GetRandomCoordinate();
            obstacleMap[randomCoord.x, randomCoord.y] = true;
            currentObstacleCount++;

            if (randomCoord != currentMap.mapCenter && MapIsFullyAccessable(obstacleMap, currentObstacleCount))
            {
                float obstacleHeight = Mathf.Lerp(currentMap.minObstacleHeight, currentMap.maxObstacleHeight, (float)prng.NextDouble());
                Vector3 obstaclePosition = CoordToPosition(randomCoord.x, randomCoord.y);

                Transform newObstacle = Instantiate(obstaclePrefab, obstaclePosition + Vector3.up * obstacleHeight / 2, Quaternion.identity);
                newObstacle.localScale = new Vector3((1 - outlinePercent) * tileSize, obstacleHeight, (1-outlinePercent) * tileSize);
                newObstacle.parent = mapHolder;

                Renderer obstacleRenderer = newObstacle.GetComponent<Renderer>();
                Material obstacleMaterial = new Material(obstacleRenderer.sharedMaterial);
                float colorPercent = randomCoord.y / (float)currentMap.mapSize.y;
                obstacleMaterial.color = Color.Lerp(currentMap.foregroundColor, currentMap.backgroundColor, colorPercent);
                obstacleRenderer.sharedMaterial = obstacleMaterial;

                allOpenCoords.Remove(randomCoord);
            }
            else
            {
                obstacleMap[randomCoord.x, randomCoord.y] = false;
                currentObstacleCount--;
            }
        }
        shuffledOpenTileCoords = new Queue<Coord>(Utility.ShuffleArray(allOpenCoords.ToArray(), currentMap.seed));

        // Creating the navmesh mask
        Transform maskLeft = Instantiate(navmeshMaskPrefab, Vector3.left * (currentMap.mapSize.x + maxMapSize.x) / 4f * tileSize, Quaternion.identity);
        maskLeft.parent = mapHolder;
        maskLeft.localScale = new Vector3((maxMapSize.x - currentMap.mapSize.x) / 2f, 1, maxMapSize.y) * tileSize;

        Transform maskRight = Instantiate(navmeshMaskPrefab, Vector3.right * (currentMap.mapSize.x + maxMapSize.x) / 4f * tileSize, Quaternion.identity);
        maskRight.parent = mapHolder;
        maskRight.localScale = new Vector3((maxMapSize.x - currentMap.mapSize.x) / 2f, 1, maxMapSize.y) * tileSize;

        Transform maskTop = Instantiate(navmeshMaskPrefab, Vector3.forward * (currentMap.mapSize.y + maxMapSize.y) / 4f * tileSize, Quaternion.identity);
        maskTop.parent = mapHolder;
        maskTop.localScale = new Vector3(currentMap.mapSize.x, 1, (maxMapSize.y - currentMap.mapSize.y) / 2f) * tileSize;

        Transform maskBottom = Instantiate(navmeshMaskPrefab, Vector3.back * (currentMap.mapSize.y + maxMapSize.y) / 4f * tileSize, Quaternion.identity);
        maskBottom.parent = mapHolder;
        maskBottom.localScale = new Vector3(currentMap.mapSize.x, 1, (maxMapSize.y - currentMap.mapSize.y) / 2f) * tileSize;

        navmeshFloor.localScale = new Vector3(maxMapSize.x, maxMapSize.y) * tileSize; // Due to rotation, the z-axis has become the y-axis
        mapFloor.localScale = new Vector3(currentMap.mapSize.x * tileSize, currentMap.mapSize.y * tileSize);
    }

    bool MapIsFullyAccessable(bool[,] obstacleMap, int currentObstacleCount)
    {
        bool[,] mapFlags = new bool[obstacleMap.GetLength(0), obstacleMap.GetLength(1)];
        Queue<Coord> queue = new Queue<Coord>();
        queue.Enqueue(currentMap.mapCenter);
        mapFlags[currentMap.mapCenter.x, currentMap.mapCenter.y] = true;

        int accessibleTileCount = 1;

        while (queue.Count > 0)
        {
            Coord tile = queue.Dequeue();

            for (int x = -1; x <= 1; x++)
            {
                for (int y = -1; y <= 1; y++)
                {
                    int neighbourX = tile.x + x;
                    int neighbourY = tile.y + y;
                    if (x == 0 || y == 0)
                    {
                        if (neighbourX >= 0 && neighbourX < obstacleMap.GetLength(0) && neighbourY >= 0 && neighbourY < obstacleMap.GetLength(1))
                        {
                            if (!mapFlags[neighbourX, neighbourY] && !obstacleMap[neighbourX, neighbourY])
                            {
                                mapFlags[neighbourX, neighbourY] = true;
                                queue.Enqueue(new Coord(neighbourX, neighbourY));
                                accessibleTileCount++;
                            }
                        }
                    }
                }
            }
        }

        int targetAccessibleTileCount = (int)(currentMap.mapSize.x * currentMap.mapSize.y - currentObstacleCount);
        return targetAccessibleTileCount == accessibleTileCount;
    }

    Vector3 CoordToPosition(int x, int y)
    {
        return new Vector3(-currentMap.mapSize.x / 2f + 0.5f + x, 0, -currentMap.mapSize.y / 2f + 0.5f + y) * tileSize; // Start at bottom left, slide so that bottom right corner is at the coordinate
    }

    public Transform GetTileFromPosition(Vector3 position)
    {
        int x = Mathf.RoundToInt(position.x / tileSize + (currentMap.mapSize.x - 1) / 2f);
        int y = Mathf.RoundToInt(position.z / tileSize + (currentMap.mapSize.y - 1) / 2f);
        x = Mathf.Clamp(x, 0, tileMap.GetLength(0) - 1);
        y = Mathf.Clamp(y, 0, tileMap.GetLength(1) - 1);

        return tileMap[x, y];
    }

    public Coord GetRandomCoordinate() // Returns from a shuffled list, but loops back on itself
    {
        Coord randomCoord = shuffledTileCoords.Dequeue(); // Gets the firt item of the array
        shuffledTileCoords.Enqueue(randomCoord); // Puts the item back in the last place of the queue
        return randomCoord;
    }

    public Transform GetRandomOpenTile()
    {
        Coord randomCoord = shuffledOpenTileCoords.Dequeue();
        shuffledOpenTileCoords.Enqueue(randomCoord);
        return tileMap[randomCoord.x, randomCoord.y];
    }

    [System.Serializable]
    public struct Coord
    {
        public int x;
        public int y;

        public Coord(int _x, int _y)
        {
            x = _x;
            y = _y;
        }

        public static bool operator == (Coord c1, Coord c2)
        {
            return c1.x == c2.x && c1.y == c2.y;
        }

        public static bool operator != (Coord c1, Coord c2)
        {
            return !(c1 == c2);
        }

        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }

    [System.Serializable]
    public class Map
    {
        public Coord mapSize;
        [Range(0, 1)]
        public float obstaclePercent;
        public int seed;

        public float minObstacleHeight;
        public float maxObstacleHeight;
        public Color foregroundColor;
        public Color backgroundColor;

        public Coord mapCenter { get { return new Coord(mapSize.x / 2, mapSize.y / 2); } }
    }
}
