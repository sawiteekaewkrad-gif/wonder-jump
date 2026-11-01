// Minimal stand-ins for the UnityEngine API used by Wonder Jump, so the C# can be compiled and the
// pure logic exercised OUTSIDE Unity (CI / no-editor environments).
// THIS IS NOT UNITY. Passing this check proves our C# is well-formed and internally consistent;
// it does NOT prove the Unity API signatures are right. Only the Unity Editor can prove that.
// Math types are real implementations so geometry tests give meaningful results.
using System;
using System.Collections;
using System.Collections.Generic;

namespace UnityEngine
{
    public class Object
    {
        private string objectName;
        public virtual string name { get { return objectName; } set { objectName = value; } }
        internal bool destroyed;
        public static void DestroyImmediate(Object o) { if (o != null) o.destroyed = true; }
        public static void Destroy(Object o) { DestroyImmediate(o); }
        public static implicit operator bool(Object o) { return !ReferenceEquals(o, null) && !o.destroyed; }
        public override string ToString() { return name; }
    }

    public class Component : Object
    {
        public GameObject gameObject { get; internal set; }
        // Unity: a component's name IS its GameObject's name.
        public override string name { get { return gameObject != null ? gameObject.name : base.name; } set { if (gameObject != null) gameObject.name = value; else base.name = value; } }
        public Transform transform { get { return gameObject.transform; } }
        public T GetComponent<T>() { return gameObject.GetComponent<T>(); }
        public T[] GetComponents<T>() { return gameObject.GetComponents<T>(); }
        public T[] GetComponentsInChildren<T>(bool includeInactive) { return gameObject.GetComponentsInChildren<T>(includeInactive); }
        public T GetComponentInParent<T>() { return gameObject.GetComponentInParent<T>(); }
    }

    public class Behaviour : Component { public bool enabled = true; }
    public class MonoBehaviour : Behaviour { }
    public class ScriptableObject : Object
    {
        public static T CreateInstance<T>() where T : ScriptableObject { return Activator.CreateInstance<T>(); }
    }

    public sealed class GameObject : Object
    {
        private readonly List<Component> components = new List<Component>();
        public Transform transform { get; private set; }
        public int layer { get; set; }
        public string tag { get; set; }
        public bool activeSelf { get; private set; }

        public GameObject() : this("New Game Object") { }
        public GameObject(string name, params Type[] types)
        {
            this.name = name;
            activeSelf = true;
            transform = new Transform();
            transform.gameObject = this;
            components.Add(transform);
            foreach (Type t in types) AddComponent(t);
        }

        public Component AddComponent(Type t)
        {
            var c = (Component)Activator.CreateInstance(t, true);
            c.gameObject = this;
            components.Add(c);
            return c;
        }
        public T AddComponent<T>() where T : Component { return (T)AddComponent(typeof(T)); }
        public T GetComponent<T>()
        {
            foreach (Component c in components) if (c is T && c) return (T)(object)c;
            return default(T);
        }
        public T[] GetComponents<T>()
        {
            var list = new List<T>();
            foreach (Component c in components) if (c is T && c) list.Add((T)(object)c);
            return list.ToArray();
        }
        public T[] GetComponentsInChildren<T>(bool includeInactive)
        {
            var list = new List<T>(GetComponents<T>());
            for (int i = 0; i < transform.childCount; i++) list.AddRange(transform.GetChild(i).gameObject.GetComponentsInChildren<T>(includeInactive));
            return list.ToArray();
        }
        public T GetComponentInParent<T>()
        {
            for (Transform t = transform; t != null; t = t.parent)
            {
                T c = t.gameObject.GetComponent<T>();
                if (c != null) return c;
            }
            return default(T);
        }
        public void SetActive(bool value) { activeSelf = value; }
        public static GameObject CreatePrimitive(PrimitiveType type)
        {
            var go = new GameObject(type.ToString(), typeof(MeshFilter), typeof(MeshRenderer));
            switch (type)
            {
                case PrimitiveType.Sphere: go.AddComponent<SphereCollider>(); break;
                case PrimitiveType.Capsule: go.AddComponent<CapsuleCollider>(); break;
                default: go.AddComponent<BoxCollider>(); break;
            }
            var mesh = new Mesh();
            mesh.bounds = type == PrimitiveType.Capsule || type == PrimitiveType.Cylinder
                ? new Bounds(Vector3.zero, new Vector3(1, 2, 1)) : new Bounds(Vector3.zero, Vector3.one);
            go.GetComponent<MeshFilter>().sharedMesh = mesh;
            return go;
        }
    }

