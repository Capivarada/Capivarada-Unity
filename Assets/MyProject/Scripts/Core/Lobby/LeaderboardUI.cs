using System;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class LeaderboardUI : MonoBehaviour
{
    [SerializeField] LeaderboardManager leaderboardManager;

    private void OnEnable()
    {
        leaderboardManager.LoadAllLeaderboards();
    }
}