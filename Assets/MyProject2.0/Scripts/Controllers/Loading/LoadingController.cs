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
    [SerializeField, Min(1f)] private float readinessTimeoutSeconds = 30f;
    private void Start()
    {
        if (versionText != null) versionText.text = $"{Application.productName} • v{Application.version}";
        nextScene = SceneLoadRequest.Consume(nextScene);
        if (loadOnStart) StartCoroutine(Load());
    }
    public void LoadScene(string sceneName) { nextScene = sceneName; SceneLoadRequest.Request(sceneName); StopAllCoroutines(); StartCoroutine(Load()); }
    private IEnumerator Load()
    {
        string loadingSceneName = gameObject.scene.name;
        AsyncOperation operation = SceneManager.LoadSceneAsync(nextScene, LoadSceneMode.Additive);
        if (operation == null)
        {
            Debug.LogError($"[Loading] Não foi possível carregar a cena '{nextScene}'. Verifique o Build Profile.", this);
            yield break;
        }

        while (!operation.isDone)
        {
            SetProgress(Mathf.Clamp01(operation.progress / .9f) * .8f);
            yield return null;
        }

        Scene target = SceneManager.GetSceneByName(nextScene);
        SceneReadinessController readiness = null;
        if (target.IsValid())
        {
            foreach (GameObject root in target.GetRootGameObjects())
            {
                readiness = root.GetComponentInChildren<SceneReadinessController>(true);
                if (readiness != null) break;
            }
        }

        float deadline = Time.realtimeSinceStartup + readinessTimeoutSeconds;
        while (readiness != null && !readiness.IsReady && Time.realtimeSinceStartup < deadline)
        {
            SetProgress(.8f + Mathf.PingPong(Time.realtimeSinceStartup * .5f, .15f));
            yield return null;
        }
        if (readiness != null && !readiness.IsReady)
            Debug.LogWarning($"[Loading] Tempo esgotado esperando os dados da cena '{nextScene}'. Continuando para não prender o jogador.", this);

        SetProgress(1f);
        if (target.IsValid()) SceneManager.SetActiveScene(target);
        yield return null;
        SceneManager.UnloadSceneAsync(loadingSceneName);
    }
    private void SetProgress(float value) { if (progressSlider != null) progressSlider.value = value; if (progressText != null) progressText.text = $"{value * 100f:0}%"; }
}
