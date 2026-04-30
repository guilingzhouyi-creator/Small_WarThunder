using UnityEngine;


public struct FCSSnapshot
{
    // public int InstanceID;
    // public Vector3 MuzzlePosition;    // 炮口世界坐标
    // public Vector3 BarrelForward;     // 炮口前向向量
    // public Matrix4x4 ViewMatrix;      // 观察矩阵（通常来自主相机）
    // public Matrix4x4 ProjectionMatrix;// 投影矩阵
    // public float ScreenWidth;
    // public float ScreenHeight;
    // public float CurrentFov;          // 用于计算密位刻度感官
    // public bool IsAiming;             // 瞄准模式状态


    public int InstanceID;
    public Vector3 MuzzlePos;    // 来自 TankWeaponController.GetBarrelMuzzlePosition() [source: 16]
    public Vector3 BarrelForward;// 来自 TankWeaponController.GetBarrelForward() [source: 16]
    public Matrix4x4 ViewMatrix;
    public Matrix4x4 ProjectionMatrix;
    public float CurrentFov;     // 来自 mainCamera.fieldOfView [source: 14]
    public float ScreenWidth;
    public float ScreenHeight;
}