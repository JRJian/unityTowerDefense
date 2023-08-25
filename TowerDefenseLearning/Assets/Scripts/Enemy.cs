using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    EnemyFactory originFactory;

    GameTile tileFrom, tileTo;
    Vector3 positionFrom, positionTo;
    float progress;

    public EnemyFactory OriginFactory {
        get => originFactory;
        set {
			Debug.Assert(originFactory == null, "Redefined origin factory!");
			originFactory = value;
        }
    }

    // 出生点
    public void SpawnOn(GameTile tile) {
		Debug.Assert(tile.NextTileOnPath != null, "Nowhere to go!", this);
        tileFrom = tile;
        tileTo = tile.NextTileOnPath;
        positionFrom = tileFrom.transform.localPosition;
        positionTo = tileFrom.ExitPoint;
        transform.localPosition = tile.transform.localPosition;
        progress = 0f;
    }

    public bool GameUpdate() {
        progress += Time.deltaTime;
        while(progress >= 1f) {
            tileFrom = tileTo;
            tileTo = tileTo.NextTileOnPath;
            // 无路可走，代表到达终点，销毁对象
            if (tileTo == null) {
                OriginFactory.Reclaim(this);
                return false;
            }
            positionFrom = positionTo;
            positionTo = tileFrom.ExitPoint;
            progress -= 1f;
        }
        transform.localPosition = Vector3.Lerp(positionFrom, positionTo, progress);
        return true;
    }
}
