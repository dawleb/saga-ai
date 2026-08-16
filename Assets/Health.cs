using UnityEngine;

public class Health : MonoBehaviour
{
    [Header("Health")]
    public float maxHealth = 100f;

    public float CurrentHealth { get; private set; }

    private Animator animator;

    private const string GetDamageTrigger = "GetDamage";
    private const string Death1Trigger = "Death1";
    private const string Death2Trigger = "Death2";

    private void Awake()
    {
        animator =
            GetComponentInChildren<Animator>();

        ResetHealth();
    }

    public void ResetHealth()
    {
        CurrentHealth = maxHealth;

        if (animator != null)
        {
            animator.Rebind();
            animator.Update(0f);

            animator.ResetTrigger(
                GetDamageTrigger
            );

            animator.ResetTrigger(
                Death1Trigger
            );

            animator.ResetTrigger(
                Death2Trigger
            );
        }
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

        if (IsDead())
            return;

        SetHealth(
            CurrentHealth - damage
        );

        if (IsDead())
        {
            PlayDeathAnimation();
        }
        else
        {
            PlayDamageAnimation();
        }
    }

    private void PlayDamageAnimation()
    {
        if (animator == null)
            return;

        animator.SetTrigger(
            GetDamageTrigger
        );
    }

    private void PlayDeathAnimation()
    {
        if (animator == null)
            return;

        if (Random.value < 0.5f)
        {
            animator.SetTrigger(
                Death1Trigger
            );
        }
        else
        {
            animator.SetTrigger(
                Death2Trigger
            );
        }
    }

    public bool IsDead()
    {
        return CurrentHealth <= 0f;
    }
}