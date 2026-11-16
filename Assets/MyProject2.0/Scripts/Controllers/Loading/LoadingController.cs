using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class LoadingController : MonoBehaviour
{
    [SerializeField] private TMP_Text versionText;
    [SerializeField] private Slider progressSlider;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private TMP_Text progressText;
    [SerializeField] private string nextScene = "Auth_Scene";
    [SerializeField] private bool loadOnStart = true;
    [SerializeField, Min(1f)] private float readinessTimeoutSeconds = 30f;
    [Header("Online arena warmup")]
    [SerializeField] private string lobbyScene = "Lobby_Scene";
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
        bool waitedForArena = false;
        OnlinePresenceService presence = ServiceHub.Instance != null ? ServiceHub.Instance.Presence : null;
        PlayFabAuthManager authentication = ServiceHub.Instance != null ? ServiceHub.Instance.Authentication : PlayFabAuthManager.Instance;
        if (string.Equals(nextScene, lobbyScene, System.StringComparison.Ordinal) &&
            presence != null && authentication != null && authentication.IsLoggedIn && !authentication.IsGuest)
        {
            waitedForArena = true;
            yield return presence.WaitUntilReady((message, progress) =>
            {
                SetMessage(message);
                SetProgress(progress);
            }, message =>
            {
                Debug.LogWarning("[Loading] " + message, this);
                SetMessage(message);
            });
        }
        float loadBaseProgress = waitedForArena ? .75f : 0f;
        AsyncOperation operation = SceneManager.LoadSceneAsync(nextScene, LoadSceneMode.Additive);
        if (operation == null)
        {
            Debug.LogError($"[Loading] Não foi possível carregar a cena '{nextScene}'. Verifique o Build Profile.", this);
            yield break;
        }

        while (!operation.isDone)
        {
            SetProgress(loadBaseProgress + Mathf.Clamp01(operation.progress / .9f) * (1f - loadBaseProgress));
            SetMessage("Carregando o lobby...");
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
            SetProgress(Mathf.Max(loadBaseProgress, .85f + Mathf.PingPong(Time.realtimeSinceStartup * .5f, .1f)));
            SetMessage("Sincronizando dados do lobby...");
            yield return null;
        }
        if (readiness != null && !readiness.IsReady)
            Debug.LogWarning($"[Loading] Tempo esgotado esperando os dados da cena '{nextScene}'. Continuando para não prender o jogador.", this);

        SetProgress(1f);
        if (target.IsValid()) SceneManager.SetActiveScene(target);
        yield return null;
        SceneManager.UnloadSceneAsync(loadingSceneName);
    }
    private void SetProgress(float value) { if (progressSlider != null) progressSlider.value = value; if (progressText != null) progressText.text = $"{Mathf.Clamp01(value) * 100f:0}%"; }
    private void SetMessage(string message) { if (statusText != null) statusText.text = message; }
}
