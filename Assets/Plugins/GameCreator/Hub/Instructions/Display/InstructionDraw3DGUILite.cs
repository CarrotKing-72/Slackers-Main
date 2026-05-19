using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GameCreator.Runtime.Cameras;
using GameCreator.Runtime.Common;
using GameCreator.Runtime.VisualScripting;
using UnityEngine;

[Version(2, 2, 0)] // Version updated for alignment feature
[Title("Draw 3D GUI (Lite)")]
[Description("A lightweight instruction to draw a 2D label at a 3D world position, optionally following a target object.")]
[Category("Display/Draw 3D GUI (Lite)")]

[Parameter("Text", "The text to display. Supports rich text tags.")]
[Parameter("Text Color", "The color of the text.")]
[Parameter("Base Font Size", "The base font size at 1x scale.")]
[Parameter("Show Background", "If true, draws a semi-transparent background behind the text.")]
[Parameter("Background Color", "The color of the background.")]
[Parameter("Lifetime", "How long (in seconds) the label lasts. Set to 0.01 for one frame.")]
[Parameter("Target", "An optional target GameObject to follow. If set, World Position is ignored.")]
[Parameter("Target Offset", "A local-space offset from the Target's position.")]
[Parameter("World Position", "The 3D world position to draw the label at, if Target is not set.")]
[Parameter("Camera", "The camera to project from. If null, uses Camera.main.")]
[Parameter("Screen Offset", "Additional offset (in pixels) to apply to the projected screen position.")]
[Parameter("Near Distance", "The distance from the camera where the label is at maximum size.")]
[Parameter("Far Distance", "The distance from the camera where the label is at minimum size.")]
[Parameter("Min Scale", "The minimum scale factor when at or beyond the Far Distance.")]
[Parameter("Max Scale", "The maximum scale factor when at or closer than the Near Distance.")]
[Parameter("Hide When Behind Camera", "If true, the label is not drawn when the target is behind the camera.")]
[Parameter("Occlusion Test", "If true, the label is hidden when there is an obstacle between the camera and target.")]
[Parameter("Show Edge When Behind", "If true, the label is drawn at the nearest screen edge when behind the camera.")]
[Parameter("Occlusion Mask", "The layers considered as obstacles for occlusion tests.")]
[Parameter("Enable Shadow", "If true, draws a shadow behind the text.")]
[Parameter("Shadow Color", "The color of the text shadow.")]
[Parameter("Shadow Offset", "The pixel offset of the text shadow.")]
[Parameter("Enable Outline", "If true, draws an outline around the text (takes precedence over shadow).")]
[Parameter("Outline Color", "The color of the text outline.")]
[Parameter("Outline Size", "The thickness of the text outline, in pixels.")]
[Parameter("Clamp To Screen", "Keep label on-screen with margins.")]
[Parameter("Clamp Margin", "The margin (in pixels) from the screen edges.")]

[Image(typeof(IconString), ColorTheme.Type.Yellow)]
[Keywords("3D, Label, Text, World, Position, Follow, Target, GUI, Outline")]
[Serializable]
public class InstructionDraw3DGUILite : Instruction
{

    [Header("Content")]
    [SerializeField] private PropertyGetString m_Text = new PropertyGetString("Hello World!");
    [SerializeField] private PropertyGetColor m_TextColor = new PropertyGetColor(Color.green);
    [SerializeField] private PropertyGetInteger m_BaseFontSize = new PropertyGetInteger(12);
    [SerializeField] private bool m_ShowBackground = false;
    [SerializeField] private Color m_BackColor = new Color(0, 0, 0, 0.4f);
    [SerializeField] private PropertyGetDecimal m_Lifetime = new PropertyGetDecimal(3f);

    [Header("Target")]
    [SerializeField] private PropertyGetGameObject m_Target = GetGameObjectSelf.Create();
    [SerializeField] private Vector3 m_TargetOffset = Vector3.zero;
    [SerializeField] private PropertyGetPosition m_WorldPosition = new PropertyGetPosition();

