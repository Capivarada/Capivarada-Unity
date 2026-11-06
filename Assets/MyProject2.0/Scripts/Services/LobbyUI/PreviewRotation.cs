using UnityEngine;
using UnityEngine.UI;

public class PreviewRotation : MonoBehaviour
{
    [SerializeField] private GameObject previewObject;
    [SerializeField] private Slider slider;
    [SerializeField] private bool isLobby;

    private void Start()
    {
        slider.value = 0.4f;
        SetRotation(0.4f);
    }   

    public void SetRotation(float value)
    {
        if (isLobby)
        {
            if (value >= 0.6f)
                value = 0.6f;
            if (value <= 0.4f)
                value = 0.4f;
        }
        
        float rotation = value * 360f;

        previewObject.transform.rotation = Quaternion.Euler(0f, rotation, 0f);
    }
}