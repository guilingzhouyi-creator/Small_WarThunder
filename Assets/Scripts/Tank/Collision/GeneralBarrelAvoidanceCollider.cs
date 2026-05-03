using UnityEngine;

public enum BarrelAvoidanceColliderRole
{
    Auto = 0,
    Avoid = 1,
    Ignore = 2
}

public class GeneralBarrelAvoidanceCollider : MonoBehaviour
{
    [SerializeField] private BarrelAvoidanceColliderRole _role = BarrelAvoidanceColliderRole.Auto;

    public BarrelAvoidanceColliderRole Role => _role;
}