using UnityEngine;


public struct FCSSnapshot
{

    public int InstanceID;
    public Vector3 MuzzlePos;    // 来自 TankWeaponController.GetBarrelMuzzlePosition() 
    public Vector3 BarrelForward;// 来自 TankWeaponController.GetBarrelForward() 
    public Matrix4x4 ViewMatrix;
    public Matrix4x4 ProjectionMatrix;
    public float CurrentFov;     // 来自 mainCamera.fieldOfView 
    public float ScreenWidth;
    public float ScreenHeight;
}