    public class Transform : Component
    {
        private readonly List<Transform> children = new List<Transform>();
        public Transform parent { get; private set; }
        public Vector3 localPosition = Vector3.zero;
        public Quaternion localRotation = Quaternion.identity;
        public Vector3 localScale = Vector3.one;
        public Vector3 position { get { return localToWorldMatrix.MultiplyPoint3x4(Vector3.zero); } set { localPosition = value; } }
        public Quaternion rotation { get { return localRotation; } set { localRotation = value; } }
        public int childCount { get { return children.Count; } }
        public Transform GetChild(int i) { return children[i]; }
        public void SetParent(Transform p, bool worldPositionStays)
        {
            if (parent != null) parent.children.Remove(this);
            parent = p;
            if (p != null) p.children.Add(this);
        }
        public Transform Find(string n)
        {
            foreach (Transform c in children) if (c.gameObject.name == n) return c;
            return null;
        }
        public Matrix4x4 localToWorldMatrix
        {
            get
            {
                Matrix4x4 local = Matrix4x4.TRS(localPosition, localRotation, localScale);
                return parent == null ? local : parent.localToWorldMatrix * local;
            }
        }
        public Matrix4x4 worldToLocalMatrix { get { return localToWorldMatrix.inverse; } }
    }

    public struct Vector2
    {
        public float x, y;
        public Vector2(float x, float y) { this.x = x; this.y = y; }
    }

    public struct Vector3
    {
        public float x, y, z;
        public Vector3(float x, float y, float z) { this.x = x; this.y = y; this.z = z; }
        public static Vector3 zero { get { return new Vector3(0, 0, 0); } }
        public static Vector3 one { get { return new Vector3(1, 1, 1); } }
        public static Vector3 down { get { return new Vector3(0, -1, 0); } }
        public static Vector3 forward { get { return new Vector3(0, 0, 1); } }
        public float sqrMagnitude { get { return x * x + y * y + z * z; } }
        public float magnitude { get { return (float)Math.Sqrt(sqrMagnitude); } }
        public Vector3 normalized { get { float m = magnitude; return m > 1e-5f ? this / m : zero; } }
        public static Vector3 operator +(Vector3 a, Vector3 b) { return new Vector3(a.x + b.x, a.y + b.y, a.z + b.z); }
        public static Vector3 operator -(Vector3 a, Vector3 b) { return new Vector3(a.x - b.x, a.y - b.y, a.z - b.z); }
        public static Vector3 operator -(Vector3 a) { return new Vector3(-a.x, -a.y, -a.z); }
        public static Vector3 operator *(Vector3 a, float d) { return new Vector3(a.x * d, a.y * d, a.z * d); }
        public static Vector3 operator *(float d, Vector3 a) { return a * d; }
        public static Vector3 operator /(Vector3 a, float d) { return new Vector3(a.x / d, a.y / d, a.z / d); }
        public static Vector3 Cross(Vector3 a, Vector3 b) { return new Vector3(a.y * b.z - a.z * b.y, a.z * b.x - a.x * b.z, a.x * b.y - a.y * b.x); }
        public static float Dot(Vector3 a, Vector3 b) { return a.x * b.x + a.y * b.y + a.z * b.z; }
        public static Vector3 Min(Vector3 a, Vector3 b) { return new Vector3(Math.Min(a.x, b.x), Math.Min(a.y, b.y), Math.Min(a.z, b.z)); }
        public static Vector3 Max(Vector3 a, Vector3 b) { return new Vector3(Math.Max(a.x, b.x), Math.Max(a.y, b.y), Math.Max(a.z, b.z)); }
        public override string ToString() { return "(" + x + ", " + y + ", " + z + ")"; }
    }

