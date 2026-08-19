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

    [Header("Melee Damage")]
    public float damageMin = 8f;
    public float damageMax = 15f;

    [Header("Melee Range")]
    [Tooltip("Distance at which melee combat is allowed to start.")]
    public float attackRange = 1.4f;

    [Tooltip("Actual distance between Soldier and Monster during melee combat.")]
    public float meleeCombatDistance = 0.85f;

    [Tooltip("How quickly the Soldier moves into melee position.")]
    public float meleeApproachSpeed = 8f;

    [Tooltip("How quickly the Monster moves into melee position.")]
    public float monsterMeleeApproachSpeed = 8f;

    [Tooltip("If enabled, both characters are positioned closer before attacking.")]
    public bool forceMeleeDistance = true;

    [Header("Ranged Combat")]
    public float shootingRange = 8f;
    public float shootingInterval = 1.2f;

    [Tooltip("Number of shots the Soldier can fire before reloading.")]
    public int shotsBeforeReload = 4;

    [Tooltip("Reload duration in seconds.")]
    public float reloadDuration = 2f;

    [Header("Ranged Damage")]
    public float shootingDamageMin = 8f;
    public float shootingDamageMax = 12f;

    [Header("Shooting Aim")]
    [Tooltip("Optional Animator trigger played before Shooting.")]
    public float aimDuration = 0.15f;

    [Tooltip("How quickly the Soldier rotates toward the target.")]
    public float shootingRotationSpeed = 25f;

    [Tooltip("Keep correcting Soldier rotation while aiming/shooting.")]
    public bool continuouslyCorrectAim = true;

    [Header("Shooting Line of Sight")]
    [Tooltip("If enabled, obstacles can block ranged attacks.")]
    public bool requireLineOfSight = true;

    [Tooltip("Layers that can block ranged attacks. Add walls, rocks, buildings, crates, etc.")]
    public LayerMask shootingObstacleLayers;

    [Tooltip("Small offset used when starting the Line of Sight ray from the Soldier.")]
    public float lineOfSightStartOffset = 0.05f;

    [Tooltip("If enabled, a debug ray will be drawn in the Scene view while checking Line of Sight.")]
    public bool debugLineOfSight = false;

    [Header("Combat Rotation")]
    [Tooltip("How quickly characters rotate during melee.")]
    public float combatRotationSpeed = 10f;

    [Header("Monster Attacks")]
    [Range(0f, 1f)]
    public float monsterBiteChance = 0.5f;

    [Header("Victory")]
    public float tauntDuration = 3f;

    [Header("Player Height Protection")]
    [Tooltip("Keeps Soldier at his original Y position during combat.")]
    public bool lockPlayerHeight = true;

    // ====================================
    // ANIMATOR PARAMETERS
    // ====================================

    private const string AttackTrigger = "Attack";
    private const string BiteTrigger = "Bite";

    private const string AimTrigger = "Aim";
    private const string ShootingTrigger = "Shooting";
    private const string ReloadingTrigger = "Reloading";

    private const string TauntTrigger = "Taunt";

    private const string GetDamageTrigger = "GetDamage";
    private const string GetDamageIndexInt = "GetDamageIndex";

    private const string DeathTrigger = "Death";
    private const string DeathIndexInt = "DeathIndex";

    private const string WalkingBool = "IsWalking";

    // ====================================
    // REFERENCES
    // ====================================

    private Animator playerAnimator;
    private Animator monsterAnimator;

    private PlayerClickController playerClickController;
    private PlayerController playerController;

    // ====================================
    // STATE
    // ====================================

    private float nextRoundTime;

    private bool fightFinished;
    private bool roundInProgress;
    private bool isReloading;

    private int shotsFired;

    private float playerCombatHeight;

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
        FindPlayerController();

        if (player != null)
        {
            playerCombatHeight =
                player.transform.position.y;
        }

        DisableRootMotion(
            playerAnimator
        );

        DisableRootMotion(
            monsterAnimator
        );

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

        KeepPlayerAtCombatHeight();

        float distance =
            GetHorizontalDistance(
                player.transform,
                monster.transform
            );

        // ====================================
        // MELEE
        // ====================================

        if (distance <= attackRange)
        {
            // Stop Soldier immediately.
            StopPlayerMovementOnly();

            // Bring both combatants closer together.
            MaintainMeleeDistance();

            // Face each other.
            RotateTowardsOpponent(
                player.transform,
                monster.transform
            );

            RotateTowardsOpponent(
                monster.transform,
                player.transform
            );

            // Recalculate distance after positioning.
            distance =
                GetHorizontalDistance(
                    player.transform,
                    monster.transform
                );

            HandleMeleeCombat(
                distance
            );

            return;
        }

        // ====================================
        // RANGED
        // ====================================

        if (HasSelectedEnemy())
        {
            HandleRangedCombat(
                distance
            );
        }
    }

    // ====================================
    // FIND COMBATANTS
    // ====================================

    private void FindCombatants()
    {
        Health[] healthObjects =
            FindObjectsByType<Health>();

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

    // ====================================
    // FIND PLAYER CLICK CONTROLLER
    // ====================================

    private void FindPlayerClickController()
    {
        playerClickController = null;

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
    // FIND PLAYER CONTROLLER
    // ====================================

    private void FindPlayerController()
    {
        playerController = null;

        if (player == null)
            return;

        playerController =
            player.GetComponent<PlayerController>();

        if (playerController == null)
        {
            playerController =
                player.GetComponentInParent<PlayerController>();
        }

        if (playerController == null)
        {
            playerController =
                player.GetComponentInChildren<PlayerController>();
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
    // ROOT MOTION
    // ====================================

    private void DisableRootMotion(
        Animator animator
    )
    {
        if (animator == null)
            return;

        animator.applyRootMotion = false;
    }

    // ====================================
    // HEIGHT PROTECTION
    // ====================================

    private void KeepPlayerAtCombatHeight()
    {
        if (!lockPlayerHeight)
            return;

        if (player == null)
            return;

        Vector3 position =
            player.transform.position;

        if (Mathf.Abs(
                position.y -
                playerCombatHeight
            ) > 0.001f)
        {
            position.y =
                playerCombatHeight;

            player.transform.position =
                position;
        }
    }

    // ====================================
    // HORIZONTAL DISTANCE
    // ====================================

    private float GetHorizontalDistance(
        Transform a,
        Transform b
    )
    {
        if (a == null ||
            b == null)
        {
            return Mathf.Infinity;
        }

        Vector3 aPosition =
            a.position;

        Vector3 bPosition =
            b.position;

        aPosition.y = 0f;
        bPosition.y = 0f;

        return Vector3.Distance(
            aPosition,
            bPosition
        );
    }

    // ====================================
    // MAINTAIN MELEE DISTANCE
    // ====================================

    private void MaintainMeleeDistance()
    {
        if (!forceMeleeDistance)
            return;

        if (player == null ||
            monster == null)
            return;

        if (player.IsDead() ||
            monster.IsDead())
            return;

        float currentDistance =
            GetHorizontalDistance(
                player.transform,
                monster.transform
            );

        if (currentDistance <= meleeCombatDistance)
            return;

        Vector3 direction =
            monster.transform.position -
            player.transform.position;

        direction.y = 0f;

        if (direction.sqrMagnitude < 0.0001f)
            return;

        direction.Normalize();

        float distanceToMove =
            currentDistance -
            meleeCombatDistance;

        // ====================================
        // MOVE PLAYER TOWARD MONSTER
        // ====================================

        if (playerController != null)
        {
            playerController.StopMovement();
        }

        float playerMove =
            Mathf.Min(
                distanceToMove,
                meleeApproachSpeed *
                Time.deltaTime
            );

        Vector3 playerPosition =
            player.transform.position;

        playerPosition +=
            direction *
            playerMove;

        playerPosition.y =
            playerCombatHeight;

        player.transform.position =
            playerPosition;

        // ====================================
        // MOVE MONSTER TOWARD PLAYER
        // ====================================

        float remainingDistance =
            GetHorizontalDistance(
                player.transform,
                monster.transform
            );

        if (remainingDistance <= meleeCombatDistance)
            return;

        float monsterMove =
            Mathf.Min(
                remainingDistance -
                meleeCombatDistance,
                monsterMeleeApproachSpeed *
                Time.deltaTime
            );

        Vector3 monsterDirection =
            player.transform.position -
            monster.transform.position;

        monsterDirection.y = 0f;

        if (monsterDirection.sqrMagnitude > 0.0001f)
        {
            monsterDirection.Normalize();

            Vector3 monsterPosition =
                monster.transform.position;

            monsterPosition +=
                monsterDirection *
                monsterMove;

            monster.transform.position =
                monsterPosition;
        }
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
            in animator.parameters)
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
    // SAFE TRIGGER RESET
    // ====================================

    private static void SafeResetTrigger(
        Animator animator,
        string triggerName
    )
    {
        if (!HasParameter(
                animator,
                triggerName,
                AnimatorControllerParameterType.Trigger
            ))
        {
            return;
        }

        animator.ResetTrigger(
            triggerName
        );
    }

    // ====================================
    // SELECTED ENEMY
    // ====================================

    private bool HasSelectedEnemy()
    {
        if (playerClickController == null)
            return false;

        return
            playerClickController.SelectedEnemy ==
            monster;
    }

    // ====================================
    // LINE OF SIGHT
    // ====================================

    private bool HasLineOfSightToMonster()
    {
        if (!requireLineOfSight)
            return true;

        if (player == null ||
            monster == null)
        {
            return false;
        }

        Vector3 origin =
            GetShootingOrigin();

        Vector3 target =
            GetAimPosition(
                monster
            );

        Vector3 direction =
            target -
            origin;

        float distance =
            direction.magnitude;

        if (distance <= 0.001f)
            return true;

        direction.Normalize();

        origin +=
            direction *
            lineOfSightStartOffset;

        distance -=
            lineOfSightStartOffset;

        if (distance <= 0f)
            return true;

        RaycastHit[] hits =
            Physics.RaycastAll(
                origin,
                direction,
                distance,
                shootingObstacleLayers,
                QueryTriggerInteraction.Ignore
            );

        if (debugLineOfSight)
        {
            Debug.DrawRay(
                origin,
                direction * distance,
                hits.Length > 0
                    ? Color.red
                    : Color.green
            );
        }

        if (hits.Length == 0)
        {
            return true;
        }

        // Sort hits so the closest object is checked first.
        System.Array.Sort(
            hits,
            (a, b) =>
                a.distance.CompareTo(
                    b.distance
                )
        );

        foreach (RaycastHit hit in hits)
        {
            if (hit.collider == null)
                continue;

            // Ignore the Monster itself.
            if (hit.collider.transform.IsChildOf(
                    monster.transform
                ) ||
                monster.transform.IsChildOf(
                    hit.collider.transform
                ))
            {
                continue;
            }

            // Any other object on the obstacle layers blocks the shot.
            return false;
        }

        return true;
    }

    // ====================================
    // SHOOTING ORIGIN
    // ====================================

    private Vector3 GetShootingOrigin()
    {
        if (player == null)
            return Vector3.zero;

        Collider playerCollider =
            player.GetComponentInChildren<Collider>();

        if (playerCollider != null)
        {
            Bounds bounds =
                playerCollider.bounds;

            return new Vector3(
                bounds.center.x,
                bounds.center.y,
                bounds.center.z
            );
        }

        Vector3 position =
            player.transform.position;

        position.y += 1f;

        return position;
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

        if (distance > meleeCombatDistance + 0.1f)
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

        // Make sure they are close before the first attack.
        MaintainMeleeDistance();

        RotateTowardsOpponent(
            player.transform,
            monster.transform
        );

        RotateTowardsOpponent(
            monster.transform,
            player.transform
        );

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

        // Make sure they remain close after the Player attack.
        MaintainMeleeDistance();

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
                Time.time +
                roundCooldown;
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
        {
            yield break;
        }

        if (!attacker.gameObject.activeInHierarchy ||
            !defender.gameObject.activeInHierarchy)
        {
            yield break;
        }

        if (attacker.IsDead() ||
            defender.IsDead())
        {
            yield break;
        }

        // Keep combatants close.
        MaintainMeleeDistance();

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
            SafeResetTrigger(
                attackerAnimator,
                AttackTrigger
            );

            SafeResetTrigger(
                attackerAnimator,
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
        {
            yield break;
        }

        if (attacker.IsDead() ||
            defender.IsDead())
        {
            yield break;
        }

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

        if (defender.IsDead())
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

        // Soldier must be completely stopped.
        StopPlayerMovementOnly();

        // Keep facing the target.
        RotateSoldierTowardsTarget();

        // Do not shoot through obstacles.
        if (!HasLineOfSightToMonster())
        {
            return;
        }

        if (roundInProgress)
            return;

        if (Time.time < nextRoundTime)
            return;

        StartCoroutine(
            PerformShoot()
        );
    }

    // ====================================
    // SHOOT SEQUENCE
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

        float distance =
            GetHorizontalDistance(
                player.transform,
                monster.transform
            );

        if (distance > shootingRange)
        {
            roundInProgress = false;
            yield break;
        }

        // ====================================
        // STEP 1 - STOP
        // ====================================

        StopPlayerMovementOnly();

        // ====================================
        // STEP 2 - CHECK LINE OF SIGHT
        // ====================================

        if (!HasLineOfSightToMonster())
        {
            roundInProgress = false;

            Debug.Log(
                "[COMBAT] Shooting blocked by an obstacle."
            );

            yield break;
        }

        // ====================================
        // STEP 3 - AIM
        // ====================================

        RotateSoldierTowardsTarget();

        if (playerAnimator != null &&
            HasParameter(
                playerAnimator,
                AimTrigger,
                AnimatorControllerParameterType.Trigger
            ))
        {
            SafeResetTrigger(
                playerAnimator,
                AimTrigger
            );

            SafeResetTrigger(
                playerAnimator,
                ShootingTrigger
            );

            playerAnimator.SetTrigger(
                AimTrigger
            );

            Debug.Log(
                "[COMBAT] Soldier AIM."
            );
        }

        // ====================================
        // STEP 4 - AIM TIME
        // ====================================

        float aimTimer = 0f;

        while (aimTimer < aimDuration)
        {
            if (fightFinished)
            {
                roundInProgress = false;
                yield break;
            }

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

            distance =
                GetHorizontalDistance(
                    player.transform,
                    monster.transform
                );

            if (distance > shootingRange)
            {
                roundInProgress = false;
                yield break;
            }

            // Recheck Line of Sight while aiming.
            if (!HasLineOfSightToMonster())
            {
                roundInProgress = false;

                Debug.Log(
                    "[COMBAT] Shooting cancelled because an obstacle blocked Line of Sight."
                );

                yield break;
            }

            // Keep Soldier perfectly aimed.
            RotateSoldierTowardsTarget();

            // Keep Soldier at the correct Y position.
            KeepPlayerAtCombatHeight();

            aimTimer +=
                Time.deltaTime;

            yield return null;
        }

        // ====================================
        // STEP 5 - FINAL AIM
        // ====================================

        RotateSoldierTowardsTarget();

        // ====================================
        // STEP 6 - FINAL LINE OF SIGHT CHECK
        // ====================================

        if (!HasLineOfSightToMonster())
        {
            roundInProgress = false;

            Debug.Log(
                "[COMBAT] Shot cancelled because an obstacle blocked Line of Sight."
            );

            yield break;
        }

        // ====================================
        // STEP 7 - SHOOT
        // ====================================

        if (playerAnimator != null &&
            HasParameter(
                playerAnimator,
                ShootingTrigger,
                AnimatorControllerParameterType.Trigger
            ))
        {
            SafeResetTrigger(
                playerAnimator,
                ShootingTrigger
            );

            playerAnimator.SetTrigger(
                ShootingTrigger
            );

            Debug.Log(
                "[COMBAT] Soldier SHOOTING."
            );
        }

        // ====================================
        // STEP 8 - SHOOT DELAY
        // ====================================

        float elapsed = 0f;

        while (elapsed < damageDelay)
        {
            if (fightFinished)
            {
                roundInProgress = false;
                yield break;
            }

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

            if (continuouslyCorrectAim)
            {
                RotateSoldierTowardsTarget();
            }

            // Keep checking the obstacle during the shot delay.
            if (!HasLineOfSightToMonster())
            {
                roundInProgress = false;

                Debug.Log(
                    "[COMBAT] Shot cancelled because an obstacle appeared."
                );

                yield break;
            }

            KeepPlayerAtCombatHeight();

            elapsed +=
                Time.deltaTime;

            yield return null;
        }

        // ====================================
        // FINAL VALIDATION
        // ====================================

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
            GetHorizontalDistance(
                player.transform,
                monster.transform
            );

        if (distance > shootingRange)
        {
            roundInProgress = false;
            yield break;
        }

        // Final Line of Sight validation before damage.
        if (!HasLineOfSightToMonster())
        {
            roundInProgress = false;

            Debug.Log(
                "[COMBAT] Shot did not hit because Line of Sight was blocked."
            );

            yield break;
        }

        // Final correction before bullet damage.
        RotateSoldierTowardsTarget();

        // ====================================
        // DAMAGE
        // ====================================

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

        if (monster.IsDead())
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
            Time.time +
            shootingInterval;

        roundInProgress = false;
    }

    // ====================================
    // ROTATE SOLDIER
    // ====================================

    private void RotateSoldierTowardsTarget()
    {
        if (player == null ||
            monster == null)
        {
            return;
        }

        Vector3 aimPosition =
            GetAimPosition(
                monster
            );

        Vector3 direction =
            aimPosition -
            player.transform.position;

        // Soldier rotates only horizontally.
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.001f)
            return;

        Quaternion targetRotation =
            Quaternion.LookRotation(
                direction.normalized,
                Vector3.up
            );

        player.transform.rotation =
            Quaternion.Slerp(
                player.transform.rotation,
                targetRotation,
                shootingRotationSpeed *
                Time.deltaTime
            );
    }

    // ====================================
    // AIM POSITION
    // ====================================

    private Vector3 GetAimPosition(
        Health target
    )
    {
        if (target == null)
            return Vector3.zero;

        Collider targetCollider =
            target.GetComponentInChildren<Collider>();

        if (targetCollider != null)
        {
            Bounds bounds =
                targetCollider.bounds;

            // Aim around the upper-middle of the enemy.
            return new Vector3(
                bounds.center.x,
                bounds.min.y +
                bounds.size.y * 0.65f,
                bounds.center.z
            );
        }

        Vector3 position =
            target.transform.position;

        position.y += 1f;

        return position;
    }

    // ====================================
    // RELOAD
    // ====================================

    private IEnumerator Reload()
    {
        if (isReloading)
            yield break;

        isReloading = true;

        StopPlayerMovementOnly();

        Debug.Log(
            "[COMBAT] Soldier Reloading."
        );

        if (playerAnimator != null)
        {
            SafeResetTrigger(
                playerAnimator,
                AimTrigger
            );

            SafeResetTrigger(
                playerAnimator,
                ShootingTrigger
            );

            if (HasParameter(
                    playerAnimator,
                    ReloadingTrigger,
                    AnimatorControllerParameterType.Trigger
                ))
            {
                SafeResetTrigger(
                    playerAnimator,
                    ReloadingTrigger
                );

                playerAnimator.SetTrigger(
                    ReloadingTrigger
                );
            }
        }

        float elapsed = 0f;

        while (elapsed < reloadDuration)
        {
            if (fightFinished)
            {
                isReloading = false;
                yield break;
            }

            KeepPlayerAtCombatHeight();

            elapsed +=
                Time.deltaTime;

            yield return null;
        }

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
        {
            return;
        }

        Vector3 direction =
            opponent.position -
            fighter.position;

        direction.y = 0f;

        if (direction.sqrMagnitude < 0.001f)
            return;

        Quaternion targetRotation =
            Quaternion.LookRotation(
                direction.normalized,
                Vector3.up
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
    // STOP PLAYER MOVEMENT
    // ====================================

    private void StopPlayerMovementOnly()
    {
        if (playerController == null)
        {
            FindPlayerController();
        }

        if (playerController != null)
        {
            playerController.StopMovement();
        }

        if (playerAnimator != null &&
            HasParameter(
                playerAnimator,
                WalkingBool,
                AnimatorControllerParameterType.Bool
            ))
        {
            playerAnimator.SetBool(
                WalkingBool,
                false
            );
        }

        KeepPlayerAtCombatHeight();
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
                $"GetDamageIndex: {randomIndex}"
            );
        }

        SafeResetTrigger(
            animator,
            GetDamageTrigger
        );

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
        {
            return;
        }

        fightFinished = true;

        StopAllCoroutines();

        bool playerWon =
            loser == monster;

        Debug.Log(
            playerWon
                ? "[COMBAT] PLAYER WINS!"
                : "[COMBAT] MONSTER WINS!"
        );

        // ====================================
        // HIDE PLAYER SELECTION
        // ====================================

        if (playerClickController != null)
        {
            playerClickController.SetSelected(
                false
            );

            playerClickController.enabled =
                false;

            Debug.Log(
                "[COMBAT] Player selection visuals disabled."
            );
        }

        // ====================================
        // PLAYER DEATH
        // ====================================

        if (loser == player)
        {
            StopPlayerMovement();
        }

        // ====================================
        // MONSTER DEATH
        // ====================================

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

        // ====================================
        // HEALTH BAR
        // ====================================

        HideHealthBar(
            loser
        );

        // ====================================
        // DEATH
        // ====================================

        PlayDeath(
            loser
        );

        // ====================================
        // VICTORY
        // ====================================

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
    // STOP PLAYER AFTER DEATH
    // ====================================

    private void StopPlayerMovement()
    {
        FindPlayerController();

        if (playerController != null)
        {
            playerController.SetDead();

            Debug.Log(
                "[COMBAT] PlayerController marked as dead."
            );
        }

        FindPlayerClickController();

        if (playerClickController != null)
        {
            playerClickController.SetSelected(
                false
            );

            playerClickController.enabled =
                false;

            Debug.Log(
                "[COMBAT] PlayerClickController disabled."
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

        foreach (HealthBar healthBar in healthBars)
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

        if (loser == player)
        {
            StopPlayerMovement();
        }

        Animator loserAnimator =
            loser == player
                ? playerAnimator
                : monsterAnimator;

        if (loserAnimator == null)
            return;

        loserAnimator.applyRootMotion =
            false;

        // ====================================
        // RESET TRIGGERS
        // ====================================

        SafeResetTrigger(
            loserAnimator,
            AttackTrigger
        );

        SafeResetTrigger(
            loserAnimator,
            BiteTrigger
        );

        SafeResetTrigger(
            loserAnimator,
            AimTrigger
        );

        SafeResetTrigger(
            loserAnimator,
            ShootingTrigger
        );

        SafeResetTrigger(
            loserAnimator,
            ReloadingTrigger
        );

        SafeResetTrigger(
            loserAnimator,
            TauntTrigger
        );

        SafeResetTrigger(
            loserAnimator,
            GetDamageTrigger
        );

        // ====================================
        // STOP WALKING
        // ====================================

        if (HasParameter(
                loserAnimator,
                WalkingBool,
                AnimatorControllerParameterType.Bool
            ))
        {
            loserAnimator.SetBool(
                WalkingBool,
                false
            );
        }

        // ====================================
        // DEATH INDEX
        // ====================================

        if (HasParameter(
                loserAnimator,
                DeathIndexInt,
                AnimatorControllerParameterType.Int
            ))
        {
            int randomDeathIndex =
                Random.Range(
                    0,
                    2
                );

            loserAnimator.SetInteger(
                DeathIndexInt,
                randomDeathIndex
            );

            Debug.Log(
                $"[COMBAT] {loser.name} " +
                $"DeathIndex: {randomDeathIndex}"
            );
        }

        // ====================================
        // DEATH TRIGGER
        // ====================================

        if (HasParameter(
                loserAnimator,
                DeathTrigger,
                AnimatorControllerParameterType.Trigger
            ))
        {
            SafeResetTrigger(
                loserAnimator,
                DeathTrigger
            );

            loserAnimator.SetTrigger(
                DeathTrigger
            );
        }
        else
        {
            Debug.LogWarning(
                $"[COMBAT] {loser.name} Animator " +
                $"has no 'Death' trigger."
            );
        }
    }

    // ====================================
    // PLAYER VICTORY
    // ====================================

    private IEnumerator PlayerVictory()
    {
        if (player == null ||
            playerAnimator == null)
        {
            yield break;
        }

        StopPlayerMovementOnly();

        if (playerClickController != null)
        {
            playerClickController.SetSelected(
                false
            );

            playerClickController.enabled =
                false;
        }

        SafeResetTrigger(
            playerAnimator,
            AttackTrigger
        );

        SafeResetTrigger(
            playerAnimator,
            BiteTrigger
        );

        SafeResetTrigger(
            playerAnimator,
            AimTrigger
        );

        SafeResetTrigger(
            playerAnimator,
            ShootingTrigger
        );

        SafeResetTrigger(
            playerAnimator,
            ReloadingTrigger
        );

        SafeResetTrigger(
            playerAnimator,
            TauntTrigger
        );

        if (HasParameter(
                playerAnimator,
                WalkingBool,
                AnimatorControllerParameterType.Bool
            ))
        {
            playerAnimator.SetBool(
                WalkingBool,
                false
            );
        }

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

        SafeResetTrigger(
            playerAnimator,
            TauntTrigger
        );

        if (HasParameter(
                playerAnimator,
                WalkingBool,
                AnimatorControllerParameterType.Bool
            ))
        {
            playerAnimator.SetBool(
                WalkingBool,
                false
            );
        }
    }

    // ====================================
    // MONSTER VICTORY
    // ====================================

    private IEnumerator MonsterVictory()
    {
        if (monster == null ||
            monsterAnimator == null)
        {
            yield break;
        }

        SafeResetTrigger(
            monsterAnimator,
            AttackTrigger
        );

        SafeResetTrigger(
            monsterAnimator,
            BiteTrigger
        );

        SafeResetTrigger(
            monsterAnimator,
            AimTrigger
        );

        SafeResetTrigger(
            monsterAnimator,
            ShootingTrigger
        );

        SafeResetTrigger(
            monsterAnimator,
            ReloadingTrigger
        );

        SafeResetTrigger(
            monsterAnimator,
            TauntTrigger
        );

        if (HasParameter(
                monsterAnimator,
                WalkingBool,
                AnimatorControllerParameterType.Bool
            ))
        {
            monsterAnimator.SetBool(
                WalkingBool,
                false
            );
        }

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

        SafeResetTrigger(
            monsterAnimator,
            TauntTrigger
        );

        if (HasParameter(
                monsterAnimator,
                WalkingBool,
                AnimatorControllerParameterType.Bool
            ))
        {
            monsterAnimator.SetBool(
                WalkingBool,
                false
            );
        }
    }

    // ====================================
    // REGISTER COMBATANTS
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
        FindPlayerController();

        if (player != null)
        {
            playerCombatHeight =
                player.transform.position.y;
        }

        DisableRootMotion(
            playerAnimator
        );

        DisableRootMotion(
            monsterAnimator
        );

        nextRoundTime =
            Time.time + 0.5f;
    }
}