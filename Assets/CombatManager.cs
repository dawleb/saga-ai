using System.Collections;
using UnityEngine;

public class CombatManager : MonoBehaviour
{
    public static CombatManager Instance;

    [Header("Combatants")]
    public Health player;
    public Health monster;

    [Header("Melee Combat")]
    public float roundCooldown = 1f;

    public float attackAnimationDuration = 0.8f;
    public float damageDelay = 0.35f;

    [Header("Damage")]
    public float damageMin = 8f;
    public float damageMax = 15f;

    [Header("Melee Range")]
    public float attackRange = 1.1f;

    [Header("Ranged Combat")]
    public float shootingRange = 8f;

    public float shootingInterval = 1.2f;

    [Tooltip("Ile strzałów Soldier może oddać przed przeładowaniem.")]
    public int shotsBeforeReload = 4;

    [Tooltip("Czas trwania przeładowania.")]
    public float reloadDuration = 2f;

    [Header("Ranged Damage")]
    public float shootingDamageMin = 8f;
    public float shootingDamageMax = 12f;

    [Header("Combat Rotation")]
    public float combatRotationSpeed = 10f;

    [Header("Monster Attacks")]
    [Range(0f, 1f)]
    public float monsterBiteChance = 0.5f;

    [Header("Victory")]
    public float tauntDuration = 3f;

    // ====================================
    // ANIMATOR PARAMETERS
    // ====================================

    private const string AttackTrigger = "Attack";
    private const string BiteTrigger = "Bite";

    private const string ShootingTrigger = "Shooting";
    private const string ReloadingTrigger = "Reloading";

    private const string TauntTrigger = "Taunt";

    private const string GetDamageTrigger = "GetDamage";
    private const string GetDamageIndexInt = "GetDamageIndex";

    private const string DeathTrigger = "Death";
    private const string DeathIndexInt = "DeathIndex";

    private const string DancingBool = "IsDancing";
    private const string WalkingBool = "IsWalking";

    // ====================================
    // REFERENCES
    // ====================================

    private Animator playerAnimator;
    private Animator monsterAnimator;

    private PlayerClickController playerClickController;

    // ====================================
    // STATE
    // ====================================

    private float nextRoundTime;

    private bool fightFinished;
    private bool roundInProgress;

    private bool isReloading;

    private int shotsFired;

    // ====================================
    // UNITY
    // ====================================

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

        if (player != null &&
            monster != null)
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
                continue;

            SimpleAgent agent =
                health.GetComponentInParent<SimpleAgent>();

            if (agent == null)
                continue;

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
            return;

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
            return null;

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
    // ANIMATOR PARAMETER CHECK
    // ====================================

