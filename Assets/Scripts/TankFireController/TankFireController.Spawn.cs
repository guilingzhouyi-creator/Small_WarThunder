using UnityEngine;

public partial class TankFireController : MonoBehaviour
{
    private bool SpawnCannonBall()
    {
        ProjectileType ammoType = CurrentAmmoType;
        ProjectileData projectileData = ResolveProjectileData(ammoType);

        // 1. 检查库存（不消耗）
        if (_tank != null && !_tank.HasAmmo(ammoType))
        {
            Debug.LogWarning($"TankFireController: 当前弹种 {ammoType} 的弹药池为空，请检查弹药池预制体数目！");
            return false;
        }

        // 2. 获取对象池
        if (!TryGetAmmoPool(ammoType, out Objectpooler pool))
        {
            Debug.LogError($"TankFireController: 没有找到对应类型的弹药池！类型: {ammoType}");
            return false;
        }

        // 3. 获取炮弹实例
        GameObject cannonBallObj = pool.GetPooledObject();
        if (cannonBallObj == null)
        {
            Debug.LogError("TankFireController: 无法从对象池获取炮弹！");
            return false;
        }

        // 4. 尝试扣除弹药（包括代发弹逻辑）
        if (_tank != null)
        {
            bool hasReadyAmmoBeforeFire = _tank.HasReadyAmmo();

            if (_tank.TryConsumeAmmo(ammoType))
            {
                // // 扣除弹药成功，处理代发弹计数
                // if (_tank.HasReadyAmmo())
                // {
                //     _tank.ConsumeReadyAmmo();
                //     Debug.Log($"代发弹药剩余: {_tank.CurrentReadyAmmo}");
                // }
                // else
                // {
                //     Debug.LogWarning("代发弹药架已空，装填速度将减慢！");
                // }

                if (hasReadyAmmoBeforeFire)
                {
                    Debug.Log($"[发射] 消耗了代发弹。剩余代发: {_tank.CurrentReadyAmmo}");
                }
                else
                {
                    Debug.LogWarning("代发弹药架已空，装填速度将减慢！");
                }

            }
            else
            {
                Debug.LogWarning($"扣除弹药失败！");
                pool.ReturnToPool(cannonBallObj);
                return false;
            }
        }

        // 5. 物理发射逻辑（现在这部分终于能被执行到了）
        if (projectileData == null)
        {
            pool.ReturnToPool(cannonBallObj);
            return false;
        }

        CannonBall cannonBallScript = cannonBallObj.GetComponent<CannonBall>();
        if (cannonBallScript == null)
        {
            pool.ReturnToPool(cannonBallObj);
            return false;
        }

        Vector3 fireDirection = ResolveFireDirection(_firePoint);
        if (fireDirection == Vector3.zero) fireDirection = _firePoint.forward;

        // 设置位置和方向
        cannonBallObj.transform.SetParent(pool.transform);
        cannonBallObj.transform.position = _firePoint.position;
        cannonBallObj.transform.rotation = Quaternion.LookRotation(fireDirection);
        cannonBallObj.SetActive(true);

        // 真正的开火
        cannonBallScript.Shoot(fireDirection, projectileData);
        PlayFireAudio();

        return true;
    }
}