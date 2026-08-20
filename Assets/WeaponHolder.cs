using UnityEngine;

// =============================================================
// WEAPON HOLDER
// =============================================================
//
// Weapon jest tworzony podczas uruchomienia gry:
//
// Player
// └── Model
//     └── ... 
//         └── Hand.R
//             └── Weapon
//                 └── MuzzlePoint
//
// MuzzlePoint nie musi istnieć wcześniej.
// Jeśli prefab Weapon go nie posiada, WeaponHolder utworzy go
// automatycznie.
//
// CombatManager nie wymaga ręcznego przypisywania MuzzlePoint.
// WeaponHolder przekazuje go automatycznie po stworzeniu broni.
// =============================================================

public class WeaponHolder : MonoBehaviour
{
    // =========================================================
    // WEAPON
    // =========================================================

    [Header("Weapon")]

    [Tooltip(
        "Weapon prefab/model to attach to the character's hand."
    )]
    public GameObject weaponPrefab;


    // =========================================================
    // HAND
    // =========================================================

    [Header("Hand")]

    [Tooltip(
        "Humanoid bone where the weapon will be attached."
    )]
    public HumanBodyBones handBone =
        HumanBodyBones.RightHand;


    // =========================================================
    // WEAPON FIT
    // =========================================================

    [Header("Weapon Fit")]

    [Tooltip(
        "Local position of the weapon relative to the hand."
    )]
    public Vector3 localPosition =
        Vector3.zero;

    [Tooltip(
        "Local rotation of the weapon relative to the hand."
    )]
    public Vector3 localEulerAngles =
        Vector3.zero;

    [Tooltip(
        "Weapon scale."
    )]
    [Min(0.001f)]
    public float scale = 1f;


    // =========================================================
    // MUZZLE
    // =========================================================

    [Header("Muzzle Point")]

    [Tooltip(
        "Optional muzzle point. Leave empty. " +
        "WeaponHolder will automatically find or create it."
    )]
    public Transform muzzlePoint;

    [Tooltip(
        "Automatically search the spawned weapon for MuzzlePoint."
    )]
    public bool autoFindMuzzlePoint = true;

    [Tooltip(
        "Name of the muzzle point."
    )]
    public string muzzlePointName =
        "MuzzlePoint";

    [Tooltip(
        "If the weapon has no MuzzlePoint, create one automatically."
    )]
    public bool createMuzzlePointIfMissing = true;


    // =========================================================
    // AUTO MUZZLE POSITION
    // =========================================================

    [Header("Automatic Muzzle Position")]

    [Tooltip(
        "If MuzzlePoint does not exist, automatically place it " +
        "near the end of the weapon using the renderers bounds."
    )]
    public bool calculateMuzzlePositionAutomatically = true;

    [Tooltip(
        "Extra distance added in the weapon's local forward direction."
    )]
    public float automaticMuzzleForwardOffset = 0.05f;


    // =========================================================
    // DEBUG
    // =========================================================

    [Header("Debug")]

    public bool debugWeaponSetup = true;


    // =========================================================
    // INTERNAL
    // =========================================================

    private Transform weaponInstance;

    private Transform attachedHand;


    // =========================================================
    // START
    // =========================================================

    private void Start()
    {
        Spawn();
    }


    // =========================================================
    // SPAWN
    // =========================================================

