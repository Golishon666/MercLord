using MercLord.Game.Configs;
using TMPro;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MercLord.Global.Rendering
{
    [ExecuteAlways]
    public sealed class GlobalMapSceneBootstrap : MonoBehaviour
    {
        private const string GlobalMapUiName = "Global Map UI";
        private const string CellTooltipName = "Cell Tooltip Prefab";
        private const string TooltipTitleName = "Title";
        private const string TooltipBodyName = "Body";
        private const string DateLabelName = "Date Label";

        [SerializeField] private ConfigDatabase configDatabase;
        [SerializeField] private int targetCellCount = GlobalGenerationConfig.DefaultTargetCellCount;
        [SerializeField] private bool generateOnStart;
        [SerializeField] private bool showEditorPreview;
        [SerializeField] private int editorPreviewSeed = GlobalGenerationConfig.DefaultSeed;

        private bool editorPreviewGenerated;

        private void Awake()
        {
            SetupScene();
        }

        private void OnEnable()
        {
            SetupScene();

            if (!Application.isPlaying && showEditorPreview && !editorPreviewGenerated)
            {
                GenerateEditorPreview();
            }
        }

        private void SetupScene()
        {
            gameObject.name = "GlobalMap";
            var configuredCellCount = configDatabase?.GlobalGeneration != null
                ? configDatabase.GlobalGeneration.TargetCellCount
                : targetCellCount;
            targetCellCount = Mathf.Max(GlobalGenerationConfig.MinimumTargetCellCount, configuredCellCount);

            var renderer = GetComponent<ProceduralGlobalMapRenderer>();
            if (renderer == null)
            {
                renderer = gameObject.AddComponent<ProceduralGlobalMapRenderer>();
            }

            renderer.Configure(configDatabase);

            var debugController = GetComponent<GlobalMapDebugController>();
            if (debugController == null)
            {
                debugController = gameObject.AddComponent<GlobalMapDebugController>();
            }

            debugController.Configure(renderer, configDatabase, targetCellCount, generateOnStart);

            var tooltipView = EnsureTooltipView();
            var cellInfoController = GetComponent<GlobalMapCellInfoController>();
            if (cellInfoController == null)
            {
                cellInfoController = gameObject.AddComponent<GlobalMapCellInfoController>();
            }

            cellInfoController.Configure(renderer, debugController, configDatabase, tooltipView);

            var cameraTarget = EnsureCameraTarget();
            var orbitalFollow = EnsureCameraRig(cameraTarget);
            var cameraController = GetComponent<GlobalMapCameraController>();
            if (cameraController == null)
            {
                cameraController = gameObject.AddComponent<GlobalMapCameraController>();
            }

            cameraController.Configure(orbitalFollow, debugController, renderer);
            EnsureMainLight();
            EnsureEventSystem();
        }

        private void GenerateEditorPreview()
        {
            var renderer = GetComponent<ProceduralGlobalMapRenderer>();
            var debugController = GetComponent<GlobalMapDebugController>();
            if (renderer == null || debugController == null)
            {
                return;
            }

            debugController.Configure(renderer, configDatabase, targetCellCount, generateOnStart);
            debugController.Generate(editorPreviewSeed);
            editorPreviewGenerated = true;
        }

        private Transform EnsureCameraTarget()
        {
            var existing = transform.Find("Camera Target");
            if (existing != null)
            {
                return existing;
            }

            var target = new GameObject("Camera Target");
            target.transform.SetParent(transform, false);
            target.transform.localPosition = Vector3.zero;
            return target.transform;
        }

        private GlobalMapCellTooltipView EnsureTooltipView()
        {
            var canvas = EnsureUiCanvas();
            var dateLabel = EnsureDateLabel(canvas.transform);
            var tooltipRoot = EnsureTooltipRoot(canvas.transform);
            var title = EnsureTooltipText(
                tooltipRoot,
                TooltipTitleName,
                16,
                FontStyles.Bold,
                new Color(0.93f, 0.95f, 0.98f, 1f));
            var body = EnsureTooltipText(
                tooltipRoot,
                TooltipBodyName,
                13,
                FontStyles.Normal,
                new Color(0.82f, 0.86f, 0.90f, 1f));
            var tooltipView = tooltipRoot.GetComponent<GlobalMapCellTooltipView>();
            if (tooltipView == null)
            {
                tooltipView = tooltipRoot.gameObject.AddComponent<GlobalMapCellTooltipView>();
            }

            tooltipView.Configure(canvas, tooltipRoot, title, body, dateLabel);
            return tooltipView;
        }

        private Canvas EnsureUiCanvas()
        {
            var existing = transform.Find(GlobalMapUiName);
            GameObject canvasObject;
            if (existing != null)
            {
                canvasObject = existing.gameObject;
            }
            else
            {
                canvasObject = new GameObject(GlobalMapUiName);
                canvasObject.transform.SetParent(transform, false);
                SetEditorDirty(canvasObject);
            }

            var canvas = GetOrAddComponent<Canvas>(canvasObject);
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            var scaler = GetOrAddComponent<CanvasScaler>(canvasObject);
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            GetOrAddComponent<GraphicRaycaster>(canvasObject);
            SetEditorDirty(canvasObject);
            return canvas;
        }

        private static TextMeshProUGUI EnsureDateLabel(Transform canvasTransform)
        {
            var rect = EnsureUiRect(DateLabelName, canvasTransform);
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(16f, -16f);
            rect.sizeDelta = new Vector2(190f, 34f);

            var text = GetOrAddTextMeshPro(rect.gameObject);
            ApplyTextStyle(text, 14, FontStyles.Bold, Color.white, TextAlignmentOptions.MidlineLeft);
            text.text = "\u0414\u0430\u0442\u0430: \u0434\u0435\u043d\u044c 0";
            return text;
        }

        private static RectTransform EnsureTooltipRoot(Transform canvasTransform)
        {
            var rect = EnsureUiRect(CellTooltipName, canvasTransform);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.zero;
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(24f, 180f);
            rect.sizeDelta = new Vector2(250f, 134f);

            var image = GetOrAddComponent<Image>(rect.gameObject);
            image.color = new Color(0.04f, 0.05f, 0.06f, 0.88f);
            image.raycastTarget = false;

            var layout = GetOrAddComponent<VerticalLayoutGroup>(rect.gameObject);
            layout.padding = new RectOffset(12, 12, 10, 10);
            layout.spacing = 6f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            rect.gameObject.SetActive(false);
            return rect;
        }

        private static TextMeshProUGUI EnsureTooltipText(
            RectTransform tooltipRoot,
            string objectName,
            int fontSize,
            FontStyles fontStyle,
            Color color)
        {
            var rect = EnsureUiRect(objectName, tooltipRoot);
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.sizeDelta = objectName == TooltipTitleName
                ? new Vector2(0f, 24f)
                : new Vector2(0f, 78f);

            var text = GetOrAddTextMeshPro(rect.gameObject);
            ApplyTextStyle(text, fontSize, fontStyle, color, TextAlignmentOptions.TopLeft);
            text.textWrappingMode = TextWrappingModes.Normal;
            text.overflowMode = TextOverflowModes.Overflow;
            text.raycastTarget = false;
            return text;
        }

        private static RectTransform EnsureUiRect(string objectName, Transform parent)
        {
            var existing = parent.Find(objectName);
            GameObject gameObject;
            if (existing != null)
            {
                gameObject = existing.gameObject;
            }
            else
            {
                gameObject = new GameObject(objectName);
                gameObject.transform.SetParent(parent, false);
                SetEditorDirty(gameObject);
            }

            var rect = gameObject.GetComponent<RectTransform>();
            if (rect == null)
            {
                rect = gameObject.AddComponent<RectTransform>();
            }

            SetEditorDirty(gameObject);
            return rect;
        }

        private static void ApplyTextStyle(
            TextMeshProUGUI text,
            int fontSize,
            FontStyles fontStyle,
            Color color,
            TextAlignmentOptions alignment)
        {
            text.fontSize = fontSize;
            text.fontStyle = fontStyle;
            text.alignment = alignment;
            text.color = color;
            text.raycastTarget = false;
        }

        private static CinemachineOrbitalFollow EnsureCameraRig(Transform cameraTarget)
        {
            var mainCamera = Camera.main;
            if (mainCamera == null)
            {
                mainCamera = FindFirstObjectByType<Camera>();
            }

            if (mainCamera == null)
            {
                var cameraObject = new GameObject("Main Camera");
                mainCamera = cameraObject.AddComponent<Camera>();
                cameraObject.AddComponent<AudioListener>();
            }

            mainCamera.gameObject.tag = "MainCamera";
            mainCamera.clearFlags = CameraClearFlags.SolidColor;
            mainCamera.backgroundColor = new Color(0.015f, 0.018f, 0.026f, 1f);
            mainCamera.orthographic = false;
            mainCamera.fieldOfView = 42f;
            mainCamera.nearClipPlane = 0.05f;
            mainCamera.farClipPlane = 200f;

            var brain = mainCamera.GetComponent<CinemachineBrain>();
            if (brain == null)
            {
                brain = mainCamera.gameObject.AddComponent<CinemachineBrain>();
            }

            brain.UpdateMethod = CinemachineBrain.UpdateMethods.LateUpdate;
            brain.BlendUpdateMethod = CinemachineBrain.BrainUpdateMethods.LateUpdate;

            var virtualCameraObject = GameObject.Find("GlobalMap Cinemachine Camera");
            if (virtualCameraObject == null)
            {
                virtualCameraObject = new GameObject("GlobalMap Cinemachine Camera");
            }

            var virtualCamera = virtualCameraObject.GetComponent<CinemachineCamera>();
            if (virtualCamera == null)
            {
                virtualCamera = virtualCameraObject.AddComponent<CinemachineCamera>();
            }

            virtualCamera.Follow = cameraTarget;
            virtualCamera.LookAt = cameraTarget;
            virtualCamera.Priority = 20;
            var lens = virtualCamera.Lens;
            lens.FieldOfView = 42f;
            lens.NearClipPlane = 0.05f;
            lens.FarClipPlane = 200f;
            lens.ModeOverride = LensSettings.OverrideModes.Perspective;
            virtualCamera.Lens = lens;

            var orbitalFollow = virtualCameraObject.GetComponent<CinemachineOrbitalFollow>();
            if (orbitalFollow == null)
            {
                orbitalFollow = virtualCameraObject.AddComponent<CinemachineOrbitalFollow>();
            }

            orbitalFollow.OrbitStyle = CinemachineOrbitalFollow.OrbitStyles.Sphere;
            orbitalFollow.Radius = Mathf.Clamp(orbitalFollow.Radius <= 0f ? 7f : orbitalFollow.Radius, 4.2f, 10.5f);
            orbitalFollow.HorizontalAxis = new InputAxis
            {
                Value = -35f,
                Center = 0f,
                Range = new Vector2(-180f, 180f),
                Wrap = true
            };
            orbitalFollow.VerticalAxis = new InputAxis
            {
                Value = 22f,
                Center = 22f,
                Range = new Vector2(-89f, 89f),
                Wrap = false
            };
            orbitalFollow.RadialAxis = new InputAxis
            {
                Value = 1f,
                Center = 1f,
                Range = new Vector2(1f, 1f),
                Wrap = false
            };

            var hardLookAt = virtualCameraObject.GetComponent<CinemachineHardLookAt>();
            if (hardLookAt == null)
            {
                virtualCameraObject.AddComponent<CinemachineHardLookAt>();
            }

            return orbitalFollow;
        }

        private static void EnsureMainLight()
        {
            var existingLight = FindFirstObjectByType<Light>();
            if (existingLight != null)
            {
                return;
            }

            var lightObject = new GameObject("Directional Light");
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.25f;
            lightObject.transform.rotation = Quaternion.Euler(45f, -25f, 0f);
        }

        private static void EnsureEventSystem()
        {
            if (FindFirstObjectByType<EventSystem>() != null)
            {
                return;
            }

            var eventSystemObject = new GameObject("EventSystem");
            eventSystemObject.AddComponent<EventSystem>();
            eventSystemObject.AddComponent<StandaloneInputModule>();
            SetEditorDirty(eventSystemObject);
        }

        private static T GetOrAddComponent<T>(GameObject target)
            where T : Component
        {
            var component = target.GetComponent<T>();
            if (component != null)
            {
                return component;
            }

            component = target.AddComponent<T>();
            SetEditorDirty(target);
            return component;
        }

        private static TextMeshProUGUI GetOrAddTextMeshPro(GameObject target)
        {
            RemoveLegacyText(target);
            return GetOrAddComponent<TextMeshProUGUI>(target);
        }

        private static void RemoveLegacyText(GameObject target)
        {
            var legacyText = target.GetComponent("UnityEngine.UI." + "Text");
            if (legacyText == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(legacyText);
            }
            else
            {
                DestroyImmediate(legacyText);
            }

            SetEditorDirty(target);
        }

        private static void SetEditorDirty(Object target)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying && target != null)
            {
                UnityEditor.EditorUtility.SetDirty(target);
            }
#endif
        }
    }
}
