using System.Collections;
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

    [Header("Victory")]
    public float victoryDanceDuration = 10f;

    private const string AttackTrigger = "Attack";
    private const string DancingBool = "IsDancing";
    private const string WalkingBool = "IsWalking";

    private Animator playerAnimator;
    private Animator monsterAnimator;

    private PlayerClickController playerClickController;

    private float nextRoundTime;
    private bool fightFinished;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
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

        playerAnimator =
            FindAttackAnimator(
                player,
                "Player"
            );

        monsterAnimator =
            FindAttackAnimator(
                monster,
                "Monster"
            );

        if (player != null)
        {
            playerClickController =
                player.GetComponent<PlayerClickController>();
        }

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

    private Animator FindAttackAnimator(
        Health combatant,
        string label
    )
    {
        if (combatant == null)
        {
            return null;
        }

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

        if (!HasTrigger(
            animator,
            AttackTrigger
        ))
        {
            Debug.LogWarning(
                $"[COMBAT] {label}: Animator has no " +
                $"'{AttackTrigger}' trigger. " +
                "Attack animation will be skipped."
            );
        }

        return animator;
    }

    private static bool HasTrigger(
        Animator animator,
        string triggerName
    )
    {
        foreach (
            AnimatorControllerParameter parameter
            in animator.parameters
        )
        {
            if (
                parameter.type ==
                AnimatorControllerParameterType.Trigger &&
                parameter.name == triggerName
            )
            {
                return true;
            }
        }

        return false;
    }

    private void Update()
    {
        if (fightFinished)
        {
            return;
        }

        if (player == null || monster == null)
        {
            return;
        }

        if (!player.gameObject.activeSelf)
        {
            return;
        }

        if (!monster.gameObject.activeSelf)
        {
            return;
        }

        float distance =
            Vector3.Distance(
                player.transform.position,
                monster.transform.position
            );

        if (distance > attackRange)
        {
            return;
        }

        if (Time.time < nextRoundTime)
        {
            return;
        }

        ResolveRound();

        nextRoundTime =
            Time.time + roundCooldown;
    }

    private void ResolveRound()
    {
        Attack(
            playerAnimator,
            player,
            monster
        );

        if (fightFinished)
        {
            return;
        }

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
        {
            attackerAnimator.SetTrigger(
                AttackTrigger
            );
        }

        float damage =
            Random.Range(
                damageMin,
                damageMax
            );

        float oldHealth =
            defender.CurrentHealth;

        defender.TakeDamage(damage);

        Debug.Log(
            $"[ROUND] {attacker.name} attacks " +
            $"{defender.name} for {damage:F1}"
        );

        Debug.Log(
            $"[ROUND] {defender.name} HP: " +
            $"{oldHealth:F1} -> " +
            $"{defender.CurrentHealth:F1}"
        );

        if (defender.IsDead())
        {
            FinishFight(defender);
        }
    }

    private void FinishFight(
        Health loser
    )
    {
        fightFinished = true;

        bool playerWon =
            loser == monster;

        Debug.Log(
            playerWon
                ? "[COMBAT] PLAYER WINS!"
                : "[COMBAT] MONSTER WINS!"
        );

        loser.gameObject.SetActive(false);

        if (playerWon)
        {
            StartCoroutine(
                PlayerVictoryDance()
            );
        }
    }

    private IEnumerator PlayerVictoryDance()
    {
        if (player == null)
        {
            yield break;
        }

        if (playerAnimator == null)
        {
            Debug.LogWarning(
                "[COMBAT] Player has no Animator. " +
                "Victory dance cannot play."
            );

            yield break;
        }

        if (playerClickController != null)
        {
            playerClickController.enabled = false;
        }

        playerAnimator.ResetTrigger(
            AttackTrigger
        );

        playerAnimator.SetBool(
            WalkingBool,
            false
        );

        playerAnimator.SetBool(
            DancingBool,
            true
        );

        Debug.Log(
            $"[COMBAT] Victory dance started " +
            $"for {victoryDanceDuration:F1} seconds."
        );

        yield return new WaitForSeconds(
            victoryDanceDuration
        );

        playerAnimator.SetBool(
            DancingBool,
            false
        );

        playerAnimator.SetBool(
            WalkingBool,
            false
        );

        if (playerClickController != null)
        {
            playerClickController.enabled = true;
        }

        Debug.Log(
            "[COMBAT] Victory dance finished."
        );
    }

    public void RegisterCombatants(
        Health playerHealth,
        Health monsterHealth
    )
    {
        player = playerHealth;
        monster = monsterHealth;

        fightFinished = false;

        playerAnimator =
            FindAttackAnimator(
                player,
                "Player"
            );

        monsterAnimator =
            FindAttackAnimator(
                monster,
                "Monster"
            );

        if (player != null)
        {
            playerClickController =
                player.GetComponent<PlayerClickController>();
        }

        nextRoundTime =
            Time.time + 0.5f;

        Debug.Log(
            "[COMBAT] Combatants registered."
        );
    }
}