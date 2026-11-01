using UnityEngine;

namespace WonderJump.Core
{
    /// <summary>
    /// Phase 0 bootstrap: sets the mobile frame-rate target and shows a small diagnostics label
    /// (version, Unity version, FPS, resolution, orientation) to confirm a device build runs.
    /// </summary>
    public sealed class BootstrapDiagnostics : MonoBehaviour
    {
        [SerializeField] private int targetFrameRate = 60;
        [SerializeField] private bool showOverlay = true;

        private float smoothedDelta = 1f / 60f;
        private GUIStyle style;

        private void Awake()
        {
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = targetFrameRate;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
        }

        private void Update()
        {
            smoothedDelta = Mathf.Lerp(smoothedDelta, Time.unscaledDeltaTime, 0.05f);
        }

        private void OnGUI()
        {
            if (!showOverlay) return;
            if (style == null)
            {
                style = new GUIStyle(GUI.skin.label);
                style.fontSize = Mathf.Max(14, Screen.height / 30);
                style.normal.textColor = Color.white;
            }

            float fps = smoothedDelta > 0f ? 1f / smoothedDelta : 0f;
            string text = "Wonder Jump - Adventure Worlds  v" + Application.version +
                          "\nUnity " + Application.unityVersion +
                          "\n" + Mathf.RoundToInt(fps) + " FPS   " + Screen.width + "x" + Screen.height +
                          "   " + Screen.orientation;
            GUI.Label(new Rect(16, 16, Screen.width - 32, Screen.height / 3f), text, style);
        }
    }
}
