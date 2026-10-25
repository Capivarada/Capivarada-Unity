using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

/// <summary>
/// Creates one immutable preview texture per skin as its card is initialized.
/// The factory uses one camera and one clone at a time: skins never share a
/// world position, UV rectangle, model instance, or render target.
/// </summary>
public sealed class Preview3DAtlas : MonoBehaviour
{
    private const int TextureSize = 512;
    private const float FieldOfView = 30f;
    private const float FactorySeparation = 10000f;
    private static Preview3DAtlas instance;

    private readonly Dictionary<string, Slot> slotsByKey = new();
    private readonly Queue<CaptureRequest> pendingCaptures = new();
    private GameObject runtimeRoot;
    private Camera previewCamera;
    private int previewLayer = -1;
    private Coroutine captureRoutine;

    public static Preview3DAtlas Instance
    {
        get
        {
            if (instance != null) return instance;
            GameObject root = new GameObject("Preview3D Preview Factory") { hideFlags = HideFlags.HideAndDontSave };
            instance = root.AddComponent<Preview3DAtlas>();
            return instance;
        }
    }

    public sealed class Slot
    {
        internal RenderTexture texture;
        internal int references;
    }

    private readonly struct CaptureRequest
    {
        internal readonly Slot slot;
        internal readonly GameObject prefab;
        internal readonly Action<GameObject> configure;
        internal CaptureRequest(Slot slot, GameObject prefab, Action<GameObject> configure)
        {
            this.slot = slot;
            this.prefab = prefab;
            this.configure = configure;
        }
    }

    public Slot Register(GameObject prefab, Action<GameObject> configure, RawImage target, string stableKey, Rarity _)
    {
        string key = string.IsNullOrWhiteSpace(stableKey) ? "view-" + target.GetEntityId() : stableKey;
        if (!slotsByKey.TryGetValue(key, out Slot slot))
        {
            slot = new Slot { texture = CreateSkinTexture(key) };
            slotsByKey.Add(key, slot);
            pendingCaptures.Enqueue(new CaptureRequest(slot, prefab, configure));
            if (captureRoutine == null) captureRoutine = StartCoroutine(CapturePendingSkins());
        }

        slot.references++;
        target.texture = slot.texture;
        target.uvRect = new Rect(0f, 0f, 1f, 1f);
        target.raycastTarget = false;
        target.enabled = true;
        return slot;
    }

    public void Unregister(Slot slot)
    {
        if (slot != null) slot.references = Mathf.Max(0, slot.references - 1);
    }

    private RenderTexture CreateSkinTexture(string key)
    {
        RenderTextureDescriptor descriptor = new RenderTextureDescriptor(TextureSize, TextureSize, RenderTextureFormat.ARGB32, 24)
        { depthBufferBits = 24, msaaSamples = 4, sRGB = true };
        RenderTexture texture = new RenderTexture(descriptor)
        {
            name = "Preview3D " + key,
            filterMode = FilterMode.Bilinear
        };
        texture.Create();
        return texture;
    }

    private IEnumerator CapturePendingSkins()
    {
        EnsureFactory();
        while (pendingCaptures.Count > 0)
        {
            CaptureRequest request = pendingCaptures.Dequeue();
            if (request.slot.texture == null || request.prefab == null) continue;

            GameObject model = Instantiate(request.prefab, runtimeRoot.transform, false);
            request.configure?.Invoke(model);
            foreach (Animator animator in model.GetComponentsInChildren<Animator>(true)) animator.enabled = false;
            SetLayerRecursively(model, previewLayer);
            FrameModel(model);

            previewCamera.targetTexture = request.slot.texture;
            ClearTexture(request.slot.texture);
            // A full normal render frame avoids manual Camera.Render calls in URP.
            yield return null;
            previewCamera.enabled = true;
            yield return new WaitForEndOfFrame();
            previewCamera.enabled = false;
            previewCamera.targetTexture = null;
            model.SetActive(false);
            Destroy(model);
        }
        captureRoutine = null;
    }

    private void EnsureFactory()
    {
        if (runtimeRoot != null) return;
        runtimeRoot = new GameObject("Preview3D Capture Runtime") { hideFlags = HideFlags.HideAndDontSave };
        runtimeRoot.transform.SetParent(transform, false);
        runtimeRoot.transform.position = new Vector3(0f, -FactorySeparation, 0f);

        GameObject cameraObject = new GameObject("Preview3D Capture Camera") { hideFlags = HideFlags.HideAndDontSave };
        cameraObject.transform.SetParent(runtimeRoot.transform, false);
        previewCamera = cameraObject.AddComponent<Camera>();
        previewCamera.clearFlags = CameraClearFlags.SolidColor;
        previewCamera.backgroundColor = new Color(0f, 0f, 0f, 0f);
        previewCamera.fieldOfView = FieldOfView;
        previewCamera.nearClipPlane = .01f;
        previewCamera.farClipPlane = 100f;
        previewCamera.useOcclusionCulling = false;
        previewCamera.allowHDR = true;
        previewCamera.enabled = false;
        previewLayer = LayerMask.NameToLayer("Preview3D");
        if (previewLayer >= 0) previewCamera.cullingMask = 1 << previewLayer;
        else Debug.LogWarning("[Preview3D] Camada Preview3D não encontrada; usando máscara padrão.");
        cameraObject.AddComponent<UniversalAdditionalCameraData>().renderPostProcessing = false;
    }

    private void FrameModel(GameObject model)
    {
        if (!TryGetBounds(model, out Bounds bounds)) return;
        Vector3 origin = runtimeRoot.transform.position;
        model.transform.position += origin - bounds.center;
        float radius = Mathf.Max(bounds.extents.magnitude, .001f);
        float fullyFramedDistance = Mathf.Max(radius * 1.75f,
            radius / Mathf.Sin(FieldOfView * .5f * Mathf.Deg2Rad) * .85f);
        // A smaller camera distance makes the authored capybara 1.5x larger.
        float distance = Mathf.Max(radius * 1.2f, fullyFramedDistance / 1.5f);
        previewCamera.transform.position = origin + Vector3.forward * distance;
        previewCamera.transform.rotation = Quaternion.LookRotation(Vector3.back, Vector3.up);
        previewCamera.nearClipPlane = Mathf.Max(.01f, distance - radius * 1.5f);
        previewCamera.farClipPlane = distance + radius * 2f;
    }

    private static void ClearTexture(RenderTexture texture)
    {
        RenderTexture previous = RenderTexture.active;
        RenderTexture.active = texture;
        GL.Clear(true, true, Color.clear);
        RenderTexture.active = previous;
    }

    private static void SetLayerRecursively(GameObject target, int layer)
    {
        if (target == null || layer < 0) return;
        target.layer = layer;
        foreach (Transform child in target.transform) SetLayerRecursively(child.gameObject, layer);
    }

    private static bool TryGetBounds(GameObject model, out Bounds bounds)
    {
        bool found = false;
        bounds = default;
        foreach (Renderer renderer in model.GetComponentsInChildren<Renderer>(true))
        {
            if (renderer == null || !renderer.enabled) continue;
            if (!found) { bounds = renderer.bounds; found = true; }
            else bounds.Encapsulate(renderer.bounds);
        }
        return found;
    }

    private void OnDestroy()
    {
        if (instance == this) instance = null;
        if (captureRoutine != null) StopCoroutine(captureRoutine);
        foreach (Slot slot in slotsByKey.Values)
            if (slot.texture != null) { slot.texture.Release(); Destroy(slot.texture); }
        if (runtimeRoot != null) Destroy(runtimeRoot);
    }
}
