using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using PlayFab;

/// <summary>
/// HTTP gateway for operations which must stay server-authoritative.
/// Attach this component to the persistent Services object.
/// </summary>
public sealed class CapivaradaBackendService : MonoBehaviour
{
    [SerializeField] private string baseUrl = "https://capivarada-functions-dev-d9djf2f2dqe4b9hu.westus2-01.azurewebsites.net/api";
    [SerializeField] private int requestTimeoutSeconds = 20;

    public static CapivaradaBackendService Instance { get; private set; }
    public string BaseUrl => baseUrl.TrimEnd('/');

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void Get<T>(string route, Action<T> succeeded, Action<string> failed)
    {
        StartCoroutine(Send("GET", route, null, succeeded, failed));
    }

    public void Post<TRequest, TResponse>(string route, TRequest body, Action<TResponse> succeeded, Action<string> failed)
    {
        StartCoroutine(Send("POST", route, JsonUtility.ToJson(body), succeeded, failed, true));
    }

    /// <summary>Only for public endpoints that must run before an authenticated session exists.</summary>
    public void PostPublic<TRequest, TResponse>(string route, TRequest body, Action<TResponse> succeeded, Action<string> failed)
    {
        StartCoroutine(Send("POST", route, JsonUtility.ToJson(body), succeeded, failed, false));
    }

    private IEnumerator Send<T>(string method, string route, string json, Action<T> succeeded, Action<string> failed, bool requiresAuthentication = true)
    {
        string ticket = PlayFabSettings.staticPlayer.ClientSessionTicket;
        if (requiresAuthentication && string.IsNullOrWhiteSpace(ticket))
        {
            failed?.Invoke("Você precisa estar autenticado para usar este serviço.");
            yield break;
        }

        using UnityWebRequest request = new UnityWebRequest(BaseUrl + "/" + route.TrimStart('/'), method)
        {
            timeout = Mathf.Max(1, requestTimeoutSeconds),
            downloadHandler = new DownloadHandlerBuffer()
        };

        request.SetRequestHeader("Accept", "application/json");
        if (requiresAuthentication)
        {
            request.SetRequestHeader("X-PlayFab-SessionTicket", ticket);
            if (!string.IsNullOrWhiteSpace(PlayFabSettings.staticPlayer.EntityToken)) request.SetRequestHeader("X-PlayFab-EntityToken", PlayFabSettings.staticPlayer.EntityToken);
            if (!string.IsNullOrWhiteSpace(PlayFabSettings.staticPlayer.EntityId)) request.SetRequestHeader("X-PlayFab-EntityId", PlayFabSettings.staticPlayer.EntityId);
            if (!string.IsNullOrWhiteSpace(PlayFabSettings.staticPlayer.EntityType)) request.SetRequestHeader("X-PlayFab-EntityType", PlayFabSettings.staticPlayer.EntityType);
        }
        if (!string.IsNullOrWhiteSpace(json))
        {
            request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
            request.SetRequestHeader("Content-Type", "application/json; charset=utf-8");
        }

        yield return request.SendWebRequest();
        if (request.result != UnityWebRequest.Result.Success)
        {
            string message = string.IsNullOrWhiteSpace(request.downloadHandler.text) ? request.error : request.downloadHandler.text;
            failed?.Invoke($"[{request.responseCode}] {message}");
            yield break;
        }

        if (typeof(T) == typeof(EmptyResponse)) { succeeded?.Invoke(default); yield break; }
        try { succeeded?.Invoke(JsonUtility.FromJson<T>(request.downloadHandler.text)); }
        catch (Exception exception) { failed?.Invoke("Resposta do servidor inválida: " + exception.Message); }
    }

    [Serializable] public struct EmptyResponse { }
}
