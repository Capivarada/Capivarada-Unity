using System;
using UnityEngine;
using UnityEngine.UI;

public class Preview3D : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private RawImage targetImage;

    [Header("Camera")]
    [SerializeField] private float fieldOfView = 30f;
    [SerializeField] private float minimumDistance = 0.5f;
    [SerializeField] private float distanceMultiplier = 1.25f;
    [SerializeField] private float verticalOffset = 0f;

    [Header("Render Texture")]
    [SerializeField] private int textureWidth = 512;
    [SerializeField] private int textureHeight = 512;

    [Header("Preview")]
    [SerializeField] private string previewLayerName = "Preview3D";
    [SerializeField] private bool transparentBackground = true;
    [SerializeField] private Color backgroundColor =
        new Color(0.12f, 0.08f, 0.20f, 1f);

    private Camera previewCamera;
    private RenderTexture renderTexture;

    private GameObject runtimeRoot;
    private GameObject modelRoot;
    private GameObject modelInstance;

    private int previewLayer;

    public RawImage TargetImage
    {
        get => targetImage;
        set
        {
            targetImage = value;

            if (renderTexture != null && targetImage != null)
                targetImage.texture = renderTexture;
        }
    }

    public void Show(GameObject prefab)
    {
        Show(prefab, null);
    }

    public void Show(
        GameObject prefab,
        Action<GameObject> onCreated)
    {
        Clear();

        if (prefab == null)
        {
            Debug.LogWarning(
                "[Preview3D] Prefab não informado."
            );

            return;
        }

        EnsureRuntime();

        modelInstance = Instantiate(
            prefab,
            modelRoot.transform,
            false
        );

        /*
         * IMPORTANTE:
         *
         * Não alteramos:
         * - posição
         * - rotação
         * - escala
         *
         * do prefab.
         *
         * O prefab é responsável pelo próprio
         * posicionamento.
         */

        SetLayerRecursively(
            modelInstance,
            previewLayer
        );

        onCreated?.Invoke(modelInstance);

        FrameModel();
    }

    public void Clear()
    {
        ClearModel();

        if (targetImage != null)
            targetImage.texture = null;
    }

    private void EnsureRuntime()
    {
        if (runtimeRoot == null)
        {
            runtimeRoot = new GameObject(
                "PreviewRuntimeRoot"
            );

            runtimeRoot.hideFlags =
                HideFlags.HideAndDontSave;

            runtimeRoot.transform.SetParent(
                null,
                false
            );
        }

        if (modelRoot == null)
        {
            modelRoot = new GameObject(
                "PreviewModelRoot"
            );

            modelRoot.hideFlags =
                HideFlags.HideAndDontSave;

            modelRoot.transform.SetParent(
                runtimeRoot.transform,
                false
            );

            modelRoot.transform.localPosition =
                Vector3.zero;

            modelRoot.transform.localRotation =
                Quaternion.identity;

            modelRoot.transform.localScale =
                Vector3.one;
        }

        previewLayer =
            ResolvePreviewLayer();

        CreateCamera();

        CreateRenderTexture();

        if (targetImage != null)
        {
            targetImage.texture =
                renderTexture;

            targetImage.raycastTarget =
                false;
        }
    }

    private void CreateCamera()
    {
        if (previewCamera != null)
            return;

        GameObject cameraObject =
            new GameObject("PreviewCamera");

        cameraObject.hideFlags =
            HideFlags.HideAndDontSave;

        cameraObject.transform.SetParent(
            runtimeRoot.transform,
            false
        );

        previewCamera =
            cameraObject.AddComponent<Camera>();

        previewCamera.clearFlags =
            CameraClearFlags.SolidColor;

        previewCamera.backgroundColor =
            transparentBackground
                ? new Color(
                    backgroundColor.r,
                    backgroundColor.g,
                    backgroundColor.b,
                    0f
                )
                : backgroundColor;

        previewCamera.nearClipPlane =
            0.01f;

        previewCamera.farClipPlane =
            1000f;

        previewCamera.fieldOfView =
            fieldOfView;

        previewCamera.cullingMask =
            1 << previewLayer;

        previewCamera.enabled = true;
    }

    private void CreateRenderTexture()
    {
        if (renderTexture != null)
            return;

        RenderTextureDescriptor descriptor =
            new RenderTextureDescriptor(
                textureWidth,
                textureHeight,
                RenderTextureFormat.ARGB32,
                24
            );

        descriptor.depthBufferBits = 24;
        descriptor.msaaSamples = 1;
        descriptor.useMipMap = false;
        descriptor.autoGenerateMips = false;

        renderTexture =
            new RenderTexture(descriptor);

        renderTexture.name =
            "PreviewRenderTexture";

        renderTexture.Create();

        previewCamera.targetTexture =
            renderTexture;
    }

    private void FrameModel()
    {
        if (
            modelInstance == null ||
            previewCamera == null
        )
        {
            return;
        }

        Renderer[] renderers =
            modelInstance.GetComponentsInChildren<Renderer>(
                true
            );

        Bounds bounds;
        bool foundRenderer = false;

        bounds = new Bounds(
            modelInstance.transform.position,
            Vector3.zero
        );

        foreach (Renderer renderer in renderers)
        {
            if (
                renderer == null ||
                !renderer.enabled
            )
            {
                continue;
            }

            if (!foundRenderer)
            {
                bounds = renderer.bounds;
                foundRenderer = true;
            }
            else
            {
                bounds.Encapsulate(
                    renderer.bounds
                );
            }
        }

        if (!foundRenderer)
        {
            Debug.LogWarning(
                "[Preview3D] " +
                "O prefab não possui Renderer."
            );

            return;
        }

        /*
         * NÃO MOVEMOS O MODELO.
         *
         * O Bounds serve somente para
         * descobrir o tamanho necessário
         * para posicionar a câmera.
         */

        float maxSize =
            Mathf.Max(
                bounds.size.x,
                bounds.size.y,
                bounds.size.z
            );

        maxSize =
            Mathf.Max(
                maxSize,
                0.01f
            );

        float halfFov =
            previewCamera.fieldOfView *
            0.5f *
            Mathf.Deg2Rad;

        float distance =
            (maxSize * distanceMultiplier) /
            Mathf.Tan(halfFov);

        distance =
            Mathf.Max(
                distance,
                minimumDistance
            );

        Vector3 target =
            bounds.center;

        target.y += verticalOffset;

        /*
         * A câmera olha para o objeto
         * pela frente.
         *
         * O modelo NÃO é rotacionado.
         */

        previewCamera.transform.position =
            target +
            new Vector3(
                0f,
                0f,
                -distance
            );

        previewCamera.transform.LookAt(
            target
        );

        previewCamera.nearClipPlane =
            Mathf.Max(
                0.01f,
                distance * 0.01f
            );

        previewCamera.farClipPlane =
            Mathf.Max(
                100f,
                distance + maxSize * 10f
            );
    }

    private void ClearModel()
    {
        if (modelInstance == null)
            return;

        Destroy(modelInstance);

        modelInstance = null;
    }

    private int ResolvePreviewLayer()
    {
        int layer =
            LayerMask.NameToLayer(
                previewLayerName
            );

        if (layer >= 0)
            return layer;

        Debug.LogWarning(
            "[Preview3D] Layer '" +
            previewLayerName +
            "' não encontrada. " +
            "Criando fallback para Default."
        );

        return 0;
    }

    private static void SetLayerRecursively(
        GameObject root,
        int layer)
    {
        if (root == null)
            return;

        root.layer = layer;

        foreach (Transform child in root.transform)
        {
            SetLayerRecursively(
                child.gameObject,
                layer
            );
        }
    }

    private void OnDestroy()
    {
        if (previewCamera != null)
        {
            previewCamera.targetTexture =
                null;

            Destroy(previewCamera.gameObject);

            previewCamera = null;
        }

        ClearModel();

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