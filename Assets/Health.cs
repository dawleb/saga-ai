using UnityEngine;

public class Health : MonoBehaviour
{
    [Header("Health")]
    public float maxHealth = 100f;

    public float CurrentHealth { get; private set; }

    [Header("Death Animation")]
    [Tooltip("Number of death animations. Example: 2 = indexes 0 and 1.")]
    public int deathAnimationCount = 2;

    private Animator animator;

    private const string GetDamageTrigger = "GetDamage";
    private const string DeathTrigger = "Death";
    private const string DeathIndexParameter = "DeathIndex";

    private void Awake()
    {
        animator =
            GetComponentInChildren<Animator>();

        ResetHealth();
    }

    // ====================================
    // RESET HEALTH
    // ====================================

    public void ResetHealth()
    {
        CurrentHealth = maxHealth;

        if (animator == null)
        {
            return;
        }

        animator.Rebind();
        animator.Update(0f);

        animator.ResetTrigger(
            GetDamageTrigger
        );

        animator.ResetTrigger(
            DeathTrigger
        );

        animator.SetInteger(
            DeathIndexParameter,
            0
        );
    }

    // ====================================
    // SET HEALTH
    // ====================================

    public void SetHealth(float value)
    {
        CurrentHealth =
            Mathf.Clamp(
                value,
                0f,
                maxHealth
            );
    }

    // ====================================
    // TAKE DAMAGE
    // ====================================

    public void TakeDamage(float damage)
    {
        if (damage <= 0f)
        {
            return;
        }

        if (IsDead())
        {
            return;
        }

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

    // ====================================
    // DAMAGE ANIMATION
    // ====================================

    private void PlayDamageAnimation()
    {
        if (animator == null)
        {
            return;
        }

        animator.SetTrigger(
            GetDamageTrigger
        );
    }

    // ====================================
    // DEATH ANIMATION
    // ====================================

    private void PlayDeathAnimation()
    {
        if (animator == null)
        {
            return;
        }

        if (deathAnimationCount <= 0)
        {
            Debug.LogWarning(
                $"[HEALTH] {name}: " +
                "deathAnimationCount must be greater than 0."
            );

            return;
        }

        // ====================================
        // RANDOM DEATH INDEX
        // ====================================

        int deathIndex =
            Random.Range(
                0,
                deathAnimationCount
            );

        animator.SetInteger(
            DeathIndexParameter,
            deathIndex
        );

        // ====================================
        // DEATH TRIGGER
        // ====================================

        animator.SetTrigger(
            DeathTrigger
        );

        Debug.Log(
            $"[DEATH] {name} | DeathIndex = {deathIndex}"
        );
    }

    // ====================================
    // IS DEAD
    // ====================================

    public bool IsDead()
    {
        return CurrentHealth <= 0f;
    }
}