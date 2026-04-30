using UnityEngine;


/// <summary>
///火控系统的副武器控制器组件，负责管理坦克副武器（如机枪）的相关数据和逻辑，例如射击频率、弹药消耗、射击效果等
/// 但同样是受主武器控制器（TankFireController）管理的子系统，作为主武器控制器的一个子组件，专门处理与副武器相关的功能，以便将坦克的核心功能与副武器管理逻辑分离开来，提高代码的清晰度和可维护性。通过在 TankFireController 类中组合 TankMachineGunController 组件，可以实现更灵活和模块化的设计，使得坦克的副武器系统更加独立和易于扩展。
/// </summary>
public partial class TankFireController : MonoBehaviour
{
    // private void HandleMachineGunFire()
    // {
    //     Objectpooler machineGunPool = _ammoPoolGroup.GetMachineGunPool(_machineGunSubType);

    //     if (machineGunPool != null)
    //     {
    //         GameObject bullet = machineGunPool.GetPooledObject();
    //         if (bullet != null)
    //         {
    //             bullet.transform.position = _machineGunFirePoint.position;
    //             bullet.transform.rotation = _machineGunFirePoint.rotation;
    //             bullet.SetActive(true);

    //             // 这里可以添加额外的逻辑，例如设置子弹的速度、伤害等属性
    //         }
    //         else
    //         {
    //             Debug.LogWarning("TankFireController: 机枪弹药池没有可用对象！");
    //         }
    //     }
    //     else
    //     {
    //         Debug.LogWarning("TankFireController: 没有找到对应类型的机枪弹药池！");
    //     }
    // }
}

