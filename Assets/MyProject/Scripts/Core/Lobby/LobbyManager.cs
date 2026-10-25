using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// Simple lobby manager.
/// Provides a Play button that simulates searching for players (5 seconds),
/// then loads the Game scene.
/// </summary>
public class LobbyManager : MonoBehaviour
{
    #region Configuration

    [SerializeField] private float searchDuration = 1f;
    [SerializeField] private string gameSceneName = "Game";

    #endregion

    #region State

    private bool isSearching = false;

    #endregion

    #region Initialization

    private void Start()
    {

        Cursor.lockState = CursorLockMode.None;
    }

    #endregion

    #region Button Handlers

    /// <summary>
    /// Called when Play button is clicked.
    /// </summary>
    public void OnPlayButtonClicked()
    {
        if (isSearching)
            return;

        StartCoroutine(SearchAndLoadGame());
    }

    #endregion

    #region Search & Load

    private IEnumerator SearchAndLoadGame()
    {
        isSearching = true;

        Debug.Log("[LobbyManager] Starting player search...");

        float elapsedTime = 0f;

        while (elapsedTime < searchDuration)
        {
            elapsedTime += Time.deltaTime;

            int dotsCount = Mathf.FloorToInt((elapsedTime / searchDuration) * 3) + 1;
            string dots = new string('.', dotsCount);

            yield return null;
        }

        Debug.Log("[LobbyManager] Players found! Loading game scene...");

        yield return new WaitForSeconds(1f);

        LoadGameScene();
    }

    private void LoadGameScene()
    {
        isSearching = false;
        SceneManager.LoadScene(gameSceneName);
    }
    
    #endregion
}


