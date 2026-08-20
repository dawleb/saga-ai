using System;
using System.Collections;
using UnityEngine;
using Random = UnityEngine.Random;


public class CombatManager : MonoBehaviour
{
    public static CombatManager Instance;

    // =========================================================
    // COMBATANTS
    // =========================================================

    [Header("Combatants")]
    public Health player;
    public Health monster;

    // =========================================================
    // MELEE
    // =========================================================

    [Header("Melee Combat")]
    [Min(0f)]
    public float roundCooldown = 1f;

    [Min(0f)]
    public float attackAnimationDuration = 0.8f;

    [Min(0f)]
    public float damageDelay = 0.35f;

    [Header("Melee Damage")]
    [Min(0f)]
    public float damageMin = 8f;

    [Min(0f)]
    public float damageMax = 15f;

    [Header("Melee Range")]
    [Tooltip("Maximum distance at which melee combat can start.")]
    [Min(0.1f)]
    public float attackRange = 1.4f;

    [Tooltip("Actual horizontal distance between Player and Monster during melee.")]
    [Min(0.1f)]
    public float meleeCombatDistance = 0.85f;

    [Tooltip("How quickly the Player moves into melee position.")]
    [Min(0f)]
    public float meleeApproachSpeed = 8f;

    [Tooltip("How quickly the Monster moves into melee position.")]
    [Min(0f)]
    public float monsterMeleeApproachSpeed = 8f;

    [Tooltip("If enabled, both characters are moved into melee distance.")]
    public bool forceMeleeDistance = true;

    // =========================================================
    // RANGED
    // =========================================================

    [Header("Ranged Combat")]
    [Min(0.1f)]
    public float shootingRange = 8f;

    [Min(0f)]
    public float shootingInterval = 1.2f;

    [Tooltip("Number of shots before reload.")]
    [Min(1)]
    public int shotsBeforeReload = 4;

    [Tooltip("Reload duration.")]
    [Min(0f)]
    public float reloadDuration = 2f;

    [Header("Ranged Damage")]
    [Min(0f)]
    public float shootingDamageMin = 8f;

    [Min(0f)]
    public float shootingDamageMax = 12f;

    [Header("Shooting Aim")]
    [Tooltip("How long the Soldier aims before shooting.")]
    [Min(0f)]
    public float aimDuration = 0.15f;

    [Tooltip("How quickly the Soldier rotates toward the target.")]
    [Min(0f)]
    public float shootingRotationSpeed = 25f;

    [Tooltip("Keep correcting rotation during aiming/shooting.")]
    public bool continuouslyCorrectAim = true;

    // =========================================================
    // LINE OF SIGHT
    // =========================================================

    [Header("Shooting Line of Sight")]
    [Tooltip("If enabled, obstacles can block ranged attacks.")]
    public bool requireLineOfSight = true;

    [Tooltip("Layers that block ranged attacks.")]
    public LayerMask shootingObstacleLayers;

    [Tooltip("Small offset from the Player when starting the ray.")]
    [Min(0f)]
    public float lineOfSightStartOffset = 0.05f;

    [Tooltip("Draw LOS ray in Scene view.")]
    public bool debugLineOfSight = false;

    // =========================================================
    // ROTATION
    // =========================================================

    [Header("Combat Rotation")]
    [Min(0f)]
    public float combatRotationSpeed = 10f;

    // =========================================================
    // MONSTER
    // =========================================================

    [Header("Monster Attacks")]
    [Range(0f, 1f)]
    public float monsterBiteChance = 0.5f;

    // =========================================================
    // VICTORY
    // =========================================================

    [Header("Victory")]
    [Min(0f)]
    public float tauntDuration = 3f;

    // =========================================================
    // PLAYER HEIGHT
    // =========================================================

    [Header("Player Height Protection")]
    [Tooltip("Keeps Player at the original Y position.")]
    public bool lockPlayerHeight = true;

    // =========================================================
    // ANIMATOR PARAMETERS
    // =========================================================

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

    // =========================================================
    // REFERENCES
    // =========================================================

    private Animator playerAnimator;
    private Animator monsterAnimator;

    private PlayerClickController playerClickController;
    private PlayerController playerController;

    // =========================================================
    // STATE
    // =========================================================

    private float nextRoundTime;

    private bool fightFinished;
    private bool roundInProgress;
    private bool isReloading;

    private int shotsFired;

    private float playerCombatHeight;

    // =========================================================
    // UNITY
    // =========================================================

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        InitializeCombatants();

