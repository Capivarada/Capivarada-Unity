using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Presentation-only UI for navigating living match participants.</summary>
public class SpectatorUI : MonoBehaviour
{
    [SerializeField] private GameObject container;
    [SerializeField] private Button previousButton;
    [SerializeField] private Button nextButton;
    [SerializeField] private Button returnToLobbyButton;
    [SerializeField] private TMP_Text spectatedPlayerNameText;

    public event Action PreviousRequested;
    public event Action NextRequested;
    public event Action ReturnToLobbyRequested;

    private void Awake()
    {
        if (previousButton != null) previousButton.onClick.AddListener(HandlePreviousButton);
        if (nextButton != null) nextButton.onClick.AddListener(HandleNextButton);
        if (returnToLobbyButton != null) returnToLobbyButton.onClick.AddListener(HandleReturnToLobbyButton);
        Hide();
    }

    public void Show(string playerName)
    {
        if (container != null) container.SetActive(true);
        Cursor.lockState = CursorLockMode.None;
        SetPlayerName(playerName);
    }

    public void Hide() { if (container != null) container.SetActive(false); }
    public void SetPlayerName(string playerName) { if (spectatedPlayerNameText != null) spectatedPlayerNameText.text = playerName; }

    private void OnDestroy()
    {
        if (previousButton != null) previousButton.onClick.RemoveListener(HandlePreviousButton);
        if (nextButton != null) nextButton.onClick.RemoveListener(HandleNextButton);
        if (returnToLobbyButton != null) returnToLobbyButton.onClick.RemoveListener(HandleReturnToLobbyButton);
    }

    private void HandlePreviousButton() => PreviousRequested?.Invoke();
    private void HandleNextButton() => NextRequested?.Invoke();
    private void HandleReturnToLobbyButton() => ReturnToLobbyRequested?.Invoke();
}
