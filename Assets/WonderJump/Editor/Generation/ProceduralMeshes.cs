using UnityEngine;

namespace WonderJump.EditorTools.Generation
{
    /// <summary>Plain mesh arrays, built without touching UnityEngine.Mesh so the maths is unit-testable.</summary>
    public sealed class MeshData
    {
        public Vector3[] Vertices;
        public Vector3[] Normals;
        public Vector2[] Uvs;
        public int[] Triangles;
    }

    /// <summary>
    /// Replacements for the primitives that do not exist in Unity's PrimitiveType enum
    /// (Asset Pack v1 used PrimitiveType.Cone / Torus / Octahedron, which do not compile).
    ///
    /// All shapes fit a unit box centred on the origin (like Unity's Sphere/Cube), so the scale
    /// values from v1 keep the same visual size. Winding follows Unity's convention:
    /// a front face satisfies cross(b - a, c - a) pointing outward (clockwise seen from outside).
    /// </summary>
    public static class ProceduralMeshes
    {
        public const string ConeName = "WJ_Cone";
        public const string TorusName = "WJ_Torus";
        public const string OctahedronName = "WJ_Octahedron";

        /// <summary>Bump when any builder changes so existing mesh assets are regenerated.</summary>
        public const int Version = 1;

        public static MeshData Build(ShapeKind shape)
        {
            switch (shape)
            {
                case ShapeKind.Cone: return Cone(24);
                case ShapeKind.Torus: return Torus(32, 12, 0.375f, 0.125f);
                case ShapeKind.Octahedron: return Octahedron();
                default: return null;
            }
        }

        public static string AssetName(ShapeKind shape)
        {
            switch (shape)
            {
                case ShapeKind.Cone: return ConeName;
                case ShapeKind.Torus: return TorusName;
                case ShapeKind.Octahedron: return OctahedronName;
                default: return null;
            }
        }

        public static bool IsProcedural(ShapeKind shape)
        {
            return shape == ShapeKind.Cone || shape == ShapeKind.Torus || shape == ShapeKind.Octahedron;
        }

        /// <summary>Cone: base radius 0.5 at y=-0.5, apex at y=+0.5, smooth sides, flat base.</summary>
        public static MeshData Cone(int segments)
        {
            if (segments < 3) segments = 3;
            const float r = 0.5f;
            const float h = 1f;
            const float half = h * 0.5f;

            // side: per segment one apex vertex + ring of (segments + 1) base vertices (UV seam)
            // cap:  1 centre + (segments + 1) ring vertices
            int sideRing = segments + 1;
            int vertexCount = segments + sideRing + 1 + sideRing;
            var v = new Vector3[vertexCount];
            var n = new Vector3[vertexCount];
            var uv = new Vector2[vertexCount];
            var t = new int[segments * 3 * 2];

            float slope = r / h; // normal y component relative to radial
            int apexStart = 0;
            int baseStart = segments;
            for (int i = 0; i < sideRing; i++)
            {
                float a = i / (float)segments * Mathf.PI * 2f;
                float c = Mathf.Cos(a), s = Mathf.Sin(a);
                v[baseStart + i] = new Vector3(c * r, -half, s * r);
                n[baseStart + i] = new Vector3(c, slope, s).normalized;
                uv[baseStart + i] = new Vector2(i / (float)segments, 0f);
            }
            for (int i = 0; i < segments; i++)
            {
                float a = (i + 0.5f) / segments * Mathf.PI * 2f;
                v[apexStart + i] = new Vector3(0f, half, 0f);
                n[apexStart + i] = new Vector3(Mathf.Cos(a), slope, Mathf.Sin(a)).normalized;
                uv[apexStart + i] = new Vector2((i + 0.5f) / segments, 1f);
            }

            int capCenter = baseStart + sideRing;
            int capStart = capCenter + 1;
            v[capCenter] = new Vector3(0f, -half, 0f);
            n[capCenter] = Vector3.down;
            uv[capCenter] = new Vector2(0.5f, 0.5f);
            for (int i = 0; i < sideRing; i++)
            {
                float a = i / (float)segments * Mathf.PI * 2f;
                float c = Mathf.Cos(a), s = Mathf.Sin(a);
                v[capStart + i] = new Vector3(c * r, -half, s * r);
                n[capStart + i] = Vector3.down;
                uv[capStart + i] = new Vector2(0.5f + c * 0.5f, 0.5f + s * 0.5f);
            }

            int k = 0;
            for (int i = 0; i < segments; i++)
            {
                // side (apex, next base, base) -> outward
                t[k++] = apexStart + i;
                t[k++] = baseStart + i + 1;
                t[k++] = baseStart + i;
                // cap (centre, ring i, ring i+1) -> faces -Y
                t[k++] = capCenter;
                t[k++] = capStart + i;
                t[k++] = capStart + i + 1;
            }

            return new MeshData { Vertices = v, Normals = n, Uvs = uv, Triangles = t };
        }

