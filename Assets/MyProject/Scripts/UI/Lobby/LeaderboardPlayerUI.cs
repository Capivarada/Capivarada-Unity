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
        int value)
    {
        positionText.text =$"{position + 1}º";

        nameText.text = playerName;

        valueText.text = value.ToString();
    }
}