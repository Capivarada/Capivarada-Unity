using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>Serialized, scene-local loading UI. It exposes only genuine progress when available.</summary>
public sealed class LoadingController : MonoBehaviour
{
    [SerializeField] private CanvasGroup root;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private Slider progressSlider;
    [SerializeField] private GameObject indeterminateIndicator;

    private int depth;
    public void Show(string status, float? progress = null)
    {
        depth++;
        SetVisual(status, progress);
    }
    public void UpdateStatus(string status, float? progress = null) => SetVisual(status, progress);
    public void Hide()
    {
        depth = Mathf.Max(0, depth - 1);
        if (depth == 0 && root != null) { root.alpha = 0f; root.blocksRaycasts = false; root.interactable = false; }
    }
    public void LoadScene(string sceneName) => StartCoroutine(LoadSceneRoutine(sceneName));
    private IEnumerator LoadSceneRoutine(string sceneName)
    {
        Show("Carregando cena", 0f);
        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);
        while (!operation.isDone) { UpdateStatus("Carregando cena", operation.progress / .9f); yield return null; }
    }
    private void SetVisual(string status, float? progress)
    {
        if (root != null) { root.alpha = 1f; root.blocksRaycasts = true; root.interactable = true; }
        if (statusText != null) statusText.text = status ?? string.Empty;
        if (progressSlider != null) { progressSlider.gameObject.SetActive(progress.HasValue); if (progress.HasValue) progressSlider.value = Mathf.Clamp01(progress.Value); }
        if (indeterminateIndicator != null) indeterminateIndicator.SetActive(!progress.HasValue);
    }
}