    public struct Quaternion
    {
        public float x, y, z, w;
        public Quaternion(float x, float y, float z, float w) { this.x = x; this.y = y; this.z = z; this.w = w; }
        public static Quaternion identity { get { return new Quaternion(0, 0, 0, 1); } }
        public static Quaternion operator *(Quaternion a, Quaternion b)
        {
            return new Quaternion(
                a.w * b.x + a.x * b.w + a.y * b.z - a.z * b.y,
                a.w * b.y - a.x * b.z + a.y * b.w + a.z * b.x,
                a.w * b.z + a.x * b.y - a.y * b.x + a.z * b.w,
                a.w * b.w - a.x * b.x - a.y * b.y - a.z * b.z);
        }
        private static Quaternion Axis(float deg, float ax, float ay, float az)
        {
            double h = deg * Math.PI / 360.0; float s = (float)Math.Sin(h);
            return new Quaternion(ax * s, ay * s, az * s, (float)Math.Cos(h));
        }
        /// <summary>Unity order: Z, then X, then Y (q = qy * qx * qz).</summary>
        public static Quaternion Euler(float x, float y, float z) { return Axis(y, 0, 1, 0) * Axis(x, 1, 0, 0) * Axis(z, 0, 0, 1); }
        public static Quaternion Euler(Vector3 e) { return Euler(e.x, e.y, e.z); }
    }

    public struct Color
    {
        public float r, g, b, a;
        public Color(float r, float g, float b) : this(r, g, b, 1f) { }
        public Color(float r, float g, float b, float a) { this.r = r; this.g = g; this.b = b; this.a = a; }
        public static Color white { get { return new Color(1, 1, 1); } }
    }

    public struct Rect { public Rect(float x, float y, float w, float h) { } }

    public struct Bounds
    {
        public Vector3 center;
        public Vector3 extents;
        public Bounds(Vector3 center, Vector3 size) { this.center = center; extents = size * 0.5f; }
        public Vector3 size { get { return extents * 2f; } set { extents = value * 0.5f; } }
        public Vector3 min { get { return center - extents; } }
        public Vector3 max { get { return center + extents; } }
        public void Encapsulate(Vector3 p)
        {
            Vector3 mn = Vector3.Min(min, p), mx = Vector3.Max(max, p);
            center = (mn + mx) * 0.5f;
            extents = (mx - mn) * 0.5f;
        }
    }

