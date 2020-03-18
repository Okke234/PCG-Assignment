using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Utils;

public enum TileType {
    Empty = 0,
    Player,
    Enemy,
    Wall,
    Door,
    Key,
    Dagger,
    End,
    Flooded //Temporary type used in flood fill
}

public class LevelGenerator : MonoBehaviour
{
    public GameObject[] tiles;
    private static int width = 128;
    private static int height = 128;
    private int minAvailableTiles = 1000;
    private TileType[,] grid = new TileType[height, width];
    private List<Vector2Int> tilesInReach = new List<Vector2Int>();
    private Vector2Int spawnpoint = new Vector2Int();

    protected void Start() {
        InitialBlockFill(grid, width, height);
    }

    private void InitialBlockFill(TileType[,] _grid, int width, int height) {
        for (int tileY = 0; tileY < height; tileY++) {
            for (int tileX = 0; tileX < width; tileX++) {
                int i = Random.Range(0, 100);
                if (i <= 65) {
                    _grid[tileY, tileX] = TileType.Wall;
                } else {
                    _grid[tileY, tileX] = TileType.Empty;
                }
            }
        }

        for (int i = 0; i < 7; i++) {
            _grid = EvolveTiles(_grid, width, height);
        }
        while (tilesInReach.Count < minAvailableTiles) {
            FloodFill(_grid, FindSpawnpoint(_grid, width, height));
        }

        grid = _grid;

        SpawnObjects();
    }

    private void SpawnObjects() {
        SpawnPlayer();
        SpawnEnd();
        SpawnDagger();
        SpawnDoor();
        SpawnEnemy();
        SpawnKey();
        CreateTilesFromArray(grid);
    }

    private Vector2Int FindSpawnpoint(TileType[,] grid, int width, int height) {
        int x = Random.Range(1, 127);
        int y = Random.Range(1, 127);
        if (grid[y, x] == TileType.Empty) {
            spawnpoint = new Vector2Int(x, y);
            Debug.Log("x: " + x + " y: " + y + " has become the spawnpoint");
        } else {
            Debug.Log("Could not spawn the player on x: " + x + " y: " + y);
            FindSpawnpoint(grid, width, height);
        }
        return spawnpoint;
    }

    private void FloodFill(TileType[,] grid, Vector2Int spawnTile) {
        int x = spawnTile.x;
        int y = spawnTile.y;
        Queue<Vector2Int> queue = new Queue<Vector2Int>();

        FillTile(grid, spawnTile, queue);

        while (queue.Any()) {

            Vector2Int newTile = queue.Dequeue();

            FillTile(grid, newTile + new Vector2Int(0, 1), queue);
            FillTile(grid, newTile + new Vector2Int(0, -1), queue);
            FillTile(grid, newTile + new Vector2Int(1, 0), queue);
            FillTile(grid, newTile + new Vector2Int(-1, 0), queue);
        }
    }

    private void FillTile(TileType[,] grid, Vector2Int newTile, Queue<Vector2Int> queue) {
        int x = newTile.x;
        int y = newTile.y;
        if (x <= 0 || x >= width - 1) return;
        if (y <= 0 || y >= height - 1) return;

        if (grid[y, x] == TileType.Wall)
            return;
        if (grid[y, x] != TileType.Flooded) {
            grid[y, x] = TileType.Flooded;
            //Debug.Log("x: " + x + " y: " + y + " has been flooded");
            tilesInReach.Add(new Vector2Int(x, y));
            queue.Enqueue(newTile);
        }
    }

    private TileType[,] EvolveTiles(TileType[,] grid, int width, int height) {
        TileType[,] output = new TileType[width, height];
        for (int tileY = 0; tileY < height; tileY++) {
            for (int tileX = 0; tileX < width; tileX++) {
                TileType outType = grid[tileY, tileX];
                if (grid[tileY, tileX] == TileType.Wall && GetNeighbours(grid, tileX, tileY) < 5) {
                    outType = TileType.Empty;
                } else if (grid[tileY, tileX] == TileType.Empty && GetNeighbours(grid, tileX, tileY) > 5) {
                    outType = TileType.Wall;
                }
                output[tileY, tileX] = outType;
            }
        }
        return output;
    }

