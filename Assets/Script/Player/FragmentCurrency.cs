using System;
using UnityEngine;

public class FragmentCurrency : MonoBehaviour
{
    private static FragmentCurrency instance;

    [SerializeField, Min(0)] private long currentFragments;

    public static FragmentCurrency Instance => instance;
    public long CurrentFragments => currentFragments;
    public event Action<long> OnFragmentsChanged;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(this);
            return;
        }

        instance = this;
    }

    public void Add(long amount)
    {
        if (amount <= 0)
            return;

        currentFragments += amount;
        OnFragmentsChanged?.Invoke(currentFragments);
    }

    public bool TrySpend(long amount)
    {
        if (amount <= 0 || currentFragments < amount)
            return false;

        currentFragments -= amount;
        OnFragmentsChanged?.Invoke(currentFragments);
        return true;
    }
}