    /// <summary>Affine 3x4 matrix (row-major m[r,c], column 3 = translation).</summary>
    public struct Matrix4x4
    {
        private float[] m;
        private float[] M { get { if (m == null) { m = new float[12]; m[0] = m[5] = m[10] = 1f; } return m; } }
        public static Matrix4x4 TRS(Vector3 t, Quaternion q, Vector3 s)
        {
            float x = q.x, y = q.y, z = q.z, w = q.w;
            float[] r =
            {
                1 - 2 * (y * y + z * z), 2 * (x * y - z * w), 2 * (x * z + y * w),
                2 * (x * y + z * w), 1 - 2 * (x * x + z * z), 2 * (y * z - x * w),
                2 * (x * z - y * w), 2 * (y * z + x * w), 1 - 2 * (x * x + y * y)
            };
            var res = new Matrix4x4 { m = new float[12] };
            for (int row = 0; row < 3; row++)
            {
                res.m[row * 4 + 0] = r[row * 3 + 0] * s.x;
                res.m[row * 4 + 1] = r[row * 3 + 1] * s.y;
                res.m[row * 4 + 2] = r[row * 3 + 2] * s.z;
            }
            res.m[3] = t.x; res.m[7] = t.y; res.m[11] = t.z;
            return res;
        }
        public static Matrix4x4 operator *(Matrix4x4 a, Matrix4x4 b)
        {
            float[] A = a.M, B = b.M; var res = new Matrix4x4 { m = new float[12] };
            for (int r = 0; r < 3; r++)
                for (int c = 0; c < 4; c++)
                    res.m[r * 4 + c] = A[r * 4] * B[c] + A[r * 4 + 1] * B[4 + c] + A[r * 4 + 2] * B[8 + c] + (c == 3 ? A[r * 4 + 3] : 0f);
            return res;
        }
        public Vector3 MultiplyPoint3x4(Vector3 p)
        {
            float[] A = M;
            return new Vector3(A[0] * p.x + A[1] * p.y + A[2] * p.z + A[3],
                               A[4] * p.x + A[5] * p.y + A[6] * p.z + A[7],
                               A[8] * p.x + A[9] * p.y + A[10] * p.z + A[11]);
        }
        public Matrix4x4 inverse
        {
            get
            {
                float[] A = M;
                float a = A[0], b = A[1], c = A[2], d = A[4], e = A[5], f = A[6], g = A[8], h = A[9], i = A[10];
                float det = a * (e * i - f * h) - b * (d * i - f * g) + c * (d * h - e * g);
                float k = 1f / det;
                var res = new Matrix4x4 { m = new float[12] };
                res.m[0] = (e * i - f * h) * k; res.m[1] = (c * h - b * i) * k; res.m[2] = (b * f - c * e) * k;
                res.m[4] = (f * g - d * i) * k; res.m[5] = (a * i - c * g) * k; res.m[6] = (c * d - a * f) * k;
                res.m[8] = (d * h - e * g) * k; res.m[9] = (b * g - a * h) * k; res.m[10] = (a * e - b * d) * k;
                for (int r = 0; r < 3; r++)
                    res.m[r * 4 + 3] = -(res.m[r * 4] * A[3] + res.m[r * 4 + 1] * A[7] + res.m[r * 4 + 2] * A[11]);
                return res;
            }
        }
    }

    public static class Mathf
    {
        public const float PI = (float)Math.PI;
        public static float Cos(float f) { return (float)Math.Cos(f); }
        public static float Sin(float f) { return (float)Math.Sin(f); }
        public static float Min(float a, float b) { return Math.Min(a, b); }
        public static float Max(float a, float b) { return Math.Max(a, b); }
        public static int Max(int a, int b) { return Math.Max(a, b); }
        public static float Abs(float f) { return Math.Abs(f); }
        public static float Lerp(float a, float b, float t) { return a + (b - a) * Math.Max(0f, Math.Min(1f, t)); }
        public static int RoundToInt(float f) { return (int)Math.Round(f); }
    }

    public sealed class Mesh : Object
    {
        public Vector3[] vertices { get; set; }
        public Vector3[] normals { get; set; }
        public Vector2[] uv { get; set; }
        public int[] triangles { get; set; }
        public Bounds bounds { get; set; }
        public int vertexCount { get { return vertices == null ? 0 : vertices.Length; } }
        public void Clear() { vertices = null; normals = null; uv = null; triangles = null; }
        public void RecalculateBounds()
        {
            if (vertices == null || vertices.Length == 0) { bounds = new Bounds(); return; }
            var b = new Bounds(vertices[0], Vector3.zero);
            foreach (Vector3 v in vertices) b.Encapsulate(v);
            bounds = b;
        }
    }

    public sealed class Shader : Object
    {
        public bool isSupported { get { return true; } }
        public static Shader Find(string n) { return new Shader { name = n }; }
    }

    public class Material : Object
    {
        public Shader shader { get; set; }
        public Color color { get; set; }
        public bool enableInstancing { get; set; }
        public Material(Shader s) { shader = s; }
        public bool HasProperty(string p) { return true; }
        public void SetColor(string p, Color c) { }
        public void SetFloat(string p, float f) { }
    }

    public class Renderer : Component
    {
        public Material sharedMaterial { get; set; }
        public Rendering.ShadowCastingMode shadowCastingMode { get; set; }
        public bool receiveShadows { get; set; }
    }
    public sealed class MeshRenderer : Renderer { }
    public sealed class MeshFilter : Component { public Mesh sharedMesh { get; set; } }

