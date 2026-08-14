using UnityEngine;

// Runtime stopgap, applied automatically when the scene loads.
//
// The saved scene still has both characters on dynamic Rigidbodies with
// gravity, a zero-thickness BoxCollider on the Ground and a duplicate capsule
// on the Monster. Those settings fight the script driven movement: the player
// builds up downward velocity and drifts off target, and contact depenetration
// shoves the two characters apart so the Monster cannot close in.
//
// Doing it here means the game is correct every time you press Play, with no
// menu item to run and nothing to attach. Once the scene itself is saved with
// these values, this file can simply be deleted.
public static class SceneRuntimeFixes
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Apply()
    {
        NormalizeCharacter("Player");
        NormalizeCharacter("Monster");

        RemoveDegenerateGroundCollider();
        EnsureSelectionRing();
    }

    private static void NormalizeCharacter(string objectName)
    {
        GameObject character = FindIncludingInactive(objectName);

        if (character == null)
            return;

        Rigidbody body = character.GetComponent<Rigidbody>();

        if (body != null && (!body.isKinematic || body.useGravity))
        {
            // Both characters are moved entirely from script at a locked
            // height, so kinematic is what the code already assumes.
            body.isKinematic = true;
            body.useGravity = false;
            body.constraints = RigidbodyConstraints.FreezeRotation;

            Log($"{objectName}: Rigidbody set kinematic, gravity off.");
        }

        // Root motion drives the model away from the root the scripts control.
        Animator animator = character.GetComponentInChildren<Animator>();

        if (animator != null && animator.applyRootMotion)
        {
            animator.applyRootMotion = false;

            Log($"{objectName}: root motion disabled.");
        }

        RemoveDuplicateCapsules(character, objectName);
    }

    private static void RemoveDuplicateCapsules(
        GameObject character,
        string objectName
    )
    {
        CapsuleCollider[] capsules =
            character.GetComponents<CapsuleCollider>();

        if (capsules.Length <= 1)
            return;

        // Keep the first and give it sane dimensions. One of the originals
        // reached below the ground plane.
        for (int i = capsules.Length - 1; i >= 1; i--)
            Object.Destroy(capsules[i]);

        CapsuleCollider body = capsules[0];
        body.direction = 1; // Y axis
        body.radius = 0.4f;
        body.height = 1.8f;
        body.center = new Vector3(0f, 0.4f, 0f);

        Log(
            $"{objectName}: removed {capsules.Length - 1} duplicate " +
            "capsule collider(s)."
        );
    }

    private static void RemoveDegenerateGroundCollider()
    {
        GameObject ground = FindIncludingInactive("Ground");

        if (ground == null)
            return;

        // The Ground carries a MeshCollider as well, so this BoxCollider is
        // redundant, and its Y size of 2.2e-16 gives PhysX degenerate contacts.
        BoxCollider box = ground.GetComponent<BoxCollider>();

        if (box == null)
            return;

        if (ground.GetComponent<MeshCollider>() == null)
            return;

        Object.Destroy(box);

        Log("Ground: removed the degenerate zero-thickness BoxCollider.");
    }

    private static void EnsureSelectionRing()
    {
        GameObject ring = FindIncludingInactive("SelectionRing");

        if (ring == null)
            return;

        if (ring.GetComponent<SelectionRing>() == null)
        {
            ring.AddComponent<SelectionRing>();

            Log("SelectionRing: component attached automatically.");
        }
    }

    private static GameObject FindIncludingInactive(string objectName)
    {
        // SelectionRing starts inactive, so GameObject.Find would miss it.
        GameObject[] all =
            Object.FindObjectsByType<GameObject>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );

        foreach (GameObject candidate in all)
        {
            if (candidate.name == objectName)
                return candidate;
        }

        return null;
    }

    private static void Log(string message)
    {
        Debug.Log($"[RUNTIME FIX] {message}");
    }
}