    private void Spawn()
    {
        // -----------------------------------------------------
        // WEAPON PREFAB
        // -----------------------------------------------------

        if (weaponPrefab == null)
        {
            Debug.LogWarning(
                $"[WEAPON] {name}: no weapon prefab assigned.",
                this
            );

            return;
        }


        // -----------------------------------------------------
        // FIND ANIMATOR
        // -----------------------------------------------------

        Animator animator =
            GetComponentInChildren<Animator>();

        if (animator == null)
        {
            Debug.LogWarning(
                $"[WEAPON] {name}: no Animator found.",
                this
            );

            return;
        }


        // -----------------------------------------------------
        // CHECK HUMANOID
        // -----------------------------------------------------

        if (!animator.isHuman)
        {
            Debug.LogWarning(
                $"[WEAPON] {name}: Avatar is not Humanoid.",
                this
            );

            return;
        }


        // -----------------------------------------------------
        // FIND RIGHT HAND
        // -----------------------------------------------------

        Transform hand =
            animator.GetBoneTransform(handBone);

        if (hand == null)
        {
            Debug.LogWarning(
                $"[WEAPON] {name}: " +
                $"cannot find {handBone}.",
                this
            );

            return;
        }

        attachedHand = hand;


        // -----------------------------------------------------
        // REMOVE OLD WEAPON
        // -----------------------------------------------------

        if (weaponInstance != null)
        {
            Destroy(
                weaponInstance.gameObject
            );

            weaponInstance = null;
        }


        // -----------------------------------------------------
        // RESET MUZZLE REFERENCE
        // -----------------------------------------------------

        muzzlePoint = null;


        // -----------------------------------------------------
        // CREATE WEAPON
        // -----------------------------------------------------

        GameObject weapon =
            Instantiate(
                weaponPrefab,
                hand
            );

        weapon.name =
            "Weapon";


        // -----------------------------------------------------
        // WEAPON TRANSFORM
        // -----------------------------------------------------

        weapon.transform.localPosition =
            localPosition;

        weapon.transform.localEulerAngles =
            localEulerAngles;

        weapon.transform.localScale =
            Vector3.one * scale;


        // -----------------------------------------------------
        // REMOVE COLLIDERS
        // -----------------------------------------------------

        Collider[] colliders =
            weapon.GetComponentsInChildren<Collider>(
                true
            );

        for (int i = 0;
             i < colliders.Length;
             i++)
        {
            if (colliders[i] != null)
            {
                Destroy(
                    colliders[i]
                );
            }
        }


        // -----------------------------------------------------
        // REMOVE WEAPON ANIMATORS
        // -----------------------------------------------------

        Animator[] weaponAnimators =
            weapon.GetComponentsInChildren<Animator>(
                true
            );

        for (int i = 0;
             i < weaponAnimators.Length;
             i++)
        {
            if (weaponAnimators[i] != null)
            {
                Destroy(
                    weaponAnimators[i]
                );
            }
        }


        // -----------------------------------------------------
        // SAVE WEAPON
        // -----------------------------------------------------

        weaponInstance =
            weapon.transform;


        // -----------------------------------------------------
        // FIND OR CREATE MUZZLE
        // -----------------------------------------------------

        FindOrCreateMuzzlePoint();


        // -----------------------------------------------------
        // REGISTER WITH COMBAT MANAGER
        // -----------------------------------------------------

        RegisterMuzzleWithCombatManager();


        // -----------------------------------------------------
        // DEBUG
        // -----------------------------------------------------

        if (debugWeaponSetup)
        {
            Debug.Log(
                $"[WEAPON] {name}: Weapon created and attached " +
                $"to {handBone}.",
                weapon
            );

            if (muzzlePoint != null)
            {
                Debug.Log(
                    $"[WEAPON] {name}: MuzzlePoint registered: " +
                    $"{muzzlePoint.name}",
                    muzzlePoint
                );
            }
            else
            {
                Debug.LogError(
                    $"[WEAPON] {name}: FAILED to create MuzzlePoint!",
                    this
                );
            }
        }
    }


    // =========================================================
    // FIND OR CREATE MUZZLE
    // =========================================================

    private void FindOrCreateMuzzlePoint()
    {
        if (weaponInstance == null)
            return;


        // -----------------------------------------------------
        // MANUALLY ASSIGNED MUZZLE
        // -----------------------------------------------------

        if (muzzlePoint != null)
        {
            if (muzzlePoint.IsChildOf(
                    weaponInstance
                ))
            {
                return;
            }

            Debug.LogWarning(
                $"[WEAPON] {name}: assigned MuzzlePoint " +
                "does not belong to this Weapon. " +
                "It will be ignored.",
                this
            );

            muzzlePoint = null;
        }


        // -----------------------------------------------------
        // SEARCH
        // -----------------------------------------------------

        if (autoFindMuzzlePoint)
        {
            Transform[] children =
                weaponInstance.GetComponentsInChildren<Transform>(
                    true
                );

            // Exact name search
            for (int i = 0;
                 i < children.Length;
                 i++)
            {
                Transform child =
                    children[i];

                if (child == null)
                    continue;

                if (child.name ==
                    muzzlePointName)
                {
                    muzzlePoint =
                        child;

                    return;
                }
            }


            // Case-insensitive search
            for (int i = 0;
                 i < children.Length;
                 i++)
            {
                Transform child =
                    children[i];

                if (child == null)
                    continue;

                if (string.Equals(
                        child.name,
                        muzzlePointName,
                        System.StringComparison.OrdinalIgnoreCase
                    ))
                {
                    muzzlePoint =
                        child;

                    return;
                }
            }
        }


        // -----------------------------------------------------
        // CREATE
        // -----------------------------------------------------

        if (!createMuzzlePointIfMissing)
        {
            Debug.LogWarning(
                $"[WEAPON] {name}: MuzzlePoint not found.",
                weaponInstance
            );

            return;
        }


        GameObject muzzleObject =
            new GameObject(
                muzzlePointName
            );

        muzzlePoint =
            muzzleObject.transform;

        muzzlePoint.SetParent(
            weaponInstance,
            false
        );


        // -----------------------------------------------------
        // AUTOMATIC POSITION
        // -----------------------------------------------------

        if (calculateMuzzlePositionAutomatically)
        {
            SetAutomaticMuzzlePosition();
        }
        else
        {
            muzzlePoint.localPosition =
                Vector3.forward * 0.5f;

            muzzlePoint.localRotation =
                Quaternion.identity;
        }


        if (debugWeaponSetup)
        {
            Debug.Log(
                $"[WEAPON] {name}: MuzzlePoint did not exist. " +
                "Created automatically.",
                muzzlePoint
            );
        }
    }


    // =========================================================
    // AUTOMATIC MUZZLE POSITION
    // =========================================================

