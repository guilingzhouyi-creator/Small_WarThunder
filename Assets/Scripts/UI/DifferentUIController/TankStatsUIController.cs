using UnityEngine;
using System.Collections;
using TMPro;
public class TankStatsUIController : MonoBehaviour
{
    //状态UI控制器，负责显示坦克的当前状态信息，例如生命值、装填状态等
    //可以通过订阅坦克状态变化事件来更新UI显示

    public static TankStatsUIController Instance { get; private set; }
    [SerializeField] private TextMeshProUGUI ReLoadstatusLTextLab;
    [SerializeField] private TextMeshProUGUI TankerSpeedTextLab;
    [SerializeField] private TextMeshProUGUI CurrentConnonBallTextLab;
    [SerializeField] private TextMeshProUGUI NextConnonBallTextLab;
    [SerializeField] private TextMeshProUGUI TargetDistanceTextLab;
    [SerializeField] private TextMeshProUGUI ReplayStatusTextLab;//代发弹药数显示文本，准备迁移到这里

    private Tank _tank;
    private TankFireController _fireController;
    private TankMoveController _moveController;
    private bool _isBound;
    private bool _isRefreshing = false;

    private void Awake()
    {


        if (Instance != null && Instance != this)
        {
            if (Application.isPlaying)
            {
                Destroy(gameObject);
            }

            return;
        }
        else
        {
            Instance = this;
        }
    }
    private void Start()//主动调用一次刷新UI状态，确保UI显示正确的初始状态
    {
        TryBindAndRefresh();
        StartCoroutine(RefreshAfterInitialization());
    }

    private void OnEnable()
    {
        TryBindAndRefresh();
    }

    private IEnumerator RefreshAfterInitialization()
    {
        yield return null;
        TryBindAndRefresh();
    }

    private void Update()
    {
        if (!_isBound || IsBindingStale())//每帧检查绑定状态，如果发现绑定过期（例如坦克重生了），则重新绑定并刷新UI
        {
            TryBindAndRefresh();
        }
    }


    private void OnDisable()
    {
        UnbindControllers();
    }

    private void OnDestroy()
    {
        UnbindControllers();

        if (Instance == this)
        {
            Instance = null;
        }
    }

    private bool IsBindingStale()
    {
        return _tank != Tank.Instance || _fireController != TankFireController.Instance || _moveController != TankMoveController.Instance;
    }

    private void TryBindAndRefresh()
    {
        if (!TryBindControllers())
        {
            return;
        }

        RefreshAllStatus();
    }


    /// <summary>
    /// 尝试绑定坦克和相关控制器，并订阅必要的事件以保持UI更新。
    /// 1、绑定Tank实例，确保UI能获取当前坦克的状态信息
    /// 2、绑定TankFireController实例，订阅装填状态变化事件以更新装填状态显示
    /// 3、绑定TankMoveController实例，订阅速度变化事件以更新速度显示
    /// 4、订阅坦克的弹药类型和数量变化事件，以更新弹药显示
    /// 5、如果绑定过程中发现任何组件缺失或绑定过期，及时解绑并返回失败，等待下一次尝试绑定
    /// 6、成功绑定后立即刷新所有状态显示，确保UI显示当前正确的坦克状态信息
    /// </summary>
    /// <returns></returns>
    private bool TryBindControllers()
    {
        if (Tank.Instance == null || TankFireController.Instance == null || TankMoveController.Instance == null)
        {
            if (_isBound)
            {
                UnbindControllers();
            }

            return false;
        }

        if (_isBound && !IsBindingStale())
        {
            return true;
        }

        UnbindControllers();

        _tank = Tank.Instance;
        _fireController = TankFireController.Instance;
        _moveController = TankMoveController.Instance;

        _fireController.OnReloadStatusChanged += UpdateReloadStatus;
        _fireController.RangerFinderResultUpdated += UpdateTargetDistance;

        _moveController.OnSpeedChanged += UpdateTankerSpeed;

        _tank.OnCurrentAmmoTypeChanged += HandleCurrentAmmoTypeChanged;
        _tank.OnNextAmmoTypeChanged += HandleNextAmmoTypeChanged;
        _tank.OnAmmoCountChanged += HandleAmmoCountChanged;
        _tank.OnReadyAmmoChanged += HandleReadyAmmoChanged;

        _isBound = true;

        return true;
    }

    private void UnbindControllers()
    {
        if (_fireController != null)
        {
            _fireController.OnReloadStatusChanged -= UpdateReloadStatus;
            _fireController.RangerFinderResultUpdated -= UpdateTargetDistance;
        }

        if (_moveController != null)
        {
            _moveController.OnSpeedChanged -= UpdateTankerSpeed;
        }

        if (_tank != null)
        {
            _tank.OnCurrentAmmoTypeChanged -= HandleCurrentAmmoTypeChanged;
            _tank.OnNextAmmoTypeChanged -= HandleNextAmmoTypeChanged;
            _tank.OnAmmoCountChanged -= HandleAmmoCountChanged;
            _tank.OnReadyAmmoChanged -= HandleReadyAmmoChanged;
        }

        _tank = null;
        _fireController = null;
        _moveController = null;
        _isBound = false;
    }

    private void RefreshAllStatus()
    {
        if (_fireController != null)
        {
            UpdateReloadStatus(_fireController.CurrentReloadTime);
            UpdateTargetDistance(-1f);
        }

        if (_moveController != null)
        {
            UpdateTankerSpeed(_moveController.CurrentSpeed);
        }

        if (_tank != null)
        {
            RefreshAmmoTextDisplay();
        }
    }




