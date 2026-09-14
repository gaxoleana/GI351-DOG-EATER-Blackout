using UnityEngine;

[RequireComponent(typeof(Collider))]
public class FragmentPickup : MonoBehaviour
{
    [SerializeField, Min(1)] private int amount = 1;

    private bool hasBeenCollected;

    private void Awake()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    public void SetAmount(int value)
    {
        amount = Mathf.Max(1, value);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (hasBeenCollected || !other.transform.root.CompareTag("Player"))
            return;

        FragmentCurrency currency = other.transform.root.GetComponent<FragmentCurrency>();
        if (currency == null)
            return;

        hasBeenCollected = true;
        currency.Add(amount);
        Destroy(gameObject);
    }
}
