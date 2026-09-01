using System;
using UnityEngine;

public class PlayerDeath : MonoBehaviour
{
    [Header("Death")]
    [SerializeField] private float deathHeight = -5f;

    private bool isDead;

    public event Action<PlayerDeath> OnPlayerDeath;

    private void Update()
    {
        if (isDead)
            return;

        if (transform.position.y <= deathHeight)
        {
            Die();
        }
    }

    private void Die()
    {
        isDead = true;

        Debug.Log($"{gameObject.name} foi eliminado!");

        OnPlayerDeath?.Invoke(this);

        gameObject.SetActive(false);
    }

    public bool IsDead()
    {
        return isDead;
    }
}