    [Header("Projection")]
    [Tooltip("If null, uses Camera.main")]
    [SerializeField] private PropertyGetGameObject m_Camera = GetGameObjectCameraMain.Create;
    [SerializeField] private Vector2 m_ScreenOffset = Vector2.zero;

    [Header("Distance Attenuation")]
    [SerializeField] private PropertyGetDecimal m_NearDistance = new PropertyGetDecimal(2f);
    [SerializeField] private PropertyGetDecimal m_FarDistance = new PropertyGetDecimal(30f);
    [SerializeField] private PropertyGetDecimal m_MinScale = new PropertyGetDecimal(0.6f);
    [SerializeField] private PropertyGetDecimal m_MaxScale = new PropertyGetDecimal(1.8f);

    [Header("Visibility")]
    [SerializeField] private bool m_HideWhenBehindCamera = true;
    [SerializeField] private bool m_OcclusionTest = false;
    [SerializeField] private bool m_ShowEdgeWhenBehind = true;
    [SerializeField] private LayerMask m_OcclusionMask = ~0;

    [Header("Visibility Smoothing")]
    [SerializeField] private bool m_SmoothVisibility = true;
    [SerializeField] private float m_FadeInDuration = 0.15f;
    [SerializeField] private float m_FadeOutDuration = 0.15f;

    [Header("On-Screen Clamping")]
    [SerializeField] private bool m_ClampToScreen = true;
    [SerializeField] private int m_ClampMargin = 72;

    [Header("Text Effects")]
    [SerializeField] private bool m_EnableShadow = true;
    [SerializeField] private Color m_ShadowColor = new Color(0, 0, 0, 0.7f);
    [SerializeField] private Vector2 m_ShadowOffset = new Vector2(1.5f, 1.5f);
    [SerializeField] private bool m_EnableOutline = false;
    [SerializeField] private Color m_OutlineColor = Color.black;
    [Range(1, 6)] [SerializeField] private int m_OutlineSize = 2;

    public override string Title => $"Light 3D Label: {this.m_Text}";

    protected override Task Run(Args args)
    {
        if (Gui3DLabelControllerLight.Instance == null)
        {
            var go = new GameObject("GUI 3D Label Controller (Light singleton)");
            go.AddComponent<Gui3DLabelControllerLight>();
        }

        Camera cam = null;
        var camGo = this.m_Camera.Get(args);
        if (camGo != null) cam = camGo.Get<Camera>();
        if (cam == null) cam = Camera.main;

        Transform follow = this.m_Target.Get(args) != null ? this.m_Target.Get(args).transform : null;
        Vector3 worldPos = follow != null ? follow.position : this.m_WorldPosition.Get(args);

        Gui3DLabelControllerLight.Instance.Add(new Gui3DLabelControllerLight.LabelSpec
        {
            Text = this.m_Text.Get(args) ?? string.Empty,
            Color = this.m_TextColor.Get(args),
            BaseFontSize = Mathf.Max(8, (int)this.m_BaseFontSize.Get(args)),
            ShowBackground = this.m_ShowBackground,
            Background = this.m_BackColor,
            LifetimeEnd = UnityEngine.Time.unscaledTime + Mathf.Max(0.01f, (float)this.m_Lifetime.Get(args)),
            Heartbeat = ((float)this.m_Lifetime.Get(args)) <= 0f,
            
            Follow = follow,
            WorldPosition = worldPos,
            TargetOffset = this.m_TargetOffset,
            Camera = cam,
            ScreenOffset = m_ScreenOffset,

            NearDistance = Mathf.Max(0.01f, (float)this.m_NearDistance.Get(args)),
            FarDistance = Mathf.Max(0.02f, (float)this.m_FarDistance.Get(args)),
            MinScale = Mathf.Max(0.01f, (float)this.m_MinScale.Get(args)),
            MaxScale = Mathf.Max(0.01f, (float)this.m_MaxScale.Get(args)),

            HideBehindCamera = m_HideWhenBehindCamera,
            OcclusionTest = m_OcclusionTest,
            ShowEdgeWhenBehind = m_ShowEdgeWhenBehind,
            OcclusionMask = m_OcclusionMask,

            SmoothVisibility = m_SmoothVisibility,
            FadeInDuration = Mathf.Max(0.0001f, m_FadeInDuration),
            FadeOutDuration = Mathf.Max(0.0001f, m_FadeOutDuration),

            ClampToScreen = m_ClampToScreen,
            ClampMargin = m_ClampMargin,

            EnableShadow = m_EnableShadow,
            ShadowColor = m_ShadowColor,
            ShadowOffset = m_ShadowOffset,
            
            EnableOutline = m_EnableOutline,
            OutlineColor = m_OutlineColor,
            OutlineSize = m_OutlineSize
        });

        return DefaultResult;
    }
}

