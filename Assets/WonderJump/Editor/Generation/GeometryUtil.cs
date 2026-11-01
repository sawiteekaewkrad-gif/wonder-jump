using System;
using UnityEngine;

namespace WonderJump.EditorTools.Generation
{
    /// <summary>
    /// Bounds maths from mesh/collider data and transforms only (no Renderer.bounds),
    /// so it works on unsaved objects and on loaded prefab contents alike.
    /// All results are in the local space of <c>root</c>.
    /// </summary>
    public static class GeometryUtil
    {
        public static Bounds MeshBounds(Transform root, Func<Transform, bool> include)
        {
            bool any = false;
            var result = new Bounds(Vector3.zero, Vector3.zero);
            Matrix4x4 toRoot = root.worldToLocalMatrix;
            MeshFilter[] filters = root.GetComponentsInChildren<MeshFilter>(true);
            for (int i = 0; i < filters.Length; i++)
            {
                MeshFilter mf = filters[i];
                if (mf.sharedMesh == null) continue;
                if (include != null && !include(mf.transform)) continue;
                Matrix4x4 m = toRoot * mf.transform.localToWorldMatrix;
                EncapsulateBox(ref result, ref any, m, mf.sharedMesh.bounds.center, mf.sharedMesh.bounds.extents);
            }
            return result;
        }

        public static Bounds ColliderBounds(Transform root, Collider collider)
        {
            bool any = false;
            var result = new Bounds(Vector3.zero, Vector3.zero);
            Matrix4x4 m = root.worldToLocalMatrix * collider.transform.localToWorldMatrix;

            var box = collider as BoxCollider;
            if (box != null)
            {
                EncapsulateBox(ref result, ref any, m, box.center, box.size * 0.5f);
                return result;
            }
            var sphere = collider as SphereCollider;
            if (sphere != null)
            {
                EncapsulateBox(ref result, ref any, m, sphere.center, Vector3.one * sphere.radius);
                return result;
            }
            var capsule = collider as CapsuleCollider;
            if (capsule != null)
            {
                float halfAxis = Mathf.Max(capsule.height * 0.5f, capsule.radius);
                Vector3 ext = Vector3.one * capsule.radius;
                if (capsule.direction == 0) ext.x = halfAxis;
                else if (capsule.direction == 1) ext.y = halfAxis;
                else ext.z = halfAxis;
                EncapsulateBox(ref result, ref any, m, capsule.center, ext);
                return result;
            }
            return result;
        }

        private static void EncapsulateBox(ref Bounds result, ref bool any, Matrix4x4 m, Vector3 center, Vector3 extents)
        {
            for (int c = 0; c < 8; c++)
            {
                var corner = new Vector3(
                    center.x + ((c & 1) == 0 ? -extents.x : extents.x),
                    center.y + ((c & 2) == 0 ? -extents.y : extents.y),
                    center.z + ((c & 4) == 0 ? -extents.z : extents.z));
                Vector3 p = m.MultiplyPoint3x4(corner);
                if (!any)
                {
                    result = new Bounds(p, Vector3.zero);
                    any = true;
                }
                else
                {
                    result.Encapsulate(p);
                }
            }
        }
    }
}
