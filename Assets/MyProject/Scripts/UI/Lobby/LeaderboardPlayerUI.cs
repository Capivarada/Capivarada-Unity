using TMPro;
using UnityEngine;

public class LeaderboardPlayerUI : MonoBehaviour
{
    [SerializeField] private TMP_Text positionText;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text valueText;

    public void Setup(
        int position,
        string playerName,
        int value,
        bool formatAsDuration = false)
    {
        positionText.text =$"{position + 1}º";

        nameText.text = playerName;

        valueText.text = formatAsDuration
            ? System.TimeSpan.FromSeconds(Mathf.Max(0, value)).ToString(@"hh\:mm\:ss")
            : value.ToString();
    }
}
