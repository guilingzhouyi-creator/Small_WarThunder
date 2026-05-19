using UnityEngine;

namespace NAI
{
    public static class AI_TargetingUtility
    {
        private const float DefaultTargetHeightOffset = 1.6f;

        public static Vector3 ResolveTargetPoint(Transform target)
        {
            if (target == null)
            {
                return Vector3.zero;
            }

            Transform root = target.root != null ? target.root : target;
            return root.position + root.up * DefaultTargetHeightOffset;
        }
    }
}