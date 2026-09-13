using System;

public interface IResourceStat
{
    float Current { get; }
    float Max { get; }
    event Action<float, float> OnChanged;
}
