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
    // WEAPON / MUZZLE
    // =========================================================

    [Header("Weapon")]
    [Tooltip("Point at the end of the gun barrel where the tracer starts. " +
             "Can be assigned automatically by WeaponHolder.")]
    public Transform muzzlePoint;

    [Tooltip("If enabled, CombatManager automatically searches the dynamically " +
             "created Weapon for MuzzlePoint.")]
    public bool automaticallyFindMuzzle = true;

    [Tooltip("Names accepted when searching for the muzzle.")]
    public string[] muzzleNames =
    {
        "MuzzlePoint",
        "Muzzle",
        "BarrelEnd",
        "Barrel",
        "FirePoint",
        "FirePoint_R",
        "ShootPoint"
    };

    // =========================================================
    // BULLET / TRACER
    // =========================================================

    [Header("Bullet Tracer")]
    [Tooltip("Speed of the visual bullet travelling to the target.")]
    [Min(0.1f)]
    public float bulletSpeed = 35f;

    [Tooltip("Length of the subtle bullet trail.")]
    [Min(0.01f)]
    public float bulletTrailLength = 0.35f;

    [Tooltip("Width of the bullet trail at its thickest point.")]
    [Min(0.0001f)]
    public float bulletTrailWidth = 0.018f;

    [Tooltip("How long the bullet trail remains visible after reaching the target.")]
    [Min(0f)]
    public float bulletTrailFadeDuration = 0.04f;

    [Tooltip("If enabled, the tracer is visible in the Game view.")]
    public bool showBulletTracer = true;

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
    private float playerCombatHeight;

    private bool fightFinished;
    private bool roundInProgress;
    private bool isReloading;

    private int shotsFired;

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
        ValidateSettings();

        // Weapon może zostać utworzony przez WeaponHolder dopiero
        // po Start(), dlatego próbujemy znaleźć muzzle również tutaj.
        RefreshMuzzlePoint();

        if (player == null)
        {
            Debug.LogError(
                "[COMBAT] Player Health not found.",
                this
            );
        }

        if (monster == null)
        {
            Debug.LogError(
                "[COMBAT] Monster Health not found.",
                this
            );
        }
    }

    private void Update()
    {
        if (fightFinished)
            return;

        if (!IsCombatReady())
            return;

        KeepPlayerAtCombatHeight();

        // =====================================================
        // DYNAMIC WEAPON / MUZZLE
        // =====================================================

        // Weapon jest tworzony przez WeaponHolder w runtime.
        // Nie możemy zakładać, że muzzlePoint istniał w Start().
        if (automaticallyFindMuzzle &&
            muzzlePoint == null)
        {
            RefreshMuzzlePoint();
        }

        float distance =
            GetHorizontalDistance(
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
    // MUZZLE POINT API
    // =========================================================

    /// <summary>
    /// WeaponHolder może wywołać tę metodę po utworzeniu broni.
    /// Dzięki temu CombatManager zawsze zna aktualny muzzle.
    /// </summary>
    public void SetMuzzlePoint(Transform point)
    {
        if (point == null)
        {
            Debug.LogWarning(
                "[COMBAT] WeaponHolder attempted to register a NULL MuzzlePoint.",
                this
            );

            return;
        }

        muzzlePoint = point;

        Debug.Log(
            $"[COMBAT] MuzzlePoint registered: {GetTransformPath(point)}",
            point
        );
    }

    // =========================================================
    // AUTOMATIC MUZZLE SEARCH
    // =========================================================

    private void RefreshMuzzlePoint()
    {
        if (player == null)
            return;

        // Najpierw sprawdzamy, czy aktualnie przypisany muzzle
        // nadal istnieje i jest dzieckiem Playera.
        if (muzzlePoint != null)
        {
            if (muzzlePoint.gameObject != null &&
                muzzlePoint.IsChildOf(player.transform))
            {
                return;
            }

            muzzlePoint = null;
        }

        Transform[] allTransforms =
            player.GetComponentsInChildren<Transform>(true);

        // -----------------------------------------------------
        // 1. Szukamy po nazwie MuzzlePoint / Muzzle / FirePoint
        // -----------------------------------------------------

        foreach (Transform current in allTransforms)
        {
            if (current == null)
                continue;

            if (IsMuzzleName(current.name))
            {
                muzzlePoint = current;

                Debug.Log(
                    $"[COMBAT] Automatically found muzzle: " +
                    $"{GetTransformPath(current)}",
                    current
                );

                return;
            }
        }

        // -----------------------------------------------------
        // 2. Szukamy obiektu Weapon i jego dzieci
        // -----------------------------------------------------

        foreach (Transform current in allTransforms)
        {
            if (current == null)
                continue;

            if (!string.Equals(
                    current.name,
                    "Weapon",
                    StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            Transform weaponMuzzle =
                FindMuzzleInside(current);

            if (weaponMuzzle != null)
            {
                muzzlePoint = weaponMuzzle;

                Debug.Log(
                    $"[COMBAT] Found muzzle inside Weapon: " +
                    $"{GetTransformPath(weaponMuzzle)}",
                    weaponMuzzle
                );

                return;
            }
        }
    }

    private Transform FindMuzzleInside(
        Transform weapon
    )
    {
        if (weapon == null)
            return null;

        Transform[] children =
            weapon.GetComponentsInChildren<Transform>(true);

        foreach (Transform child in children)
        {
            if (child == null)
                continue;

            if (IsMuzzleName(child.name))
            {
                return child;
            }
        }

        return null;
    }

    private bool IsMuzzleName(
        string objectName
    )
    {
        if (string.IsNullOrEmpty(objectName))
            return false;

        if (muzzleNames != null)
        {
            foreach (string acceptedName in muzzleNames)
            {
                if (string.IsNullOrEmpty(acceptedName))
                    continue;

                if (string.Equals(
                        objectName,
                        acceptedName,
                        StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
        }

        string lower =
            objectName.ToLowerInvariant();

        return lower.Contains("muzzle") ||
               lower.Contains("firepoint") ||
               lower.Contains("shootpoint") ||
               lower.Contains("barrelend");
    }

    private string GetTransformPath(
        Transform target
    )
    {
        if (target == null)
            return "NULL";

        string path =
            target.name;

        Transform current =
            target.parent;

        int safety = 0;

        while (current != null &&
               safety < 100)
        {
            path =
                current.name +
                "/" +
                path;

            current =
                current.parent;

            safety++;
        }

        return path;
    }

    // =========================================================
    // VALIDATION
    // =========================================================

    private void ValidateSettings()
    {
        if (damageMax < damageMin)
        {
            Debug.LogWarning(
                "[COMBAT] damageMax is lower than damageMin. " +
                "Values will be automatically normalized.",
                this
            );
        }

        if (shootingDamageMax < shootingDamageMin)
        {
            Debug.LogWarning(
                "[COMBAT] shootingDamageMax is lower than shootingDamageMin. " +
                "Values will be automatically normalized.",
                this
            );
        }

        if (meleeCombatDistance > attackRange)
        {
            Debug.LogWarning(
                "[COMBAT] meleeCombatDistance is greater than attackRange. " +
                "Melee positioning may not behave as expected.",
                this
            );
        }

        if (muzzlePoint == null)
        {
            Debug.Log(
                "[COMBAT] Muzzle Point is currently empty. " +
                "This is OK if WeaponHolder creates the weapon at runtime.",
                this
            );
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
            playerCombatHeight =
                player.transform.position.y;
        }

        ResetCombatState();
    }

    private void CacheCombatantReferences()
    {
        playerAnimator =
            FindAttackAnimator(player, "Player");

        monsterAnimator =
            FindAttackAnimator(monster, "Monster");

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

        nextRoundTime =
            Time.time + 0.5f;

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

        if (player.IsDead() ||
            monster.IsDead())
        {
            return false;
        }

        return true;
    }

    // =========================================================
    // FIND COMBATANTS
    // =========================================================

    private void FindCombatants()
    {
        if (player != null && monster != null)
            return;

        Health[] healthObjects =
            FindObjectsOfType<Health>(true);

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

            if (targetHealth == health)
                continue;

            if (monster == null)
                monster = health;

            if (player == null)
                player = targetHealth;

            if (player != null &&
                monster != null)
            {
                break;
            }
        }

        if (player == null)
        {
            foreach (Health health in healthObjects)
            {
                if (health == null ||
                    health == monster)
                {
                    continue;
                }

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
                if (health == null ||
                    health == player)
                {
                    continue;
                }

                SimpleAgent agent =
                    health.GetComponentInParent<SimpleAgent>();

                if (agent != null)
                {
                    monster = health;
                    break;
                }
            }
        }

        if (player == null &&
            monster != null)
        {
            foreach (Health health in healthObjects)
            {
                if (health != null &&
                    health != monster)
                {
                    player = health;
                    break;
                }
            }
        }

        if (monster == null &&
            player != null)
        {
            foreach (Health health in healthObjects)
            {
                if (health != null &&
                    health != player)
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
                $"[COMBAT] {label} has no Animator.",
                combatant
            );
        }

        return animator;
    }

    private void DisableRootMotion(
        Animator animator
    )
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

        Transform playerTransform =
            player.transform;

        Vector3 position =
            playerTransform.position;

        if (Mathf.Abs(
                position.y -
                playerCombatHeight
            ) <= 0.001f)
        {
            return;
        }

        position.y =
            playerCombatHeight;

        playerTransform.position =
            position;
    }

    // =========================================================
    // DISTANCE
    // =========================================================

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

        float distance =
            GetHorizontalDistance(
                player.transform,
                monster.transform
            );

        if (distance >
            meleeCombatDistance + 0.1f)
        {
            return;
        }

        if (Time.time < nextRoundTime)
            return;

        StartCoroutine(
            ResolveMeleeRound()
        );
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

        float currentDistance =
            GetHorizontalDistance(
                player.transform,
                monster.transform
            );

        if (currentDistance <=
            meleeCombatDistance)
        {
            return;
        }

        Vector3 direction =
            monster.transform.position -
            player.transform.position;

        direction.y = 0f;

        if (direction.sqrMagnitude <
            0.0001f)
        {
            return;
        }

        direction.Normalize();

        float requiredMovement =
            currentDistance -
            meleeCombatDistance;

        if (playerController != null)
        {
            playerController.StopMovement();
        }

        float playerMovement =
            Mathf.Min(
                requiredMovement * 0.5f,
                meleeApproachSpeed *
                Time.deltaTime
            );

        Vector3 playerPosition =
            player.transform.position;

        playerPosition +=
            direction *
            playerMovement;

        playerPosition.y =
            playerCombatHeight;

        player.transform.position =
            playerPosition;

        currentDistance =
            GetHorizontalDistance(
                player.transform,
                monster.transform
            );

        if (currentDistance <=
            meleeCombatDistance)
        {
            return;
        }

        Vector3 monsterDirection =
            player.transform.position -
            monster.transform.position;

        monsterDirection.y = 0f;

        if (monsterDirection.sqrMagnitude <
            0.0001f)
        {
            return;
        }

        monsterDirection.Normalize();

        float monsterMovement =
            Mathf.Min(
                currentDistance -
                meleeCombatDistance,
                monsterMeleeApproachSpeed *
                Time.deltaTime
            );

        Vector3 monsterPosition =
            monster.transform.position;

        monsterPosition +=
            monsterDirection *
            monsterMovement;

        monster.transform.position =
            monsterPosition;
    }

    // =========================================================
    // ANIMATOR PARAMETER
    // =========================================================

    private static bool HasParameter(
        Animator animator,
        string parameterName,
        AnimatorControllerParameterType type
    )
    {
        if (animator == null)
            return false;

        if (animator.runtimeAnimatorController == null)
            return false;

        foreach (
            AnimatorControllerParameter parameter
            in animator.parameters
        )
        {
            if (parameter.name == parameterName &&
                parameter.type == type)
            {
                return true;
            }
        }

        return false;
    }

    // =========================================================
    // SAFE TRIGGERS
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

        return playerClickController.SelectedEnemy ==
               monster;
    }

    // =========================================================
    // LINE OF SIGHT
    // =========================================================

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
            GetAimPosition(monster);

        Vector3 direction =
            target - origin;

        float distance =
            direction.magnitude;

        if (distance <= 0.001f)
            return true;

        direction /= distance;

        float offset =
            Mathf.Min(
                lineOfSightStartOffset,
                distance
            );

        origin +=
            direction * offset;

        distance -= offset;

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
            return true;

        Array.Sort(
            hits,
            (a, b) =>
                a.distance.CompareTo(b.distance)
        );

        foreach (RaycastHit hit in hits)
        {
            if (hit.collider == null)
                continue;

            Transform hitTransform =
                hit.collider.transform;

            if (hitTransform.IsChildOf(
                    monster.transform
                ) ||
                monster.transform.IsChildOf(
                    hitTransform
                ))
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
        // =====================================================
        // 1. AKTUALNY MUZZLE
        // =====================================================

        if (automaticallyFindMuzzle &&
            muzzlePoint == null)
        {
            RefreshMuzzlePoint();
        }

        if (muzzlePoint != null)
        {
            // Upewniamy się, że muzzle nadal należy do Playera.
            if (player != null &&
                muzzlePoint.IsChildOf(player.transform))
            {
                return muzzlePoint.position;
            }

            muzzlePoint = null;

            RefreshMuzzlePoint();

            if (muzzlePoint != null)
                return muzzlePoint.position;
        }

        // =====================================================
        // 2. OSTATECZNY FALLBACK
        // =====================================================

        // To nie powinno być używane, jeśli WeaponHolder
        // poprawnie utworzył MuzzlePoint.
        if (player == null)
            return transform.position;

        Collider[] colliders =
            player.GetComponentsInChildren<Collider>(
                true
            );

        // Szukamy collidera, ale pomijamy Weapon,
        // żeby nie brać przypadkowego punktu z broni.
        foreach (Collider collider in colliders)
        {
            if (collider == null)
                continue;

            Transform colliderTransform =
                collider.transform;

            if (colliderTransform.name
                    .ToLowerInvariant()
                    .Contains("weapon"))
            {
                continue;
            }

            return collider.bounds.center;
        }

        Vector3 position =
            player.transform.position;

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

        yield return StartCoroutine(
            PerformMeleeAttack(
                playerAnimator,
                player,
                monster,
                false
            )
        );

        if (fightFinished ||
            !IsCombatReady())
        {
            roundInProgress = false;
            yield break;
        }

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
                Mathf.Max(
                    0f,
                    roundCooldown
                );
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
        if (attacker == null ||
            defender == null)
        {
            yield break;
        }

        if (attacker.IsDead() ||
            defender.IsDead())
        {
            yield break;
        }

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

        string selectedTrigger =
            AttackTrigger;

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
                    Random.value <= monsterBiteChance
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

        float timer = 0f;

        while (timer < damageDelay)
        {
            if (fightFinished ||
                !IsCombatReady())
            {
                yield break;
            }

            MaintainMeleeDistance();

            RotateTowardsOpponent(
                attacker.transform,
                defender.transform
            );

            timer += Time.deltaTime;

            yield return null;
        }

        if (fightFinished ||
            !IsCombatReady())
        {
            yield break;
        }

        float distance =
            GetHorizontalDistance(
                attacker.transform,
                defender.transform
            );

        if (distance >
            meleeCombatDistance + 0.35f)
        {
            yield break;
        }

        float damage =
            Random.Range(
                Mathf.Min(
                    damageMin,
                    damageMax
                ),
                Mathf.Max(
                    damageMin,
                    damageMax
                )
            );

        defender.TakeDamage(damage);

        PlayHitEffects(
            defender,
            attacker.transform
        );

        Animator defenderAnimator =
            defender == player
                ? playerAnimator
                : monsterAnimator;

        PlayRandomGetDamage(
            defenderAnimator,
            defender.name
        );

        if (defender.IsDead())
        {
            FinishFight(defender);
            yield break;
        }

        float remainingTime =
            Mathf.Max(
                0f,
                attackAnimationDuration -
                damageDelay
            );

        if (remainingTime > 0f)
        {
            yield return new WaitForSeconds(
                remainingTime
            );
        }
    }

    // =========================================================
    // HIT EFFECTS
    // =========================================================

    private void PlayHitEffects(
        Health target,
        Transform attacker
    )
    {
        if (target == null)
            return;

        if (HitEffectManager.Instance == null)
            return;

        HitEffectManager.Instance.PlayHitEffects(
            target,
            attacker != null
                ? attacker
                : transform
        );
    }

    // =========================================================
    // RANGED COMBAT
    // =========================================================

    private void HandleRangedCombat(
        float distance
    )
    {
        if (distance > shootingRange)
            return;

        if (isReloading ||
            roundInProgress)
        {
            return;
        }

        StopPlayerMovementOnly();

        RotateSoldierTowardsTarget();

        if (!HasLineOfSightToMonster())
            return;

        if (Time.time < nextRoundTime)
            return;

        StartCoroutine(
            PerformShoot()
        );
    }

    // =========================================================
    // SHOOT
    // =========================================================

    private IEnumerator PerformShoot()
    {
        roundInProgress = true;

        // Weapon może być tworzony dokładnie w tym momencie.
        // Jeszcze raz szukamy muzzle przed rozpoczęciem strzału.
        RefreshMuzzlePoint();

        if (!CanShoot())
        {
            roundInProgress = false;
            yield break;
        }

        StopPlayerMovementOnly();

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

        SafeSetTrigger(
            playerAnimator,
            AimTrigger
        );

        float timer = 0f;

        while (timer < aimDuration)
        {
            if (!CanContinueShooting())
            {
                roundInProgress = false;
                yield break;
            }

            RotateSoldierTowardsTarget();

            // WeaponHolder może odtworzyć/zmienić broń,
            // dlatego kontrolujemy muzzle również podczas aim.
            if (muzzlePoint == null)
            {
                RefreshMuzzlePoint();
            }

            KeepPlayerAtCombatHeight();

            timer += Time.deltaTime;

            yield return null;
        }

        RotateSoldierTowardsTarget();

        if (!CanContinueShooting())
        {
            roundInProgress = false;
            yield break;
        }

        SafeResetTrigger(
            playerAnimator,
            AimTrigger
        );

        SafeSetTrigger(
            playerAnimator,
            ShootingTrigger
        );

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

            if (muzzlePoint == null)
            {
                RefreshMuzzlePoint();
            }

            KeepPlayerAtCombatHeight();

            timer += Time.deltaTime;

            yield return null;
        }

        if (!CanContinueShooting())
        {
            roundInProgress = false;
            yield break;
        }

        RotateSoldierTowardsTarget();

        // =====================================================
        // SNAPSHOT SHOT
        // =====================================================

        // Najważniejsze:
        // punkt startowy jest pobierany DOPIERO tutaj.
        // Dzięki temu dynamicznie utworzony Weapon jest uwzględniony.
        Vector3 bulletStart =
            GetShootingOrigin();

        Vector3 bulletTarget =
            GetAimPosition(monster);

        Health targetAtShot =
            monster;

        float damage =
            Random.Range(
                Mathf.Min(
                    shootingDamageMin,
                    shootingDamageMax
                ),
                Mathf.Max(
                    shootingDamageMin,
                    shootingDamageMax
                )
            );

        Debug.DrawLine(
            bulletStart,
            bulletTarget,
            Color.yellow,
            1f
        );

        Debug.Log(
            $"[COMBAT] SHOOT FROM: " +
            $"{GetMuzzleDebugName()} | " +
            $"Position: {bulletStart}"
        );

        yield return StartCoroutine(
            TravelBulletToTarget(
                bulletStart,
                bulletTarget,
                targetAtShot,
                damage
            )
        );

        if (fightFinished)
        {
            roundInProgress = false;
            yield break;
        }

        shotsFired++;

        Debug.Log(
            $"[COMBAT] Shot #{shotsFired}."
        );

        if (!fightFinished &&
            shotsFired >=
            Mathf.Max(
                1,
                shotsBeforeReload
            ))
        {
            yield return StartCoroutine(
                Reload()
            );
        }

        if (!fightFinished)
        {
            nextRoundTime =
                Time.time +
                Mathf.Max(
                    0f,
                    shootingInterval
                );
        }

        roundInProgress = false;
    }

    private string GetMuzzleDebugName()
    {
        if (muzzlePoint == null)
            return "FALLBACK";

        return GetTransformPath(muzzlePoint);
    }

    // =========================================================
    // BULLET TRAVEL
    // =========================================================

    private IEnumerator TravelBulletToTarget(
        Vector3 startPosition,
        Vector3 targetPosition,
        Health target,
        float damage
    )
    {
        if (target == null)
            yield break;

        float distance =
            Vector3.Distance(
                startPosition,
                targetPosition
            );

        float travelTime =
            distance /
            Mathf.Max(
                0.1f,
                bulletSpeed
            );

        travelTime =
            Mathf.Max(
                travelTime,
                0.01f
            );

        GameObject tracerObject = null;
        LineRenderer tracer = null;

        if (showBulletTracer)
        {
            tracerObject =
                CreateBulletTracer(
                    out tracer
                );
        }

        float timer = 0f;

        Vector3 previousPosition =
            startPosition;

        while (timer < travelTime)
        {
            if (fightFinished ||
                target == null ||
                target.IsDead())
            {
                DestroyTracer(
                    tracerObject
                );

                yield break;
            }

            timer += Time.deltaTime;

            float progress =
                Mathf.Clamp01(
                    timer /
                    travelTime
                );

            float smoothProgress =
                Mathf.SmoothStep(
                    0f,
                    1f,
                    progress
                );

            Vector3 currentPosition =
                Vector3.Lerp(
                    startPosition,
                    targetPosition,
                    smoothProgress
                );

            if (tracer != null)
            {
                UpdateTracer(
                    tracer,
                    previousPosition,
                    currentPosition
                );
            }

            previousPosition =
                currentPosition;

            yield return null;
        }

        if (tracer != null)
        {
            tracer.SetPosition(
                0,
                targetPosition
            );

            tracer.SetPosition(
                1,
                targetPosition
            );
        }

        if (!fightFinished &&
            target != null &&
            !target.IsDead())
        {
            target.TakeDamage(damage);

            PlayHitEffects(
                target,
                player != null
                    ? player.transform
                    : transform
            );

            Animator defenderAnimator =
                target == player
                    ? playerAnimator
                    : monsterAnimator;

            PlayRandomGetDamage(
                defenderAnimator,
                target.name
            );

            if (target.IsDead())
            {
                FinishFight(target);
            }
        }

        if (tracerObject != null)
        {
            if (bulletTrailFadeDuration > 0f &&
                tracer != null)
            {
                yield return StartCoroutine(
                    FadeBulletTracer(
                        tracer,
                        bulletTrailFadeDuration
                    )
                );
            }

            DestroyTracer(
                tracerObject
            );
        }
    }

    // =========================================================
    // UPDATE TRACER
    // =========================================================

    private void UpdateTracer(
        LineRenderer tracer,
        Vector3 previousPosition,
        Vector3 currentPosition
    )
    {
        if (tracer == null)
            return;

        Vector3 direction =
            currentPosition -
            previousPosition;

        float directionLength =
            direction.magnitude;

        if (directionLength <= 0.0001f)
        {
            tracer.SetPosition(
                0,
                currentPosition
            );

            tracer.SetPosition(
                1,
                currentPosition
            );

            return;
        }

        direction /=
            directionLength;

        float actualTrailLength =
            Mathf.Min(
                bulletTrailLength,
                directionLength * 3f
            );

        Vector3 trailStart =
            currentPosition -
            direction *
            actualTrailLength;

        tracer.SetPosition(
            0,
            trailStart
        );

        tracer.SetPosition(
            1,
            currentPosition
        );
    }

    // =========================================================
    // CREATE BULLET TRACER
    // =========================================================

    private GameObject CreateBulletTracer(
        out LineRenderer lineRenderer
    )
    {
        GameObject tracerObject =
            new GameObject(
                "BulletTracer"
            );

        lineRenderer =
            tracerObject.AddComponent<LineRenderer>();

        lineRenderer.positionCount = 2;
        lineRenderer.useWorldSpace = true;

        lineRenderer.startWidth =
            bulletTrailWidth;

        lineRenderer.endWidth =
            bulletTrailWidth * 0.15f;

        lineRenderer.numCapVertices = 2;
        lineRenderer.numCornerVertices = 2;

        lineRenderer.shadowCastingMode =
            UnityEngine.Rendering.ShadowCastingMode.Off;

        lineRenderer.receiveShadows = false;

        Shader shader =
            Shader.Find("Sprites/Default");

        if (shader != null)
        {
            Material material =
                new Material(shader);

            material.name =
                "CombatManager_BulletTracer_Material";

            lineRenderer.material =
                material;
        }

        Gradient gradient =
            new Gradient();

        GradientColorKey[] colorKeys =
        {
            new GradientColorKey(
                Color.white,
                0f
            ),

            new GradientColorKey(
                Color.white,
                1f
            )
        };

        GradientAlphaKey[] alphaKeys =
        {
            new GradientAlphaKey(
                0f,
                0f
            ),

            new GradientAlphaKey(
                0.18f,
                0.35f
            ),

            new GradientAlphaKey(
                0.75f,
                1f
            )
        };

        gradient.SetKeys(
            colorKeys,
            alphaKeys
        );

        lineRenderer.colorGradient =
            gradient;

        return tracerObject;
    }

    // =========================================================
    // DESTROY TRACER
    // =========================================================

    private void DestroyTracer(
        GameObject tracerObject
    )
    {
        if (tracerObject == null)
            return;

        LineRenderer lineRenderer =
            tracerObject.GetComponent<LineRenderer>();

        if (lineRenderer != null &&
            lineRenderer.material != null)
        {
            Destroy(
                lineRenderer.material
            );
        }

        Destroy(
            tracerObject
        );
    }

    // =========================================================
    // FADE BULLET TRACER
    // =========================================================

    private IEnumerator FadeBulletTracer(
        LineRenderer lineRenderer,
        float duration
    )
    {
        if (lineRenderer == null)
            yield break;

        if (duration <= 0f)
            yield break;

        Gradient originalGradient =
            lineRenderer.colorGradient;

        GradientColorKey[] originalColorKeys =
            originalGradient.colorKeys;

        GradientAlphaKey[] originalAlphaKeys =
            originalGradient.alphaKeys;

        float timer = 0f;

        while (timer < duration)
        {
            if (lineRenderer == null)
                yield break;

            timer += Time.deltaTime;

            float progress =
                Mathf.Clamp01(
                    timer / duration
                );

            float alphaMultiplier =
                1f - progress;

            GradientAlphaKey[] fadedKeys =
                new GradientAlphaKey[
                    originalAlphaKeys.Length
                ];

            for (
                int i = 0;
                i < originalAlphaKeys.Length;
                i++
            )
            {
                fadedKeys[i] =
                    new GradientAlphaKey(
                        originalAlphaKeys[i].alpha *
                        alphaMultiplier,
                        originalAlphaKeys[i].time
                    );
            }

            Gradient gradient =
                new Gradient();

            gradient.SetKeys(
                originalColorKeys,
                fadedKeys
            );

            lineRenderer.colorGradient =
                gradient;

            yield return null;
        }
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

        float distance =
            GetHorizontalDistance(
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

        float distance =
            GetHorizontalDistance(
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
    // ROTATE SOLDIER
    // =========================================================

    private void RotateSoldierTowardsTarget()
    {
        if (player == null ||
            monster == null)
        {
            return;
        }

        Vector3 target =
            GetAimPosition(monster);

        Vector3 direction =
            target -
            player.transform.position;

        direction.y = 0f;

        if (direction.sqrMagnitude <
            0.001f)
        {
            return;
        }

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

        Debug.Log(
            "[COMBAT] Soldier Reloading."
        );

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

        Debug.Log(
            "[COMBAT] Reload finished."
        );
    }

    // =========================================================
    // ROTATION
    // =========================================================

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

        if (direction.sqrMagnitude <
            0.001f)
        {
            return;
        }

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
    // STOP PLAYER MOVEMENT ONLY
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
                Random.Range(0, 2);

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
        roundInProgress = false;
        isReloading = false;

        StopAllCoroutines();

        bool playerWon =
            loser == monster;

        Debug.Log(
            playerWon
                ? "[COMBAT] PLAYER WINS!"
                : "[COMBAT] MONSTER WINS!"
        );

        DisablePlayerSelection();

        if (loser == player)
        {
            StopPlayerMovement();
        }

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

        HideHealthBar(loser);

        PlayDeath(loser);

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
            anchor.gameObject.SetActive(false);
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
                healthBar.gameObject.SetActive(false);
            }
        }
    }

    // =========================================================
    // DEATH
    // =========================================================

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

        Animator animator =
            loser == player
                ? playerAnimator
                : monsterAnimator;

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

        SetWalking(
            animator,
            false
        );

        if (HasParameter(
                animator,
                DeathIndexInt,
                AnimatorControllerParameterType.Int
            ))
        {
            int randomDeathIndex =
                Random.Range(0, 2);

            animator.SetInteger(
                DeathIndexInt,
                randomDeathIndex
            );

            Debug.Log(
                $"[COMBAT] {loser.name} " +
                $"DeathIndex: {randomDeathIndex}"
            );
        }

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
                $"has no Death trigger.",
                loser
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

        ResetCombatAnimator(
            playerAnimator
        );

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

        ResetCombatAnimator(
            monsterAnimator
        );

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

    private void ResetCombatAnimator(
        Animator animator
    )
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

        if (!HasParameter(
                animator,
                WalkingBool,
                AnimatorControllerParameterType.Bool
            ))
        {
            return;
        }

        animator.SetBool(
            WalkingBool,
            value
        );
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

        // Weapon może już istnieć po rejestracji.
        muzzlePoint = null;
        RefreshMuzzlePoint();

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
    // RESET FIGHT
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

        // Weapon może zostać utworzony ponownie podczas resetu.
        muzzlePoint = null;
        RefreshMuzzlePoint();

        ResetCombatAnimator(
            playerAnimator
        );

        ResetCombatAnimator(
            monsterAnimator
        );

        nextRoundTime =
            Time.time + 0.5f;
    }

    // =========================================================
    // CLEANUP
    // =========================================================

    private void OnDestroy()
    {
        StopAllCoroutines();

        if (Instance == this)
        {
            Instance = null;
        }
    }
}