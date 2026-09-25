using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(WardrobeSkin))]
public class WardrobeSkinEditor : Editor
{
    private static readonly SkinColorSlot[] Slots =
    {
        SkinColorSlot.Skin,
        SkinColorSlot.Tail,
        SkinColorSlot.Paw,
        SkinColorSlot.Nose,
        SkinColorSlot.Ears,
        SkinColorSlot.Mouth,
        SkinColorSlot.Eyes
    };

    private PreviewRenderUtility previewUtility;
    private GameObject previewObject;
    private float rotationY = 135f;
    private bool isDragging;
    private Vector2 lastMousePosition;
    private double lastUpdateTime;
    private double lastRepaintTime;
    private bool previewNeedsFrame;
    private const float PreviewRefreshInterval = 1f / 30f;

    public override bool HasPreviewGUI() => true;

    private void OnEnable()
    {
        lastUpdateTime = EditorApplication.timeSinceStartup;
        EnsurePreview();
        EditorApplication.update -= UpdatePreview;
        EditorApplication.update += UpdatePreview;
    }

    private void OnDisable()
    {
        EditorApplication.update -= UpdatePreview;
        CleanupPreview();
    }

    private void UpdatePreview()
    {
        if (previewUtility == null || previewObject == null)
            return;

        double currentTime = EditorApplication.timeSinceStartup;
        float deltaTime = Mathf.Clamp((float)(currentTime - lastUpdateTime), 0f, 0.25f);
        lastUpdateTime = currentTime;

        if ((currentTime - lastRepaintTime) < PreviewRefreshInterval)
            return;

        lastRepaintTime = currentTime;
        rotationY += 25f * deltaTime;
        if (rotationY >= 360f)
            rotationY -= 360f;

        previewObject.transform.rotation = Quaternion.Euler(0f, rotationY, 0f);
        Repaint();
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        bool changed = false;
        EditorGUI.BeginChangeCheck();
        DrawPropertiesExcluding(serializedObject, "colors");

        SerializedProperty colors = serializedObject.FindProperty("colors");
        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("Skin Material Slots", EditorStyles.boldLabel);

        if (colors != null)
        {
            for (int i = 0; i < Slots.Length; i++)
            {
                if (i >= colors.arraySize)
                    break;

                SerializedProperty element = colors.GetArrayElementAtIndex(i);
                EditorGUILayout.LabelField(ObjectNames.NicifyVariableName(Slots[i].ToString()), EditorStyles.miniBoldLabel);
                EditorGUI.indentLevel++;

                SerializedProperty color = element.FindPropertyRelative("color");
                SerializedProperty metallic = element.FindPropertyRelative("metallic");
                SerializedProperty smoothness = element.FindPropertyRelative("smoothness");

                if (color != null)
                    EditorGUILayout.PropertyField(color);
                if (metallic != null)
                    EditorGUILayout.PropertyField(metallic);
                if (smoothness != null)
                    EditorGUILayout.PropertyField(smoothness);

                EditorGUI.indentLevel--;
                EditorGUILayout.Space(3);
            }
        }

        changed = EditorGUI.EndChangeCheck();
        serializedObject.ApplyModifiedProperties();

        if (changed)
        {
            previewNeedsFrame = true;
            ApplySkin();
            Repaint();
        }

        EditorGUILayout.Space(12);
        EditorGUILayout.LabelField("Skin Preview", EditorStyles.boldLabel);

        Rect previewRect = GUILayoutUtility.GetRect(220f, 220f, GUILayout.ExpandWidth(true));
        EnsurePreview();
        DrawPreview(previewRect, false);
    }

    public override void OnPreviewGUI(Rect rect, GUIStyle background)
    {
        EnsurePreview();
        DrawPreview(rect, true);
    }

    public override void OnInteractivePreviewGUI(Rect rect, GUIStyle background)
    {
        EnsurePreview();
        HandlePreviewInput(rect);
        DrawPreview(rect, true);
    }

    private void EnsurePreview()
    {
        if (previewUtility == null)
        {
            previewUtility = new PreviewRenderUtility(true);
            SetupCamera();
            SetupLighting();
        }

        if (previewObject == null)
        {
            CreatePreviewObject();
            previewNeedsFrame = true;
        }

        if (previewNeedsFrame)
        {
            FramePreview();
            previewNeedsFrame = false;
        }
    }