    private void SetAutomaticMuzzlePosition()
    {
        if (weaponInstance == null ||
            muzzlePoint == null)
        {
            return;
        }


        Renderer[] renderers =
            weaponInstance.GetComponentsInChildren<Renderer>(
                true
            );


        if (renderers == null ||
            renderers.Length == 0)
        {
            muzzlePoint.localPosition =
                Vector3.forward *
                0.5f;

            muzzlePoint.localRotation =
                Quaternion.identity;

            return;
        }


        // -----------------------------------------------------
        // WORLD BOUNDS
        // -----------------------------------------------------

        Bounds bounds =
            renderers[0].bounds;

        for (int i = 1;
             i < renderers.Length;
             i++)
        {
            if (renderers[i] == null)
                continue;

            bounds.Encapsulate(
                renderers[i].bounds
            );
        }


        // -----------------------------------------------------
        // CONVERT BOUNDS TO WEAPON LOCAL SPACE
        // -----------------------------------------------------

        Vector3 worldCenter =
            bounds.center;

        Vector3 worldForward =
            weaponInstance.forward;


        // Find the point of the bounds furthest in the
        // weapon's forward direction.

        Vector3[] corners =
        {
            new Vector3(bounds.min.x, bounds.min.y, bounds.min.z),
            new Vector3(bounds.min.x, bounds.min.y, bounds.max.z),
            new Vector3(bounds.min.x, bounds.max.y, bounds.min.z),
            new Vector3(bounds.min.x, bounds.max.y, bounds.max.z),

            new Vector3(bounds.max.x, bounds.min.y, bounds.min.z),
            new Vector3(bounds.max.x, bounds.min.y, bounds.max.z),
            new Vector3(bounds.max.x, bounds.max.y, bounds.min.z),
            new Vector3(bounds.max.x, bounds.max.y, bounds.max.z)
        };


        float bestDot =
            float.MinValue;

        Vector3 bestPoint =
            worldCenter;


        for (int i = 0;
             i < corners.Length;
             i++)
        {
            Vector3 point =
                corners[i];

            Vector3 direction =
                point - worldCenter;

            float dot =
                Vector3.Dot(
                    direction,
                    worldForward
                );

            if (dot > bestDot)
            {
                bestDot = dot;
                bestPoint = point;
            }
        }


        // -----------------------------------------------------
        // CONVERT WORLD → LOCAL
        // -----------------------------------------------------

        muzzlePoint.localPosition =
            weaponInstance.InverseTransformPoint(
                bestPoint
            );


        // Push slightly forward.
        muzzlePoint.localPosition +=
            Vector3.forward *
            automaticMuzzleForwardOffset;


        // -----------------------------------------------------
        // ROTATION
        // -----------------------------------------------------

        muzzlePoint.localRotation =
            Quaternion.identity;
    }


    // =========================================================
    // REGISTER MUZZLE
    // =========================================================

    private void RegisterMuzzleWithCombatManager()
    {
        if (muzzlePoint == null)
        {
            Debug.LogError(
                $"[WEAPON] {name}: " +
                "MuzzlePoint is NULL. Cannot register.",
                this
            );

            return;
        }


        // -----------------------------------------------------
        // GET COMBAT MANAGER
        // -----------------------------------------------------

        CombatManager combatManager =
            CombatManager.Instance;


        if (combatManager == null)
        {
            combatManager =
                FindObjectOfType<CombatManager>();
        }


        if (combatManager == null)
        {
            Debug.LogWarning(
                $"[WEAPON] {name}: CombatManager not found.",
                this
            );

            return;
        }


        // -----------------------------------------------------
        // REGISTER
        // -----------------------------------------------------

        combatManager.SetMuzzlePoint(
            muzzlePoint
        );


        if (debugWeaponSetup)
        {
            Debug.Log(
                $"[WEAPON] {name}: MuzzlePoint " +
                $"'{muzzlePoint.name}' registered with CombatManager.",
                muzzlePoint
            );
        }
    }


    // =========================================================
    // LATE UPDATE
    // =========================================================

    private void LateUpdate()
    {
        if (weaponInstance == null)
            return;


        // Make sure the weapon stays correctly fitted
        // to the hand after Animator updates.

        weaponInstance.localPosition =
            localPosition;

        weaponInstance.localEulerAngles =
            localEulerAngles;

        weaponInstance.localScale =
            Vector3.one * scale;
    }


    // =========================================================
    // PUBLIC GETTERS
    // =========================================================

    public Transform GetWeapon()
    {
        return weaponInstance;
    }


    public Transform GetMuzzlePoint()
    {
        return muzzlePoint;
    }


    // =========================================================
    // GIZMOS
    // =========================================================

    private void OnDrawGizmos()
    {
        if (muzzlePoint == null)
            return;


        Gizmos.color =
            Color.red;

        Gizmos.DrawSphere(
            muzzlePoint.position,
            0.025f
        );


        Gizmos.color =
            Color.yellow;

        Gizmos.DrawLine(
            muzzlePoint.position,
            muzzlePoint.position +
            muzzlePoint.forward * 0.25f
        );
    }
}