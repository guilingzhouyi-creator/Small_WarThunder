using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace NAI
{
    /// <summary>
    /// AI专属开火控制器，独立于Tank文件夹下的TankFireController
    /// 直接通过TankAmmoPoolGroup全局单例获取对象池，维护AI自己的弹药库存和装填状态
    /// </summary>
    public class AI_FireController : MonoBehaviour
    {
        [Header("--- 炮口设置 ---")]
        [SerializeField] private Transform _firePoint;

        [Header("--- 武器数据 ---")]
        [SerializeField] private TankWeaponData _weaponData;

        [Header("--- 弹药数据（回退） ---")]
        [SerializeField] private ProjectileData _fallbackProjectileData;

        [Header("--- 弹药库存 ---")]
        [SerializeField] private int _initialApAmmo = 20;
        [SerializeField] private int _initialHeAmmo = 5;
        [SerializeField] private int _initialHeatAmmo = 5;
        [SerializeField] private int _initialReadyAmmo = 5;
        [SerializeField] private float _reloadTime = 5f;

        private Dictionary<ProjectileType, int> _ammoInventory;
        private Dictionary<ProjectileType, ProjectileData> _projectileDataLookup;
        private int _currentReadyAmmo;
        private ProjectileType _currentAmmoType = ProjectileType.AP;
        private ProjectileType _nextAmmoType = ProjectileType.AP;
        private bool _isReloading;
        private float _currentReloadTime;
        private float _lastFireTime;
        private const float FIRE_COOLDOWN = 0.5f;

        public bool IsReloading => _isReloading;
        public bool CanFire => !_isReloading && Time.time - _lastFireTime >= FIRE_COOLDOWN;
        public ProjectileType CurrentAmmoType => _currentAmmoType;

        private void Awake()
        {
            InitializeAmmoInventory();
            InitializeProjectileDataLookup();
        }

        /// <summary>
        /// 初始化弹药库存
        /// </summary>
        private void InitializeAmmoInventory()
        {
            _ammoInventory = new Dictionary<ProjectileType, int>
            {
                { ProjectileType.AP, _initialApAmmo },
                { ProjectileType.HE, _initialHeAmmo },
                { ProjectileType.HEAT, _initialHeatAmmo }
            };
            _currentReadyAmmo = _initialReadyAmmo;
        }

        /// <summary>
        /// 根据武器SO配置建立弹种数据查找表
        /// </summary>
        private void InitializeProjectileDataLookup()
        {
            _projectileDataLookup = new Dictionary<ProjectileType, ProjectileData>();

            if (_weaponData == null || _weaponData.ProjectileSOList == null)
            {
                Debug.LogWarning("[AI_FireController] 武器数据或弹种列表为空，将使用回退数据");
                return;
            }

            foreach (ProjectileData projectileData in _weaponData.ProjectileSOList)
            {
                if (projectileData == null) continue;
                _projectileDataLookup[projectileData.cannonType] = projectileData;
            }
        }

        /// <summary>
        /// 检查指定弹种是否有库存
        /// </summary>
        public bool HasAmmo(ProjectileType ammoType)
        {
            return _ammoInventory.TryGetValue(ammoType, out int count) && count > 0;
        }

        /// <summary>
        /// 检查是否有代发弹
        /// </summary>
        public bool HasReadyAmmo()
        {
            return _currentReadyAmmo > 0;
        }

        /// <summary>
        /// 尝试消耗一枚弹药
        /// </summary>
        public bool TryConsumeAmmo(ProjectileType ammoType)
        {
            if (!_ammoInventory.TryGetValue(ammoType, out int count) || count <= 0)
                return false;

            _ammoInventory[ammoType] = count - 1;

            if (HasReadyAmmo())
            {
                _currentReadyAmmo--;
                Debug.Log($"[AI_FireController] 消耗代发弹，剩余: {_currentReadyAmmo}");
            }

            return true;
        }

        /// <summary>
        /// 获取当前可用弹种（循环遍历AP/HE/HEAT）
        /// </summary>
        public ProjectileType GetAvailableAmmoType()
        {
            ProjectileType[] types = { ProjectileType.AP, ProjectileType.HE, ProjectileType.HEAT };
            for (int i = 0; i < types.Length; i++)
            {
                int startIdx = (System.Array.IndexOf(types, _currentAmmoType) + i) % types.Length;
                if (HasAmmo(types[startIdx]))
                    return types[startIdx];
            }
            return _currentAmmoType;
        }

        /// <summary>
        /// 切换弹药类型
        /// </summary>
        public void SwitchAmmoType(ProjectileType newType)
        {
            if (!HasAmmo(newType)) return;
            _nextAmmoType = newType;

            if (_isReloading)
            {
                _currentReloadTime += 1f;
            }
        }

        /// <summary>
        /// AI调用主炮开火
        /// </summary>
        public bool FireMainGun()
        {
            if (_isReloading)
            {
                Debug.Log("[AI_FireController] 装填中，无法开火");
                return false;
            }

            if (Time.time - _lastFireTime < FIRE_COOLDOWN)
            {
                return false;
            }

            // 确保当前弹种有库存，否则自动切换
            if (!HasAmmo(_currentAmmoType))
            {
                ProjectileType available = GetAvailableAmmoType();
                if (!HasAmmo(available))
                {
                    Debug.LogWarning("[AI_FireController] 所有弹药耗尽！");
                    return false;
                }
                _currentAmmoType = available;
                _nextAmmoType = available;
            }

            if (SpawnProjectile())
            {
                _lastFireTime = Time.time;
                StartCoroutine(ReloadRoutine());
                return true;
            }

            return false;
        }

        /// <summary>
        /// 生成并发射炮弹
        /// </summary>
        private bool SpawnProjectile()
        {
            ProjectileType ammoType = _currentAmmoType;
            ProjectileData projectileData = ResolveProjectileData(ammoType);

            if (projectileData == null)
            {
                Debug.LogError($"[AI_FireController] 无法解析弹种 {ammoType} 的数据");
                return false;
            }

            TankAmmoPoolGroup poolGroup = TankAmmoPoolGroup.Instance;
            if (poolGroup == null)
            {
                Debug.LogError("[AI_FireController] TankAmmoPoolGroup单例不存在");
                return false;
            }

            if (!poolGroup.TryGetPool(ammoType, out Objectpooler pool) || pool == null)
            {
                Debug.LogError($"[AI_FireController] 未找到弹种 {ammoType} 的对象池");
                return false;
            }

            GameObject cannonBallObj = pool.GetPooledObject();
            if (cannonBallObj == null)
            {
                Debug.LogError("[AI_FireController] 无法从对象池获取炮弹实例");
                return false;
            }

            if (!TryConsumeAmmo(ammoType))
            {
                pool.ReturnToPool(cannonBallObj);
                return false;
            }

            CannonBall cannonBall = cannonBallObj.GetComponent<CannonBall>();
            if (cannonBall == null)
            {
                pool.ReturnToPool(cannonBallObj);
                return false;
            }

            Vector3 fireDirection = _firePoint != null ? _firePoint.forward : transform.forward;

            cannonBallObj.transform.SetParent(pool.transform);
            cannonBallObj.transform.position = _firePoint != null ? _firePoint.position : transform.position + transform.forward;
            cannonBallObj.transform.rotation = Quaternion.LookRotation(fireDirection);
            cannonBallObj.SetActive(true);

            cannonBall.Shoot(fireDirection, projectileData);

            Debug.Log($"[AI_FireController] 发射炮弹: {ammoType}, 剩余: {_ammoInventory[ammoType]}");
            return true;
        }

        /// <summary>
        /// 根据弹种查找ProjectileData，优先使用武器SO列表，其次使用回退数据
        /// </summary>
        private ProjectileData ResolveProjectileData(ProjectileType ammoType)
        {
            if (_projectileDataLookup != null && _projectileDataLookup.TryGetValue(ammoType, out ProjectileData data) && data != null)
                return data;

            if (_fallbackProjectileData != null && _fallbackProjectileData.cannonType == ammoType)
                return _fallbackProjectileData;

            Debug.LogWarning($"[AI_FireController] 弹种 {ammoType} 无对应ProjectileData，使用回退数据");
            return _fallbackProjectileData;
        }

        /// <summary>
        /// 装填协程
        /// </summary>
        private IEnumerator ReloadRoutine()
        {
            _isReloading = true;
            _currentReloadTime = 0f;

            float effectiveReloadTime = HasReadyAmmo() ? _reloadTime * 0.6f : _reloadTime;

            while (_currentReloadTime < effectiveReloadTime)
            {
                _currentReloadTime += Time.deltaTime;
                yield return null;
            }

            _currentAmmoType = _nextAmmoType;
            _isReloading = false;

            if (!HasReadyAmmo())
            {
                _currentReadyAmmo = Mathf.Min(_initialReadyAmmo, _ammoInventory[_currentAmmoType]);
            }

            Debug.Log($"[AI_FireController] 装填完成，当前弹种: {_currentAmmoType}");
        }

        /// <summary>
        /// 获取剩余弹药数量
        /// </summary>
        public int GetAmmoCount(ProjectileType ammoType)
        {
            return _ammoInventory.TryGetValue(ammoType, out int count) ? count : 0;
        }
    }
}
