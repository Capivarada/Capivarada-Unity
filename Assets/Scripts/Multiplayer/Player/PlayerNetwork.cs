using Fusion;
using TMPro;
using UnityEngine;

public class PlayerNetwork : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerController playerController;
    [SerializeField] private ImpulseIndicator impulseIndicator;
    [SerializeField] private TMP_Text nicknameText;
    [SerializeField] private PlayerCameraController cameraController;

    [Networked]
    public NetworkString<_32> Nickname { get; set; }

    public bool IsLocal =>
        Object != null &&
        Object.HasInputAuthority;

    public override void Spawned()
    {
        bool isLocal = IsLocal;

        if (playerController != null)
            playerController.enabled = isLocal;

        if (impulseIndicator != null)
            impulseIndicator.gameObject.SetActive(isLocal);
        
        if (isLocal)
            SetupLocalCamera();
        
        UpdateNickname();

        Debug.Log(
            $"[Fusion] Player Spawned | " +
            $"Nome: {Nickname} | " +
            $"Local: {isLocal}"
        );
    }
    
    private void SetupLocalCamera()
    {
        if (!IsLocal)
            return;

        PlayerCameraController camera =
            FindFirstObjectByType<PlayerCameraController>();

        if (camera == null)
        {
            Debug.LogError(
                "[Camera] PlayerCameraController não encontrado."
            );

            return;
        }

        Transform target =
            transform.Find("CameraTarget");

        if (target == null)
        {
            Debug.LogError(
                "[Camera] CameraTarget não encontrado."
            );

            return;
        }

        camera.SetTarget(target);
    }

    public override void Render()
    {
        UpdateNickname();
    }

    private void UpdateNickname()
    {
        if (nicknameText == null)
            return;

        nicknameText.text = Nickname.ToString();
    }

    public void SetNickname(string nickname)
    {
        if (!Object.HasStateAuthority)
            return;

        Nickname = nickname;
    }
}