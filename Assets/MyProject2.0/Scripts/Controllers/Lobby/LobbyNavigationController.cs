using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class LobbyNavigationController : MonoBehaviour
{
    [SerializeField] private PanelNavigatorController panels; [SerializeField] private string menuPanel = "Menu"; [SerializeField] private string gameScene = "Game_Scene"; [SerializeField] private string trainingScene = "Training_Scene";
    public void Open(string panelId) => panels?.Show(panelId); public void BackToMenu() => panels?.Show(menuPanel); public void Play() { if (!string.IsNullOrWhiteSpace(gameScene)) SceneManager.LoadScene(gameScene); } public void Training() { if (!string.IsNullOrWhiteSpace(trainingScene)) SceneManager.LoadScene(trainingScene); } public void Quit() => Application.Quit();
}
