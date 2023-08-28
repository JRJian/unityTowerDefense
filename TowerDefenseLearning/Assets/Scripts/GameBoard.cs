using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class GameBoard : MonoBehaviour
{
    // The [SerializeField] attribute is used to mark non-public fields as serializable: 
    // so that Unity can save and load those values (all public fields are serialized by default)
    // even though they are not public.
    [SerializeField]
    Transform groud = default;

    [SerializeField]
    GameTile tilePrefab = default;

    [SerializeField]
    Texture2D gridTexture = default;

    Vector2Int size;

    GameTile[] tiles;

    Queue<GameTile> searchFrontier = new Queue<GameTile>();

    List<GameTile> spawnPoints = new List<GameTile>();

    GameTileContentFactory contentFactory;

    bool showGrid, showPaths;

    public int SpawnPointCount => spawnPoints.Count;
    List<GameTileContent> updatingContent = new List<GameTileContent>();

    public bool ShowGrid {
        get => showGrid;
        set {
            showGrid = value;
            Material m = groud.GetComponent<MeshRenderer>().material;
            if (showGrid) {
                m.mainTexture = gridTexture;
                m.SetTextureScale("_MainTex", size);
            }
            else {
                m.mainTexture = null;
            }
        }
    }

    public bool ShowPaths {
        get => showPaths;
        set {
            showPaths = value;
            if (showPaths) {
                foreach (GameTile tile in tiles) {
                    tile.ShowPath();
                }
            }
            else {
                foreach (GameTile tile in tiles) {
                    tile.HidePath();
                }
            }
        }
    }

    public void Initialize (
        Vector2Int size, GameTileContentFactory contentFactory
    ) {
        this.size = size;
        this.contentFactory = contentFactory;
        groud.localScale = new Vector3(size.x, size.y, 1f);

        Vector2 offset = new Vector2(
            (size.x - 1) * 0.5f, (size.y - 1) * 0.5f
        );
        tiles = new GameTile[size.x * size.y];
        for (int i = 0, y = 0; y < size.y; y++) {
            for (int x = 0; x < size.x; x++, i++) {
                GameTile tile = tiles[i] = Instantiate(tilePrefab);
                tile.transform.SetParent(transform, false);
                tile.transform.localPosition = new Vector3(
                    x - offset.x, 0f, y - offset.y
                );

                if (x > 0) {
                    GameTile.MakeEastWestNeighbors(tile, tiles[i - 1]);
                }
                if (y > 0) {
                    GameTile.MakeNorthSouthNeighbors(tile, tiles[i - size.x]);
                }

                tile.IsAlternative = (x & 1) == 0;
                if ((y & 1) == 0) {
                    tile.IsAlternative = !tile.IsAlternative;
                }

                tile.Content = contentFactory.Get(GameTileContentType.Empty);
            }
        }

        ToggleDestination(tiles[tiles.Length / 2]);
        ToggleSpawnPoint(tiles[0]);
    }

	public void ToggleDestination (GameTile tile) {
		if (tile.Content.Type == GameTileContentType.Destination) {
			tile.Content = contentFactory.Get(GameTileContentType.Empty);
			if (!FindPaths()) {
				tile.Content =
					contentFactory.Get(GameTileContentType.Destination);
				FindPaths();
			}
		}
		else if (tile.Content.Type == GameTileContentType.Empty) {
			tile.Content = contentFactory.Get(GameTileContentType.Destination);
			FindPaths();
		}
	}

	public void ToggleWall (GameTile tile) {
		if (tile.Content.Type == GameTileContentType.Wall) {
			tile.Content = contentFactory.Get(GameTileContentType.Empty);
			FindPaths();
		}
		else if (tile.Content.Type == GameTileContentType.Empty) {
			tile.Content = contentFactory.Get(GameTileContentType.Wall);
			if (!FindPaths()) {
				tile.Content = contentFactory.Get(GameTileContentType.Empty);
				FindPaths();
			}
		}
	}

    public void ToggleSpawnPoint(GameTile tile) {
        if (tile.Content.Type == GameTileContentType.SpawnPoint) {
            // 至少有一个产卵点
            if (spawnPoints.Count > 1) {
                spawnPoints.Remove(tile);
                tile.Content = contentFactory.Get(GameTileContentType.Empty);
            }
        }
        else if (tile.Content.Type == GameTileContentType.Empty) {
            tile.Content = contentFactory.Get(GameTileContentType.SpawnPoint);
            spawnPoints.Add(tile);
        }
    }

    public void ToggleTower(GameTile tile) {
        if (tile.Content.Type == GameTileContentType.Tower) {
            updatingContent.Remove(tile.Content);
			tile.Content = contentFactory.Get(GameTileContentType.Empty);
			FindPaths();
        }
        else if (tile.Content.Type == GameTileContentType.Empty) {
            tile.Content = contentFactory.Get(GameTileContentType.Tower);
            if(FindPaths()) {
                updatingContent.Add(tile.Content);
            }
            else {
                tile.Content = contentFactory.Get(GameTileContentType.Empty);
                FindPaths();
            }
        }
        else if (tile.Content.Type == GameTileContentType.Wall) {
            tile.Content = contentFactory.Get(GameTileContentType.Tower);
            updatingContent.Add(tile.Content);
        }
    }

    public GameTile GetTile(Ray ray) {
		if (Physics.Raycast(ray, out RaycastHit hit, float.MaxValue, 1)) {
			int x = (int)(hit.point.x + size.x * 0.5f);
			int y = (int)(hit.point.z + size.y * 0.5f);
			if (x >= 0 && x < size.x && y >= 0 && y < size.y) {
				return tiles[x + y * size.x];
			}
		}
		return null;
    }
    
    public GameTile GetSpawnPoint(int index) {
        return spawnPoints[index];
    }

    private bool FindPaths() {
        foreach(GameTile tile in tiles) {
            if (tile.Content.Type == GameTileContentType.Destination) {
                tile.BecomeDestination();
                searchFrontier.Enqueue(tile);
            }
            else {
                tile.ClearPath();
            }
        }

        if (searchFrontier.Count == 0) {
            return false;
        }

        while (searchFrontier.Count > 0) {
            GameTile tile = searchFrontier.Dequeue();
            if (tile != null) {
                if (tile.IsAlternative) {
					searchFrontier.Enqueue(tile.GrowPathNorth());
					searchFrontier.Enqueue(tile.GrowPathSouth());
					searchFrontier.Enqueue(tile.GrowPathEast());
					searchFrontier.Enqueue(tile.GrowPathWest());
                }
                else {
					searchFrontier.Enqueue(tile.GrowPathWest());
					searchFrontier.Enqueue(tile.GrowPathEast());
					searchFrontier.Enqueue(tile.GrowPathSouth());
					searchFrontier.Enqueue(tile.GrowPathNorth());
                }
            }
        }

        foreach (GameTile tile in tiles) {
            if (!tile.HasPath) {
                return false;
            }
        }

		if (showPaths) {
			foreach (GameTile tile in tiles) {
				tile.ShowPath();
			}
        }
        return true;
    }

    public void GameUpdate() {
        for (int i = 0; i < updatingContent.Count; i++) {
            updatingContent[i].GameUpdate();
        }
    }
}