    private void SetupCamera()
    {
        if (previewUtility == null)
            return;

        Camera camera = previewUtility.camera;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0f, 0f, 0f, 0f);
        camera.fieldOfView = 30f;
        camera.nearClipPlane = 0.01f;
        camera.farClipPlane = 100f;
        camera.cullingMask = 1 << ResolvePreviewLayer();
    }

    private void SetupLighting()
    {
        if (previewUtility == null)
            return;

        Light keyLight = previewUtility.lights[0];
        keyLight.type = LightType.Directional;
        keyLight.intensity = 1.2f;
        keyLight.transform.rotation = Quaternion.Euler(35f, -35f, 0f);

        Light fillLight = previewUtility.lights[1];
        fillLight.type = LightType.Directional;
        fillLight.intensity = 0.5f;
        fillLight.transform.rotation = Quaternion.Euler(20f, 145f, 0f);

        previewUtility.ambientColor = new Color(0.25f, 0.25f, 0.25f);
    }

    private void CreatePreviewObject()
    {
        GameObject source = WardrobePreviewUtility.GetCapybaraPreviewPrefab();
        if (source == null)
        {
            Debug.LogWarning("[WardrobeSkinEditor] Capybara preview prefab not found.");
            return;
        }

        previewObject = Instantiate(source);
        previewObject.hideFlags = HideFlags.HideAndDontSave;
        previewObject.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        previewObject.transform.localScale = Vector3.one;

        int layer = ResolvePreviewLayer();
        if (layer >= 0)
        {
            previewObject.layer = layer;
            SetLayerRecursively(previewObject, layer);
        }

        if (previewUtility != null)
            previewUtility.AddSingleGO(previewObject);

        PlayerCustomizationApplier applier = previewObject.GetComponent<PlayerCustomizationApplier>();
        if (applier == null)
            applier = previewObject.AddComponent<PlayerCustomizationApplier>();

        ApplySkin();
    }

    private void ApplySkin()
    {
        if (previewObject == null)
            return;

        WardrobeSkin skin = target as WardrobeSkin;
        if (skin == null)
            return;

        PlayerCustomizationApplier applier = previewObject.GetComponent<PlayerCustomizationApplier>();
        if (applier == null)
            applier = previewObject.AddComponent<PlayerCustomizationApplier>();

        applier.Apply(new PlayerCustomization { Skin = skin });
    }

    private void FramePreview()
    {
        if (previewUtility == null || previewObject == null)
            return;

        Bounds bounds = GetBounds(previewObject);
        if (bounds.size == Vector3.zero)
            return;

        previewObject.transform.rotation = Quaternion.Euler(0f, rotationY, 0f);
        previewObject.transform.position = -bounds.center;

        float size = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
        size = Mathf.Max(size, 0.1f);

        float halfFov = previewUtility.camera.fieldOfView * 0.5f * Mathf.Deg2Rad;
        float distance = Mathf.Max(size * 0.18f / Mathf.Tan(halfFov), 0.18f);

        Camera camera = previewUtility.camera;
        camera.transform.position = new Vector3(0f, size * 0.18f, -distance);
        camera.transform.LookAt(Vector3.zero);
        camera.nearClipPlane = Mathf.Max(0.01f, distance * 0.1f);
        camera.farClipPlane = Mathf.Max(distance * 4f, size * 10f);
    }

    private void DrawPreview(Rect rect, bool interactive)
    {
        if (previewUtility == null || previewObject == null)
        {
            DrawPreviewError(rect, "Preview indisponível");
            return;
        }

        previewObject.transform.rotation = Quaternion.Euler(0f, rotationY, 0f);

        Camera camera = previewUtility.camera;
        camera.transform.position = camera.transform.position;
        camera.transform.LookAt(Vector3.zero);

        previewUtility.BeginPreview(rect, GUIStyle.none);
        camera.Render();
        Texture preview = previewUtility.EndPreview();

        if (preview != null)
            GUI.DrawTexture(rect, preview, ScaleMode.ScaleToFit, false);
    }

    private void HandlePreviewInput(Rect rect)
    {
        Event currentEvent = Event.current;
        if (!rect.Contains(currentEvent.mousePosition))
            return;

        if (currentEvent.type == EventType.MouseDown && currentEvent.button == 0)
        {
            isDragging = true;
            lastMousePosition = currentEvent.mousePosition;
            currentEvent.Use();
        }

        if (currentEvent.type == EventType.MouseUp && currentEvent.button == 0)
        {
            isDragging = false;
            currentEvent.Use();
        }

        if (isDragging && currentEvent.type == EventType.MouseDrag)
        {
            Vector2 delta = currentEvent.mousePosition - lastMousePosition;
            rotationY += delta.x * 0.5f;
            lastMousePosition = currentEvent.mousePosition;
            Repaint();
            currentEvent.Use();
        }

        if (currentEvent.type == EventType.ScrollWheel)
            currentEvent.Use();
    }

    private static Bounds GetBounds(GameObject root)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        if (renderers == null || renderers.Length == 0)
            return new Bounds(Vector3.zero, Vector3.zero);

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            if (renderers[i] == null || !renderers[i].enabled)
                continue;
            bounds.Encapsulate(renderers[i].bounds);
        }

        return bounds;
    }

    private static void SetLayerRecursively(GameObject root, int layer)
    {
        if (root == null)
            return;

        root.layer = layer;
        foreach (Transform child in root.transform)
            SetLayerRecursively(child.gameObject, layer);
    }

    private static int ResolvePreviewLayer()
    {
        int layer = LayerMask.NameToLayer("Preview3D");
        if (layer < 0)
        {
            Debug.LogWarning("[WardrobeSkinEditor] Layer 'Preview3D' was not found. Create it in Project Settings > Tags and Layers.");
            return 0;
        }

        return layer;
    }

    private static void DrawPreviewError(Rect rect, string message)
    {
        EditorGUI.DrawRect(rect, new Color(0.08f, 0.08f, 0.08f, 1f));
        GUI.Label(rect, message, EditorStyles.centeredGreyMiniLabel);
    }

    private void CleanupPreview()
    {
        if (previewObject != null)
        {
            DestroyImmediate(previewObject);
            previewObject = null;
        }

        if (previewUtility != null)
        {
            previewUtility.Cleanup();
            previewUtility = null;
        }
    }
}