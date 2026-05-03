using UnityEngine;
using Unity.Cinemachine;
using System.Collections;

public class CameraPosition : MonoBehaviour
{

    [Header("引用设置")]
    private CinemachineCamera cinemachineCamera;
    [SerializeField] private float targetHorizontal;
    [SerializeField] private float targetVertical;
    [SerializeField] private float targetRadial;

    //是否首次设置摄像机位置
    private bool isFirstSet = false;

    public bool HasCameraReference => cinemachineCamera != null;
    public string CameraName => cinemachineCamera != null ? cinemachineCamera.Name : string.Empty;

    private void Awake()
    {
        ResolveCameraReference();
        SetCameraPosition();
    }

    private void ResolveCameraReference()
    {
        cinemachineCamera = GetComponent<CinemachineCamera>();

        if (cinemachineCamera == null)
        {
            cinemachineCamera = GetComponentInChildren<CinemachineCamera>(true);
        }
    }

    public void BindTarget(Transform followTarget, Transform lookAtTarget)
    {
        if (cinemachineCamera == null || followTarget == null || lookAtTarget == null)
        {
            return;
        }

        cinemachineCamera.Follow = followTarget;
        cinemachineCamera.LookAt = lookAtTarget;
    }


    private void SetCameraPosition()
    {
        if (cinemachineCamera == null)
        {
            return;
        }

        var orbitalFollowState = cinemachineCamera.GetComponent<CinemachineOrbitalFollow>();

        if (orbitalFollowState != null && !isFirstSet)
        {
            float _wait = 0.5f;
            float _time = 1f;

            orbitalFollowState.HorizontalAxis.Center = targetHorizontal;
            orbitalFollowState.VerticalAxis.Center = targetVertical;
            orbitalFollowState.RadialAxis.Center = targetRadial;

            orbitalFollowState.HorizontalAxis.Recentering.Enabled = true;
            orbitalFollowState.VerticalAxis.Recentering.Enabled = true;
            orbitalFollowState.RadialAxis.Recentering.Enabled = true;

            orbitalFollowState.HorizontalAxis.Recentering.Wait = _wait;
            orbitalFollowState.VerticalAxis.Recentering.Wait = _wait;
            orbitalFollowState.RadialAxis.Recentering.Wait = _wait;
            orbitalFollowState.HorizontalAxis.Recentering.Time = _time;
            orbitalFollowState.VerticalAxis.Recentering.Time = _time;
            orbitalFollowState.RadialAxis.Recentering.Time = _time;

            //采用协程当设置完成后禁用视角初始位置
            StartCoroutine(DisenableSetting(orbitalFollowState, _time));

        }
    }
    private IEnumerator DisenableSetting(CinemachineOrbitalFollow orbitalFollowState, float time)
    {
        yield return new WaitForSeconds(time);

        if (orbitalFollowState != null)
        {
            orbitalFollowState.HorizontalAxis.Recentering.Enabled = false;
            orbitalFollowState.VerticalAxis.Recentering.Enabled = false;
            orbitalFollowState.RadialAxis.Recentering.Enabled = false;
            isFirstSet = true;
        }
    }


}