    private void UpdateReloadStatus(float reloadTime)
    {
        if (ReLoadstatusLTextLab == null)
        {
            return;
        }

        if (reloadTime > 0)
        {
            ReLoadstatusLTextLab.text = $"{reloadTime:F1}s";
            ReLoadstatusLTextLab.color = Color.red;
        }
        else
        {
            //全部弹药打完后，显示Exhausted状态
            if (_tank != null && _tank.GetAmmoCount(_tank.CurrentAmmoType) == 0)
            {
                ReLoadstatusLTextLab.text = "弹药耗尽!";
                ReLoadstatusLTextLab.color = Color.red;
                return;
            }
            else
            {
                ReLoadstatusLTextLab.text = "Up!";
                ReLoadstatusLTextLab.color = Color.green;
            }

        }

    }

    private void UpdateTankerSpeed(float speed)
    {
        if (TankerSpeedTextLab == null)
        {
            return;
        }

        //保留到个位——转换为Km/h并显示
        float speedKmhPer = 3.6f;
        TankerSpeedTextLab.text = $"{Mathf.RoundToInt(speed * speedKmhPer)} Km/h";
    }

    private void UpdateTargetDistance(float distance)
    {
        if (TargetDistanceTextLab == null)
        {
            return;
        }

        if (distance >= 0)
        {
            TargetDistanceTextLab.text = $"{distance:F1} m";
            TargetDistanceTextLab.color = Color.white;
        }
        else
        {
            TargetDistanceTextLab.text = "N/A";
            TargetDistanceTextLab.color = Color.gray;
        }
    }

    /// <summary>
    /// 更新有关弹药的一切UI显示
    //1、当前弹药类型显示：显示当前选定的弹药类型，并使用不同颜色区分不同类型（例如AP白色，HE黄色，HEAT青色）
    //2、代发弹药数显示：尾舱弹药架还剩多少发弹药
    /// </summary>


    private void RefreshAmmoTextDisplay()
    {
        if (_tank == null || _isRefreshing) return;

        _isRefreshing = true;

        try
        {
            if (CurrentConnonBallTextLab != null)
            {
                CurrentConnonBallTextLab.text = $"{RenderAmmoTypeRichText(_tank.CurrentAmmoType)}";
            }

            if (NextConnonBallTextLab != null)
            {
                int nextAmmoCount = _tank.GetAmmoCount(_tank.NextAmmoType);
                NextConnonBallTextLab.text = $"{RenderAmmoTypeRichText(_tank.NextAmmoType)} : {nextAmmoCount}";
            }

            if (ReplayStatusTextLab != null)
            {
                UpdateReadyAmmoUI(_tank.CurrentReadyAmmo, _tank.MainData.TankMaxReadyAmmo);
            }
        }
        finally
        {
            _isRefreshing = false;
        }

    }

    private void UpdateReadyAmmoUI(int current, int max)
    {
        if (ReplayStatusTextLab == null) return;

        // 使用富文本渲染：根据当前剩余百分比切换颜色
        // 如果剩余不足 30%，显示红色；否则显示绿色（或白色）

        string colorHex = GetAmmoCurrentReadyAmmoColorHex(current, max);
        ReplayStatusTextLab.text = $"<color=#{colorHex}>{current}</color> / {max}";
    }

    private void HandleCurrentAmmoTypeChanged(ProjectileType ammoType)
    {
        RefreshAmmoTextDisplay();
    }

    private void HandleNextAmmoTypeChanged(ProjectileType ammoType)
    {
        RefreshAmmoTextDisplay();
    }

    private void HandleAmmoCountChanged(ProjectileType ammoType, int ammoCount)
    {
        RefreshAmmoTextDisplay();
    }

    private void HandleReadyAmmoChanged(int current, int max)
    {
        UpdateReadyAmmoUI(current, max);
    }

    /// <summary>
    /// 将弹药种类渲染成带颜色的富文本，供当前/下一发等任意 UI 复用。
    /// </summary>
    private string RenderAmmoTypeRichText(ProjectileType ammoType)
    {
        return $"<color=#{GetAmmoTypeColorHex(ammoType)}>{ammoType}</color>";
    }

    private string GetAmmoTypeColorHex(ProjectileType ammoType)
    {
        if (ammoType == ProjectileType.AP)
        {
            return ColorUtility.ToHtmlStringRGB(Color.white);
        }

        if (ammoType == ProjectileType.HE)
        {
            return ColorUtility.ToHtmlStringRGB(Color.yellow);
        }

        if (ammoType == ProjectileType.HEAT)
        {
            return ColorUtility.ToHtmlStringRGB(Color.cyan);
        }

        return ColorUtility.ToHtmlStringRGB(Color.white);
    }
    /// <summary>
    /// 
    /// </summary>
    private string GetAmmoCurrentReadyAmmoColorHex(int current, int max)
    {
        if (max <= 0) return ColorUtility.ToHtmlStringRGB(Color.white);

        float percentage = (float)current / max;

        // 弹架剩余低于 30% 时显示红色警告
        if (percentage <= 0.3f)
        {
            return ColorUtility.ToHtmlStringRGB(Color.red);
        }
        // 弹架满载显示绿色
        else if (current == max)
        {
            return ColorUtility.ToHtmlStringRGB(Color.green);
        }

        return ColorUtility.ToHtmlStringRGB(Color.white);

    }
}