using UnityEngine;

public class BotController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerController playerController;
    [SerializeField] private Rigidbody rb;

    [Header("Arena")]
    [SerializeField] private ArenaBounds arenaBounds;
    [SerializeField] private float dangerDistanceFromEdge = 3f;

    [Header("AI")]
    [Range(0f, 1f)]
    [SerializeField] private float sideMovementChance = 0.30f;

    [Range(0f, 1f)]
    [SerializeField] private float fakeDirectionChance = 0.20f;

    [Range(0f, 1f)]
    [SerializeField] private float predictionChance = 0.70f;

    private MatchManager matchManager;
    private RoundManager roundManager;

    private GameObject currentTarget;

    private void Awake()
    {
        if (playerController == null)
            playerController =
                GetComponent<PlayerController>();

        if (rb == null)
            rb =
                GetComponent<Rigidbody>();
    }

    private void Start()
    {
        matchManager =
            FindObjectOfType<MatchManager>();

        roundManager =
            FindObjectOfType<RoundManager>();

        if (roundManager != null)
        {
            roundManager.OnStateChanged +=
                HandleRoundStateChanged;
        }
        else
        {
            Debug.LogError(
                $"[BOT] {gameObject.name}: " +
                "RoundManager não encontrado!"
            );
        }
    }

    public void SetArenaBounds(
        ArenaBounds bounds
    )
    {
        arenaBounds = bounds;
    }

    private void OnDestroy()
    {
        if (roundManager != null)
        {
            roundManager.OnStateChanged -=
                HandleRoundStateChanged;
        }
    }

    private void HandleRoundStateChanged(
        RoundState state
    )
    {
        if (state != RoundState.Choosing)
            return;

        MakeDecision();
    }

    #region DECISION

    private void MakeDecision()
    {
        if (
            playerController == null ||
            matchManager == null
        )
            return;

        FindBestTarget();

        Vector3 direction;
        int charge;

        /*
         * PRIORIDADE 1
         * Preciso sobreviver.
         */
        if (IsNearEdge())
        {
            direction =
                GetDirectionToCenter();

            charge =
                ChooseEscapeCharge();
        }
        /*
         * PRIORIDADE 2
         * Atacar alguém vulnerável.
         */
        else if (
            currentTarget != null &&
            IsTargetNearEdge()
        )
        {
            direction =
                GetAttackDirection();

            PlayerConfig config = GameManager.Instance != null ? GameManager.Instance.PlayerConfig : null;
            if (config == null)
                return;
            charge =
                Random.Range(
                    Mathf.Clamp(7, Mathf.CeilToInt(config.MinCharge), Mathf.FloorToInt(config.MaxCharge)),
                    (int)config.MaxCharge + 1
                );
        }
        /*
         * PRIORIDADE 3
         * Comportamento normal.
         */
        else
        {
            direction =
                ChooseBestDirection();

            charge =
                ChooseCharge();
        }

        playerController.SetAimDirection(
            direction
        );

        playerController.SetCharge(
            charge
        );

        Debug.Log(
            $"[BOT] {gameObject.name} | " +
            $"Força: {charge} | " +
            $"Direção: {direction}"
        );
    }
    private int ChooseEscapeCharge()
    {
        if (arenaBounds == null)
            return 5;

        float distanceFromEdge =
            arenaBounds.GetDistanceFromEdge(
                transform.position
            );

        /*
         * Extremamente perto da borda.
         */
        if (distanceFromEdge < 1f)
        {
            return ClampCharge(Random.Range(5, 8));
        }

        /*
         * Perigo moderado.
         */
        if (distanceFromEdge < 2f)
        {
            return ClampCharge(Random.Range(3, 6));
        }

        /*
         * Ainda está relativamente seguro.
         */
        return ClampCharge(Random.Range(2, 5));
    }
    #endregion

    #region TARGET

    private void FindBestTarget()
    {
        currentTarget = null;

        float bestScore =
            float.MinValue;

        foreach (
            GameObject player
            in matchManager.AllPlayers
        )
        {
            if (
                player == null ||
                player == gameObject ||
                !player.activeInHierarchy
            )
                continue;

            PlayerDeath death =
                player.GetComponent<PlayerDeath>();

            if (
                death != null &&
                death.IsDead()
            )
                continue;

            float score =
                EvaluateTarget(player);

            if (score > bestScore)
            {
                bestScore = score;
                currentTarget = player;
            }
        }
    }

    private float EvaluateTarget(
        GameObject target
    )
    {
        float distance =
            Vector3.Distance(
                transform.position,
                target.transform.position
            );

        float score =
            -distance;

        if (arenaBounds != null)
        {
            float edgeDistance =
                arenaBounds.GetDistanceFromEdge(
                    target.transform.position
                );

            /*
             * Quanto mais perto da borda,
             * mais interessante o alvo.
             */
            if (
                edgeDistance <
                dangerDistanceFromEdge
            )
            {
                score += 15f;
            }

            /*
             * Alvos no meio da arena
             * continuam sendo válidos,
             * mas têm prioridade menor.
             */
            else if (edgeDistance < 6f)
            {
                score += 5f;
            }
        }

        return score;
    }

    #endregion

    #region DIRECTION

    private Vector3 ChooseBestDirection()
    {
        float random =
            Random.value;

        /*
         * FINTA
         */
        if (
            random <
            fakeDirectionChance
        )
        {
            return GetSideDirection();
        }

        /*
         * MOVIMENTO LATERAL
         */
        if (
            random <
            fakeDirectionChance +
            sideMovementChance
        )
        {
            return GetSideDirection();
        }

        /*
         * ATAQUE
         */
        return GetAttackDirection();
    }

    private Vector3 GetAttackDirection()
    {
        if (currentTarget == null)
            return GetDirectionToCenter();

        Vector3 targetPosition =
            currentTarget.transform.position;

        /*
         * Às vezes tenta prever
         * a posição do alvo.
         */
        Rigidbody targetRb =
            currentTarget.GetComponent<Rigidbody>();

        if (
            targetRb != null &&
            Random.value < predictionChance
        )
        {
            Vector3 velocity =
                targetRb.linearVelocity;

            velocity.y = 0f;

            targetPosition +=
                velocity * 0.25f;
        }

        Vector3 direction =
            targetPosition -
            transform.position;

        direction.y = 0f;

        if (
            direction.sqrMagnitude <=
            0.001f
        )
        {
            return transform.forward;
        }

        return direction.normalized;
    }

    private Vector3 GetSideDirection()
    {
        Vector3 targetDirection =
            GetAttackDirection();

        if (
            targetDirection.sqrMagnitude <=
            0.001f
        )
        {
            targetDirection =
                transform.forward;
        }

        Vector3 sideDirection;

        if (Random.value > 0.5f)
        {
            sideDirection =
                Quaternion.Euler(
                    0f,
                    90f,
                    0f
                ) *
                targetDirection;
        }
        else
        {
            sideDirection =
                Quaternion.Euler(
                    0f,
                    -90f,
                    0f
                ) *
                targetDirection;
        }

        /*
         * Não fica completamente lateral.
         * Isso cria diagonais e fintas.
         */
        return (
            sideDirection * 0.75f +
            targetDirection * 0.25f
        ).normalized;
    }

    private Vector3 GetDirectionToCenter()
    {
        if (arenaBounds == null)
            return transform.forward;

        return arenaBounds.GetDirectionToCenter(
            transform.position
        );
    }

    #endregion

    #region CHARGE

    private int ChooseCharge()
    {
        int round =
            CurrentRound;

        /*
         * PRIMEIRA RODADA
         *
         * Todo mundo começa cauteloso.
         */
        if (round == 1)
        {
            return ClampCharge(Random.Range(2, 6));
        }

        if (currentTarget == null)
        {
            return ClampCharge(Random.Range(3, 6));
        }

        float distance =
            Vector3.Distance(
                transform.position,
                currentTarget.transform.position
            );

        if (distance < 3f)
        {
            return ClampCharge(Random.Range(2, 5));
        }

        if (distance < 6f)
        {
            return ClampCharge(Random.Range(3, 7));
        }

        if (distance < 10f)
        {
            return ClampCharge(Random.Range(5, 8));
        }

        return ClampCharge(Random.Range(6, 9));
    }

    private int ClampCharge(int charge)
    {
        PlayerConfig config = GameManager.Instance != null ? GameManager.Instance.PlayerConfig : null;
        return config == null ? charge : Mathf.Clamp(charge, Mathf.CeilToInt(config.MinCharge), Mathf.FloorToInt(config.MaxCharge));
    }

    #endregion

    #region EDGE

    private bool IsNearEdge()
    {
        if (arenaBounds == null)
            return false;

        return arenaBounds.GetDistanceFromEdge(
            transform.position
        ) <
        dangerDistanceFromEdge;
    }

    private bool IsTargetNearEdge()
    {
        if (
            arenaBounds == null ||
            currentTarget == null
        )
            return false;

        return arenaBounds.GetDistanceFromEdge(
            currentTarget.transform.position
        ) <
        dangerDistanceFromEdge;
    }

    private int CurrentRound
    {
        get
        {
            if (roundManager == null)
                return 1;

            return roundManager.CurrentRound;
        }
    }
    #endregion
}
