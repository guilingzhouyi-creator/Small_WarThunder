using UnityEngine;

public class TankAmmoPoolMarker : MonoBehaviour
{
    [SerializeField] private ProjectileType ammoType;

    public ProjectileType AmmoType => ammoType;
}