using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(Collider))]
public class LaserProjectile : MonoBehaviour
{
    private Rigidbody rb;
    private GameObject owner;
    private float damage;
    private float lifetime;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        GetComponent<Collider>().isTrigger = true;
    }

    public void Launch(Vector3 direction, float speed, float damageAmount, float lifetimeSeconds, GameObject projectileOwner)
    {
        owner = projectileOwner;
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
        if (owner != null && other.transform.root.gameObject == owner.transform.root.gameObject)
            return;

        IDamageable damageable = other.GetComponentInParent<IDamageable>();
        if (damageable == null)
            return;

        damageable.TakeDamage(damage);
        Destroy(gameObject);
    }
}
