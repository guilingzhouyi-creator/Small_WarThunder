using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class TrackController : MonoBehaviour
{
    [Header("引用")]
    public Transform pathRoot;
    public GameObject trackPrefab;
    public TankSuspensionManager suspensionManager;

    [Header("差速联动")]
    public bool followTankMovement = true;
    public bool autoDetectTrackSide = true;
    public bool isLeftTrack = true;

    [Header("参数")]
    public int linkCount = 120;           // 建议调高，根据你的履带长度
    public float scrollSpeed = 2f;
    public Vector3 rotationOffset = Vector3.zero;
    public float scrollSpeedLerp = 10f;

    private List<Transform> nodes = new List<Transform>();
    private List<GameObject> spawnedLinks = new List<GameObject>();
    private TankMoveController moveController;
    private TankTrackSideDrivePoint sideDrivePoint;
    private float _visualScrollSpeed;
    private float _trackOffset;

    public float VisualScrollSpeed => _visualScrollSpeed;
    public bool IsLeftTrack => GetTrackIsLeftSide();

    void Start()
    {
        moveController = TankMoveController.Instance != null ? TankMoveController.Instance : GetComponentInParent<TankMoveController>();
        sideDrivePoint = GetComponentInParent<TankTrackSideDrivePoint>();
        if (suspensionManager == null)
        {
            suspensionManager = GetComponentInParent<TankSuspensionManager>();
        }
        InitializeNodes();
        SpawnTracks();
        _visualScrollSpeed = ResolveScrollSpeed();
    }

    void OnEnable()
    {
        moveController = TankMoveController.Instance != null ? TankMoveController.Instance : GetComponentInParent<TankMoveController>();
        sideDrivePoint = GetComponentInParent<TankTrackSideDrivePoint>();
        if (suspensionManager == null)
        {
            suspensionManager = GetComponentInParent<TankSuspensionManager>();
        }
    }

    void InitializeNodes()
    {
        nodes.Clear();
        foreach (Transform child in pathRoot.GetComponentsInChildren<Transform>())
        {
            if (child != pathRoot && child.name.Contains("Node"))
                nodes.Add(child);
        }

        // 按编号排序
        nodes = nodes.OrderBy(n =>
        {
            var match = System.Text.RegularExpressions.Regex.Match(n.name, @"\d+");
            return match.Success ? int.Parse(match.Value) : 0;
        }).ToList();
    }

    void SpawnTracks()
    {
        // 只清理上一次生成的履带片，避免误删轮组/悬挂子物体
        foreach (var child in transform.Cast<Transform>().ToList())
        {
            if (child == transform) continue;

            if (child.name.StartsWith("TrackLink_"))
            {
                Destroy(child.gameObject);
            }
        }

        spawnedLinks.Clear();

        for (int i = 0; i < linkCount; i++)
        {
            GameObject link = Instantiate(trackPrefab, transform);
            link.name = $"TrackLink_{i}";
            spawnedLinks.Add(link);
        }
    }

    void LateUpdate()
    {
        if (nodes.Count < 2 || spawnedLinks.Count == 0) return;

        float totalLength = GetTotalPathLength(); // 新增：计算真实路径长度
        float effectiveScrollSpeed = ResolveScrollSpeed();
        float lerpT = Mathf.Clamp01(scrollSpeedLerp * Time.deltaTime);
        _visualScrollSpeed = Mathf.Lerp(_visualScrollSpeed, effectiveScrollSpeed, lerpT);
        _trackOffset = Mathf.Repeat(_trackOffset + _visualScrollSpeed * Time.deltaTime, totalLength);

        for (int i = 0; i < spawnedLinks.Count; i++)
        {
            float distanceAlongTrack = Mathf.Repeat((float)i / spawnedLinks.Count * totalLength + _trackOffset, totalLength);

            UpdateLinkPosition(spawnedLinks[i].transform, distanceAlongTrack, totalLength);
        }
    }

    void UpdateLinkPosition(Transform link, float distance, float totalLength)
    {
        float traveled = 0f;
        for (int i = 0; i < nodes.Count; i++)
        {
            int next = (i + 1) % nodes.Count;
            float segmentLength = Vector3.Distance(nodes[i].position, nodes[next].position);

            if (traveled + segmentLength >= distance)
            {
                float t = (distance - traveled) / segmentLength;

                Vector3 pos = Vector3.Lerp(nodes[i].position, nodes[next].position, t);
                Vector3 tangent = (nodes[next].position - nodes[i].position).normalized;

                // 使用坦克自身的 right 作为 binormal（永远垂直于履带平面）
                Vector3 binormal = transform.right;

                // Cross(tangent, binormal) 会自动让：
                //   底面 → up = +Y（履带片朝下）
                //   顶面 → up = -Y（履带片朝上，朝外）
                //   轮子弧形 → 平滑过渡，无扭曲
                Vector3 normal = Vector3.Cross(tangent, binormal).normalized;

                Quaternion rot = Quaternion.LookRotation(tangent, normal)
                                 * Quaternion.Euler(rotationOffset);

                link.position = pos;
                link.rotation = rot;
                return;
            }

            traveled += segmentLength;
        }
    }

    float GetTotalPathLength()
    {
        float len = 0f;
        for (int i = 0; i < nodes.Count; i++)
        {
            int next = (i + 1) % nodes.Count;
            len += Vector3.Distance(nodes[i].position, nodes[next].position);
        }
        return len;
    }

    private float ResolveScrollSpeed()
    {
        float visualMultiplier = sideDrivePoint != null ? sideDrivePoint.CurrentVisualSpeedMultiplier : 1f;

        if (followTankMovement && moveController != null)
        {
            return moveController.GetDifferentialTrackSpeed(GetTrackIsLeftSide()) * visualMultiplier;
        }

        return scrollSpeed * visualMultiplier;
    }

    public float GetWheelVisualSpeed()
    {
        float visualMultiplier = suspensionManager != null ? suspensionManager.WheelVisualDirectionMultiplier : 1f;
        float sideLockMultiplier = sideDrivePoint != null ? sideDrivePoint.CurrentVisualSpeedMultiplier : 1f;
        return _visualScrollSpeed * visualMultiplier * sideLockMultiplier;
    }

    private bool GetTrackIsLeftSide()
    {
        if (!autoDetectTrackSide)
        {
            return isLeftTrack;
        }

        if (sideDrivePoint != null)
        {
            return sideDrivePoint.Side == TankTrackSideDrivePoint.TrackSide.Left;
        }

        return transform.localPosition.x < 0f;
    }

}