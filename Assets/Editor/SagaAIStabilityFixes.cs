using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

// Asset-level fixes: import settings and the Monster's AnimatorController.
// These cannot be done at runtime, unlike the scene fixes in SceneRuntimeFixes.
//
// Runs automatically when Unity loads or recompiles, so there is no menu item
// to remember. Everything is idempotent, so once applied it does nothing.
public static class SagaAIStabilityFixes
{
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

    private const string SessionKey = "SagaAI.AssetFixesApplied";

    [InitializeOnLoadMethod]
    private static void AutoApply()
    {
        if (SessionState.GetBool(SessionKey, false))
            return;

        SessionState.SetBool(SessionKey, true);

        // Deferred: the AssetDatabase is not ready during load itself.
        EditorApplication.delayCall += Apply;
    }

    [MenuItem("Tools/SagaAI/Apply Asset Fixes")]
    private static void Apply()
    {
        EnableIdleLooping();

        // The punch is the Monster's move, so it copies the Beast avatar the
        // way Crouched Walking already does.
        MakeHumanoid(FistFightClipPath, BeastModelPath, "PopolBeast");
        MakeHumanoid(FallingClipPath, RogerModelPath, "Mercenary_Roger");

        // Must run after the clip is Humanoid, or there is nothing to assign.
        AddMonsterAttackState();

        AssetDatabase.SaveAssets();
    }

    private static void EnableIdleLooping()
    {
        ModelImporter importer =
            AssetImporter.GetAtPath(IdleClipPath) as ModelImporter;

        if (importer == null)
            return;

        ModelImporterClipAnimation[] clips = importer.clipAnimations;

        if (clips == null || clips.Length == 0)
            clips = importer.defaultClipAnimations;

        if (clips == null || clips.Length == 0)
            return;

        bool changed = false;

        for (int i = 0; i < clips.Length; i++)
        {
            if (clips[i].loopTime)
                continue;

            clips[i].loopTime = true;
            changed = true;
        }

        if (!changed)
            return;

        importer.clipAnimations = clips;
        importer.SaveAndReimport();

        Log("Idle.fbx: Loop Time enabled, it no longer freezes.");
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
            return;

        Avatar sourceAvatar = LoadAvatar(avatarModelPath);

        if (sourceAvatar == null)
        {
            Warn($"No Avatar in {avatarModelPath}, skipped {clipPath}.");
            return;
        }

        importer.animationType = ModelImporterAnimationType.Human;
        importer.avatarSetup = ModelImporterAvatarSetup.CopyFromOther;
        importer.sourceAvatar = sourceAvatar;
        importer.SaveAndReimport();

        Log($"{clipPath}: now Humanoid, avatar copied from {avatarLabel}.");
    }

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
                $"No AnimationClip in {FistFightClipPath}, " +
                "Attack state not created."
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

            Log("MonsterAnimator: AnyState -> Attack wired to the trigger.");
        }

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

    private static void Log(string message)
    {
        Debug.Log($"[ASSET FIX] {message}");
    }

    private static void Warn(string message)
    {
        Debug.LogWarning($"[ASSET FIX] {message}");
    }
}
