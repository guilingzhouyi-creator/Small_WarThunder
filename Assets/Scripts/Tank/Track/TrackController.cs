using UnityEngine;

public class TrackController : TrackPathRendererBase
{
    [Header("引用")]
    public TankSuspensionManager suspensionManager;

    [Header("差速联动")]
    public bool followTankMovement = true;

    private TankMoveController moveController;

    protected override void CacheRuntimeReferences()
    {
        base.CacheRuntimeReferences();
        moveController = TankMoveController.Instance != null ? TankMoveController.Instance : GetComponentInParent<TankMoveController>();
        if (suspensionManager == null)
        {
            suspensionManager = GetComponentInParent<TankSuspensionManager>();
        }
    }

    protected override float ResolveScrollSpeed()
    {
        float visualMultiplier = ResolveSideDriveVisualMultiplier();

        if (followTankMovement && moveController != null)
        {
            return moveController.GetDifferentialTrackSpeed(IsLeftTrack) * visualMultiplier;
        }

        return scrollSpeed * visualMultiplier;
    }

    public override float GetWheelVisualSpeed()
    {
        float visualMultiplier = suspensionManager != null ? suspensionManager.WheelVisualDirectionMultiplier : 1f;
        return VisualScrollSpeed * visualMultiplier * ResolveSideDriveVisualMultiplier();
    }

    public override void SetTankSuspensionManager(TankSuspensionManager manager)
    {
        suspensionManager = manager;
    }
}