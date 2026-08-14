using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;

// One-shot maintenance utility. Idempotent: safe to run more than once.
//
// Fixes the physics, collider, import and Animator problems behind the
// "player flies around" and "monster never plays its attack" bugs.
// It saves the scene and assets itself, so nothing depends on Ctrl+S.
public static class SagaAIStabilityFixes
{
    private const string PlayerName = "Player";
    private const string MonsterName = "Monster";
    private const string GroundName = "Ground";
    private const string SelectionRingName = "SelectionRing";

    private const string AttackTrigger = "Attack";
    private const string AttackStateName = "Attack";

    private const string MonsterControllerPath =
        "Assets/MonsterAnimator.controller";

    private const string RogerModelPath =
        "Assets/Characters/Warrior/fbx/Character_x1/Mercenary_Roger.fbx";

    private const string BeastModelPath =
        "Assets/Characters/PopulBeast/PopolBeast.fbx";

    private const string IdleClipPath =
        "Assets/Animations/Idle.fbx";

    private const string FistFightClipPath =
        "Assets/Animations/Fist Fight A.fbx";

    private const string FallingClipPath =
        "Assets/Animations/Falling.fbx";

    [MenuItem("Tools/SagaAI/Apply Stability Fixes")]
    private static void Apply()
    {
        // --- Scene fixes ---
        FixCharacterRigidbody(PlayerName);
        FixCharacterRigidbody(MonsterName);
        FixMonsterColliders();
        RemoveGroundBoxCollider();
        RemoveSelectionRingCollider();
        DisablePlayerRootMotion();

        // --- Import fixes (must run before the Animator is wired) ---
        EnableIdleLooping();
        MakeHumanoid(FistFightClipPath, BeastModelPath, "PopolBeast");
        MakeHumanoid(FallingClipPath, RogerModelPath, "Mercenary_Roger");

        // --- Animator wiring ---
        AddMonsterAttackState();

        SaveEverything();
    }

    // ---------------------------------------------------------------- scene

    private static void FixCharacterRigidbody(string objectName)
    {
        GameObject character = FindInScene(objectName);

        if (character == null)
        {
            Warn($"GameObject '{objectName}' not found.");
            return;
        }

        Rigidbody body = character.GetComponent<Rigidbody>();

        if (body == null)
        {
            Warn($"'{objectName}' has no Rigidbody.");
            return;
        }

        Undo.RecordObject(body, "Fix Character Rigidbody");

        // Both characters are moved entirely from script at a locked height.
        // While the body was dynamic, gravity kept adding downward velocity
        // that the scripts never cleared, so the character sank through the
        // ground and every later click inherited the corrupted height.
        body.isKinematic = true;
        body.useGravity = false;

        // Rotation Z was left free, which let contacts topple the character.
        body.constraints = RigidbodyConstraints.FreezeRotation;

        EditorUtility.SetDirty(body);

        Log($"{objectName}: Rigidbody kinematic, gravity off, rotation frozen.");
    }

    private static void FixMonsterColliders()
    {
        GameObject monster = FindInScene(MonsterName);

        if (monster == null)
        {
            Warn($"GameObject '{MonsterName}' not found.");
            return;
        }

        CapsuleCollider[] capsules =
            monster.GetComponents<CapsuleCollider>();

        if (capsules.Length == 0)
        {
            Warn("Monster has no CapsuleCollider.");
            return;
        }

        // Any extra capsule is a leftover. The survivor is reconfigured
        // below, so which one we keep does not matter.
        for (int i = capsules.Length - 1; i >= 1; i--)
        {
            Undo.DestroyObjectImmediate(capsules[i]);
            Log("Monster: removed a duplicate CapsuleCollider.");
        }

        CapsuleCollider body = capsules[0];

        Undo.RecordObject(body, "Fix Monster Collider");

        // The Monster root sits at y = 0.5 and its model child is offset by
        // -0.5, so the feet are at local y = -0.5. One of the old capsules
        // reached down to -0.5 in world space, i.e. through the ground.
        body.direction = 1; // Y axis
        body.radius = 0.4f;
        body.height = 1.8f;
        body.center = new Vector3(0f, 0.4f, 0f);

        EditorUtility.SetDirty(body);

        Log("Monster: capsule resized to stand on the ground.");
    }

