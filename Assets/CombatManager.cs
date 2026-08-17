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

    [Header("Monster Attacks")]
    [Range(0f, 1f)]
    public float monsterBiteChance = 0.5f;

    [Header("Victory")]
    public float tauntDuration = 3f;

    // Nazwy parametrów w Animatorze
    private const string AttackTrigger = "Attack";
    private const string BiteTrigger = "Bite";
    private const string TauntTrigger = "Taunt";

    private const string GetDamageTrigger = "GetDamage";
    private const string GetDamageIndexInt = "GetDamageIndex"; // Parameter Int (0 lub 1)

    private const string DeathTrigger = "Death";
    private const string DeathIndexInt = "DeathIndex";         // Parameter Int (0 lub 1)

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

        if (player == null) Debug.LogError("[COMBAT] Player Health not found!");
        if (monster == null) Debug.LogError("[COMBAT] Monster Health not found!");

        playerAnimator = FindAttackAnimator(player, "Player");
        monsterAnimator = FindAttackAnimator(monster, "Monster");

        FindPlayerClickController();

        if (player != null && monster != null)
        {
            Debug.Log($"[COMBAT] Player HP: {player.CurrentHealth}");
            Debug.Log($"[COMBAT] Monster HP: {monster.CurrentHealth}");

            nextRoundTime = Time.time + 0.5f;
        }
    }

    private void FindCombatants()
    {
        Health[] healthObjects = FindObjectsOfType<Health>();

        foreach (Health health in healthObjects)
        {
            if (health == null) continue;

            SimpleAgent agent = health.GetComponentInParent<SimpleAgent>();
            if (agent == null) continue;

            monster = health;

            if (agent.target != null)
            {
                Health targetHealth = agent.target.GetComponentInChildren<Health>();
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
        if (player == null) return;

        playerClickController = player.GetComponent<PlayerClickController>();
        if (playerClickController == null) playerClickController = player.GetComponentInParent<PlayerClickController>();
        if (playerClickController == null) playerClickController = player.GetComponentInChildren<PlayerClickController>();
    }

    private Animator FindAttackAnimator(Health combatant, string label)
    {
        if (combatant == null) return null;

        Animator animator = combatant.GetComponentInChildren<Animator>();
        if (animator == null) animator = combatant.GetComponentInParent<Animator>();

        if (animator == null)
        {
            Debug.LogWarning($"[COMBAT] {label} has no Animator.");
            return null;
        }

        return animator;
    }

    private static bool HasParameter(Animator animator, string paramName, AnimatorControllerParameterType type)
    {
        if (animator == null) return false;

        foreach (AnimatorControllerParameter parameter in animator.parameters)
        {
            if (parameter.type == type && parameter.name == paramName)
            {
                return true;
            }
        }

        return false;
    }

    private void Update()
    {
        if (fightFinished || player == null || monster == null) return;
        if (!player.gameObject.activeInHierarchy || !monster.gameObject.activeInHierarchy) return;
        if (player.IsDead() || monster.IsDead()) return;

        float distance = Vector3.Distance(player.transform.position, monster.transform.position);

        if (distance <= attackRange)
        {
            RotateTowardsOpponent(player.transform, monster.transform);
            RotateTowardsOpponent(monster.transform, player.transform);
        }

        if (roundInProgress || distance > attackRange || Time.time < nextRoundTime) return;

        StartCoroutine(ResolveRound());
    }

    private void RotateTowardsOpponent(Transform fighter, Transform opponent)
    {
        if (fighter == null || opponent == null) return;

        Vector3 direction = opponent.position - fighter.position;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.001f) return;

        Quaternion targetRotation = Quaternion.LookRotation(direction);
        fighter.rotation = Quaternion.Slerp(fighter.rotation, targetRotation, combatRotationSpeed * Time.deltaTime);
    }

    private IEnumerator ResolveRound()
    {
        roundInProgress = true;

        yield return StartCoroutine(PerformAttack(playerAnimator, player, monster, false));

        if (fightFinished)
        {
            roundInProgress = false;
            yield break;
        }

        yield return StartCoroutine(PerformAttack(monsterAnimator, monster, player, true));

        if (!fightFinished)
        {
            nextRoundTime = Time.time + roundCooldown;
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
        if (attacker == null || defender == null) yield break;
        if (!attacker.gameObject.activeInHierarchy || !defender.gameObject.activeInHierarchy) yield break;
        if (attacker.IsDead() || defender.IsDead()) yield break;

        RotateTowardsOpponent(attacker.transform, defender.transform);

        // Wybór ataku
        string selectedTrigger = AttackTrigger;

        if (monsterAttacking)
        {
            bool hasBite = HasParameter(attackerAnimator, BiteTrigger, AnimatorControllerParameterType.Trigger);
            bool hasAttack = HasParameter(attackerAnimator, AttackTrigger, AnimatorControllerParameterType.Trigger);

            if (hasBite && hasAttack)
            {
                selectedTrigger = (Random.value < monsterBiteChance) ? BiteTrigger : AttackTrigger;
            }
            else if (hasBite) selectedTrigger = BiteTrigger;
            else if (hasAttack) selectedTrigger = AttackTrigger;
            else yield break;
        }

        if (attackerAnimator != null)
        {
            attackerAnimator.ResetTrigger(AttackTrigger);
            attackerAnimator.ResetTrigger(BiteTrigger);

            if (HasParameter(attackerAnimator, selectedTrigger, AnimatorControllerParameterType.Trigger))
            {
                attackerAnimator.SetTrigger(selectedTrigger);
            }
        }

        yield return new WaitForSeconds(damageDelay);

        if (fightFinished) yield break;
        if (!attacker.gameObject.activeInHierarchy || !defender.gameObject.activeInHierarchy) yield break;
        if (attacker.IsDead() || defender.IsDead()) yield break;

        // Zadanie obrażeń
        float damage = Random.Range(damageMin, damageMax);
        defender.TakeDamage(damage);

        // --- LOSOWANIE WARIANTU GETDAMAGE (50% / 50%) ---
        Animator defenderAnimator = (defender == player) ? playerAnimator : monsterAnimator;
        PlayRandomGetDamage(defenderAnimator, defender.name);

        // Śmierć
        if (defender.IsDead() || defender.CurrentHealth <= 0f)
        {
            FinishFight(defender);
            yield break;
        }

        float remainingTime = Mathf.Max(0f, attackAnimationDuration - damageDelay);
        yield return new WaitForSeconds(remainingTime);
    }

    private void PlayRandomGetDamage(Animator animator, string defenderName)
    {
        if (animator == null) return;

        if (HasParameter(animator, GetDamageTrigger, AnimatorControllerParameterType.Trigger))
        {
            // Jeśli istnieje parametr Int "GetDamageIndex", losujemy 0 lub 1
            if (HasParameter(animator, GetDamageIndexInt, AnimatorControllerParameterType.Int))
            {
                int randomIndex = Random.Range(0, 2); // Zwraca 0 lub 1
                animator.SetInteger(GetDamageIndexInt, randomIndex);
                Debug.Log($"[COMBAT] {defenderName} GetDamage index: {randomIndex}");
            }

            animator.SetTrigger(GetDamageTrigger);
        }
    }

    private void FinishFight(Health loser)
    {
        if (fightFinished || loser == null) return;

        fightFinished = true;
        bool playerWon = (loser == monster);

        if (loser == monster)
        {
            SimpleAgent agent = monster.GetComponentInParent<SimpleAgent>();
            if (agent != null)
            {
                agent.SetDead();
                agent.enabled = false;
            }
        }

        HideHealthBar(loser);
        PlayDeath(loser);

        if (playerWon) StartCoroutine(PlayerVictory());
        else StartCoroutine(MonsterVictory());
    }

    private void HideHealthBar(Health loser)
    {
        if (loser == null) return;

        Transform anchor = loser.transform.Find("HealthBarAnchor");
        if (anchor == null)
        {
            Transform[] children = loser.GetComponentsInChildren<Transform>(true);
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

        HealthBar[] healthBars = loser.GetComponentsInChildren<HealthBar>(true);
        foreach (HealthBar healthBar in healthBars)
        {
            if (healthBar != null) healthBar.gameObject.SetActive(false);
        }
    }

    private void PlayDeath(Health loser)
    {
        if (loser == null) return;

        Animator loserAnimator = (loser == player) ? playerAnimator : monsterAnimator;
        if (loserAnimator == null) return;

        loserAnimator.applyRootMotion = false;

        loserAnimator.ResetTrigger(AttackTrigger);
        loserAnimator.ResetTrigger(BiteTrigger);
        loserAnimator.ResetTrigger(TauntTrigger);
        loserAnimator.ResetTrigger(GetDamageTrigger);

        loserAnimator.SetBool(WalkingBool, false);
        loserAnimator.SetBool(DancingBool, false);

        // --- LOSOWANIE WARIANTU DEATH (50% / 50%) ---
        if (HasParameter(loserAnimator, DeathTrigger, AnimatorControllerParameterType.Trigger))
        {
            // Jeśli istnieje parametr Int "DeathIndex", losujemy 0 lub 1
            if (HasParameter(loserAnimator, DeathIndexInt, AnimatorControllerParameterType.Int))
            {
                int randomIndex = Random.Range(0, 2); // Zwraca 0 lub 1
                loserAnimator.SetInteger(DeathIndexInt, randomIndex);
                Debug.Log($"[COMBAT] {loser.name} Death index: {randomIndex}");
            }

            loserAnimator.SetTrigger(DeathTrigger);
        }

        if (loser == player)
        {
            FindPlayerClickController();
            if (playerClickController != null) playerClickController.enabled = false;
        }
    }

    private IEnumerator PlayerVictory()
    {
        if (player == null || playerAnimator == null) yield break;

        if (playerClickController != null) playerClickController.enabled = false;

        playerAnimator.ResetTrigger(AttackTrigger);
        playerAnimator.ResetTrigger(BiteTrigger);
        playerAnimator.ResetTrigger(TauntTrigger);
        playerAnimator.SetBool(WalkingBool, false);
        playerAnimator.SetBool(DancingBool, false);

        if (HasParameter(playerAnimator, TauntTrigger, AnimatorControllerParameterType.Trigger))
        {
            playerAnimator.SetTrigger(TauntTrigger);
        }

        yield return new WaitForSeconds(tauntDuration);

        playerAnimator.ResetTrigger(TauntTrigger);
        playerAnimator.SetBool(DancingBool, false);
        playerAnimator.SetBool(WalkingBool, false);

        if (playerClickController != null) playerClickController.enabled = true;
    }

    private IEnumerator MonsterVictory()
    {
        if (monster == null || monsterAnimator == null) yield break;

        monsterAnimator.ResetTrigger(AttackTrigger);
        monsterAnimator.ResetTrigger(BiteTrigger);
        monsterAnimator.ResetTrigger(TauntTrigger);
        monsterAnimator.SetBool(WalkingBool, false);
        monsterAnimator.SetBool(DancingBool, false);

        if (HasParameter(monsterAnimator, TauntTrigger, AnimatorControllerParameterType.Trigger))
        {
            monsterAnimator.SetTrigger(TauntTrigger);
        }

        yield return new WaitForSeconds(tauntDuration);

        monsterAnimator.ResetTrigger(TauntTrigger);
        monsterAnimator.SetBool(DancingBool, false);
        monsterAnimator.SetBool(WalkingBool, false);
    }

    public void RegisterCombatants(Health playerHealth, Health monsterHealth)
    {
        player = playerHealth;
        monster = monsterHealth;

        fightFinished = false;
        roundInProgress = false;

        playerAnimator = FindAttackAnimator(player, "Player");
        monsterAnimator = FindAttackAnimator(monster, "Monster");

        FindPlayerClickController();

        nextRoundTime = Time.time + 0.5f;
    }
}