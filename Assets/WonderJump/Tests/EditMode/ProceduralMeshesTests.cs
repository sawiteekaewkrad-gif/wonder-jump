using NUnit.Framework;
using UnityEngine;
using WonderJump.EditorTools.Generation;

namespace WonderJump.Tests.EditMode
{
    public class ProceduralMeshesTests
    {
        private static readonly ShapeKind[] Shapes = { ShapeKind.Cone, ShapeKind.Torus, ShapeKind.Octahedron };

        [Test]
        public void AllProceduralShapes_AreWellFormed()
        {
            foreach (ShapeKind shape in Shapes)
            {
                MeshData d = ProceduralMeshes.Build(shape);
                Assert.IsNotNull(d, shape.ToString());
                Assert.Greater(d.Vertices.Length, 0, shape + " vertices");
                Assert.AreEqual(d.Vertices.Length, d.Normals.Length, shape + " normals");
                Assert.AreEqual(d.Vertices.Length, d.Uvs.Length, shape + " uvs");
                Assert.AreEqual(0, d.Triangles.Length % 3, shape + " triangle count");
                foreach (int index in d.Triangles)
                    Assert.That(index, Is.InRange(0, d.Vertices.Length - 1), shape + " index range");
                foreach (Vector3 n in d.Normals)
                    Assert.AreEqual(1f, n.magnitude, 1e-3f, shape + " unit normals");
            }
        }

        [Test]
        public void AllProceduralShapes_FitUnitBox()
        {
            foreach (ShapeKind shape in Shapes)
            {
                MeshData d = ProceduralMeshes.Build(shape);
                foreach (Vector3 v in d.Vertices)
                {
                    Assert.That(v.x, Is.InRange(-0.5001f, 0.5001f), shape + " x");
                    Assert.That(v.y, Is.InRange(-0.5001f, 0.5001f), shape + " y");
                    Assert.That(v.z, Is.InRange(-0.5001f, 0.5001f), shape + " z");
                }
            }
        }

        [Test]
        public void AllProceduralShapes_FaceOutward_UnityWinding()
        {
            foreach (ShapeKind shape in Shapes)
            {
                MeshData d = ProceduralMeshes.Build(shape);
                for (int t = 0; t < d.Triangles.Length; t += 3)
                {
                    int a = d.Triangles[t], b = d.Triangles[t + 1], c = d.Triangles[t + 2];
                    Vector3 face = Vector3.Cross(d.Vertices[b] - d.Vertices[a], d.Vertices[c] - d.Vertices[a]);
                    if (face.sqrMagnitude < 1e-12f) continue; // degenerate apex slivers are harmless
                    Vector3 avg = d.Normals[a] + d.Normals[b] + d.Normals[c];
                    Assert.Greater(Vector3.Dot(face, avg), 0f, shape + " triangle " + (t / 3) + " is inward-facing");
                }
            }
        }

        [Test]
        public void BuiltInShapes_AreNotProcedural()
        {
            Assert.IsNull(ProceduralMeshes.Build(ShapeKind.Sphere));
            Assert.IsFalse(ProceduralMeshes.IsProcedural(ShapeKind.Cube));
            Assert.AreEqual(PrimitiveType.Capsule, PrefabBuilder.ToPrimitive(ShapeKind.Capsule));
        }
    }
}