    private static void RemoveGroundBoxCollider()
    {
        GameObject ground = FindInScene(GroundName);

        if (ground == null)
        {
            Warn($"GameObject '{GroundName}' not found.");
            return;
        }

        BoxCollider box = ground.GetComponent<BoxCollider>();

        if (box == null)
        {
            Log("Ground: no BoxCollider to remove.");
            return;
        }

        // The Ground carried a MeshCollider *and* a BoxCollider whose Y size
        // was 2.2e-16. A zero-thickness box gives PhysX degenerate contacts,
        // which is what let the player fall through and pop back out.
        Undo.DestroyObjectImmediate(box);

        Log("Ground: removed the degenerate zero-thickness BoxCollider.");
    }

    private static void RemoveSelectionRingCollider()
    {
        GameObject ring = FindInScene(SelectionRingName);

        if (ring == null)
        {
            Warn($"GameObject '{SelectionRingName}' not found.");
            return;
        }

        Collider[] colliders = ring.GetComponents<Collider>();

        if (colliders.Length == 0)
        {
            Log("SelectionRing: already has no Collider.");
            return;
        }

        // It was already disabled, so it was not blocking anything, but the
        // ring is purely decorative and should not own a collider at all.
        for (int i = colliders.Length - 1; i >= 0; i--)
            Undo.DestroyObjectImmediate(colliders[i]);

        Log("SelectionRing: removed its Collider entirely.");
    }

    private static void DisablePlayerRootMotion()
    {
        GameObject player = FindInScene(PlayerName);

        if (player == null)
        {
            Warn($"GameObject '{PlayerName}' not found.");
            return;
        }

        Animator animator = player.GetComponentInChildren<Animator>();

        if (animator == null)
        {
            Warn("Player has no Animator.");
            return;
        }

        if (!animator.applyRootMotion)
        {
            Log("Player: root motion already disabled.");
            return;
        }

        Undo.RecordObject(animator, "Disable Player Root Motion");

        // The Monster already had this off. With it on, the animation drives
        // the model away from the character root that the scripts control.
        animator.applyRootMotion = false;

        EditorUtility.SetDirty(animator);

        Log("Player: Apply Root Motion disabled on the Animator.");
    }

    // --------------------------------------------------------------- import

    private static void EnableIdleLooping()
    {
        ModelImporter importer =
            AssetImporter.GetAtPath(IdleClipPath) as ModelImporter;

        if (importer == null)
        {
            Warn($"No ModelImporter for {IdleClipPath}.");
            return;
        }

        ModelImporterClipAnimation[] clips = importer.clipAnimations;

        if (clips == null || clips.Length == 0)
            clips = importer.defaultClipAnimations;

        if (clips == null || clips.Length == 0)
        {
            Warn("Idle.fbx has no animation clips.");
            return;
        }

        bool changed = false;

        for (int i = 0; i < clips.Length; i++)
        {
            if (clips[i].loopTime)
                continue;

            clips[i].loopTime = true;
            changed = true;
        }

        if (!changed)
        {
            Log("Idle.fbx: already looping.");
            return;
        }

        importer.clipAnimations = clips;
        importer.SaveAndReimport();

        Log("Idle.fbx: Loop Time enabled.");
    }

    private static void MakeHumanoid(
        string clipPath,
        string avatarModelPath,
        string avatarLabel
    )
    {
        ModelImporter importer =
            AssetImporter.GetAtPath(clipPath) as ModelImporter;

        if (importer == null)
        {
            Warn($"No ModelImporter for {clipPath}.");
            return;
        }

        if (importer.animationType == ModelImporterAnimationType.Human)
        {
            Log($"{clipPath}: already Humanoid.");
            return;
        }

        Avatar sourceAvatar = LoadAvatar(avatarModelPath);

        if (sourceAvatar == null)
        {
            Warn($"No Avatar found in {avatarModelPath}, skipped {clipPath}.");
            return;
        }

        importer.animationType = ModelImporterAnimationType.Human;
        importer.avatarSetup = ModelImporterAvatarSetup.CopyFromOther;
        importer.sourceAvatar = sourceAvatar;
        importer.SaveAndReimport();

        Log($"{clipPath}: Humanoid, avatar copied from {avatarLabel}.");
    }

