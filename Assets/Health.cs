using UnityEngine;

public class Health : MonoBehaviour
{
    public float maxHealth = 100f;

    private float currentHealth;

    private void Start()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(float damage)
    {
        currentHealth -= damage;

        Debug.Log($"[{gameObject.name}] HP: {currentHealth}");

        if (currentHealth <= 0f)
        {
            Die();
        }
    }

    private void Die()
    {
        Debug.Log($"[{gameObject.name}] Died");

        gameObject.SetActive(false);
    }
}