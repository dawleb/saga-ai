using UnityEngine;

public class TrainingCombatManager : MonoBehaviour
{
    [Header("References")]
    public SimpleAgent monster;
    public Transform player;

    [Header("Training")]
    public int killsPerEpisode = 3;

    [Header("Rewards")]
    public float killReward = 8f;
    public float episodeWinReward = 12f;
    public float monsterDeathPenalty = 8f;

    [Header("Spawn Positions")]
    public Vector3 monsterSpawn =
        new Vector3(5f, 0.5f, 5f);

    public Vector3 playerSpawn =
        new Vector3(-5f, 0.5f, -5f);

    private Health monsterHealth;
    private Health playerHealth;

    private int monsterKills;
    private int playerKills;

    private bool handlingDeath;

    public void Initialize()
    {
        if (monster == null)
        {
            Debug.LogError(
                "[TRAINING] Monster is NOT assigned!"
            );

            enabled = false;
            return;
        }

        if (player == null)
        {
            Debug.LogError(
                "[TRAINING] Player is NOT assigned!"
            );

            enabled = false;
            return;
        }

        monsterHealth =
            monster.GetComponent<Health>();

        if (monsterHealth == null)
        {
            Debug.LogError(
                "[TRAINING] Monster has NO Health component!"
            );

            enabled = false;
            return;
        }

        playerHealth =
            player.GetComponent<Health>();

        if (playerHealth == null)
        {
            Debug.LogError(
                "[TRAINING] Player has NO Health component!"
            );

            enabled = false;
            return;
        }

        Debug.Log(
            "[TRAINING] Combat Manager initialized correctly."
        );
    }

    private void Awake()
    {
        Initialize();
    }

    private void Start()
    {
        ResetFight();
    }

    private void Update()
    {
        if (handlingDeath)
            return;

        if (monster == null ||
            player == null ||
            monsterHealth == null ||
            playerHealth == null)
        {
            return;
        }

        // =====================================================
        // NIE RESETUJEMY WALKI PRZEZ DYSTANS
        // =====================================================
        //
        // Agent może:
        //
        // - podejść,
        // - walczyć,
        // - uciec,
        // - oddalić się,
        // - wrócić do walki.
        //
        // Dystans NIE resetuje walki.
        //

        // =====================================================
        // NIE RESETUJEMY WALKI PRZEZ CZAS
        // =====================================================
        //
        // Brak sztucznego timeoutu.
        // Walka kończy się dopiero przez śmierć.
        //

        // =====================================================
        // PLAYER DEAD
        // =====================================================

        if (playerHealth.CurrentHealth <= 0f)
        {
            HandlePlayerDeath();
            return;
        }

        // =====================================================
        // MONSTER DEAD
        // =====================================================

        if (monsterHealth.CurrentHealth <= 0f)
        {
            HandleMonsterDeath();
            return;
        }
    }

    // =========================================================
    // PLAYER DEATH
    // =========================================================

    private void HandlePlayerDeath()
    {
        handlingDeath = true;

        monsterKills++;

        // =====================================================
        // KILL REWARD
        // =====================================================

        monster.AddReward(
            killReward
        );

        Debug.Log(
            $"[TRAINING] MONSTER KILL #{monsterKills} " +
            $"| Reward +{killReward}"
        );

        // =====================================================
        // EPISODE WON
        // =====================================================

        if (monsterKills >= killsPerEpisode)
        {
            monster.AddReward(
                episodeWinReward
            );

            Debug.Log(
                $"[TRAINING] MONSTER WON EPISODE " +
                $"| Reward +{episodeWinReward}"
            );

            handlingDeath = false;

            monster.EndEpisode();

            return;
        }

        // =====================================================
        // RESPAWN PLAYER
        // =====================================================

        player.position =
            playerSpawn;

        playerHealth.ResetHealth();

        // =====================================================
        // RESET AI STATE
        // =====================================================

        monster.ResetForNewFight();

        handlingDeath = false;

        Debug.Log(
            "[TRAINING] PLAYER RESPAWNED"
        );

        Debug.Log(
            "[TRAINING] NEW FIGHT STARTED"
        );
    }

    // =========================================================
    // MONSTER DEATH
    // =========================================================

    private void HandleMonsterDeath()
    {
        handlingDeath = true;

        playerKills++;

        // =====================================================
        // DEATH PENALTY
        // =====================================================

        monster.AddReward(
            -monsterDeathPenalty
        );

        Debug.Log(
            $"[TRAINING] MONSTER DIED " +
            $"| Reward -{monsterDeathPenalty}"
        );

        // =====================================================
        // EPISODE LOST
        // =====================================================

        if (playerKills >= killsPerEpisode)
        {
            Debug.Log(
                "[TRAINING] PLAYER WON EPISODE"
            );

            handlingDeath = false;

            monster.EndEpisode();

            return;
        }

        // =====================================================
        // RESPAWN MONSTER
        // =====================================================

        monster.transform.position =
            monsterSpawn;

        monsterHealth.ResetHealth();

        // =====================================================
        // RESPAWN PLAYER
        // =====================================================

        player.position =
            playerSpawn;

        playerHealth.ResetHealth();

        // =====================================================
        // RESET AI STATE
        // =====================================================

        monster.ResetForNewFight();

        handlingDeath = false;

        Debug.Log(
            "[TRAINING] MONSTER RESPAWNED"
        );

        Debug.Log(
            "[TRAINING] NEW FIGHT STARTED"
        );
    }

    // =========================================================
    // FULL RESET
    // =========================================================

    private void ResetFight()
    {
        if (monster == null ||
            player == null ||
            monsterHealth == null ||
            playerHealth == null)
        {
            Debug.LogError(
                "[TRAINING] ResetFight failed - missing reference."
            );

            return;
        }

        monsterKills = 0;
        playerKills = 0;

        handlingDeath = false;

        // =====================================================
        // INITIAL POSITIONS
        // =====================================================

        monster.transform.position =
            monsterSpawn;

        player.position =
            playerSpawn;

        // =====================================================
        // INITIAL HP
        // =====================================================

        monsterHealth.ResetHealth();

        playerHealth.ResetHealth();

        // =====================================================
        // INITIAL AI STATE
        // =====================================================

        monster.ResetForNewFight();

        Debug.Log(
            "[TRAINING] FIGHT RESET"
        );
    }
}