    // ------------------------------------------------------------- animator

    private static void AddMonsterAttackState()
    {
        AnimatorController controller =
            AssetDatabase.LoadAssetAtPath<AnimatorController>(
                MonsterControllerPath
            );

        if (controller == null)
        {
            Warn($"Could not load {MonsterControllerPath}.");
            return;
        }

        AnimationClip attackClip = LoadClip(FistFightClipPath);

        if (attackClip == null)
        {
            Warn(
                $"No AnimationClip inside {FistFightClipPath}, " +
                "the Attack state was not created."
            );

            return;
        }

        if (!HasParameter(controller, AttackTrigger))
        {
            controller.AddParameter(
                AttackTrigger,
                AnimatorControllerParameterType.Trigger
            );

            Log($"MonsterAnimator: added '{AttackTrigger}' trigger.");
        }

        AnimatorStateMachine machine =
            controller.layers[0].stateMachine;

        AnimatorState locomotionState = machine.defaultState;
        AnimatorState attackState = FindState(machine, AttackStateName);

        if (attackState == null)
        {
            attackState = machine.AddState(AttackStateName);
            Log($"MonsterAnimator: added '{AttackStateName}' state.");
        }

        attackState.motion = attackClip;

        if (!HasAnyStateTransitionTo(machine, attackState))
        {
            AnimatorStateTransition entry =
                machine.AddAnyStateTransition(attackState);

            entry.AddCondition(AnimatorConditionMode.If, 0f, AttackTrigger);
            entry.hasExitTime = false;
            entry.duration = 0.1f;
            entry.canTransitionToSelf = false;

            Log("MonsterAnimator: AnyState -> Attack on the trigger.");
        }

        // Return to the walk/idle state once the punch has played.
        if (attackState.transitions.Length == 0 &&
            locomotionState != null &&
            locomotionState != attackState)
        {
            AnimatorStateTransition exit =
                attackState.AddTransition(locomotionState);

            exit.hasExitTime = true;
            exit.exitTime = 0.85f;
            exit.duration = 0.15f;

            Log($"MonsterAnimator: Attack -> {locomotionState.name}.");
        }

        EditorUtility.SetDirty(controller);
    }

    private static bool HasParameter(
        AnimatorController controller,
        string parameterName
    )
    {
        foreach (AnimatorControllerParameter parameter in controller.parameters)
        {
            if (parameter.name == parameterName)
                return true;
        }

        return false;
    }

    private static AnimatorState FindState(
        AnimatorStateMachine machine,
        string stateName
    )
    {
        foreach (ChildAnimatorState child in machine.states)
        {
            if (child.state != null && child.state.name == stateName)
                return child.state;
        }

        return null;
    }

    private static bool HasAnyStateTransitionTo(
        AnimatorStateMachine machine,
        AnimatorState target
    )
    {
        foreach (AnimatorStateTransition transition in machine.anyStateTransitions)
        {
            if (transition.destinationState == target)
                return true;
        }

        return false;
    }

    // --------------------------------------------------------------- helpers

    private static GameObject FindInScene(string objectName)
    {
        // Includes inactive objects, because SelectionRing starts disabled.
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

    private static Avatar LoadAvatar(string modelPath)
    {
        foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(modelPath))
        {
            if (asset is Avatar avatar)
                return avatar;
        }

        return null;
    }

    private static AnimationClip LoadClip(string modelPath)
    {
        foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(modelPath))
        {
            AnimationClip clip = asset as AnimationClip;

            if (clip != null && !clip.name.StartsWith("__preview__"))
                return clip;
        }

        return null;
    }

    private static void SaveEverything()
    {
        EditorSceneManager.MarkAllScenesDirty();
        EditorSceneManager.SaveOpenScenes();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Log("Scene and assets written to disk.");
    }

    private static void Log(string message)
    {
        Debug.Log($"[FIX] {message}");
    }

    private static void Warn(string message)
    {
        Debug.LogWarning($"[FIX] {message}");
    }
}
