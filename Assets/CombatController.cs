using UnityEngine;

public class CombatController : MonoBehaviour
{
    public float attackRange = 1.5f;
    public float damage = 10f;
    public float attackCooldown = 1f;

    private float nextAttackTime;

    private void Update()
    {
        Collider[] hits = Physics.OverlapSphere(
            transform.position,
            attackRange
        );

        foreach (Collider hit in hits)
        {
            Health targetHealth = hit.GetComponent<Health>();

            if (targetHealth == null)
                continue;

            if (hit.gameObject == gameObject)
                continue;

            if (Time.time < nextAttackTime)
                continue;

            targetHealth.TakeDamage(damage);

            nextAttackTime = Time.time + attackCooldown;

            Debug.Log(
                $"[{gameObject.name}] attacked {hit.gameObject.name}"
            );

            break;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.DrawWireSphere(
            transform.position,
            attackRange
        );
    }
}