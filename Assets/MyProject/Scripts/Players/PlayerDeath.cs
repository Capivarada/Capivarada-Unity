using System;
using System.Collections;
using UnityEngine;

public class PlayerDeath : MonoBehaviour
{
    [Header("Death")]
    [SerializeField]
    private float deathHeight = -5f;

    private bool isDead;

    private GameObject lastAttacker;

    public event Action<PlayerDeath> OnPlayerDeath;

    public bool IsDead()
    {
        return isDead;
    }

    public void SetLastAttacker(
        GameObject attacker
    )
    {
        if (
            attacker == null ||
            attacker == gameObject
        )
        {
            return;
        }

        lastAttacker = attacker;
    }

    private void Update()
    {
        if (isDead)
            return;

        if (
            transform.position.y <=
            deathHeight
        )
        {
            Die();
        }
    }

    private void Die()
    {
        if (isDead)
            return;

        isDead = true;
        
        PlayerController player = gameObject.GetComponent<PlayerController>();
        
        //player.animator.SetTrigger("Die");
        
        RegisterKill();

        Debug.Log(
            $"{gameObject.name} foi eliminado!"
        );

        OnPlayerDeath?.Invoke(this);
        gameObject.SetActive(false);
    }

    private void RegisterKill()
    {
        if (lastAttacker == null)
            return;

        PlayerMatchData attackerData =
            lastAttacker.GetComponent<
                PlayerMatchData
            >();

        if (attackerData == null)
            return;

        attackerData.AddKill();

        Debug.Log(
            $"[KILL] {lastAttacker.name} " +
            $"eliminou {gameObject.name}"
        );
    }
}