public class Gui3DLabelControllerLight : MonoBehaviour
{
    public static Gui3DLabelControllerLight Instance { get; private set; }

    [Serializable]
    public class LabelSpec
    {
        // ----- Content -----
        public string Text;
        public Color Color = Color.white;
        public int BaseFontSize = 24;
        public bool ShowBackground = true;
        public Color Background = new Color(0, 0, 0, 0.4f);
        public float LifetimeEnd;

        // ----- Target / Projection -----
        public Transform Follow;
        public Vector3 WorldPosition;
        public Vector3 TargetOffset;
        public Camera Camera;
        public Vector2 ScreenOffset;

        // ----- Attenuation -----
        public float NearDistance = 2f;
        public float FarDistance = 30f;
        public float MinScale = 0.6f;
        public float MaxScale = 1.8f;

        // ----- Visibility -----
        public bool HideBehindCamera = true;
        public bool OcclusionTest = false;
        public bool ShowEdgeWhenBehind = true;
        public LayerMask OcclusionMask = ~0;

        // ----- Visibility Smoothing -----
        public bool SmoothVisibility = true;
        public float FadeInDuration = 0.15f;
        public float FadeOutDuration = 0.15f;
        internal float Visible01;
        internal float TargetVisible01;
        internal bool PendingExpire;

        // ----- Clamping -----
        public bool ClampToScreen = true;
        public int ClampMargin = 12;

        // ----- Text Effects -----
        public bool EnableShadow = true;
        public Color ShadowColor = new Color(0, 0, 0, 0.7f);
        public Vector2 ShadowOffset = new Vector2(1.5f, 1.5f);
        public bool EnableOutline = false;
        public Color OutlineColor = Color.black;
        public int OutlineSize = 2;


        // ----- Runtime -----
        public bool Heartbeat = false;
        internal bool SeenThisFrame = false;
    }

    private readonly List<LabelSpec> _labels = new List<LabelSpec>();
    private GUIStyle _style;
    private static Texture2D _pixel;

    private static Color MulA(Color c, float a) { c.a *= Mathf.Clamp01(a); return c; }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        _style = new GUIStyle { alignment = TextAnchor.MiddleCenter, wordWrap = false, richText = true }; // Rich text for colors etc.

        if (_pixel == null)
        {
            _pixel = new Texture2D(1, 1, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            _pixel.SetPixel(0, 0, Color.white);
            _pixel.Apply();
        }
    }

