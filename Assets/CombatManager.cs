using UnityEngine;

public class CombatManager : MonoBehaviour
{
    public static CombatManager Instance;

    [Header("Combat")]
    public float roundCooldown = 1f;

    [Header("Damage")]
    public float damageMin = 8f;
    public float damageMax = 15f;

    [Header("Range")]
    public float attackRange = 1.5f;

    private float nextRoundTime;

    private Health player;
    private Health monster;

    private void Awake()
    {
        Instance = this;
    }

    public void RegisterCombatants(
        Health playerHealth,
        Health monsterHealth
    )
    {
        player = playerHealth;
        monster = monsterHealth;

        nextRoundTime = Time.time + 0.1f;

        Debug.Log(
            "[COMBAT] Player and Monster registered."
        );
    }

    private void Update()
    {
        if (player == null || monster == null)
            return;

        if (!player.gameObject.activeSelf)
            return;

        if (!monster.gameObject.activeSelf)
            return;

        float distance = Vector3.Distance(
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
        // Random damage for both fighters.
        float playerDamage = Random.Range(
            damageMin,
            damageMax
        );

        float monsterDamage = Random.Range(
            damageMin,
            damageMax
        );

        // Save current HP before the round.
        float playerOldHP =
            player.CurrentHealth;

        float monsterOldHP =
            monster.CurrentHealth;

        // Calculate both results BEFORE
        // applying any damage.
        float playerNewHP =
            playerOldHP - monsterDamage;

        float monsterNewHP =
            monsterOldHP - playerDamage;

        // Prevent both fighters from dying
        // in the same round.
        if (playerNewHP <= 0f &&
            monsterNewHP <= 0f)
        {
            // Randomly choose who survives.
            if (Random.value < 0.5f)
            {
                playerNewHP = 1f;
                monsterNewHP = 0f;

                Debug.Log(
                    "[COMBAT] Player survives the final exchange!"
                );
            }
            else
            {
                playerNewHP = 0f;
                monsterNewHP = 1f;

                Debug.Log(
                    "[COMBAT] Monster survives the final exchange!"
                );
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
            $"{playerOldHP:F1} -> " +
            $"{Mathf.Max(0f, playerNewHP):F1}"
        );

        Debug.Log(
            $"[ROUND] Monster HP: " +
            $"{monsterOldHP:F1} -> " +
            $"{Mathf.Max(0f, monsterNewHP):F1}"
        );

        // Apply both results after calculations.
        player.SetHealth(playerNewHP);
        monster.SetHealth(monsterNewHP);

        // Announce winner.
        if (playerNewHP > 0f &&
            monsterNewHP <= 0f)
        {
            Debug.Log(
                "[COMBAT] 🏆 PLAYER WINS!"
            );
        }
        else if (monsterNewHP > 0f &&
                 playerNewHP <= 0f)
        {
            Debug.Log(
                "[COMBAT] 🏆 MONSTER WINS!"
            );
        }
    }
}