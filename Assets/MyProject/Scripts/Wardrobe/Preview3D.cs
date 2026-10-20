using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Isolated renderer for an already selected preview object.</summary>
public sealed class Preview3D : MonoBehaviour
{
    private const float FieldOfView = 30f;
    private const int TextureSize = 512;
    private const string PreviewLayerName = "Preview3D";
    private static readonly Color BackgroundColor = new(0.12f, 0.08f, 0.20f, 0f);

    private RawImage targetImage;
    private GameObject runtimeRoot;
    private GameObject modelInstance;
    private Camera previewCamera;
    private RenderTexture renderTexture;
    private int previewLayer;

    public void Show(GameObject prefab) => Show(prefab, null);

    public void Show(GameObject prefab, Action<GameObject> configure)
    {
        Clear();
        if (prefab == null) return;

        if (!EnsureRuntime()) return;
        modelInstance = Instantiate(prefab, runtimeRoot.transform, false);
        SetLayerRecursively(modelInstance, previewLayer);
        configure?.Invoke(modelInstance);
        FrameModel();
    }

    public void Clear()
    {
        if (modelInstance != null) Destroy(modelInstance);
        modelInstance = null;
        if (targetImage != null)
        {
            targetImage.texture = null;
            targetImage.enabled = false;
        }
    }

    private void Awake() => ResolveTargetImage();

    private void ResolveTargetImage()
    {
        if (targetImage == null) targetImage = GetComponentInChildren<RawImage>(true);
    }

    private bool EnsureRuntime()
    {
        ResolveTargetImage();
        previewLayer = LayerMask.NameToLayer(PreviewLayerName);
        if (previewLayer < 0)
        {
            Debug.LogError($"[{nameof(Preview3D)}] Layer '{PreviewLayerName}' is missing.", this);
            return false;
        }

        if (runtimeRoot == null)
        {
            runtimeRoot = new GameObject("Preview3D Runtime") { hideFlags = HideFlags.HideAndDontSave };
            previewCamera = CreateCamera("PreviewCamera", runtimeRoot.transform);
            var descriptor = new RenderTextureDescriptor(TextureSize, TextureSize, RenderTextureFormat.ARGB32, 24)
            {
                depthBufferBits = 24,
                msaaSamples = 1,
                useMipMap = false,
                autoGenerateMips = false
            };
            renderTexture = new RenderTexture(descriptor) { name = "Preview3D Runtime Texture" };
            renderTexture.Create();
            previewCamera.targetTexture = renderTexture;
        }

        previewCamera.cullingMask = 1 << previewLayer;
        if (targetImage != null)
        {
            targetImage.texture = renderTexture;
            targetImage.raycastTarget = false;
            targetImage.enabled = true;
        }
        return true;
    }

    private Camera CreateCamera(string objectName, Transform parent)
    {
        var cameraObject = new GameObject(objectName) { hideFlags = HideFlags.HideAndDontSave };
        cameraObject.transform.SetParent(parent, false);
        var camera = cameraObject.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = BackgroundColor;
        camera.fieldOfView = FieldOfView;
        camera.nearClipPlane = 0.001f;
        camera.farClipPlane = 10000f;
        camera.cullingMask = 1 << Mathf.Max(0, LayerMask.NameToLayer(PreviewLayerName));
        camera.useOcclusionCulling = false;
        return camera;
    }

    private void FrameModel()
    {
        if (modelInstance == null || previewCamera == null) return;
        var renderers = modelInstance.GetComponentsInChildren<Renderer>(true);
        var found = false;
        var bounds = new Bounds();
        foreach (var renderer in renderers)
        {
            if (renderer == null || !renderer.enabled || !renderer.gameObject.activeInHierarchy) continue;
            if (!found) { bounds = renderer.bounds; found = true; }
            else bounds.Encapsulate(renderer.bounds);
        }
        if (!found) return;

        // Move only the isolated clone so the visual bounds are centered. Keep its authored rotation and scale.
        modelInstance.transform.position -= bounds.center;
        bounds.center = Vector3.zero;
        float radius = Mathf.Max(bounds.extents.magnitude, 0.001f);
        float distance = radius / Mathf.Sin(previewCamera.fieldOfView * 0.8f * Mathf.Deg2Rad) * 1.08f;
        // Convention: preview assets face Unity forward (+Z); view them from that side.
        previewCamera.transform.position = new Vector3(0f, 0f, distance);
        previewCamera.transform.rotation = Quaternion.LookRotation(Vector3.back, Vector3.up);
        previewCamera.nearClipPlane = Mathf.Max(distance - radius * 1.5f, 0.0001f);
        previewCamera.farClipPlane = distance + radius * 2f;
    }

    private static void SetLayerRecursively(GameObject root, int layer)
    {
        root.layer = layer;
        foreach (Transform child in root.transform) SetLayerRecursively(child.gameObject, layer);
    }

    private void OnDestroy()
    {
        Clear();
        if (previewCamera != null) Destroy(previewCamera.gameObject);
        if (runtimeRoot != null) Destroy(runtimeRoot);
        if (renderTexture != null) { renderTexture.Release(); Destroy(renderTexture); }
    }
}