    private int GetNeighbours(TileType[,] grid, int x, int y) {
        int wallNeighbours = 0;
        for (int i = -1; i <= 1; i++) {
            for (int j = -1; j <= 1; j++) {
                if (i != 0 || j != 0) {
                    int gx = x + i;
                    int gy = y + j;
                    if (gx >= 0 && gx < width && gy > 0 && gy < height) {
                        if (grid[gy, gx] == TileType.Wall) {
                            wallNeighbours++;
                        }
                    }
                }
            }
        }
        //Code I used before switching to the above
        //if (y < height - 1 && grid[y + 1, x] == TileType.Wall) wallNeighbours++;
        //if (y > 0 && grid[y - 1, x] == TileType.Wall) wallNeighbours++;
        //if (x < width - 1 && grid[y, x + 1] == TileType.Wall) wallNeighbours++;
        //if (x > 0 && grid[y, x - 1] == TileType.Wall) wallNeighbours++;
        return wallNeighbours;
    }

    //fill part of array with tiles
    private void FillBlock(TileType[,] grid, int x, int y, int width, int height, TileType fillType) {
        for (int tileY = 0; tileY < height; tileY++) {
            for (int tileX = 0; tileX < width; tileX++) {
                grid[tileY + y, tileX + x] = fillType;
            }
        }
    }

    //use array to create tiles
    private void CreateTilesFromArray(TileType[,] grid) {
        int height = grid.GetLength(0);
        int width = grid.GetLength(1);
        for (int y = 0; y < height; y++) {
            for (int x = 0; x < width; x++) {
                TileType tile = grid[y, x];
                //Add borders
                if ((y == 0 || y == height - 1 || x == 0 || x == width - 1) && tile != TileType.Wall) {
                    CreateTile(x, y, TileType.Wall);
                }
                if (tile == TileType.Flooded) {
                    tile = TileType.Empty;
                }
                if (tile != TileType.Empty) {
                    CreateTile(x, y, tile);
                }
            }
        }
    }

    //create a single tile
    private GameObject CreateTile(int x, int y, TileType type) {
        int tileID = ((int)type) - 1;
        if (tileID >= 0 && tileID < tiles.Length)
        {
            GameObject tilePrefab = tiles[tileID];
            if (tilePrefab != null) {
                GameObject newTile = GameObject.Instantiate(tilePrefab, new Vector3(x, y, 0), Quaternion.identity);
                newTile.transform.SetParent(transform);
                return newTile;
            }

        } else {
            Debug.LogError("Invalid tile type selected");
        }

        return null;
    }

    private void SpawnPlayer() {
        FillBlock(grid, spawnpoint.x, spawnpoint.y, 1, 1, TileType.Player);
        tilesInReach.Remove(spawnpoint);
    }

    private void SpawnEnd() {
        List<Vector2Int> distantTiles = new List<Vector2Int>();
        int failsafe = 0;
        Vector2Int endTile;
        while (distantTiles.Count == 0) {
            foreach (Vector2Int tile in tilesInReach) {
                if (Mathf.Abs(spawnpoint.x - tile.x) + Mathf.Abs(spawnpoint.y - tile.y) >= 40 - failsafe &&
                    Mathf.Abs(spawnpoint.x - tile.x) + Mathf.Abs(spawnpoint.y - tile.y) < 75) {
                    distantTiles.Add(tile);
                }
            }
            failsafe++;
            Debug.Log(failsafe);
        }
        int randomTile = Random.Range(0, distantTiles.Count);
        endTile = distantTiles[randomTile];
        FillBlock(grid, endTile.x, endTile.y, 1, 1, TileType.End);
        tilesInReach.Remove(endTile);
    }