        /// <summary>Torus in the XY plane (ring faces the camera, axis = Z). Outer diameter = 2 * (R + r).</summary>
        public static MeshData Torus(int majorSegments, int minorSegments, float majorRadius, float minorRadius)
        {
            if (majorSegments < 3) majorSegments = 3;
            if (minorSegments < 3) minorSegments = 3;
            int cols = majorSegments + 1;
            int rows = minorSegments + 1;
            var v = new Vector3[cols * rows];
            var n = new Vector3[cols * rows];
            var uv = new Vector2[cols * rows];
            var t = new int[majorSegments * minorSegments * 6];

            for (int i = 0; i < cols; i++)
            {
                float u = i / (float)majorSegments * Mathf.PI * 2f;
                var radial = new Vector3(Mathf.Cos(u), Mathf.Sin(u), 0f);
                Vector3 center = radial * majorRadius;
                for (int j = 0; j < rows; j++)
                {
                    float w = j / (float)minorSegments * Mathf.PI * 2f;
                    Vector3 normal = radial * Mathf.Cos(w) + Vector3.forward * Mathf.Sin(w);
                    int idx = i * rows + j;
                    v[idx] = center + normal * minorRadius;
                    n[idx] = normal;
                    uv[idx] = new Vector2(i / (float)majorSegments, j / (float)minorSegments);
                }
            }

            int k = 0;
            for (int i = 0; i < majorSegments; i++)
            {
                for (int j = 0; j < minorSegments; j++)
                {
                    int a = i * rows + j;
                    int b = (i + 1) * rows + j;
                    int c = i * rows + j + 1;
                    int d = (i + 1) * rows + j + 1;
                    t[k++] = a; t[k++] = b; t[k++] = c;
                    t[k++] = b; t[k++] = d; t[k++] = c;
                }
            }

            return new MeshData { Vertices = v, Normals = n, Uvs = uv, Triangles = t };
        }

        /// <summary>Flat-shaded octahedron with vertices at ±0.5 on each axis (24 vertices, 8 faces).</summary>
        public static MeshData Octahedron()
        {
            var v = new Vector3[24];
            var n = new Vector3[24];
            var uv = new Vector2[24];
            var t = new int[24];
            int k = 0;
            for (int face = 0; face < 8; face++)
            {
                float sx = (face & 1) == 0 ? 1f : -1f;
                float sy = (face & 2) == 0 ? 1f : -1f;
                float sz = (face & 4) == 0 ? 1f : -1f;
                var px = new Vector3(sx * 0.5f, 0f, 0f);
                var py = new Vector3(0f, sy * 0.5f, 0f);
                var pz = new Vector3(0f, 0f, sz * 0.5f);
                Vector3 normal = new Vector3(sx, sy, sz).normalized;

                bool keep = sx * sy * sz > 0f; // odd number of mirrored axes flips the winding
                int baseIndex = face * 3;
                v[baseIndex] = px;
                v[baseIndex + 1] = keep ? py : pz;
                v[baseIndex + 2] = keep ? pz : py;
                for (int i = 0; i < 3; i++) n[baseIndex + i] = normal;
                uv[baseIndex] = new Vector2(0f, 0f);
                uv[baseIndex + 1] = new Vector2(1f, 0f);
                uv[baseIndex + 2] = new Vector2(0.5f, 1f);
                t[k++] = baseIndex;
                t[k++] = baseIndex + 1;
                t[k++] = baseIndex + 2;
            }
            return new MeshData { Vertices = v, Normals = n, Uvs = uv, Triangles = t };
        }

        /// <summary>Copies data into a Mesh (new or existing, keeping the asset's GUID).</summary>
        public static void WriteTo(Mesh mesh, MeshData data, string name)
        {
            mesh.Clear();
            mesh.name = name;
            mesh.vertices = data.Vertices;
            mesh.normals = data.Normals;
            mesh.uv = data.Uvs;
            mesh.triangles = data.Triangles;
            mesh.RecalculateBounds();
        }
    }
}
