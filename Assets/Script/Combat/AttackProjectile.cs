using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(Collider))]
public class AttackProjectile : MonoBehaviour
{
    private Rigidbody rb;
    private Transform ownerRoot;
    private float damage;
    private float lifetime;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        GetComponent<Collider>().isTrigger = true;
    }

    public void Launch(Vector3 direction, float speed, float damageAmount, float lifetimeSeconds, GameObject ownerObject)
    {
        ownerRoot = ownerObject != null ? ownerObject.transform.root : null;
        damage = Mathf.Max(0f, damageAmount);
        lifetime = Mathf.Max(0.01f, lifetimeSeconds);
        direction.y = 0f;

        if (direction.sqrMagnitude <= 0.0001f)
            direction = transform.right;

        direction.Normalize();
        transform.right = direction;

        if (rb != null)
            rb.linearVelocity = direction * Mathf.Max(0f, speed);

        Destroy(gameObject, lifetime);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (ownerRoot != null && other.transform.root == ownerRoot)
            return;

        IDamageable damageable = other.GetComponentInParent<PlayerDamageReceiver>();
        damageable ??= other.GetComponentInParent<IDamageable>();

        if (damageable == null)
            return;

        damageable.TakeDamage(damage);
        Destroy(gameObject);
    }
}
