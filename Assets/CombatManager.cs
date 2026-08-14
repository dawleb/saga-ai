using UnityEngine;

public class CombatManager : MonoBehaviour
{
    public static CombatManager Instance;

    [Header("Combatants")]
    public Health player;
    public Health monster;

    [Header("Combat")]
    public float roundCooldown = 1f;

    [Header("Damage")]
    public float damageMin = 8f;
    public float damageMax = 15f;

    [Header("Range")]
    public float attackRange = 1.5f;

    // Trigger name on the Animator controllers. The animation is purely
    // visual: CombatManager applies the damage itself.
    private const string AttackTrigger = "Attack";

    private Animator playerAnimator;
    private Animator monsterAnimator;

    private float nextRoundTime;
    private bool fightFinished;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        // Jeżeli nie ustawiono ręcznie w Inspectorze,
        // spróbuj znaleźć Health w scenie.
        if (player == null || monster == null)
        {
            Health[] healthObjects =
                FindObjectsOfType<Health>();

            foreach (Health health in healthObjects)
            {
                SimpleAgent agent =
                    health.GetComponent<SimpleAgent>();

                if (agent != null)
                {
                    monster = health;

                    if (agent.target != null)
                    {
                        player =
                            agent.target.GetComponent<Health>();
                    }

                    break;
                }
            }
        }

        if (player == null)
        {
            Debug.LogError(
                "[COMBAT] Player Health nie został znaleziony!"
            );
        }

        if (monster == null)
        {
            Debug.LogError(
                "[COMBAT] Monster Health nie został znaleziony!"
            );
        }

        playerAnimator = FindAttackAnimator(player, "Player");
        monsterAnimator = FindAttackAnimator(monster, "Monster");

        if (player != null && monster != null)
        {
            Debug.Log(
                $"[COMBAT] Player HP: {player.CurrentHealth}"
            );

            Debug.Log(
                $"[COMBAT] Monster HP: {monster.CurrentHealth}"
            );

            nextRoundTime =
                Time.time + 0.5f;
        }
    }

    // Returns the combatant's Animator, but only if it can actually play an
    // attack. Damage does not depend on this, so a missing animation never
    // stops the fight.
    private Animator FindAttackAnimator(
        Health combatant,
        string label
    )
    {
        if (combatant == null)
            return null;

        Animator animator =
            combatant.GetComponentInChildren<Animator>();

        if (animator == null)
        {
            Debug.LogWarning(
                $"[COMBAT] {label} has no Animator, " +
                "it will fight without an attack animation."
            );

            return null;
        }

        // Dropping the reference avoids one warning per round from Unity.
        if (!HasAttackTrigger(animator))
        {
            Debug.LogWarning(
                $"[COMBAT] {label}: Animator has no '{AttackTrigger}' " +
                "trigger, it will fight without an attack animation."
            );

            return null;
        }

        return animator;
    }

    private static bool HasAttackTrigger(Animator animator)
    {
        foreach (AnimatorControllerParameter parameter in animator.parameters)
        {
            if (parameter.type == AnimatorControllerParameterType.Trigger &&
                parameter.name == AttackTrigger)
            {
                return true;
            }
        }

        return false;
    }

    private void Update()
    {
        if (fightFinished)
            return;

        if (player == null || monster == null)
            return;

        if (!player.gameObject.activeSelf)
            return;

        if (!monster.gameObject.activeSelf)
            return;

        float distance =
            Vector3.Distance(
                player.transform.position,
                monster.transform.position
            );

        if (distance > attackRange)
            return;

        if (Time.time < nextRoundTime)
            return;

        ResolveRound();

        nextRoundTime =
            Time.time + roundCooldown;
    }

    private void ResolveRound()
    {
        // Player attacks first, then the monster, so both can never drop to
        // zero in the same round.
        Attack(
            playerAnimator,
            player,
            monster
        );

        if (fightFinished)
            return;

        Attack(
            monsterAnimator,
            monster,
            player
        );
    }

    private void Attack(
        Animator attackerAnimator,
        Health attacker,
        Health defender
    )
    {
        if (attackerAnimator != null)
            attackerAnimator.SetTrigger(AttackTrigger);

        float damage =
            Random.Range(
                damageMin,
                damageMax
            );

        float oldHealth =
            defender.CurrentHealth;

        defender.TakeDamage(damage);

        Debug.Log(
            $"[ROUND] {attacker.name} attacks {defender.name} " +
            $"for {damage:F1}"
        );

        Debug.Log(
            $"[ROUND] {defender.name} HP: " +
            $"{oldHealth:F1} -> {defender.CurrentHealth:F1}"
        );

        if (defender.IsDead())
            FinishFight(defender);
    }

    private void FinishFight(Health loser)
    {
        fightFinished = true;

        Debug.Log(
            loser == monster
                ? "[COMBAT] PLAYER WINS!"
                : "[COMBAT] MONSTER WINS!"
        );

        loser.gameObject.SetActive(false);
    }

    public void RegisterCombatants(
        Health playerHealth,
        Health monsterHealth
    )
    {
        player = playerHealth;
        monster = monsterHealth;

        fightFinished = false;

        playerAnimator = FindAttackAnimator(player, "Player");
        monsterAnimator = FindAttackAnimator(monster, "Monster");

        nextRoundTime =
            Time.time + 0.5f;

        Debug.Log(
            "[COMBAT] Combatants registered."
        );
    }
}
