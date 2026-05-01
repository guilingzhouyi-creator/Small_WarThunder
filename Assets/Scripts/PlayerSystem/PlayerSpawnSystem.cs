using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 玩家坦克生成系统（执行层）。
/// 负责在 GameScene 中查找 PlayerSpawnPosition 并生成玩家坦克。
/// 由 GameManager（总控层）在场景加载后调用 SpawnPlayer()。
/// </summary>
public class PlayerSpawnSystem : MonoBehaviour
{
    public static PlayerSpawnSystem Instance { get; private set; }

    [SerializeField] private GameObject _tankPrefab;
    private GameObject _playerTank;
    private Transform _playerSpawnPoint;

    public GameObject PlayerTank => _playerTank;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    /// <summary>
    /// 在当前 GameScene 中生成玩家坦克。
    /// 如果已有坦克且仍属于当前场景，则跳过。
    /// </summary>
    public void SpawnPlayer()
    {
        Scene scene = SceneManager.GetActiveScene();

        // 如果已有坦克但属于旧场景，销毁
        if (_playerTank != null && _playerTank.scene != scene)
        {
            _playerTank = null;
        }

        // 已有有效坦克，跳过
        if (_playerTank != null)
        {
            return;
        }

        if (!ValidateSpawn())
        {
            return;
        }

        if (!EnsurePlayerSpawnPoint(scene))
        {
            Debug.LogError("[PlayerSpawnSystem] 无法生成玩家坦克：当前 GameScene 中没有找到 PlayerSpawnPosition。", this);
            return;
        }

        _playerTank = Instantiate(_tankPrefab, _playerSpawnPoint.position, _playerSpawnPoint.rotation);
        _playerTank.name = _tankPrefab.name;
    }

    private bool ValidateSpawn()
    {
        if (_tankPrefab == null)
        {
            Debug.LogError("[PlayerSpawnSystem] tankPrefab 未设置，请在 Inspector 中分配 Tank 预制体。", this);
            return false;
        }

        return true;
    }

    private bool EnsurePlayerSpawnPoint(Scene scene)
    {
        if (_playerSpawnPoint != null && _playerSpawnPoint.gameObject.scene == scene)
        {
            return true;
        }

        Transform localMatch = FindTransformRecursive(transform, "PlayerSpawnPosition");
        if (localMatch != null)
        {
            _playerSpawnPoint = localMatch;
            return true;
        }

        _playerSpawnPoint = FindTransformInScene(scene, "PlayerSpawnPosition");
        return _playerSpawnPoint != null;
    }

    private Transform FindTransformInScene(Scene scene, string targetName)
    {
        if (!scene.IsValid() || !scene.isLoaded || string.IsNullOrWhiteSpace(targetName))
        {
            return null;
        }

        GameObject[] rootObjects = scene.GetRootGameObjects();
        for (int i = 0; i < rootObjects.Length; i++)
        {
            Transform match = FindTransformRecursive(rootObjects[i].transform, targetName);
            if (match != null)
            {
                return match;
            }
        }

        return null;
    }

    private Transform FindTransformRecursive(Transform root, string targetName)
    {
        if (root == null)
        {
            return null;
        }

        if (root.name == targetName)
        {
            return root;
        }

        for (int i = 0; i < root.childCount; i++)
        {
            Transform match = FindTransformRecursive(root.GetChild(i), targetName);
            if (match != null)
            {
                return match;
            }
        }

        return null;
    }
}
