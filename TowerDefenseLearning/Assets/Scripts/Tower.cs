using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Tower : GameTileContent
{
    Tower towerPrefab = default;

    [SerializeField, Range(1.5f, 10.5f)]
    float targetingRange = 1.5f;

    TargetPoint target;

    const int enemyLayerMask = 1 << 9;
    static Collider[] targetsBuffer = new Collider[1];

    // 获得目标
    bool AcquireTarget() {
        Vector3 a = transform.localPosition;
        Vector3 b = a;
        b.y += 2f;
        int hits = Physics.OverlapCapsuleNonAlloc(
            a, b, targetingRange, targetsBuffer, enemyLayerMask
        );
        if (hits > 0) {
            target = targetsBuffer[0].GetComponent<TargetPoint>();
			Debug.Assert(target != null, "Targeted non-enemy!", targetsBuffer[0]);
			return true;
        }
        target = null;
        return false;
    }

    bool TrackTarget() {
        if (target == null) {
            return false;
        }
        // 检测是否在跟踪范围内
        Vector3 a = transform.localPosition;
        Vector3 b = target.Position;
        float x = a.x - b.x;
        float z = a.z - b.z;
        float r = targetingRange + 0.125f * target.Enemy.Scale;
        if (x * x + z * z > r * r ) {
            target = null;
            return false;
        }
        return true;
    }

    // 鼠标点击到脚本挂载的物体的身上的时候运行
    // 不管有多少个父类对象，它都会执行
    // Scene场景显示，Game窗口不显示
    void OnDrawGizmosSelected() {
        Gizmos.color = Color.yellow;
        Vector3 position = transform.localPosition;
        position.y += 0.01f;
        Gizmos.DrawWireSphere(position, targetingRange);

        if (target != null) {
            Gizmos.DrawLine(position, target.Position);
        }
    }

	public override void GameUpdate () {
        if (TrackTarget() || AcquireTarget()) {
			Debug.Log("Locked on target!");
        }
    }
}
