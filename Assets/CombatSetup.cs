using UnityEngine;

public class CombatSetup : MonoBehaviour
{
    public Health player;
    public Health monster;

    private void Start()
    {
        if (CombatManager.Instance == null)
        {
            Debug.LogError(
                "[COMBAT] CombatManager not found!"
            );

            return;
        }

        CombatManager.Instance.RegisterCombatants(
            player,
            monster
        );

        Debug.Log(
            "[COMBAT] Player and Monster registered."
        );
    }
}