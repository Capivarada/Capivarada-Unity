    using TMPro;
    using UnityEngine;
    using UnityEngine.UI;

    public class LobbyProgressUI : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private Slider xpSlider;
        [SerializeField] private TMP_Text levelText;
        [SerializeField] private TMP_Text xpText;

        [Header("Manager")]
        [SerializeField] private LobbyPlayerProgress playerProgress;

        private void Start()
        {
            if (PlayFabAuthManager.Instance == null)
            {
                Debug.LogError(
                    "[LobbyProgressUI] PlayFabAuthManager não encontrado."
                );

                return;
            }

            if (playerProgress == null)
            {
                playerProgress =
                    FindFirstObjectByType<LobbyPlayerProgress>();
            }

            if (playerProgress == null)
            {
                Debug.LogError(
                    "[LobbyProgressUI] LobbyPlayerProgress não encontrado."
                );

                return;
            }

            PlayFabAuthManager.Instance.OnLoginSuccess += OnLoginSuccess;

            // Caso o login já tenha terminado antes deste objeto ser inicializado.
            if (PlayFabAuthManager.Instance.IsLoggedIn)
            {
                OnLoginSuccess();
            }
        }
        
        private void OnLoginSuccess()
        {
            playerProgress.OnProgressLoaded += UpdateProgress;
            playerProgress.LoadProgress();
        }

        private void OnDestroy()
        {
            if (PlayFabAuthManager.Instance != null)
            {
                PlayFabAuthManager.Instance.OnLoginSuccess -= OnLoginSuccess;
            }

            if (playerProgress != null)
            {
                playerProgress.OnProgressLoaded -= UpdateProgress;
            }
        }

        private void UpdateProgress(
            int currentXP,
            int level)
        {
            int xpIntoLevel =
                currentXP % 1500;

            if (xpSlider != null)
            {
                xpSlider.minValue = 0;
                xpSlider.maxValue = 1500;
                xpSlider.value = xpIntoLevel;
            }

            if (levelText != null)
            {
                levelText.text =
                    $"{level}";
            }

            if (xpText != null)
            {
                xpText.text =
                    $"{xpIntoLevel} / 1500 XP";
            }
        }
    }