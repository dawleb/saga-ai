using UnityEngine;

// Attaches a weapon model to the character's hand.
//
// The Warrior rig is Humanoid, so the hand is resolved through the Avatar with
// GetBoneTransform rather than by hard coding a bone name. That keeps working
// if the character model is ever swapped.
//
// The fit values are applied every frame, so you can tune them while the game
// is playing and then copy the numbers back into the Inspector.
public class WeaponHolder : MonoBehaviour
{
    [Header("Weapon")]
    [Tooltip("Model to spawn, e.g. gun.fbx from Characters/Warrior/fbx/Weapons_x3.")]
    public GameObject weaponPrefab;

    [Header("Hand")]
    public HumanBodyBones handBone = HumanBodyBones.RightHand;

    [Header("Fit")]
    public Vector3 localPosition = Vector3.zero;
    public Vector3 localEulerAngles = Vector3.zero;
    public float scale = 1f;

    private Transform weaponInstance;

    private void Start()
    {
        Spawn();
    }

    private void Spawn()
    {
        if (weaponPrefab == null)
        {
            Debug.LogWarning(
                $"[WEAPON] {name}: no weapon prefab assigned."
            );

            return;
        }

        Animator animator = GetComponentInChildren<Animator>();

        if (animator == null)
        {
            Debug.LogWarning(
                $"[WEAPON] {name}: no Animator found."
            );

            return;
        }

        if (!animator.isHuman)
        {
            Debug.LogWarning(
                $"[WEAPON] {name}: the Avatar is not Humanoid, " +
                "so the hand bone cannot be resolved."
            );

            return;
        }

        Transform hand = animator.GetBoneTransform(handBone);

        if (hand == null)
        {
            Debug.LogWarning(
                $"[WEAPON] {name}: the rig has no {handBone}."
            );

            return;
        }

        GameObject weapon = Instantiate(weaponPrefab, hand);
        weapon.name = "Weapon";

        // A held prop must not take part in physics, and must never block a
        // click meant for the ground.
        Collider[] colliders = weapon.GetComponentsInChildren<Collider>();

        for (int i = 0; i < colliders.Length; i++)
            Destroy(colliders[i]);

        // The model may ship with its own Animator, which would fight the
        // bone it is parented to.
        Animator[] weaponAnimators = weapon.GetComponentsInChildren<Animator>();

        for (int i = 0; i < weaponAnimators.Length; i++)
            Destroy(weaponAnimators[i]);

        weaponInstance = weapon.transform;

        Debug.Log(
            $"[WEAPON] {name}: {weaponPrefab.name} attached to {handBone}."
        );
    }

    // LateUpdate so the fit is applied after the Animator has posed the hand.
    private void LateUpdate()
    {
        if (weaponInstance == null)
            return;

        weaponInstance.localPosition = localPosition;
        weaponInstance.localEulerAngles = localEulerAngles;
        weaponInstance.localScale = Vector3.one * scale;
    }
}
