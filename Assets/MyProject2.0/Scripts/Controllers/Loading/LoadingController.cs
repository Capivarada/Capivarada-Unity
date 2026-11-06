using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class LoadingController : MonoBehaviour
{
    [SerializeField] private TMP_Text versionText;
    [SerializeField] private Slider progressSlider;
    [SerializeField] private TMP_Text progressText;
    [SerializeField] private string nextScene = "Auth_Scene";
    [SerializeField] private bool loadOnStart = true;
    private void Start() { if (versionText != null) versionText.text = $"{Application.productName} • v{Application.version}"; if (loadOnStart) StartCoroutine(Load()); }
    public void LoadScene(string sceneName) { nextScene = sceneName; StopAllCoroutines(); StartCoroutine(Load()); }
    private IEnumerator Load()
    {
        AsyncOperation operation = SceneManager.LoadSceneAsync(nextScene); operation.allowSceneActivation = false;
        while (operation.progress < .9f) { SetProgress(operation.progress / .9f); yield return null; }
        SetProgress(1f); yield return null; operation.allowSceneActivation = true;
    }
    private void SetProgress(float value) { if (progressSlider != null) progressSlider.value = value; if (progressText != null) progressText.text = $"{value * 100f:0}%"; }
}
