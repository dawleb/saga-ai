using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// One-shot maintenance utility.
//
// Fixes the physics/import problems in SampleScene that make the characters
// float, topple and jitter. Every change is undoable (Ctrl+Z) and logged.
// Safe to delete this file once it has been run.
public static class SagaAIStabilityFixes
{
    private const string PlayerName = "Player";
    private const string MonsterName = "Monster";

    private const string RogerModelPath =
        "Assets/Characters/Warrior/fbx/Character_x1/Mercenary_Roger.fbx";

    private const string IdleClipPath =
        "Assets/Animations/Idle.fbx";

    // These two are imported as Generic, so they cannot retarget onto the
    // humanoid avatars. Idle/Walking/Running all use Copy From Other Avatar,
    // and these share the same rig, so we match that setup.
    private static readonly string[] GenericClipsToConvert =
    {
        "Assets/Animations/Falling.fbx",
        "Assets/Animations/Fist Fight A.fbx",
    };

    [MenuItem("Tools/SagaAI/Apply Stability Fixes")]
    private static void Apply()
    {
        // Scene edits first, then asset reimports (a reimport can interrupt).
        FixMonsterColliders();
        FixCharacterRigidbody(PlayerName);
        FixCharacterRigidbody(MonsterName);

        EditorSceneManager.MarkAllScenesDirty();

        EnableIdleLooping();
        ConvertGenericClipsToHumanoid();

        Debug.Log(
            "[FIX] Finished. Press Ctrl+S to save the scene changes."
        );
    }

    private static void FixMonsterColliders()
    {
        GameObject monster = GameObject.Find(MonsterName);

        if (monster == null)
        {
            Debug.LogWarning(
                $"[FIX] GameObject '{MonsterName}' not found in the open scene."
            );

            return;
        }

        CapsuleCollider[] capsules =
            monster.GetComponents<CapsuleCollider>();

        if (capsules.Length == 0)
        {
            Debug.LogWarning(
                "[FIX] Monster has no CapsuleCollider."
            );

            return;
        }

        // Any extra capsule is a leftover. The survivor is reconfigured below,
        // so which one we keep does not matter.
        for (int i = capsules.Length - 1; i >= 1; i--)
        {
            Undo.DestroyObjectImmediate(capsules[i]);

            Debug.Log(
                "[FIX] Monster: removed a duplicate CapsuleCollider."
            );
        }

        CapsuleCollider body = capsules[0];

        Undo.RecordObject(body, "Fix Monster Collider");

        // The Monster root sits at y = 0.5 and its model child is offset by
        // -0.5, so the feet are at local y = -0.5. Putting the capsule bottom
        // there stops it from poking through the ground.
        body.direction = 1; // Y axis
        body.radius = 0.4f;
        body.height = 1.8f;
        body.center = new Vector3(0f, 0.4f, 0f);

        EditorUtility.SetDirty(body);

        Debug.Log(
            "[FIX] Monster: capsule resized to sit on the ground " +
            "(radius 0.4, height 1.8, center y 0.4)."
        );
    }

    private static void FixCharacterRigidbody(string objectName)
    {
        GameObject character = GameObject.Find(objectName);

        if (character == null)
        {
            Debug.LogWarning(
                $"[FIX] GameObject '{objectName}' not found in the open scene."
            );

            return;
        }

        Rigidbody body = character.GetComponent<Rigidbody>();

        if (body == null)
        {
            Debug.LogWarning(
                $"[FIX] '{objectName}' has no Rigidbody."
            );

            return;
        }

        Undo.RecordObject(body, "Fix Character Rigidbody");

        // Both characters are positioned entirely from script with a locked Y,
        // so kinematic is what the code already assumes. This stops gravity
        // from accumulating velocity behind the scripted movement and stops
        // physics from fighting the transform writes.
        body.isKinematic = true;
        body.useGravity = false;

        // Rotation Z was left free, which let collisions topple the character.
        body.constraints = RigidbodyConstraints.FreezeRotation;

        EditorUtility.SetDirty(body);

        Debug.Log(
            $"[FIX] {objectName}: Rigidbody is now kinematic, " +
            "gravity off, rotation frozen."
        );
    }

    private static void EnableIdleLooping()
    {
        ModelImporter importer =
            AssetImporter.GetAtPath(IdleClipPath) as ModelImporter;

        if (importer == null)
        {
            Debug.LogWarning(
                $"[FIX] Could not load ModelImporter for {IdleClipPath}."
            );

            return;
        }

        ModelImporterClipAnimation[] clips =
            importer.clipAnimations;

        if (clips == null || clips.Length == 0)
            clips = importer.defaultClipAnimations;

        if (clips == null || clips.Length == 0)
        {
            Debug.LogWarning(
                "[FIX] Idle.fbx has no animation clips to configure."
            );

            return;
        }

        for (int i = 0; i < clips.Length; i++)
            clips[i].loopTime = true;

        importer.clipAnimations = clips;
        importer.SaveAndReimport();

        Debug.Log(
            "[FIX] Idle.fbx: Loop Time enabled " +
            "(it previously froze after ~8 seconds)."
        );
    }

    private static void ConvertGenericClipsToHumanoid()
    {
        Avatar sourceAvatar = LoadAvatar(RogerModelPath);

        if (sourceAvatar == null)
        {
            Debug.LogWarning(
                $"[FIX] No Avatar found in {RogerModelPath}. " +
                "Skipped the Humanoid conversion."
            );

            return;
        }

        foreach (string path in GenericClipsToConvert)
        {
            ModelImporter importer =
                AssetImporter.GetAtPath(path) as ModelImporter;

            if (importer == null)
            {
                Debug.LogWarning(
                    $"[FIX] Could not load ModelImporter for {path}."
                );

                continue;
            }

            importer.animationType = ModelImporterAnimationType.Human;
            importer.avatarSetup = ModelImporterAvatarSetup.CopyFromOther;
            importer.sourceAvatar = sourceAvatar;
            importer.SaveAndReimport();

            Debug.Log(
                $"[FIX] {path}: set to Humanoid, " +
                "avatar copied from Mercenary_Roger."
            );
        }
    }

    private static Avatar LoadAvatar(string modelPath)
    {
        Object[] assets =
            AssetDatabase.LoadAllAssetsAtPath(modelPath);

        foreach (Object asset in assets)
        {
            if (asset is Avatar avatar)
                return avatar;
        }

        return null;
    }
}