    public class Collider : Component
    {
        public bool isTrigger { get; set; }
        public Rigidbody attachedRigidbody { get { return GetComponentInParent<Rigidbody>(); } }
    }
    public sealed class BoxCollider : Collider { public Vector3 center { get; set; } public Vector3 size { get; set; } }
    public sealed class SphereCollider : Collider { public Vector3 center { get; set; } public float radius { get; set; } }
    public sealed class CapsuleCollider : Collider
    {
        public Vector3 center { get; set; }
        public float radius { get; set; }
        public float height { get; set; }
        public int direction { get; set; }
    }

    public enum RigidbodyInterpolation { None, Interpolate, Extrapolate }
    public enum CollisionDetectionMode { Discrete, Continuous, ContinuousDynamic, ContinuousSpeculative }
    [Flags]
    public enum RigidbodyConstraints
    {
        None = 0, FreezePositionX = 2, FreezePositionY = 4, FreezePositionZ = 8,
        FreezeRotationX = 16, FreezeRotationY = 32, FreezeRotationZ = 64,
        FreezePosition = 14, FreezeRotation = 112, FreezeAll = 126
    }
    public sealed class Rigidbody : Component
    {
        public bool isKinematic { get; set; }
        public bool useGravity { get; set; }
        public RigidbodyInterpolation interpolation { get; set; }
        public CollisionDetectionMode collisionDetectionMode { get; set; }
        public RigidbodyConstraints constraints { get; set; }
        public void MovePosition(Vector3 p) { transform.localPosition = p; }
    }

    public enum PrimitiveType { Sphere = 0, Capsule = 1, Cylinder = 2, Cube = 3, Plane = 4, Quad = 5 }
    public enum ColorSpace { Uninitialized = -1, Gamma = 0, Linear = 1 }
    public enum ScreenOrientation { Portrait = 1, PortraitUpsideDown = 2, LandscapeLeft = 3, LandscapeRight = 4, AutoRotation = 5 }

    public struct LayerMask
    {
        public static readonly Dictionary<string, int> Layers = new Dictionary<string, int>();
        public static int NameToLayer(string n) { int v; return Layers.TryGetValue(n, out v) ? v : -1; }
    }

    public static class Debug
    {
        public static void Log(object m) { Console.WriteLine(m); }
        public static void LogWarning(object m) { Console.WriteLine("WARN " + m); }
        public static void LogWarning(object m, Object ctx) { LogWarning(m); }
        public static void LogError(object m) { Console.WriteLine("ERR " + m); }
    }

    public static class Application
    {
        public static string dataPath { get { return System.IO.Path.Combine(Environment.CurrentDirectory, "Assets"); } }
        public static bool isBatchMode { get { return true; } }
        public static string version { get { return "0.0.0"; } }
        public static string unityVersion { get { return "stub"; } }
        public static int targetFrameRate { get; set; }
    }
    public static class QualitySettings
    {
        public static int vSyncCount { get; set; }
        public static string[] names { get { return new[] { "Default" }; } }
        public static int GetQualityLevel() { return 0; }
        public static void SetQualityLevel(int i, bool applyExpensive) { }
        public static Rendering.RenderPipelineAsset renderPipeline { get; set; }
    }
    public static class SleepTimeout { public const int NeverSleep = -1; }
    public static class Screen
    {
        public static int width { get { return 1920; } }
        public static int height { get { return 1080; } }
        public static ScreenOrientation orientation { get { return ScreenOrientation.LandscapeLeft; } }
        public static int sleepTimeout { get; set; }
    }
    public static class Time { public static float unscaledDeltaTime { get { return 1f / 60f; } } public static float time { get { return 0f; } } }