    //Before end
    private void SpawnEnemy() {
        List<Vector2Int> distantTiles = new List<Vector2Int>();
        int failsafe = 0;
        Vector2Int enemyTile;
        while (distantTiles.Count == 0) {
            foreach (Vector2Int tile in tilesInReach) {
                if (Mathf.Abs(spawnpoint.x - tile.x) + Mathf.Abs(spawnpoint.y - tile.y) >= 15 - failsafe &&
                    Mathf.Abs(spawnpoint.x - tile.x) + Mathf.Abs(spawnpoint.y - tile.y) < 25) {
                    distantTiles.Add(tile);
                }
            }
            failsafe++;
        }
        int randomTile = Random.Range(0, distantTiles.Count);
        enemyTile = distantTiles[randomTile];
        FillBlock(grid, enemyTile.x, enemyTile.y, 1, 1, TileType.Enemy);
        tilesInReach.Remove(enemyTile);
    }

    //Before end
    private void SpawnDoor() {
        List<Vector2Int> distantTiles = new List<Vector2Int>();
        int failsafe = 0;
        Vector2Int doorTile;
        while (distantTiles.Count == 0) {
            foreach (Vector2Int tile in tilesInReach) {
                if (Mathf.Abs(spawnpoint.x - tile.x) + Mathf.Abs(spawnpoint.y - tile.y) >= 15 - failsafe &&
                    Mathf.Abs(spawnpoint.x - tile.x) + Mathf.Abs(spawnpoint.y - tile.y) < 25) {
                    distantTiles.Add(tile);
                }
            }
            failsafe++;
        }
        int randomTile = Random.Range(0, distantTiles.Count);
        doorTile = distantTiles[randomTile];
        FillBlock(grid, doorTile.x, doorTile.y, 1, 1, TileType.Door);
        tilesInReach.Remove(doorTile);
    }

    //Before enemy
    private void SpawnDagger() {
        List<Vector2Int> distantTiles = new List<Vector2Int>();
        int failsafe = 0;
        Vector2Int daggerTile;
        while (distantTiles.Count == 0) {
            foreach (Vector2Int tile in tilesInReach) {
                if (Mathf.Abs(spawnpoint.x - tile.x) + Mathf.Abs(spawnpoint.y - tile.y) >= 5 - failsafe &&
                    Mathf.Abs(spawnpoint.x - tile.x) + Mathf.Abs(spawnpoint.y - tile.y) < 15) {
                    distantTiles.Add(tile);
                }
            }
            failsafe++;
        }
        int randomTile = Random.Range(0, distantTiles.Count);
        daggerTile = distantTiles[randomTile];
        FillBlock(grid, daggerTile.x, daggerTile.y, 1, 1, TileType.Dagger);
        tilesInReach.Remove(daggerTile);
    }
    
    //Before door
    private void SpawnKey() {
        List<Vector2Int> distantTiles = new List<Vector2Int>();
        int failsafe = 0;
        Vector2Int keyTile;
        while (distantTiles.Count == 0) {
            foreach (Vector2Int tile in tilesInReach) {
                if (Mathf.Abs(spawnpoint.x - tile.x) + Mathf.Abs(spawnpoint.y - tile.y) >= 5 - failsafe &&
                    Mathf.Abs(spawnpoint.x - tile.x) + Mathf.Abs(spawnpoint.y - tile.y) < 15) {
                    distantTiles.Add(tile);
                }
            }
            failsafe++;
        }
        int randomTile = Random.Range(0, distantTiles.Count);
        keyTile = distantTiles[randomTile];
        FillBlock(grid, keyTile.x, keyTile.y, 1, 1, TileType.Key);
        tilesInReach.Remove(keyTile);
    }

    protected void Update() {
        if (Input.GetKeyDown(KeyCode.Q)) {
            grid = EvolveTiles(grid, width, height);
        }
    }
}
