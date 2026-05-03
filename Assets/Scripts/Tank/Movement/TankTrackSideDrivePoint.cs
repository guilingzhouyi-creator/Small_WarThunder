using UnityEngine;

public class TankTrackSideDrivePoint : MonoBehaviour
{
    public enum TrackSide
    {
        Left,
        Right
    }

    [Header("履带侧别")]
    [SerializeField] private TrackSide trackSide;

    [Header("接触点")]
    [SerializeField] private Transform trackCenterPoint;

    public TrackSide Side => trackSide;
    public float CurrentVisualSpeedMultiplier { get; private set; } = 1f;
    public bool IsLocked { get; private set; }

    public void ApplyTrackPhysics(Vector3 forwardAxis, float driveForce, float brakeForce, float cogHeight)
    {
        Rigidbody rb = GetComponentInParent<Rigidbody>();
        if (rb == null) return;

        Transform centerAnchor = trackCenterPoint != null ? trackCenterPoint : transform;
        Vector3 contactPoint = centerAnchor.position - transform.up * cogHeight;

        // 1. 施加驱动力
        if (Mathf.Abs(driveForce) > 0.1f)
        {
            rb.AddForceAtPosition(forwardAxis * driveForce, contactPoint, ForceMode.Force);
        }

        // 2. 施加制动力 (阻碍当前履带点的运动速度)
        Vector3 pointVelocity = rb.GetPointVelocity(contactPoint);
        float localForwardSpeed = Vector3.Dot(pointVelocity, forwardAxis);

        if (brakeForce > 0.1f && Mathf.Abs(localForwardSpeed) > 0.01f)
        {
            Vector3 brakeVec = -forwardAxis * Mathf.Sign(localForwardSpeed) * brakeForce;
            rb.AddForceAtPosition(brakeVec, contactPoint, ForceMode.Force);
        }

        IsLocked = Mathf.Abs(localForwardSpeed) < 0.01f && brakeForce > 0.1f;
        CurrentVisualSpeedMultiplier = IsLocked ? 0f : 1f;
    }
}