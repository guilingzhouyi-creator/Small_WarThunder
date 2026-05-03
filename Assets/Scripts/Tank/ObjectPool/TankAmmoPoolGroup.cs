using UnityEngine;

public class TankAmmoPoolGroup : MonoBehaviour
{
    public static TankAmmoPoolGroup Instance { get; private set; }

    private Objectpooler _cannonBallPoolAP;
    private Objectpooler _cannonBallPoolHE;
    private Objectpooler _cannonBallPoolHEAT;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("TankAmmoPoolGroup: 已经存在一个实例了，新的实例将被销毁以保持单例模式。", this);
            Destroy(gameObject);

            return;
        }

        Instance = this;

        AutoCollectPools();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void OnValidate()
    {
        AutoCollectPools();
    }

    private void AutoCollectPools()
    {
        TankAmmoPoolMarker[] poolMarkers = GetComponentsInChildren<TankAmmoPoolMarker>(true);
        _cannonBallPoolAP = null;
        _cannonBallPoolHE = null;
        _cannonBallPoolHEAT = null;

        if (poolMarkers == null || poolMarkers.Length == 0)
        {
            return;
        }

        for (int i = 0; i < poolMarkers.Length; i++)
        {
            TankAmmoPoolMarker marker = poolMarkers[i];
            if (marker == null)
            {
                continue;
            }

            Objectpooler pool = marker.GetComponent<Objectpooler>();
            if (pool == null)
            {
                continue;
            }

            switch (marker.AmmoType)
            {
                case ProjectileType.AP:
                    _cannonBallPoolAP = pool;
                    break;
                case ProjectileType.HE:
                    _cannonBallPoolHE = pool;
                    break;
                case ProjectileType.HEAT:
                    _cannonBallPoolHEAT = pool;
                    break;
            }
        }
    }

    public bool TryGetPool(ProjectileType ammoType, out Objectpooler pool)
    {
        pool = null;

        switch (ammoType)
        {
            case ProjectileType.AP:
                pool = _cannonBallPoolAP;
                break;
            case ProjectileType.HE:
                pool = _cannonBallPoolHE;
                break;
            case ProjectileType.HEAT:
                pool = _cannonBallPoolHEAT;
                break;
        }

        return pool != null;
    }
}
