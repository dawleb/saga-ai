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
                $"[COMBAT] {label} has no Animator."
            );

            return null;
        }

        return animator;
    }

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

        // --------------------------------
        // OBRÓT W STRONĘ PRZECIWNIKA
        // --------------------------------

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

        // --------------------------------
        // WALKA
        // --------------------------------

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

        // Nie pozwalamy obracać się góra/dół.
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

    private IEnumerator ResolveRound()
    {
        roundInProgress = true;

        // --------------------------------
        // PLAYER ATTACK
        // --------------------------------

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

        // --------------------------------
        // MONSTER ATTACK
        // --------------------------------

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

        if (!attacker.gameObject.activeSelf ||
            !defender.gameObject.activeSelf)
        {
            yield break;
        }

        // --------------------------------
        // USTAWIENIE W STRONĘ PRZECIWNIKA
        // --------------------------------

        RotateTowardsOpponent(
            attacker.transform,
            defender.transform
        );

        // --------------------------------
        // WYBÓR ANIMACJI
        // --------------------------------

        string selectedTrigger =
            AttackTrigger;

        // Zombie losowo wybiera Attack albo Bite.
        if (monsterAttacking)
        {
            bool useBite =
                Random.value < 0.5f;

            if (
                useBite &&
                HasTrigger(
                    attackerAnimator,
                    BiteTrigger
                )
            )
            {
                selectedTrigger =
                    BiteTrigger;
            }
        }

        // --------------------------------
        // START ANIMACJI
        // --------------------------------

        if (attackerAnimator != null)
        {
            attackerAnimator.ResetTrigger(
                AttackTrigger
            );

            attackerAnimator.ResetTrigger(
                BiteTrigger
            );

            attackerAnimator.SetTrigger(
                selectedTrigger
            );
        }

        Debug.Log(
            $"[COMBAT] {attacker.name} uses " +
            $"{selectedTrigger}."
        );

        // --------------------------------
        // OPÓŹNIENIE OBRAŻEŃ
        // --------------------------------

        yield return new WaitForSeconds(
            damageDelay
        );

        if (fightFinished)
        {
            yield break;
        }

        if (!attacker.gameObject.activeSelf ||
            !defender.gameObject.activeSelf)
        {
            yield break;
        }

        // --------------------------------
        // DAMAGE
        // --------------------------------

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
            yield break;
        }

        // --------------------------------
        // DOKOŃCZENIE ANIMACJI
        // --------------------------------

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

        // --------------------------------
        // PLAYER WYGRYWA
        // --------------------------------

        if (playerWon)
        {
            StartCoroutine(
                PlayerVictory()
            );
        }
        else
        {
            // --------------------------------
            // MONSTER WYGRYWA
            // --------------------------------

            StartCoroutine(
                MonsterVictory()
            );
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
            Debug.LogWarning(
                "[COMBAT] Player has no Animator."
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
        else
        {
            Debug.LogWarning(
                "[COMBAT] Player Animator has no " +
                "'Taunt' trigger."
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
            playerClickController.enabled = true;
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
            Debug.LogWarning(
                "[COMBAT] Monster has no Animator."
            );

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
        else
        {
            Debug.LogWarning(
                "[COMBAT] Monster Animator has no " +
                "'Taunt' trigger."
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