using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class BossPoisonHazard : MonoBehaviour
{
    private readonly Dictionary<IDamageable, float> nextDamageTime = new();
    private float damage;
    private float tickInterval = 0.5f;

    public void Initialize(float damageAmount, float lifetimeSeconds, float radius)
    {
        damage = Mathf.Max(0f, damageAmount);
        Collider hazardCollider = GetComponent<Collider>();
        hazardCollider.isTrigger = true;
        if (hazardCollider is SphereCollider sphereCollider)
            sphereCollider.radius = Mathf.Max(0.1f, radius);
        Destroy(gameObject, Mathf.Max(0.1f, lifetimeSeconds));
    }

    private void OnTriggerStay(Collider other)
    {
        IDamageable damageable = other.GetComponentInParent<PlayerDamageReceiver>();
        if (damageable == null)
            return;

        float currentTime = Time.time;
        if (nextDamageTime.TryGetValue(damageable, out float nextTime) && currentTime < nextTime)
            return;

        damageable.TakeDamage(damage);
        nextDamageTime[damageable] = currentTime + tickInterval;
    }

    private void OnDisable()
    {
        nextDamageTime.Clear();
    }
}
