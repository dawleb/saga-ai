using UnityEngine;

public class Health : MonoBehaviour
{
    public float maxHealth = 100f;

    private float currentHealth;

    public float CurrentHealth => currentHealth;

    private void Start()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(float damage)
    {
        if (currentHealth <= 0f)
            return;

        currentHealth -= damage;

        Debug.Log(
            $"[{gameObject.name}] HP: {currentHealth}"
        );

        if (currentHealth <= 0f)
        {
            currentHealth = 0f;
            Die();
        }
    }

    public void SetHealth(float newHealth)
    {
        currentHealth = Mathf.Max(0f, newHealth);

        Debug.Log(
            $"[{gameObject.name}] HP: {currentHealth}"
        );

        if (currentHealth <= 0f)
        {
            Die();
        }
    }

    private void Die()
    {
        if (!gameObject.activeSelf)
            return;

        Debug.Log(
            $"[{gameObject.name}] Died"
        );

        gameObject.SetActive(false);
    }
}