    public enum CameraClearFlags { Skybox = 1, SolidColor = 2, Depth = 3, Nothing = 4 }
    public sealed class Camera : Behaviour
    {
        public CameraClearFlags clearFlags { get; set; }
        public Color backgroundColor { get; set; }
        public float fieldOfView { get; set; }
        public float nearClipPlane { get; set; }
        public float farClipPlane { get; set; }
    }
    public enum LightType { Spot = 0, Directional = 1, Point = 2 }
    public enum LightShadows { None, Hard, Soft }
    public sealed class Light : Behaviour
    {
        public LightType type { get; set; }
        public float intensity { get; set; }
        public LightShadows shadows { get; set; }
    }
    public static class RenderSettings
    {
        public static Rendering.AmbientMode ambientMode { get; set; }
        public static Color ambientLight { get; set; }
    }

    public class GUIStyleState { public Color textColor; }
    public class GUIStyle
    {
        public int fontSize;
        public GUIStyleState normal = new GUIStyleState();
        public GUIStyle() { }
        public GUIStyle(GUIStyle other) { }
    }
    public sealed class GUISkin { public GUIStyle label = new GUIStyle(); }
    public static class GUI
    {
        public static GUISkin skin = new GUISkin();
        public static void Label(Rect r, string text, GUIStyle style) { }
    }

    public static class JsonUtility
    {
        public static T FromJson<T>(string json) { throw new NotSupportedException("stub"); }
        public static string ToJson(object o, bool pretty) { throw new NotSupportedException("stub"); }
    }

    public class YieldInstruction { }
    public sealed class WaitForFixedUpdate : YieldInstruction { }

    // ---- attributes
    [AttributeUsage(AttributeTargets.Field)] public sealed class SerializeField : Attribute { }
    [AttributeUsage(AttributeTargets.Field)] public sealed class HeaderAttribute : Attribute { public HeaderAttribute(string h) { } }
    [AttributeUsage(AttributeTargets.Field)] public sealed class MinAttribute : Attribute { public MinAttribute(float m) { } }
    [AttributeUsage(AttributeTargets.Field)] public sealed class RangeAttribute : Attribute { public RangeAttribute(float a, float b) { } }
    [AttributeUsage(AttributeTargets.Class)] public sealed class DisallowMultipleComponent : Attribute { }
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)] public sealed class RequireComponent : Attribute { public RequireComponent(Type t) { } }
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class CreateAssetMenuAttribute : Attribute { public string menuName { get; set; } public string fileName { get; set; } }
    public enum RuntimeInitializeLoadType { AfterSceneLoad, BeforeSceneLoad, AfterAssembliesLoaded, BeforeSplashScreen, SubsystemRegistration }
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class RuntimeInitializeOnLoadMethodAttribute : Attribute { public RuntimeInitializeOnLoadMethodAttribute(RuntimeInitializeLoadType t) { } }
}

namespace UnityEngine.Rendering
{
    public enum ShadowCastingMode { Off, On, TwoSided, ShadowsOnly }
    public enum AmbientMode { Skybox = 0, Trilight = 1, Flat = 3, Custom = 4 }
    public enum GraphicsDeviceType { OpenGLES3 = 11, Vulkan = 21 }
    public abstract class RenderPipelineAsset : UnityEngine.ScriptableObject { }
    public static class GraphicsSettings
    {
        public static RenderPipelineAsset currentRenderPipeline { get { return null; } }
        public static RenderPipelineAsset defaultRenderPipeline { get; set; }
    }
}

namespace UnityEngine.SceneManagement
{
    public struct Scene { }
}

namespace UnityEngine.TestTools
{
    [AttributeUsage(AttributeTargets.Method)] public sealed class UnityTestAttribute : Attribute { }
}

namespace UnityEngine.Rendering.Universal
{
    public abstract class ScriptableRendererData : UnityEngine.ScriptableObject { }
    public class UniversalRendererData : ScriptableRendererData { }
    public class UniversalRenderPipelineAsset : UnityEngine.Rendering.RenderPipelineAsset
    {
        public bool supportsHDR { get; set; }
        public int msaaSampleCount { get; set; }
        public float renderScale { get; set; }
        public float shadowDistance { get; set; }
        public static UniversalRenderPipelineAsset Create(ScriptableRendererData rendererData = null) { return new UniversalRenderPipelineAsset(); }
    }
}