    public void Add(LabelSpec spec)
    {
        // --- FIXED: Find existing label to update, preventing fade-in on move ---
        int idx = -1;
        for (int i = 0; i < _labels.Count; i++)
        {
            var L = _labels[i];
            bool cameraMatch = L.Camera == (spec.Camera ?? Camera.main);
            
            bool positionMatch;
            if (spec.Follow != null) // If following, the Transform is the unique ID
            {
                positionMatch = L.Follow == spec.Follow;
            }
            else // Not following, use world position as the ID
            {
                positionMatch = L.Follow == null && (L.WorldPosition - spec.WorldPosition).sqrMagnitude <= 0.000001f;
            }

            if (cameraMatch && positionMatch)
            {
                idx = i;
                break;
            }
        }

        if (idx >= 0) // Found existing label, update it
        {
            var L = _labels[idx];
            spec.Visible01 = L.Visible01; // Preserve current fade alpha
            spec.TargetVisible01 = 1f;
            spec.PendingExpire = false;
            spec.Heartbeat = spec.Heartbeat || L.Heartbeat;
            spec.SeenThisFrame = true;

            if (!spec.Heartbeat)
                spec.LifetimeEnd = Mathf.Max(spec.LifetimeEnd, Time.unscaledTime + 0.0001f);

            _labels[idx] = spec;
        }
        else // New label, add it
        {
            spec.Visible01 = spec.SmoothVisibility ? 0f : 1f;
            spec.TargetVisible01 = 1f;
            spec.PendingExpire = false;
            spec.SeenThisFrame = true;
            _labels.Add(spec);
        }

        enabled = true;
    }

    private void Update()
    {
        float dt = Time.unscaledDeltaTime;

        for (int i = _labels.Count - 1; i >= 0; i--)
        {
            var l = _labels[i];

            // --- MOVED: All visibility logic is now handled here in Update ---
            var cam = l.Camera != null ? l.Camera : Camera.main;
            bool isVisible = cam != null;
            
            if (isVisible)
            {
                var origin = l.Follow ? l.Follow.TransformPoint(l.TargetOffset) : l.WorldPosition;
                Vector3 sp = cam.WorldToScreenPoint(origin);
                bool isBehind = sp.z < 0f;

                // Condition 1: Is it behind the camera?
                if (isBehind && l.HideBehindCamera && !l.ShowEdgeWhenBehind)
                {
                    isVisible = false;
                }

                // Condition 2: Is it occluded? (Only check if currently visible, not behind, and test is enabled)
                if (isVisible && !isBehind && l.OcclusionTest)
                {
                    Vector3 dir = origin - cam.transform.position;
                    float rayLen = dir.magnitude - 0.05f; // Offset to avoid hitting the target itself
                    if (rayLen > 0.01f && Physics.Raycast(cam.transform.position, dir.normalized, rayLen, l.OcclusionMask, QueryTriggerInteraction.Ignore))
                    {
                        isVisible = false;
                    }
                }
            }
            // --- End of visibility logic ---

            if (l.Heartbeat)
            {
                if (!l.SeenThisFrame) l.PendingExpire = true;
                else l.PendingExpire = false;
            }
            else if (!l.PendingExpire && Time.unscaledTime >= l.LifetimeEnd)
            {
                l.PendingExpire = true;
            }
            
            if (l.PendingExpire) l.TargetVisible01 = 0f;
            else l.TargetVisible01 = isVisible ? 1f : 0f; // Use calculated visibility


            if (l.SmoothVisibility)
            {
                float dur = (l.TargetVisible01 > l.Visible01) ? l.FadeInDuration : l.FadeOutDuration;
                l.Visible01 = Mathf.MoveTowards(l.Visible01, l.TargetVisible01, dt / Mathf.Max(0.0001f, dur));
            }
            else
            {
                l.Visible01 = l.TargetVisible01;
            }

            if (l.PendingExpire && l.Visible01 <= 0.0001f)
            {
                _labels.RemoveAt(i);
                continue;
            }

            l.SeenThisFrame = false;
        }

        if (_labels.Count == 0) enabled = false;
    }

