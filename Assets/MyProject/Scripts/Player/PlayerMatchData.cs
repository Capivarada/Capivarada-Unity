using UnityEngine;

public class PlayerMatchData : MonoBehaviour
{
    private int kills;
    private int roundsSurvived;

    public int Kills => kills;
    public int RoundsSurvived => roundsSurvived;

    public void AddKill()
    {
        kills++;

        Debug.Log(
            $"[PLAYER DATA] {gameObject.name} eliminou um jogador. " +
            $"Kills: {kills}"
        );
    }

    public void AddRoundSurvived()
    {
        roundsSurvived++;
    }

    public void ResetMatchData()
    {
        kills = 0;
        roundsSurvived = 0;
    }
}