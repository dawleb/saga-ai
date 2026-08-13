using UnityEngine;

public class Health : MonoBehaviour
{
    [Header("Health")]
    public float maxHealth = 100f;

    public float CurrentHealth { get; private set; }

    private void Awake()
    {
        ResetHealth();
    }

    public void ResetHealth()
    {
        CurrentHealth = maxHealth;
    }

    public void SetHealth(float value)
    {
        CurrentHealth = Mathf.Clamp(
            value,
            0f,
            maxHealth
        );
    }

    public void TakeDamage(float damage)
    {
        if (damage <= 0f)
            return;

        SetHealth(
            CurrentHealth - damage
        );
    }

    public bool IsDead()
    {
        return CurrentHealth <= 0f;
    }
}