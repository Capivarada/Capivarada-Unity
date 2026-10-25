using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>One isolated model/camera/texture per visible UI preview. No shared preview layer is required.</summary>
public sealed class Preview3D : MonoBehaviour
{
    private const float FieldOfView = 30f;
    private const int TextureSize = 768;
    private const float PreviewSeparation = 10000f;
    private static int nextPreviewSlot;
    private static readonly Stack<int> recycledPreviewSlots = new();
    private static readonly Color BackgroundColor = new(0.12f, 0.08f, 0.20f, 0f);

    [SerializeField] private RawImage targetImage;
    private GameObject runtimeRoot;
    private GameObject modelInstance;
    private Camera previewCamera;
    private RenderTexture renderTexture;
    private Vector3 previewOrigin;
    private int previewSlot = -1;
    private Coroutine renderCoroutine;

    public void Show(GameObject prefab) => Show(prefab, null);
    public void Show(GameObject prefab, Action<GameObject> configure)
    {
        ClearModel();
        if (prefab == null || !EnsureRuntime()) return;
        modelInstance = Instantiate(prefab, runtimeRoot.transform, false);
        configure?.Invoke(modelInstance);
        FrameModel();
        targetImage.texture = renderTexture;
        targetImage.enabled = true;
        QueueRender();
    }

    public void Clear()
    {
        ClearModel();
        if (targetImage != null) { targetImage.texture = null; targetImage.enabled = false; }
        if (previewCamera != null) previewCamera.enabled = false;
    }

    private void Awake() { if (targetImage == null) targetImage = GetComponentInChildren<RawImage>(true); }
    private void OnEnable()
    {
        if (previewCamera != null && modelInstance != null)
            QueueRender();
    }

    private void OnDisable()
    {
        if (renderCoroutine != null) StopCoroutine(renderCoroutine);
        renderCoroutine = null;
        if (previewCamera != null) previewCamera.enabled = false;
    }

    private bool EnsureRuntime()
    {
        if (targetImage == null) targetImage = GetComponentInChildren<RawImage>(true);
        if (targetImage == null) { Debug.LogError("[Preview3D] RawImage não configurada.", this); return false; }
        if (runtimeRoot != null) return true;

        previewSlot = recycledPreviewSlots.Count > 0 ? recycledPreviewSlots.Pop() : ++nextPreviewSlot;
        previewOrigin = new Vector3(previewSlot * PreviewSeparation, -PreviewSeparation, 0f);
        runtimeRoot = new GameObject("Preview3D Runtime") { hideFlags = HideFlags.HideAndDontSave };
        runtimeRoot.transform.position = previewOrigin;
        var cameraObject = new GameObject("Preview3D Camera") { hideFlags = HideFlags.HideAndDontSave };
        cameraObject.transform.SetParent(runtimeRoot.transform, false);
        previewCamera = cameraObject.AddComponent<Camera>();
        previewCamera.clearFlags = CameraClearFlags.SolidColor;
        previewCamera.backgroundColor = BackgroundColor;
        previewCamera.fieldOfView = FieldOfView;
        previewCamera.nearClipPlane = 0.01f;
        previewCamera.farClipPlane = 100f; // smaller than PreviewSeparation: another preview cannot be rendered.
        previewCamera.useOcclusionCulling = false;
        previewCamera.allowHDR = true;
        previewCamera.enabled = false;

        var descriptor = new RenderTextureDescriptor(TextureSize, TextureSize, RenderTextureFormat.ARGB32, 24)
        {
            depthBufferBits = 24,
            msaaSamples = 2,
            useMipMap = false,
            autoGenerateMips = false,
            sRGB = true
        };
        renderTexture = new RenderTexture(descriptor) { name = "Preview3D Texture" };
        renderTexture.filterMode = FilterMode.Bilinear;
        renderTexture.Create();
        previewCamera.targetTexture = renderTexture;
        targetImage.raycastTarget = false;
        return true;
    }

    private void QueueRender()
    {
        if (!isActiveAndEnabled || previewCamera == null || renderTexture == null)
            return;
        if (renderCoroutine != null) StopCoroutine(renderCoroutine);
        renderCoroutine = StartCoroutine(RenderAtEndOfFrame());
    }

    private System.Collections.IEnumerator RenderAtEndOfFrame()
    {
        // URP renders the preview camera during the normal frame. Keeping
        // dozens of card cameras enabled afterwards caused WebGL/GPU pressure
        // and alternating low-quality preview frames on reopened box details.
        if (!renderTexture.IsCreated()) renderTexture.Create();
        previewCamera.enabled = true;
        yield return new WaitForEndOfFrame();
        if (previewCamera != null) previewCamera.enabled = false;
        renderCoroutine = null;
    }

    private void FrameModel()
    {
        var renderers = modelInstance.GetComponentsInChildren<Renderer>(true);
        bool found = false;
        Bounds bounds = default;
        foreach (Renderer renderer in renderers)
        {
            if (renderer == null || !renderer.enabled) continue;
            if (!found) { bounds = renderer.bounds; found = true; } else bounds.Encapsulate(renderer.bounds);
        }
        if (!found) { Debug.LogWarning("[Preview3D] Modelo sem Renderer.", this); return; }

        // Centre the clone around its own isolated origin, retaining authored rotation and scale.
        modelInstance.transform.position += previewOrigin - bounds.center;
        float radius = Mathf.Max(bounds.extents.magnitude, 0.001f);
        // Card previews need a close, readable capybara rather than the distant
        // full-body lobby framing.
        float fullyFramedDistance = radius / Mathf.Sin(previewCamera.fieldOfView * 0.5f * Mathf.Deg2Rad) * 1.12f;
        // Dividing the framing distance by 2.5 placed the camera *inside* the
        // model. Keep the requested close crop but never cross the mesh bounds.
        float distance = Mathf.Max(radius * 1.75f, fullyFramedDistance / 2.5f);
        previewCamera.transform.position = previewOrigin + Vector3.forward * distance;
        previewCamera.transform.rotation = Quaternion.LookRotation(Vector3.back, Vector3.up);
        previewCamera.nearClipPlane = Mathf.Max(0.01f, distance - radius * 1.5f);
        previewCamera.farClipPlane = Mathf.Min(PreviewSeparation * .45f, distance + radius * 2f);
    }

    private void ClearModel()
    {
        if (modelInstance != null) Destroy(modelInstance);
        modelInstance = null;
    }

    private void OnDestroy()
    {
        if (renderCoroutine != null) StopCoroutine(renderCoroutine);
        Clear();
        if (renderTexture != null) { renderTexture.Release(); Destroy(renderTexture); }
        if (runtimeRoot != null) Destroy(runtimeRoot);
        if (previewSlot >= 0)
        {
            recycledPreviewSlots.Push(previewSlot);
            previewSlot = -1;
        }
    }
}
