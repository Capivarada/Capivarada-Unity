using TMPro;
using UnityEngine;

public class PlayerNickname : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI nicknameText;

    private Camera mainCamera;

    private void Awake()
    {
        if (nicknameText == null)
        {
            nicknameText = GetComponentInChildren<TextMeshProUGUI>();
        }
    }

    private void Start()
    {
        mainCamera = Camera.main;
    }

    private void LateUpdate()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;

            if (mainCamera == null)
                return;
        }

        transform.forward =
            mainCamera.transform.forward;
    }

    public void SetNickname(string name)
    {
        if (nicknameText != null)
        {
            nicknameText.text = name;
        }
    }
}