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

    [Header("Attack Timing")]
    public float attackAnimationDuration = 0.8f;
    public float damageDelay = 0.35f;

    [Header("Damage")]
    public float damageMin = 8f;
    public float damageMax = 15f;

    [Header("Range")]
    public float attackRange = 1.1f;

    [Header("Combat Rotation")]
    public float combatRotationSpeed = 10f;

    [Header("Victory")]
    public float tauntDuration = 3f;

    private const string AttackTrigger = "Attack";
    private const string BiteTrigger = "Bite";
    private const string DeathTrigger = "Death";
    private const string TauntTrigger = "Taunt";

    private const string DancingBool = "IsDancing";
    private const string WalkingBool = "IsWalking";

    private Animator playerAnimator;
    private Animator monsterAnimator;

    private PlayerClickController playerClickController;

    private float nextRoundTime;

    private bool fightFinished;
    private bool roundInProgress;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        FindCombatants();

        if (player == null)
        {
            Debug.LogError(
                "[COMBAT] Player Health not found!"
            );
        }

        if (monster == null)
        {
            Debug.LogError(
                "[COMBAT] Monster Health not found!"
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

        FindPlayerClickController();

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

    // ====================================
    // FIND COMBATANTS
    // ====================================

    private void FindCombatants()
    {
        Health[] healthObjects =
            FindObjectsOfType<Health>();

        foreach (Health health in healthObjects)
        {
            if (health == null)
            {
                continue;
            }

            SimpleAgent agent =
                health.GetComponentInParent<SimpleAgent>();

            if (agent == null)
            {
                continue;
            }

            monster = health;

            if (agent.target != null)
            {
                Health targetHealth =
                    agent.target.GetComponentInChildren<Health>();

                if (targetHealth != null)
                {
                    player = targetHealth;
                }
            }

            break;
        }
    }

    private void FindPlayerClickController()
    {
        if (player == null)
        {
            return;
        }

        playerClickController =
            player.GetComponent<PlayerClickController>();

        if (playerClickController == null)
        {
            playerClickController =
                player.GetComponentInParent<PlayerClickController>();
        }

        if (playerClickController == null)
        {
            playerClickController =
                player.GetComponentInChildren<PlayerClickController>();
        }
    }

    // ====================================
    // FIND ANIMATOR
    // ====================================

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
            animator =
                combatant.GetComponentInParent<Animator>();
        }

        if (animator == null)
        {
            Debug.LogWarning(
                $"[COMBAT] {label} has no Animator."
            );

            return null;
        }

        return animator;
    }

    // ====================================
    // CHECK ANIMATOR PARAMETER
    // ====================================

    private static bool HasTrigger(
        Animator animator,
        string triggerName
    )
    {
        if (animator == null)
        {
            return false;
        }

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

    // ====================================
    // UPDATE
    // ====================================

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

        if (!player.gameObject.activeInHierarchy)
        {
            return;
        }

        if (!monster.gameObject.activeInHierarchy)
        {
            return;
        }

        if (player.IsDead() || monster.IsDead())
        {
            return;
        }

        float distance =
            Vector3.Distance(
                player.transform.position,
                monster.transform.position
            );

        if (distance <= attackRange)
        {
            RotateTowardsOpponent(
                player.transform,
                monster.transform
            );

            RotateTowardsOpponent(
                monster.transform,
                player.transform
            );
        }

        if (roundInProgress)
        {
            return;
        }

        if (distance > attackRange)
        {
            return;
        }

        if (Time.time < nextRoundTime)
        {
            return;
        }

        StartCoroutine(
            ResolveRound()
        );
    }

    // ====================================
    // ROTATION
    // ====================================

    private void RotateTowardsOpponent(
        Transform fighter,
        Transform opponent
    )
    {
        if (fighter == null || opponent == null)
        {
            return;
        }

        Vector3 direction =
            opponent.position -
            fighter.position;

        direction.y = 0f;

        if (direction.sqrMagnitude < 0.001f)
        {
            return;
        }

        Quaternion targetRotation =
            Quaternion.LookRotation(
                direction
            );

        fighter.rotation =
            Quaternion.Slerp(
                fighter.rotation,
                targetRotation,
                combatRotationSpeed *
                Time.deltaTime
            );
    }

    // ====================================
    // RESOLVE ROUND
    // ====================================

    private IEnumerator ResolveRound()
    {
        roundInProgress = true;

        yield return StartCoroutine(
            PerformAttack(
                playerAnimator,
                player,
                monster,
                false
            )
        );

        if (fightFinished)
        {
            roundInProgress = false;
            yield break;
        }

        yield return StartCoroutine(
            PerformAttack(
                monsterAnimator,
                monster,
                player,
                true
            )
        );

        if (!fightFinished)
        {
            nextRoundTime =
                Time.time + roundCooldown;
        }

        roundInProgress = false;
    }

    // ====================================
    // PERFORM ATTACK
    // ====================================

    private IEnumerator PerformAttack(
        Animator attackerAnimator,
        Health attacker,
        Health defender,
        bool monsterAttacking
    )
    {
        if (attacker == null || defender == null)
        {
            yield break;
        }

        if (!attacker.gameObject.activeInHierarchy ||
            !defender.gameObject.activeInHierarchy)
        {
            yield break;
        }

        if (attacker.IsDead() || defender.IsDead())
        {
            yield break;
        }

        RotateTowardsOpponent(
            attacker.transform,
            defender.transform
        );

        string selectedTrigger =
            AttackTrigger;

        if (monsterAttacking)
        {
            bool hasBite =
                HasTrigger(
                    attackerAnimator,
                    BiteTrigger
                );

            bool hasAttack =
                HasTrigger(
                    attackerAnimator,
                    AttackTrigger
                );

            if (hasBite)
            {
                selectedTrigger =
                    BiteTrigger;
            }
            else if (hasAttack)
            {
                selectedTrigger =
                    AttackTrigger;

                Debug.LogWarning(
                    "[COMBAT] Zombie has no Bite trigger. " +
                    "Falling back to Attack."
                );
            }
            else
            {
                Debug.LogError(
                    "[COMBAT] Zombie Animator has neither " +
                    "'Bite' nor 'Attack' trigger."
                );

                yield break;
            }
        }

        if (attackerAnimator != null)
        {
            attackerAnimator.ResetTrigger(
                AttackTrigger
            );

            attackerAnimator.ResetTrigger(
                BiteTrigger
            );

            if (HasTrigger(
                attackerAnimator,
                selectedTrigger
            ))
            {
                attackerAnimator.SetTrigger(
                    selectedTrigger
                );

                Debug.Log(
                    $"[COMBAT] {attacker.name} uses " +
                    $"{selectedTrigger}."
                );
            }
            else
            {
                Debug.LogWarning(
                    $"[COMBAT] {attacker.name} cannot use " +
                    $"'{selectedTrigger}'. Trigger does not exist."
                );
            }
        }

        yield return new WaitForSeconds(
            damageDelay
        );

        if (fightFinished)
        {
            yield break;
        }

        if (!attacker.gameObject.activeInHierarchy ||
            !defender.gameObject.activeInHierarchy)
        {
            yield break;
        }

        if (attacker.IsDead() || defender.IsDead())
        {
            yield break;
        }

        float damage =
            Random.Range(
                damageMin,
                damageMax
            );

        float oldHealth =
            defender.CurrentHealth;

        defender.TakeDamage(
            damage
        );

        Debug.Log(
            $"[ROUND] {attacker.name} attacks " +
            $"{defender.name} for {damage:F1}"
        );

        Debug.Log(
            $"[ROUND] {defender.name} HP: " +
            $"{oldHealth:F1} -> " +
            $"{defender.CurrentHealth:F1}"
        );

        if (defender.IsDead() ||
            defender.CurrentHealth <= 0f)
        {
            FinishFight(
                defender
            );

            yield break;
        }

        float remainingTime =
            Mathf.Max(
                0f,
                attackAnimationDuration -
                damageDelay
            );

        yield return new WaitForSeconds(
            remainingTime
        );
    }

    // ====================================
    // FINISH FIGHT
    // ====================================

    private void FinishFight(
        Health loser
    )
    {
        if (fightFinished || loser == null)
        {
            return;
        }

        fightFinished = true;

        bool playerWon =
            loser == monster;

        Debug.Log(
            playerWon
                ? "[COMBAT] PLAYER WINS!"
                : "[COMBAT] MONSTER WINS!"
        );

        // --------------------------------
        // STOP DEAD MONSTER AI
        // --------------------------------

        if (loser == monster)
        {
            SimpleAgent agent =
                monster.GetComponentInParent<SimpleAgent>();

            if (agent != null)
            {
                agent.SetDead();

                agent.enabled = false;

                Debug.Log(
                    "[COMBAT] Monster AI stopped after death."
                );
            }
        }

        // --------------------------------
        // HIDE LOSER HEALTH BAR
        // --------------------------------

        HideHealthBar(
            loser
        );

        // --------------------------------
        // DEATH
        // --------------------------------

        PlayDeath(
            loser
        );

        // --------------------------------
        // VICTORY
        // --------------------------------

        if (playerWon)
        {
            StartCoroutine(
                PlayerVictory()
            );
        }
        else
        {
            StartCoroutine(
                MonsterVictory()
            );
        }
    }

    // ====================================
    // HIDE HEALTH BAR
    // ====================================

    private void HideHealthBar(
        Health loser
    )
    {
        if (loser == null)
        {
            return;
        }

        HealthBar[] healthBars =
            loser.GetComponentsInChildren<HealthBar>(
                true
            );

        foreach (HealthBar healthBar in healthBars)
        {
            if (healthBar == null)
            {
                continue;
            }

            // IMPORTANT:
            // Do not only disable the HealthBar component.
            // The component creates a separate World Space Canvas.
            // We must disable that generated Canvas too.

            healthBar.HideBar();

            Debug.Log(
                $"[COMBAT] Health bar hidden for {loser.name}."
            );
        }
    }

    // ====================================
    // DEATH
    // ====================================

    private void PlayDeath(
        Health loser
    )
    {
        if (loser == null)
        {
            return;
        }

        Animator loserAnimator =
            loser == player
                ? playerAnimator
                : monsterAnimator;

        if (loserAnimator == null)
        {
            Debug.LogWarning(
                "[COMBAT] Dead character has no Animator."
            );

            return;
        }

        loserAnimator.applyRootMotion =
            false;

        loserAnimator.ResetTrigger(
            AttackTrigger
        );

        loserAnimator.ResetTrigger(
            BiteTrigger
        );

        loserAnimator.ResetTrigger(
            TauntTrigger
        );

        loserAnimator.SetBool(
            WalkingBool,
            false
        );

        loserAnimator.SetBool(
            DancingBool,
            false
        );

        if (HasTrigger(
            loserAnimator,
            DeathTrigger
        ))
        {
            loserAnimator.SetTrigger(
                DeathTrigger
            );

            Debug.Log(
                $"[COMBAT] {loser.name} Death started."
            );
        }
        else
        {
            Debug.LogWarning(
                $"[COMBAT] {loser.name} Animator has no " +
                "'Death' trigger."
            );
        }

        if (loser == player)
        {
            FindPlayerClickController();

            if (playerClickController != null)
            {
                playerClickController.enabled =
                    false;
            }
        }
    }

    // ====================================
    // PLAYER VICTORY
    // ====================================

    private IEnumerator PlayerVictory()
    {
        if (player == null)
        {
            yield break;
        }

        if (playerAnimator == null)
        {
            yield break;
        }

        if (playerClickController != null)
        {
            playerClickController.enabled =
                false;
        }

        playerAnimator.ResetTrigger(
            AttackTrigger
        );

        playerAnimator.ResetTrigger(
            BiteTrigger
        );

        playerAnimator.ResetTrigger(
            TauntTrigger
        );

        playerAnimator.SetBool(
            WalkingBool,
            false
        );

        playerAnimator.SetBool(
            DancingBool,
            false
        );

        if (HasTrigger(
            playerAnimator,
            TauntTrigger
        ))
        {
            playerAnimator.SetTrigger(
                TauntTrigger
            );

            Debug.Log(
                "[COMBAT] Player victory Taunt started."
            );
        }

        yield return new WaitForSeconds(
            tauntDuration
        );

        playerAnimator.ResetTrigger(
            TauntTrigger
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
            playerClickController.enabled =
                true;
        }

        Debug.Log(
            "[COMBAT] Player victory Taunt finished."
        );
    }

    // ====================================
    // MONSTER VICTORY
    // ====================================

    private IEnumerator MonsterVictory()
    {
        if (monster == null)
        {
            yield break;
        }

        if (monsterAnimator == null)
        {
            yield break;
        }

        monsterAnimator.ResetTrigger(
            AttackTrigger
        );

        monsterAnimator.ResetTrigger(
            BiteTrigger
        );

        monsterAnimator.ResetTrigger(
            TauntTrigger
        );

        monsterAnimator.SetBool(
            WalkingBool,
            false
        );

        monsterAnimator.SetBool(
            DancingBool,
            false
        );

        if (HasTrigger(
            monsterAnimator,
            TauntTrigger
        ))
        {
            monsterAnimator.SetTrigger(
                TauntTrigger
            );

            Debug.Log(
                "[COMBAT] Monster victory Taunt started."
            );
        }

        yield return new WaitForSeconds(
            tauntDuration
        );

        monsterAnimator.ResetTrigger(
            TauntTrigger
        );

        monsterAnimator.SetBool(
            DancingBool,
            false
        );

        monsterAnimator.SetBool(
            WalkingBool,
            false
        );

        Debug.Log(
            "[COMBAT] Monster victory Taunt finished."
        );
    }

    // ====================================
    // REGISTER COMBATANTS
    // ====================================

    public void RegisterCombatants(
        Health playerHealth,
        Health monsterHealth
    )
    {
        player = playerHealth;
        monster = monsterHealth;

        fightFinished = false;
        roundInProgress = false;

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

        FindPlayerClickController();

        nextRoundTime =
            Time.time + 0.5f;

        Debug.Log(
            "[COMBAT] Combatants registered."
        );
    }
}