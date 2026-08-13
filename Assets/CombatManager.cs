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

        if (player != null && monster != null)
        {
            Debug.Log(
                "[COMBAT] Player i Monster znalezieni."
            );

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
        float playerDamage =
            Random.Range(
                damageMin,
                damageMax
            );

        float monsterDamage =
            Random.Range(
                damageMin,
                damageMax
            );

        float playerOldHP =
            player.CurrentHealth;

        float monsterOldHP =
            monster.CurrentHealth;

        float playerNewHP =
            Mathf.Max(
                0f,
                playerOldHP - monsterDamage
            );

        float monsterNewHP =
            Mathf.Max(
                0f,
                monsterOldHP - playerDamage
            );

        // Nie pozwalamy na remis.
        if (playerNewHP <= 0f &&
            monsterNewHP <= 0f)
        {
            if (Random.value < 0.5f)
            {
                playerNewHP = 1f;
                monsterNewHP = 0f;
            }
            else
            {
                playerNewHP = 0f;
                monsterNewHP = 1f;
            }
        }

        Debug.Log(
            $"[ROUND] Player attacks Monster " +
            $"for {playerDamage:F1}"
        );

        Debug.Log(
            $"[ROUND] Monster attacks Player " +
            $"for {monsterDamage:F1}"
        );

        Debug.Log(
            $"[ROUND] Player HP: " +
            $"{playerOldHP:F1} -> {playerNewHP:F1}"
        );

        Debug.Log(
            $"[ROUND] Monster HP: " +
            $"{monsterOldHP:F1} -> {monsterNewHP:F1}"
        );

        // WAŻNE:
        // Health musi posiadać metodę SetHealth().
        player.SetHealth(playerNewHP);
        monster.SetHealth(monsterNewHP);

        if (monsterNewHP <= 0f)
        {
            Debug.Log(
                "[COMBAT] PLAYER WINS!"
            );

            fightFinished = true;
        }
        else if (playerNewHP <= 0f)
        {
            Debug.Log(
                "[COMBAT] MONSTER WINS!"
            );

            fightFinished = true;
        }
    }

    public void RegisterCombatants(
        Health playerHealth,
        Health monsterHealth
    )
    {
        player = playerHealth;
        monster = monsterHealth;

        fightFinished = false;

        nextRoundTime =
            Time.time + 0.5f;

        Debug.Log(
            "[COMBAT] Combatants registered."
        );
    }
}