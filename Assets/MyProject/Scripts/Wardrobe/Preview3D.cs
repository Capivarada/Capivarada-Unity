using System;
using UnityEngine;
using UnityEngine.UI;

public class Preview3D : MonoBehaviour
{
    [SerializeField] private RawImage targetImage;
    [SerializeField] private float rotationSpeed = 35f;
    [SerializeField] private float minimumDistance = 0.75f;
    [SerializeField] private int textureWidth = 512;
    [SerializeField] private int textureHeight = 512;
    [SerializeField] private string previewLayerName = "Preview3D";
    [SerializeField] private bool transparentBackground = true;
    [SerializeField] private Color backgroundColor = new Color(0.12f, 0.08f, 0.20f, 1f);

    private Camera previewCamera;
    private RenderTexture renderTexture;
    private GameObject runtimeRoot;
    private GameObject modelRoot;
    private GameObject modelInstance;
    private int previewLayer;

    public RawImage TargetImage
    {
        get => targetImage;
        set => targetImage = value;
    }

    public void Show(GameObject prefab)
    {
        Show(prefab, null);
    }

    public void Show(GameObject prefab, Action<GameObject> onCreated)
    {
        if (prefab == null)
        {
            Debug.LogWarning("[Preview3D] Preview prefab is null.");
            Clear();
            return;
        }

        EnsureRuntime();
        ClearModel();

        modelInstance = Instantiate(prefab, modelRoot.transform, false);
        modelInstance.transform.localPosition = Vector3.zero;
        modelInstance.transform.localRotation = Quaternion.identity;
        modelInstance.transform.localScale = Vector3.one;
        SetLayerRecursively(modelInstance, previewLayer);

        onCreated?.Invoke(modelInstance);
        AutoFrameModel();
    }

    public void Clear()
    {
        ClearModel();
    }

    public void SetRotationSpeed(float speed)
    {
        rotationSpeed = Mathf.Max(0f, speed);
    }

    private void Update()
    {
        if (modelInstance != null && modelRoot != null && rotationSpeed > 0f)
            modelRoot.transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.World);
    }

    private void EnsureRuntime()
    {
        if (runtimeRoot == null)
        {
            runtimeRoot = new GameObject("PreviewRuntimeRoot");
            runtimeRoot.transform.SetParent(null, false);
            runtimeRoot.transform.position = Vector3.zero;
            runtimeRoot.transform.rotation = Quaternion.identity;
            runtimeRoot.hideFlags = HideFlags.HideAndDontSave;
        }

        if (modelRoot == null)
        {
            modelRoot = new GameObject("PreviewModelRoot");
            modelRoot.transform.SetParent(runtimeRoot.transform, false);
            modelRoot.transform.localPosition = Vector3.zero;
            modelRoot.transform.localRotation = Quaternion.identity;
            modelRoot.transform.localScale = Vector3.one;
        }

        previewLayer = ResolvePreviewLayer();

        if (previewCamera == null)
        {
            GameObject cameraObject = new GameObject("PreviewCamera");
            previewCamera = cameraObject.AddComponent<Camera>();
            previewCamera.gameObject.transform.SetParent(runtimeRoot.transform, false);
            previewCamera.transform.localPosition = Vector3.zero;
            previewCamera.transform.localRotation = Quaternion.identity;
            previewCamera.clearFlags = CameraClearFlags.SolidColor;
            previewCamera.backgroundColor = transparentBackground
                ? new Color(backgroundColor.r, backgroundColor.g, backgroundColor.b, 0f)
                : backgroundColor;
            previewCamera.nearClipPlane = 0.01f;
            previewCamera.farClipPlane = 100f;
            previewCamera.fieldOfView = 30f;
            previewCamera.enabled = true;
            previewCamera.cullingMask = 1 << previewLayer;
        }

        if (renderTexture == null)
        {
            RenderTextureDescriptor descriptor = new RenderTextureDescriptor(
                textureWidth,
                textureHeight,
                RenderTextureFormat.ARGB32,
                24
            );
            descriptor.msaaSamples = 1;
            descriptor.depthBufferBits = 24;
            descriptor.depthStencilFormat = UnityEngine.Experimental.Rendering.GraphicsFormat.D24_UNorm_S8_UInt;
            descriptor.dimension = UnityEngine.Rendering.TextureDimension.Tex2D;
            descriptor.useMipMap = false;
            descriptor.autoGenerateMips = false;
            descriptor.sRGB = true;

            renderTexture = new RenderTexture(descriptor);
            renderTexture.name = "PreviewRenderTexture";
            renderTexture.Create();
        }

        if (previewCamera.targetTexture != renderTexture)
            previewCamera.targetTexture = renderTexture;

        if (targetImage != null)
        {
            targetImage.texture = renderTexture;
            targetImage.raycastTarget = false;
        }
    }

    private void ClearModel()
    {
        if (modelInstance != null)
        {
            Destroy(modelInstance);
            modelInstance = null;
        }
    }

    private void AutoFrameModel()
    {
        if (modelInstance == null || previewCamera == null || modelRoot == null)
            return;

        Renderer[] renderers = modelInstance.GetComponentsInChildren<Renderer>(true);
        if (renderers == null || renderers.Length == 0)
        {
            Debug.LogWarning("[Preview3D] No Renderer found in prefab.");
            return;
        }

        Bounds combinedBounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            if (renderers[i] != null && renderers[i].enabled)
                combinedBounds.Encapsulate(renderers[i].bounds);
        }

        if (combinedBounds.size == Vector3.zero)
            return;

        modelInstance.transform.localPosition = -combinedBounds.center;

        float maxSize = Mathf.Max(combinedBounds.size.x, combinedBounds.size.y, combinedBounds.size.z);
        float distance = Mathf.Max(minimumDistance, maxSize * 1.8f / Mathf.Tan(previewCamera.fieldOfView * 0.5f * Mathf.Deg2Rad));
        previewCamera.transform.localPosition = new Vector3(0f, maxSize * 0.35f, -distance);
        previewCamera.transform.LookAt(Vector3.zero);
    }

    private int ResolvePreviewLayer()
    {
        if (!string.IsNullOrWhiteSpace(previewLayerName))
        {
            int namedLayer = LayerMask.NameToLayer(previewLayerName);
            if (namedLayer >= 0)
                return namedLayer;
        }

        int generatedLayer = LayerMask.NameToLayer("Preview3D");
        if (generatedLayer >= 0)
            return generatedLayer;

        Debug.LogWarning("[Preview3D] Layer 'Preview3D' was not found. Create it in Project Settings > Tags and Layers.");
        return 0;
    }

    private static void SetLayerRecursively(GameObject root, int layer)
    {
        if (root == null)
            return;

        root.layer = layer;
        foreach (Transform child in root.transform)
            SetLayerRecursively(child.gameObject, layer);
    }

    private void OnDestroy()
    {
        ClearModel();

        if (previewCamera != null)
        {
            previewCamera.targetTexture = null;
            Destroy(previewCamera.gameObject);
            previewCamera = null;
        }

        if (modelRoot != null)
        {
            Destroy(modelRoot);
            modelRoot = null;
        }

        if (runtimeRoot != null)
        {
            Destroy(runtimeRoot);
            runtimeRoot = null;
        }

        if (renderTexture != null)
        {
            renderTexture.Release();
            Destroy(renderTexture);
            renderTexture = null;
        }
    }
}