    private static bool HasParameter(
        Animator animator,
        string paramName,
        AnimatorControllerParameterType type
    )
    {
        if (animator == null)
            return false;

        foreach (
            AnimatorControllerParameter parameter
            in animator.parameters
        )
        {
            if (
                parameter.type == type &&
                parameter.name == paramName
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
            return;

        if (player == null ||
            monster == null)
            return;

        if (!player.gameObject.activeInHierarchy ||
            !monster.gameObject.activeInHierarchy)
            return;

        if (player.IsDead() ||
            monster.IsDead())
            return;

        float distance =
            Vector3.Distance(
                player.transform.position,
                monster.transform.position
            );

        // ====================================
        // VERY CLOSE = MELEE
        // ====================================

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

            HandleMeleeCombat(distance);

            return;
        }

        // ====================================
        // PLAYER RANGED COMBAT
        // ====================================

        if (HasSelectedEnemy())
        {
            HandleRangedCombat(distance);
        }
    }

    // ====================================
    // SELECTED ENEMY
    // ====================================

    private bool HasSelectedEnemy()
    {
        if (playerClickController == null)
            return false;

        return
            playerClickController.SelectedEnemy == monster;
    }

    // ====================================
    // MELEE
    // ====================================

    private void HandleMeleeCombat(
        float distance
    )
    {
        if (isReloading)
            return;

        if (roundInProgress)
            return;

        if (distance > attackRange)
            return;

        if (Time.time < nextRoundTime)
            return;

        StartCoroutine(
            ResolveMeleeRound()
        );
    }

    private IEnumerator ResolveMeleeRound()
    {
        roundInProgress = true;

        yield return StartCoroutine(
            PerformMeleeAttack(
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
            PerformMeleeAttack(
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
    // MELEE ATTACK
    // ====================================

    private IEnumerator PerformMeleeAttack(
        Animator attackerAnimator,
        Health attacker,
        Health defender,
        bool monsterAttacking
    )
    {
        if (attacker == null ||
            defender == null)
            yield break;

        if (!attacker.gameObject.activeInHierarchy ||
            !defender.gameObject.activeInHierarchy)
            yield break;

        if (attacker.IsDead() ||
            defender.IsDead())
            yield break;

        RotateTowardsOpponent(
            attacker.transform,
            defender.transform
        );

        string selectedTrigger =
            AttackTrigger;

        // ====================================
        // MONSTER ATTACK
        // ====================================

        if (monsterAttacking)
        {
            bool hasBite =
                HasParameter(
                    attackerAnimator,
                    BiteTrigger,
                    AnimatorControllerParameterType.Trigger
                );

            bool hasAttack =
                HasParameter(
                    attackerAnimator,
                    AttackTrigger,
                    AnimatorControllerParameterType.Trigger
                );

            if (hasBite && hasAttack)
            {
                selectedTrigger =
                    Random.value <
                    monsterBiteChance
                        ? BiteTrigger
                        : AttackTrigger;
            }
            else if (hasBite)
            {
                selectedTrigger =
                    BiteTrigger;
            }
            else if (hasAttack)
            {
                selectedTrigger =
                    AttackTrigger;
            }
            else
            {
                yield break;
            }
        }

        // ====================================
        // PLAY ATTACK
        // ====================================

        if (attackerAnimator != null)
        {
            attackerAnimator.ResetTrigger(
                AttackTrigger
            );

            attackerAnimator.ResetTrigger(
                BiteTrigger
            );

            if (HasParameter(
                attackerAnimator,
                selectedTrigger,
                AnimatorControllerParameterType.Trigger
            ))
            {
                attackerAnimator.SetTrigger(
                    selectedTrigger
                );
            }
        }

        yield return new WaitForSeconds(
            damageDelay
        );

        if (fightFinished)
            yield break;

        if (!attacker.gameObject.activeInHierarchy ||
            !defender.gameObject.activeInHierarchy)
            yield break;

        if (attacker.IsDead() ||
            defender.IsDead())
            yield break;

        // ====================================
        // DAMAGE
        // ====================================

        float damage =
            Random.Range(
                damageMin,
                damageMax
            );

        defender.TakeDamage(
            damage
        );

        PlayRandomGetDamage(
            defender == player
                ? playerAnimator
                : monsterAnimator,
            defender.name
        );

        // ====================================
        // DEATH
        // ====================================

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
    // RANGED COMBAT
    // ====================================

    private void HandleRangedCombat(
        float distance
    )
    {
        if (distance > shootingRange)
            return;

        if (isReloading)
            return;

        RotateTowardsOpponent(
            player.transform,
            monster.transform
        );

        if (roundInProgress)
            return;

        if (Time.time < nextRoundTime)
            return;

        StartCoroutine(
            PerformShoot()
        );
    }

    // ====================================
    // SHOOT
    // ====================================

    private IEnumerator PerformShoot()
    {
        roundInProgress = true;

        if (player == null ||
            monster == null)
        {
            roundInProgress = false;
            yield break;
        }

        if (player.IsDead() ||
            monster.IsDead())
        {
            roundInProgress = false;
            yield break;
        }

        // ====================================
        // CHECK DISTANCE
        // ====================================

        float distance =
            Vector3.Distance(
                player.transform.position,
                monster.transform.position
            );

        if (distance > shootingRange)
        {
            roundInProgress = false;
            yield break;
        }

        // ====================================
        // FACE ZOMBIE
        // ====================================

        RotateTowardsOpponent(
            player.transform,
            monster.transform
        );

        // ====================================
        // SHOOT ANIMATION
        // ====================================

        if (playerAnimator != null)
        {
            if (HasParameter(
                playerAnimator,
                ShootingTrigger,
                AnimatorControllerParameterType.Trigger
            ))
            {
                playerAnimator.SetTrigger(
                    ShootingTrigger
                );

                Debug.Log(
                    "[COMBAT] Soldier Shooting."
                );
            }
            else
            {
                Debug.LogWarning(
                    "[COMBAT] Player Animator has no " +
                    "'Shooting' trigger."
                );
            }
        }

        // ====================================
        // DAMAGE
        // ====================================

        yield return new WaitForSeconds(
            damageDelay
        );

        if (fightFinished)
        {
            roundInProgress = false;
            yield break;
        }

        if (player.IsDead() ||
            monster.IsDead())
        {
            roundInProgress = false;
            yield break;
        }

        distance =
            Vector3.Distance(
                player.transform.position,
                monster.transform.position
            );

        if (distance > shootingRange)
        {
            roundInProgress = false;
            yield break;
        }

        float damage =
            Random.Range(
                shootingDamageMin,
                shootingDamageMax
            );

        monster.TakeDamage(
            damage
        );

        PlayRandomGetDamage(
            monsterAnimator,
            monster.name
        );

        shotsFired++;

        Debug.Log(
            $"[COMBAT] Soldier shot #{shotsFired}. " +
            $"Zombie HP: {monster.CurrentHealth:F1}"
        );

        // ====================================
        // DEATH
        // ====================================

        if (monster.IsDead() ||
            monster.CurrentHealth <= 0f)
        {
            FinishFight(
                monster
            );

            roundInProgress = false;
            yield break;
        }

        // ====================================
        // RELOAD
        // ====================================

        if (shotsFired >= shotsBeforeReload)
        {
            yield return StartCoroutine(
                Reload()
            );
        }

        nextRoundTime =
            Time.time + shootingInterval;

        roundInProgress = false;
    }

    // ====================================
    // RELOAD
    // ====================================

    private IEnumerator Reload()
    {
        if (isReloading)
            yield break;

        isReloading = true;

        Debug.Log(
            "[COMBAT] Soldier Reloading."
        );

        if (playerAnimator != null)
        {
            if (HasParameter(
                playerAnimator,
                ReloadingTrigger,
                AnimatorControllerParameterType.Trigger
            ))
            {
                playerAnimator.SetTrigger(
                    ReloadingTrigger
                );
            }
            else
            {
                Debug.LogWarning(
                    "[COMBAT] Player Animator has no " +
                    "'Reloading' trigger."
                );
            }
        }

        yield return new WaitForSeconds(
            reloadDuration
        );

        shotsFired = 0;

        isReloading = false;

        Debug.Log(
            "[COMBAT] Soldier Reload finished."
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
        if (fighter == null ||
            opponent == null)
            return;

        Vector3 direction =
            opponent.position -
            fighter.position;

        direction.y = 0f;

        if (direction.sqrMagnitude < 0.001f)
            return;

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
    // GET DAMAGE
    // ====================================

    private void PlayRandomGetDamage(
        Animator animator,
        string defenderName
    )
    {
        if (animator == null)
            return;

        if (!HasParameter(
            animator,
            GetDamageTrigger,
            AnimatorControllerParameterType.Trigger
        ))
        {
            return;
        }

        if (HasParameter(
            animator,
            GetDamageIndexInt,
            AnimatorControllerParameterType.Int
        ))
        {
            int randomIndex =
                Random.Range(
                    0,
                    2
                );

            animator.SetInteger(
                GetDamageIndexInt,
                randomIndex
            );

            Debug.Log(
                $"[COMBAT] {defenderName} " +
                $"GetDamage index: {randomIndex}"
            );
        }

        animator.SetTrigger(
            GetDamageTrigger
        );
    }

    // ====================================
    // FINISH FIGHT
    // ====================================

    private void FinishFight(
        Health loser
    )
    {
        if (fightFinished ||
            loser == null)
            return;

        fightFinished = true;

        StopAllCoroutines();

        bool playerWon =
            loser == monster;

        Debug.Log(
            playerWon
                ? "[COMBAT] PLAYER WINS!"
                : "[COMBAT] MONSTER WINS!"
        );

        if (loser == monster)
        {
            SimpleAgent agent =
                monster.GetComponentInParent<SimpleAgent>();

            if (agent != null)
            {
                agent.SetDead();
                agent.enabled = false;
            }
        }

        HideHealthBar(
            loser
        );

        PlayDeath(
            loser
        );

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
            return;

        Transform anchor =
            loser.transform.Find(
                "HealthBarAnchor"
            );

        if (anchor == null)
        {
            Transform[] children =
                loser.GetComponentsInChildren<Transform>(
                    true
                );

            foreach (Transform child in children)
            {
                if (child.name ==
                    "HealthBarAnchor")
                {
                    anchor = child;
                    break;
                }
            }
        }

        if (anchor != null)
        {
            anchor.gameObject.SetActive(
                false
            );

            return;
        }

        HealthBar[] healthBars =
            loser.GetComponentsInChildren<HealthBar>(
                true
            );

        foreach (
            HealthBar healthBar
            in healthBars
        )
        {
            if (healthBar != null)
            {
                healthBar.gameObject.SetActive(
                    false
                );
            }
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
            return;

        Animator loserAnimator =
            loser == player
                ? playerAnimator
                : monsterAnimator;

        if (loserAnimator == null)
            return;

        loserAnimator.applyRootMotion =
            false;

        loserAnimator.ResetTrigger(
            AttackTrigger
        );

        loserAnimator.ResetTrigger(
            BiteTrigger
        );

        loserAnimator.ResetTrigger(
            ShootingTrigger
        );

        loserAnimator.ResetTrigger(
            ReloadingTrigger
        );

        loserAnimator.ResetTrigger(
            TauntTrigger
        );

        loserAnimator.ResetTrigger(
            GetDamageTrigger
        );

        loserAnimator.SetBool(
            WalkingBool,
            false
        );

        loserAnimator.SetBool(
            DancingBool,
            false
        );

        if (HasParameter(
            loserAnimator,
            DeathTrigger,
            AnimatorControllerParameterType.Trigger
        ))
        {
            if (HasParameter(
                loserAnimator,
                DeathIndexInt,
                AnimatorControllerParameterType.Int
            ))
            {
                int randomIndex =
                    Random.Range(
                        0,
                        2
                    );

                loserAnimator.SetInteger(
                    DeathIndexInt,
                    randomIndex
                );
            }

            loserAnimator.SetTrigger(
                DeathTrigger
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
        if (player == null ||
            playerAnimator == null)
            yield break;

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
            ShootingTrigger
        );

        playerAnimator.ResetTrigger(
            ReloadingTrigger
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

        if (HasParameter(
            playerAnimator,
            TauntTrigger,
            AnimatorControllerParameterType.Trigger
        ))
        {
            playerAnimator.SetTrigger(
                TauntTrigger
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
    }

    // ====================================
    // MONSTER VICTORY
    // ====================================

    private IEnumerator MonsterVictory()
    {
        if (monster == null ||
            monsterAnimator == null)
            yield break;

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

        if (HasParameter(
            monsterAnimator,
            TauntTrigger,
            AnimatorControllerParameterType.Trigger
        ))
        {
            monsterAnimator.SetTrigger(
                TauntTrigger
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
    }

    // ====================================
    // REGISTER
    // ====================================

    public void RegisterCombatants(
        Health playerHealth,
        Health monsterHealth
    )
    {
        player =
            playerHealth;

        monster =
            monsterHealth;

        fightFinished = false;
        roundInProgress = false;

        isReloading = false;
        shotsFired = 0;

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
    }
}