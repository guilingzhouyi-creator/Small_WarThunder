using UnityEngine;

public class AI_TrackController : TrackPathRendererBase
{
    [Header("引用")]
    public AI_TankSuspensionManager aiSuspensionManager;

    [Header("差速联动")]
    public bool followTankMovement = true;

    private NAI.AI_MotionDriver _motionDriver;

    protected override void CacheRuntimeReferences()
    {
        base.CacheRuntimeReferences();
        if (_motionDriver == null)
        {
            _motionDriver = GetComponentInParent<NAI.AI_MotionDriver>();
        }

        if (aiSuspensionManager == null)
        {
            aiSuspensionManager = GetComponentInParent<AI_TankSuspensionManager>();
        }
    }

    protected override float ResolveScrollSpeed()
    {
        float visualMultiplier = ResolveSideDriveVisualMultiplier();

        if (followTankMovement && _motionDriver != null)
        {
            return _motionDriver.GetDifferentialTrackSpeed(IsLeftTrack) * visualMultiplier;
        }

        return scrollSpeed * visualMultiplier;
    }

    public override float GetWheelVisualSpeed()
    {
        float visualMultiplier = aiSuspensionManager != null ? aiSuspensionManager.WheelVisualDirectionMultiplier : 1f;
        return VisualScrollSpeed * visualMultiplier * ResolveSideDriveVisualMultiplier();
    }

    public override void SetAiSuspensionManager(AI_TankSuspensionManager manager)
    {
        aiSuspensionManager = manager;
    }
}