    private void OnGUI()
    {
        if (_labels.Count == 0) return;

        foreach (var l in _labels)
        {
            var cam = l.Camera != null ? l.Camera : Camera.main;
            if (cam == null) continue;

            if (l.Visible01 <= 0.001f) continue;

            var origin = l.Follow ? l.Follow.TransformPoint(l.TargetOffset) : l.WorldPosition;
            Vector3 sp = cam.WorldToScreenPoint(origin);
            bool isBehind = sp.z < 0f;

            Vector2 guiPos;
            if (isBehind && l.ShowEdgeWhenBehind)
            {
                Vector3 camToTargetLocal = cam.transform.InverseTransformDirection(origin - cam.transform.position);
                const float FAR = 10000f;
                float offX = camToTargetLocal.x < 0f ? -FAR : Screen.width + FAR;
                float offY = camToTargetLocal.y > 0f ? -FAR : Screen.height + FAR;
                guiPos = new Vector2(offX, offY) + l.ScreenOffset;
            }
            else
            {
                guiPos = new Vector2(sp.x, Screen.height - sp.y) + l.ScreenOffset;
            }

            float dist = Vector3.Distance(cam.transform.position, origin);
            float t = Mathf.InverseLerp(l.FarDistance, l.NearDistance, dist);
            float scale = Mathf.Lerp(l.MinScale, l.MaxScale, Mathf.SmoothStep(0, 1, t));
            _style.fontSize = Mathf.Max(8, Mathf.RoundToInt(l.BaseFontSize * scale));
            
            Vector2 textSize = _style.CalcSize(new GUIContent(l.Text));

            // --- REVERTED: Reverted to center-alignment ---
            _style.alignment = TextAnchor.MiddleCenter;
            var rect = new Rect(guiPos.x - textSize.x / 2, guiPos.y - textSize.y / 2, textSize.x, textSize.y);


            if (l.ClampToScreen)
            {
                float m = l.ClampMargin;
                rect.x = Mathf.Clamp(rect.x, m, Screen.width - rect.width - m);
                rect.y = Mathf.Clamp(rect.y, m, Screen.height - rect.height - m);
            }

            if (l.ShowBackground)
            {
                var prev = GUI.color;
                GUI.color = MulA(l.Background, l.Visible01);
                GUI.DrawTexture(new Rect(rect.x - 6, rect.y - 3, rect.width + 12, rect.height + 6), _pixel);
                GUI.color = prev;
            }

            if (l.EnableOutline)
            {
                DrawOutline(rect, l.Text, _style, MulA(l.OutlineColor, l.Visible01), l.OutlineSize);
            }
            else if (l.EnableShadow)
            {
                DrawShadow(rect, l.Text, _style, MulA(l.ShadowColor, l.Visible01), l.ShadowOffset);
            }

            var prevColor = _style.normal.textColor;
            _style.normal.textColor = MulA(l.Color, l.Visible01);
            GUI.Label(rect, l.Text, _style);
            _style.normal.textColor = prevColor;
        }
    }
    
    private void DrawOutline(Rect rect, string text, GUIStyle style, Color color, int size)
    {
        var prev = style.normal.textColor;
        style.normal.textColor = color;
        
        for (int dx = -size; dx <= size; dx++)
        {
            for (int dy = -size; dy <= size; dy++)
            {
                if (dx == 0 && dy == 0) continue;
                if (Mathf.Abs(dx) + Mathf.Abs(dy) > size) continue; // Diamond-shaped outline for better look
                GUI.Label(new Rect(rect.x + dx, rect.y + dy, rect.width, rect.height), text, style);
            }
        }

        style.normal.textColor = prev;
    }

    private void DrawShadow(Rect rect, string text, GUIStyle style, Color color, Vector2 offset)
    {
        var prev = style.normal.textColor;
        style.normal.textColor = color;
        GUI.Label(new Rect(rect.x + offset.x, rect.y + offset.y, rect.width, rect.height), text, style);
        style.normal.textColor = prev;
    }
}


