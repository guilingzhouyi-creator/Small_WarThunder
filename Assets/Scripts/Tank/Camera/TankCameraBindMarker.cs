using UnityEngine;

[DisallowMultipleComponent]
public class TankCameraBindMarker : MonoBehaviour
{
    public enum BindRole
    {
        ThirdPersonFollow,
        ThirdPersonLookAt,
        ZoomFollow,
        ZoomLookAt,
        Aimfollow,
        AimLookAt,
        MapOverhead
    }

    [SerializeField] private BindRole bindRole;

    public BindRole Role => bindRole;
}