        if (player == null)
        {
            Debug.LogError("[COMBAT] Player Health not found.");
        }

        if (monster == null)
        {
            Debug.LogError("[COMBAT] Monster Health not found.");
        }
    }

    private void Update()
    {
        if (fightFinished)
            return;

        if (!IsCombatReady())
            return;

        KeepPlayerAtCombatHeight();

        float distance = GetHorizontalDistance(
            player.transform,
            monster.transform
        );

        // =====================================================
        // MELEE HAS PRIORITY
        // =====================================================

        if (distance <= attackRange)
        {
            HandleMeleeState();
            return;
        }

        // =====================================================
        // RANGED
        // =====================================================

        if (HasSelectedEnemy())
        {
            HandleRangedCombat(distance);
        }
    }

    // =========================================================
    // INITIALIZATION
    // =========================================================

    private void InitializeCombatants()
    {
        FindCombatants();
        CacheCombatantReferences();

        if (player != null)
        {
            playerCombatHeight = player.transform.position.y;
        }

        ResetCombatState();
    }

    private void CacheCombatantReferences()
    {
        playerAnimator = FindAttackAnimator(player, "Player");
        monsterAnimator = FindAttackAnimator(monster, "Monster");

        FindPlayerClickController();
        FindPlayerController();

        DisableRootMotion(playerAnimator);
        DisableRootMotion(monsterAnimator);
    }

    private void ResetCombatState()
    {
        StopAllCoroutines();

        fightFinished = false;
        roundInProgress = false;
        isReloading = false;

        shotsFired = 0;

        nextRoundTime = Time.time + 0.5f;

        ResetCombatAnimator(playerAnimator);
        ResetCombatAnimator(monsterAnimator);
    }

    private bool IsCombatReady()
    {
        if (fightFinished)
            return false;

        if (player == null || monster == null)
            return false;

        if (!player.gameObject.activeInHierarchy ||
            !monster.gameObject.activeInHierarchy)
        {
            return false;
        }

        if (player.IsDead() || monster.IsDead())
            return false;

        return true;
    }

    // =========================================================
    // FIND COMBATANTS
    // =========================================================

    private void FindCombatants()
    {
        // If both are already assigned, don't touch them.
        if (player != null && monster != null)
            return;

        Health[] healthObjects = FindObjectsByType<Health>();

        // -----------------------------------------------------
        // First pass:
        // Find a SimpleAgent whose target points to Player.
        // That Health is treated as Monster.
        // -----------------------------------------------------

        foreach (Health health in healthObjects)
        {
            if (health == null)
                continue;

            SimpleAgent agent =
                health.GetComponentInParent<SimpleAgent>();

            if (agent == null)
                continue;

            if (agent.target == null)
                continue;

            Health targetHealth =
                agent.target.GetComponentInChildren<Health>();

            if (targetHealth == null)
                continue;

            if (monster == null)
            {
                monster = health;
            }

            if (player == null)
            {
                player = targetHealth;
            }

            if (player != null && monster != null)
                break;
        }

        // -----------------------------------------------------
        // Second pass:
        // Find missing references.
        // -----------------------------------------------------

        if (player == null)
        {
            foreach (Health health in healthObjects)
            {
                if (health == null)
                    continue;

                if (health == monster)
                    continue;

                PlayerController controller =
                    health.GetComponentInParent<PlayerController>();

                if (controller != null)
                {
                    player = health;
                    break;
                }
            }
        }

        if (monster == null)
        {
            foreach (Health health in healthObjects)
            {
                if (health == null)
                    continue;

                if (health == player)
                    continue;

                SimpleAgent agent =
                    health.GetComponentInParent<SimpleAgent>();

                if (agent != null)
                {
                    monster = health;
                    break;
                }
            }
        }

        // -----------------------------------------------------
        // Final fallback.
        // -----------------------------------------------------

        if (player == null && monster != null)
        {
            foreach (Health health in healthObjects)
            {
                if (health != null && health != monster)
                {
                    player = health;
                    break;
                }
            }
        }

        if (monster == null && player != null)
        {
            foreach (Health health in healthObjects)
            {
                if (health != null && health != player)
                {
                    monster = health;
                    break;
                }
            }
        }
    }

    // =========================================================
    // PLAYER CLICK CONTROLLER
    // =========================================================

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

    // =========================================================
    // PLAYER CONTROLLER
    // =========================================================

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

    // =========================================================
    // ANIMATOR
    // =========================================================

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
        }

        return animator;
    }

    private void DisableRootMotion(Animator animator)
    {
        if (animator == null)
            return;

        animator.applyRootMotion = false;
    }

    // =========================================================
    // PLAYER HEIGHT
    // =========================================================

    private void KeepPlayerAtCombatHeight()
    {
        if (!lockPlayerHeight)
            return;

        if (player == null)
            return;

        Vector3 position = player.transform.position;

        if (Mathf.Abs(position.y - playerCombatHeight) <= 0.001f)
            return;

        position.y = playerCombatHeight;

        player.transform.position = position;
    }

    // =========================================================
    // DISTANCE
    // =========================================================

    private float GetHorizontalDistance(
        Transform a,
        Transform b
    )
    {
        if (a == null || b == null)
            return Mathf.Infinity;

        Vector3 aPosition = a.position;
        Vector3 bPosition = b.position;

        aPosition.y = 0f;
        bPosition.y = 0f;

        return Vector3.Distance(aPosition, bPosition);
    }

    // =========================================================
    // MELEE STATE
    // =========================================================

    private void HandleMeleeState()
    {
        StopPlayerMovementOnly();

        MaintainMeleeDistance();

        RotateTowardsOpponent(
            player.transform,
            monster.transform
        );

        RotateTowardsOpponent(
            monster.transform,
            player.transform
        );

        if (isReloading)
            return;

        if (roundInProgress)
            return;

        float distance = GetHorizontalDistance(
            player.transform,
            monster.transform
        );

        if (distance > meleeCombatDistance + 0.1f)
            return;

        if (Time.time < nextRoundTime)
            return;

        StartCoroutine(ResolveMeleeRound());
    }

    // =========================================================
    // MELEE POSITION
    // =========================================================

    private void MaintainMeleeDistance()
    {
        if (!forceMeleeDistance)
            return;

        if (!IsCombatReady())
            return;

        float currentDistance = GetHorizontalDistance(
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

        float requiredMovement =
            currentDistance - meleeCombatDistance;

        // -----------------------------------------------------
        // PLAYER
        // -----------------------------------------------------

        if (playerController != null)
        {
            playerController.StopMovement();
        }

        float playerMovement = Mathf.Min(
            requiredMovement * 0.5f,
            meleeApproachSpeed * Time.deltaTime
        );

        Vector3 playerPosition =
            player.transform.position;

        playerPosition += direction * playerMovement;
        playerPosition.y = playerCombatHeight;

        player.transform.position = playerPosition;

        // -----------------------------------------------------
        // MONSTER
        // -----------------------------------------------------

        currentDistance = GetHorizontalDistance(
            player.transform,
            monster.transform
        );

        if (currentDistance <= meleeCombatDistance)
            return;

        Vector3 monsterDirection =
            player.transform.position -
            monster.transform.position;

        monsterDirection.y = 0f;

        if (monsterDirection.sqrMagnitude < 0.0001f)
            return;

        monsterDirection.Normalize();

        float monsterMovement = Mathf.Min(
            currentDistance - meleeCombatDistance,
            monsterMeleeApproachSpeed * Time.deltaTime
        );

        Vector3 monsterPosition =
            monster.transform.position;

        monsterPosition +=
            monsterDirection * monsterMovement;

        monster.transform.position = monsterPosition;
    }

    // =========================================================
    // ANIMATOR PARAMETER
    // =========================================================

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
            if (parameter.name == paramName &&
                parameter.type == type)
            {
                return true;
            }
        }

        return false;
    }

    // =========================================================
    // SAFE TRIGGER
    // =========================================================

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

        animator.ResetTrigger(triggerName);
    }

    private static void SafeSetTrigger(
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

        animator.ResetTrigger(triggerName);
        animator.SetTrigger(triggerName);
    }

    // =========================================================
    // SELECTED ENEMY
    // =========================================================

    private bool HasSelectedEnemy()
    {
        if (playerClickController == null)
        {
            FindPlayerClickController();
        }

        if (playerClickController == null)
            return false;

        return playerClickController.SelectedEnemy == monster;
    }

    // =========================================================
    // LINE OF SIGHT
    // =========================================================

    private bool HasLineOfSightToMonster()
    {
        if (!requireLineOfSight)
            return true;

        if (player == null || monster == null)
            return false;

        Vector3 origin = GetShootingOrigin();
        Vector3 target = GetAimPosition(monster);

        Vector3 direction = target - origin;

        float distance = direction.magnitude;

        if (distance <= 0.001f)
            return true;

        direction /= distance;

        origin += direction * lineOfSightStartOffset;

        distance -= lineOfSightStartOffset;

        if (distance <= 0f)
            return true;

        RaycastHit[] hits = Physics.RaycastAll(
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
                hits.Length > 0 ? Color.red : Color.green
            );
        }

        if (hits.Length == 0)
            return true;

        Array.Sort(
            hits,
            (a, b) => a.distance.CompareTo(b.distance)
        );

        foreach (RaycastHit hit in hits)
        {
            if (hit.collider == null)
                continue;

            Transform hitTransform = hit.collider.transform;

            // Monster is not an obstacle.
            if (hitTransform.IsChildOf(monster.transform) ||
                monster.transform.IsChildOf(hitTransform))
            {
                continue;
            }

            return false;
        }

        return true;
    }

    // =========================================================
    // SHOOTING ORIGIN
    // =========================================================

    private Vector3 GetShootingOrigin()
    {
        if (player == null)
            return Vector3.zero;

        Collider collider =
            player.GetComponentInChildren<Collider>();

        if (collider != null)
            return collider.bounds.center;

        Vector3 position = player.transform.position;
        position.y += 1f;

        return position;
    }

    // =========================================================
    // MELEE ROUND
    // =========================================================

    private IEnumerator ResolveMeleeRound()
    {
        roundInProgress = true;

        if (!IsCombatReady())
        {
            roundInProgress = false;
            yield break;
        }

        MaintainMeleeDistance();

        RotateTowardsOpponent(
            player.transform,
            monster.transform
        );

        RotateTowardsOpponent(
            monster.transform,
            player.transform
        );

        // -----------------------------------------------------
        // PLAYER ATTACK
        // -----------------------------------------------------

        yield return StartCoroutine(
            PerformMeleeAttack(
                playerAnimator,
                player,
                monster,
                false
            )
        );

        if (fightFinished || !IsCombatReady())
        {
            roundInProgress = false;
            yield break;
        }

        // -----------------------------------------------------
        // MONSTER ATTACK
        // -----------------------------------------------------

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
                Time.time + roundCooldown;
        }

        roundInProgress = false;
    }

    // =========================================================
    // MELEE ATTACK
    // =========================================================

    private IEnumerator PerformMeleeAttack(
        Animator attackerAnimator,
        Health attacker,
        Health defender,
        bool monsterAttacking
    )
    {
        if (attacker == null || defender == null)
            yield break;

        if (attacker.IsDead() || defender.IsDead())
            yield break;

        if (!attacker.gameObject.activeInHierarchy ||
            !defender.gameObject.activeInHierarchy)
        {
            yield break;
        }

        MaintainMeleeDistance();

        RotateTowardsOpponent(
            attacker.transform,
            defender.transform
        );

        string selectedTrigger = AttackTrigger;

        // -----------------------------------------------------
        // MONSTER ATTACK TYPE
        // -----------------------------------------------------

        if (monsterAttacking)
        {
            bool hasBite = HasParameter(
                attackerAnimator,
                BiteTrigger,
                AnimatorControllerParameterType.Trigger
            );

            bool hasAttack = HasParameter(
                attackerAnimator,
                AttackTrigger,
                AnimatorControllerParameterType.Trigger
            );

            if (hasBite && hasAttack)
            {
                selectedTrigger =
                    Random.value <= monsterBiteChance
                        ? BiteTrigger
                        : AttackTrigger;
            }
            else if (hasBite)
            {
                selectedTrigger = BiteTrigger;
            }
            else if (hasAttack)
            {
                selectedTrigger = AttackTrigger;
            }
            else
            {
                yield break;
            }
        }
        else
        {
            if (!HasParameter(
                    attackerAnimator,
                    AttackTrigger,
                    AnimatorControllerParameterType.Trigger
                ))
            {
                yield break;
            }
        }

        // -----------------------------------------------------
        // PLAY ATTACK
        // -----------------------------------------------------

        SafeResetTrigger(
            attackerAnimator,
            AttackTrigger
        );

        SafeResetTrigger(
            attackerAnimator,
            BiteTrigger
        );

        SafeSetTrigger(
            attackerAnimator,
            selectedTrigger
        );

        // -----------------------------------------------------
        // WAIT FOR HIT
        // -----------------------------------------------------

        float timer = 0f;

        while (timer < damageDelay)
        {
            if (fightFinished || !IsCombatReady())
                yield break;

            MaintainMeleeDistance();

            RotateTowardsOpponent(
                attacker.transform,
                defender.transform
            );

            timer += Time.deltaTime;

            yield return null;
        }

        // -----------------------------------------------------
        // FINAL VALIDATION
        // -----------------------------------------------------

        if (fightFinished || !IsCombatReady())
            yield break;

        // -----------------------------------------------------
        // DAMAGE
        // -----------------------------------------------------

        float damage = Random.Range(
            Mathf.Min(damageMin, damageMax),
            Mathf.Max(damageMin, damageMax)
        );

        defender.TakeDamage(damage);

        // -----------------------------------------------------
        // HIT FX
        // -----------------------------------------------------

        if (HitEffectManager.Instance != null)
        {
            HitEffectManager.Instance.PlayHitEffects(
                defender,
                attacker.transform
            );
        }

        // -----------------------------------------------------
        // DAMAGE ANIMATION
        // -----------------------------------------------------

        Animator defenderAnimator =
            defender == player
                ? playerAnimator
                : monsterAnimator;

        PlayRandomGetDamage(
            defenderAnimator,
            defender.name
        );

        // -----------------------------------------------------
        // DEATH
        // -----------------------------------------------------

        if (defender.IsDead())
        {
            FinishFight(defender);
            yield break;
        }

        // -----------------------------------------------------
        // FINISH ATTACK ANIMATION
        // -----------------------------------------------------

        float remainingTime = Mathf.Max(
            0f,
            attackAnimationDuration - damageDelay
        );

        if (remainingTime > 0f)
        {
            yield return new WaitForSeconds(
                remainingTime
            );
        }
    }

    // =========================================================
    // RANGED COMBAT
    // =========================================================

    private void HandleRangedCombat(float distance)
    {
        if (distance > shootingRange)
            return;

        if (isReloading || roundInProgress)
            return;

        StopPlayerMovementOnly();

        RotateSoldierTowardsTarget();

        if (!HasLineOfSightToMonster())
            return;

        if (Time.time < nextRoundTime)
            return;

        StartCoroutine(PerformShoot());
    }

    // =========================================================
    // SHOOT
    // =========================================================

    private IEnumerator PerformShoot()
    {
        roundInProgress = true;

        if (!IsCombatReady())
        {
            roundInProgress = false;
            yield break;
        }

        if (!HasSelectedEnemy())
        {
            roundInProgress = false;
            yield break;
        }

        if (!CanShoot())
        {
            roundInProgress = false;
            yield break;
        }

        // -----------------------------------------------------
        // STOP
        // -----------------------------------------------------

        StopPlayerMovementOnly();

        // -----------------------------------------------------
        // AIM
        // -----------------------------------------------------

        if (!HasLineOfSightToMonster())
        {
            roundInProgress = false;
            yield break;
        }

        RotateSoldierTowardsTarget();

        SafeResetTrigger(
            playerAnimator,
            AimTrigger
        );

        SafeResetTrigger(
            playerAnimator,
            ShootingTrigger
        );

        SafeSetTrigger(
            playerAnimator,
            AimTrigger
        );

        // -----------------------------------------------------
        // AIM TIMER
        // -----------------------------------------------------

        float timer = 0f;

        while (timer < aimDuration)
        {
            if (!CanContinueShooting())
            {
                roundInProgress = false;
                yield break;
            }

            RotateSoldierTowardsTarget();
            KeepPlayerAtCombatHeight();

            timer += Time.deltaTime;

            yield return null;
        }

        // -----------------------------------------------------
        // FINAL AIM
        // -----------------------------------------------------

        RotateSoldierTowardsTarget();

        if (!CanContinueShooting())
        {
            roundInProgress = false;
            yield break;
        }

        // -----------------------------------------------------
        // SHOOT ANIMATION
        // -----------------------------------------------------

        SafeResetTrigger(
            playerAnimator,
            AimTrigger
        );

        SafeSetTrigger(
            playerAnimator,
            ShootingTrigger
        );

        // -----------------------------------------------------
        // BULLET DELAY
        // -----------------------------------------------------

        timer = 0f;

        while (timer < damageDelay)
        {
            if (!CanContinueShooting())
            {
                roundInProgress = false;
                yield break;
            }

            if (continuouslyCorrectAim)
            {
                RotateSoldierTowardsTarget();
            }

            KeepPlayerAtCombatHeight();

            timer += Time.deltaTime;

            yield return null;
        }

        // -----------------------------------------------------
        // FINAL VALIDATION
        // -----------------------------------------------------

        if (!CanContinueShooting())
        {
            roundInProgress = false;
            yield break;
        }

        RotateSoldierTowardsTarget();

        // -----------------------------------------------------
        // DAMAGE
        // -----------------------------------------------------

        float damage = Random.Range(
            Mathf.Min(
                shootingDamageMin,
                shootingDamageMax
            ),
            Mathf.Max(
                shootingDamageMin,
                shootingDamageMax
            )
        );

        monster.TakeDamage(damage);

        // -----------------------------------------------------
        // HIT FX
        // -----------------------------------------------------

        if (HitEffectManager.Instance != null)
        {
            HitEffectManager.Instance.PlayHitEffects(
                monster,
                player.transform
            );
        }

        // -----------------------------------------------------
        // DAMAGE ANIMATION
        // -----------------------------------------------------

        PlayRandomGetDamage(
            monsterAnimator,
            monster.name
        );

        shotsFired++;

        Debug.Log(
            $"[COMBAT] Shot #{shotsFired}. " +
            $"Monster HP: {monster.CurrentHealth:F1}"
        );

        // -----------------------------------------------------
        // DEATH
        // -----------------------------------------------------

        if (monster.IsDead())
        {
            FinishFight(monster);

            roundInProgress = false;
            yield break;
        }

        // -----------------------------------------------------
        // RELOAD
        // -----------------------------------------------------

        if (shotsFired >= Mathf.Max(1, shotsBeforeReload))
        {
            yield return StartCoroutine(Reload());
        }

        if (!fightFinished)
        {
            nextRoundTime =
                Time.time + shootingInterval;
        }

        roundInProgress = false;
    }

    // =========================================================
    // CAN SHOOT
    // =========================================================

    private bool CanShoot()
    {
        if (!IsCombatReady())
            return false;

        if (!HasSelectedEnemy())
            return false;

        float distance = GetHorizontalDistance(
            player.transform,
            monster.transform
        );

        if (distance > shootingRange)
            return false;

        if (!HasLineOfSightToMonster())
            return false;

        return true;
    }

    private bool CanContinueShooting()
    {
        if (!IsCombatReady())
            return false;

        if (!HasSelectedEnemy())
            return false;

        float distance = GetHorizontalDistance(
            player.transform,
            monster.transform
        );

        if (distance > shootingRange)
            return false;

        if (!HasLineOfSightToMonster())
            return false;

        return true;
    }

    // =========================================================
    // ROTATE PLAYER TO MONSTER
    // =========================================================

    private void RotateSoldierTowardsTarget()
    {
        if (player == null || monster == null)
            return;

        Vector3 target =
            GetAimPosition(monster);

        Vector3 direction =
            target - player.transform.position;

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
                Mathf.Clamp01(
                    shootingRotationSpeed *
                    Time.deltaTime
                )
            );
    }

    // =========================================================
    // AIM POSITION
    // =========================================================

    private Vector3 GetAimPosition(Health target)
    {
        if (target == null)
            return Vector3.zero;

        Collider targetCollider =
            target.GetComponentInChildren<Collider>();

        if (targetCollider != null)
        {
            Bounds bounds =
                targetCollider.bounds;

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

    // =========================================================
    // RELOAD
    // =========================================================

    private IEnumerator Reload()
    {
        if (isReloading)
            yield break;

        isReloading = true;

        StopPlayerMovementOnly();

        Debug.Log("[COMBAT] Soldier Reloading.");

        SafeResetTrigger(
            playerAnimator,
            AimTrigger
        );

        SafeResetTrigger(
            playerAnimator,
            ShootingTrigger
        );

        SafeSetTrigger(
            playerAnimator,
            ReloadingTrigger
        );

        float timer = 0f;

        while (timer < reloadDuration)
        {
            if (fightFinished)
            {
                isReloading = false;
                yield break;
            }

            KeepPlayerAtCombatHeight();

            timer += Time.deltaTime;

            yield return null;
        }

        shotsFired = 0;
        isReloading = false;

        SafeResetTrigger(
            playerAnimator,
            ReloadingTrigger
        );

        Debug.Log("[COMBAT] Reload finished.");
    }

    // =========================================================
    // ROTATION
    // =========================================================

    private void RotateTowardsOpponent(
        Transform fighter,
        Transform opponent
    )
    {
        if (fighter == null || opponent == null)
            return;

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
                Mathf.Clamp01(
                    combatRotationSpeed *
                    Time.deltaTime
                )
            );
    }

    // =========================================================
    // STOP PLAYER
    // =========================================================

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

        SetWalking(
            playerAnimator,
            false
        );

        KeepPlayerAtCombatHeight();
    }

    // =========================================================
    // DAMAGE ANIMATION
    // =========================================================

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
                UnityEngine.Random.Range(0, 2);

            animator.SetInteger(
                GetDamageIndexInt,
                randomIndex
            );

            Debug.Log(
                $"[COMBAT] {defenderName} " +
                $"GetDamageIndex: {randomIndex}"
            );
        }

        SafeSetTrigger(
            animator,
            GetDamageTrigger
        );
    }

    // =========================================================
    // FINISH FIGHT
    // =========================================================

    private void FinishFight(Health loser)
    {
        if (fightFinished || loser == null)
            return;

        fightFinished = true;
        roundInProgress = false;
        isReloading = false;

        StopAllCoroutines();

        bool playerWon = loser == monster;

        Debug.Log(
            playerWon
                ? "[COMBAT] PLAYER WINS!"
                : "[COMBAT] MONSTER WINS!"
        );

        // -----------------------------------------------------
        // Disable selection
        // -----------------------------------------------------

        DisablePlayerSelection();

        // -----------------------------------------------------
        // Player death
        // -----------------------------------------------------

        if (loser == player)
        {
            StopPlayerMovement();
        }

        // -----------------------------------------------------
        // Monster death
        // -----------------------------------------------------

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

        // -----------------------------------------------------
        // Health bar
        // -----------------------------------------------------

        HideHealthBar(loser);

        // -----------------------------------------------------
        // Death animation
        // -----------------------------------------------------

        PlayDeath(loser);

        // -----------------------------------------------------
        // Victory
        // -----------------------------------------------------

        if (playerWon)
        {
            StartCoroutine(PlayerVictory());
        }
        else
        {
            StartCoroutine(MonsterVictory());
        }
    }

    // =========================================================
    // DISABLE PLAYER SELECTION
    // =========================================================

    private void DisablePlayerSelection()
    {
        FindPlayerClickController();

        if (playerClickController == null)
            return;

        playerClickController.SetSelected(false);
        playerClickController.enabled = false;
    }

    // =========================================================
    // PLAYER DEATH MOVEMENT
    // =========================================================

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

        DisablePlayerSelection();
    }

    // =========================================================
    // HEALTH BAR
    // =========================================================

    private void HideHealthBar(Health loser)
    {
        if (loser == null)
            return;

        Transform anchor =
            loser.transform.Find("HealthBarAnchor");

        if (anchor == null)
        {
            Transform[] children =
                loser.GetComponentsInChildren<Transform>(true);

            foreach (Transform child in children)
            {
                if (child.name == "HealthBarAnchor")
                {
                    anchor = child;
                    break;
                }
            }
        }

        if (anchor != null)
        {
            anchor.gameObject.SetActive(false);
            return;
        }

        HealthBar[] healthBars =
            loser.GetComponentsInChildren<HealthBar>(true);

        foreach (HealthBar healthBar in healthBars)
        {
            if (healthBar != null)
            {
                healthBar.gameObject.SetActive(false);
            }
        }
    }

    // =========================================================
    // DEATH
    // =========================================================

    private void PlayDeath(Health loser)
    {
        if (loser == null)
            return;

        if (loser == player)
        {
            StopPlayerMovement();
        }

        Animator animator =
            loser == player
                ? playerAnimator
                : monsterAnimator;

        if (animator == null)
            return;

        animator.applyRootMotion = false;

        // -----------------------------------------------------
        // Reset combat triggers
        // -----------------------------------------------------

        SafeResetTrigger(
            animator,
            AttackTrigger
        );

        SafeResetTrigger(
            animator,
            BiteTrigger
        );

        SafeResetTrigger(
            animator,
            AimTrigger
        );

        SafeResetTrigger(
            animator,
            ShootingTrigger
        );

        SafeResetTrigger(
            animator,
            ReloadingTrigger
        );

        SafeResetTrigger(
            animator,
            TauntTrigger
        );

        SafeResetTrigger(
            animator,
            GetDamageTrigger
        );

        // -----------------------------------------------------
        // Stop walking
        // -----------------------------------------------------

        SetWalking(
            animator,
            false
        );

        // -----------------------------------------------------
        // Death index
        // -----------------------------------------------------

        if (HasParameter(
                animator,
                DeathIndexInt,
                AnimatorControllerParameterType.Int
            ))
        {
            int randomDeathIndex =
                UnityEngine.Random.Range(0, 2);

            animator.SetInteger(
                DeathIndexInt,
                randomDeathIndex
            );

            Debug.Log(
                $"[COMBAT] {loser.name} " +
                $"DeathIndex: {randomDeathIndex}"
            );
        }

        // -----------------------------------------------------
        // Death trigger
        // -----------------------------------------------------

        if (HasParameter(
                animator,
                DeathTrigger,
                AnimatorControllerParameterType.Trigger
            ))
        {
            SafeSetTrigger(
                animator,
                DeathTrigger
            );
        }
        else
        {
            Debug.LogWarning(
                $"[COMBAT] {loser.name} Animator " +
                $"has no Death trigger."
            );
        }
    }

    // =========================================================
    // PLAYER VICTORY
    // =========================================================

    private IEnumerator PlayerVictory()
    {
        if (player == null)
            yield break;

        StopPlayerMovementOnly();
        DisablePlayerSelection();

        ResetCombatAnimator(playerAnimator);

        if (playerAnimator != null &&
            HasParameter(
                playerAnimator,
                TauntTrigger,
                AnimatorControllerParameterType.Trigger
            ))
        {
            SafeSetTrigger(
                playerAnimator,
                TauntTrigger
            );
        }

        if (tauntDuration > 0f)
        {
            yield return new WaitForSeconds(
                tauntDuration
            );
        }

        SafeResetTrigger(
            playerAnimator,
            TauntTrigger
        );

        SetWalking(
            playerAnimator,
            false
        );
    }

    // =========================================================
    // MONSTER VICTORY
    // =========================================================

    private IEnumerator MonsterVictory()
    {
        if (monster == null)
            yield break;

        ResetCombatAnimator(monsterAnimator);

        if (monsterAnimator != null &&
            HasParameter(
                monsterAnimator,
                TauntTrigger,
                AnimatorControllerParameterType.Trigger
            ))
        {
            SafeSetTrigger(
                monsterAnimator,
                TauntTrigger
            );
        }

        if (tauntDuration > 0f)
        {
            yield return new WaitForSeconds(
                tauntDuration
            );
        }

        SafeResetTrigger(
            monsterAnimator,
            TauntTrigger
        );

        SetWalking(
            monsterAnimator,
            false
        );
    }

    // =========================================================
    // RESET COMBAT ANIMATOR
    // =========================================================

    private void ResetCombatAnimator(Animator animator)
    {
        if (animator == null)
            return;

        animator.applyRootMotion = false;

        SafeResetTrigger(
            animator,
            AttackTrigger
        );

        SafeResetTrigger(
            animator,
            BiteTrigger
        );

        SafeResetTrigger(
            animator,
            AimTrigger
        );

        SafeResetTrigger(
            animator,
            ShootingTrigger
        );

        SafeResetTrigger(
            animator,
            ReloadingTrigger
        );

        SafeResetTrigger(
            animator,
            TauntTrigger
        );

        SafeResetTrigger(
            animator,
            GetDamageTrigger
        );

        SafeResetTrigger(
            animator,
            DeathTrigger
        );

        SetWalking(
            animator,
            false
        );
    }

    // =========================================================
    // WALKING
    // =========================================================

    private void SetWalking(
        Animator animator,
        bool value
    )
    {
        if (animator == null)
            return;

        if (HasParameter(
                animator,
                WalkingBool,
                AnimatorControllerParameterType.Bool
            ))
        {
            animator.SetBool(
                WalkingBool,
                value
            );
        }
    }

    // =========================================================
    // REGISTER COMBATANTS
    // =========================================================

    public void RegisterCombatants(
        Health playerHealth,
        Health monsterHealth
    )
    {
        StopAllCoroutines();

        player = playerHealth;
        monster = monsterHealth;

        CacheCombatantReferences();

        if (player != null)
        {
            playerCombatHeight =
                player.transform.position.y;
        }

        ResetCombatState();

        Debug.Log(
            $"[COMBAT] Registered Player: " +
            $"{(player != null ? player.name : "NULL")}"
        );

        Debug.Log(
            $"[COMBAT] Registered Monster: " +
            $"{(monster != null ? monster.name : "NULL")}"
        );
    }

    // =========================================================
    // OPTIONAL RESET
    // =========================================================

    public void ResetFight()
    {
        StopAllCoroutines();

        fightFinished = false;
        roundInProgress = false;
        isReloading = false;

        shotsFired = 0;

        if (player != null)
        {
            playerCombatHeight =
                player.transform.position.y;
        }

        CacheCombatantReferences();

        ResetCombatAnimator(playerAnimator);
        ResetCombatAnimator(monsterAnimator);

        nextRoundTime =
            Time.time + 0.5f;
    }

    // =========================================================
    // CLEANUP
    // =========================================================

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}