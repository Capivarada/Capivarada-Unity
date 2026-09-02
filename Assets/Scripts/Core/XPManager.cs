using UnityEngine;

public class XPManager : MonoBehaviour
{
    [Header("Placement XP")]
    [SerializeField]
    private int firstPlaceXP = 500;

    [SerializeField]
    private int secondPlaceXP = 300;

    [SerializeField]
    private int thirdPlaceXP = 200;

    [SerializeField]
    private int otherPlacementXP = 100;

    [Header("Match XP")]
    [SerializeField]
    private int xpPerKill = 50;

    [SerializeField]
    private int xpPerRound = 20;


    public int CalculateXP(
        int placement,
        int kills,
        int roundsSurvived
    )
    {
        int placementXP =
            GetPlacementXP(
                placement
            );

        int killsXP =
            kills * xpPerKill;

        int roundsXP =
            roundsSurvived *
            xpPerRound;

        return
            placementXP +
            killsXP +
            roundsXP;
    }


    public int GetPlacementXP(
        int placement
    )
    {
        return placement switch
        {
            1 => firstPlaceXP,
            2 => secondPlaceXP,
            3 => thirdPlaceXP,
            _ => otherPlacementXP
        };
    }


    public int GetKillsXP(
        int kills
    )
    {
        return kills * xpPerKill;
    }


    public int GetRoundsXP(
        int rounds
    )
    {
        return rounds * xpPerRound;
    }
}