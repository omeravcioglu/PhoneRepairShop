using UnityEngine;

namespace PhoneRepair.Core
{
    public static class BoundsUtility
    {
        /// <summary>Bounds of every renderer under <paramref name="root"/>, in root's local space.</summary>
        public static Bounds LocalBounds(Transform root)
        {
            Matrix4x4 toRoot = root.worldToLocalMatrix;
            bool hasBounds = false;
            Bounds result = default;
            foreach (Renderer r in root.GetComponentsInChildren<Renderer>(true))
            {
                Bounds local = r.localBounds;
                Matrix4x4 toRootFromRenderer = toRoot * r.localToWorldMatrix;
                for (int i = 0; i < 8; i++)
                {
                    var corner = new Vector3(
                        (i & 1) == 0 ? local.min.x : local.max.x,
                        (i & 2) == 0 ? local.min.y : local.max.y,
                        (i & 4) == 0 ? local.min.z : local.max.z);
                    Vector3 p = toRootFromRenderer.MultiplyPoint3x4(corner);
                    if (hasBounds)
                    {
                        result.Encapsulate(p);
                    }
                    else
                    {
                        result = new Bounds(p, Vector3.zero);
                        hasBounds = true;
                    }
                }
            }

            return result;
        }

        /// <summary>Axis-aligned box that contains <paramref name="bounds"/> after rotating it about the origin.</summary>
        public static Bounds Rotate(Bounds bounds, Quaternion rotation)
        {
            Matrix4x4 m = Matrix4x4.Rotate(rotation);
            Vector3 e = bounds.extents;
            var extents = new Vector3(
                Mathf.Abs(m.m00) * e.x + Mathf.Abs(m.m01) * e.y + Mathf.Abs(m.m02) * e.z,
                Mathf.Abs(m.m10) * e.x + Mathf.Abs(m.m11) * e.y + Mathf.Abs(m.m12) * e.z,
                Mathf.Abs(m.m20) * e.x + Mathf.Abs(m.m21) * e.y + Mathf.Abs(m.m22) * e.z);
            return new Bounds(rotation * bounds.center, extents * 2f);
        }
    }
}
