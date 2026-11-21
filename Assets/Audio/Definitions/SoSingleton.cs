using System;
using UnityEngine;

namespace UnholyTrinity.Audio
{
public abstract class SoSingleton<T> : ScriptableObject
    where T : ScriptableObject
{
    public static T Instance { get; private set; }

    protected virtual void Awake()
    {
        if (Instance != null && Instance != this)
        {
            throw new InvalidOperationException(
                $"Can't have more than 1 {nameof(T)}");
        }

        Instance = this as T;
    }

    protected virtual void OnEnable()
    {
        if (Instance == null)
        {
            Instance = this as T;
        }
    }

    protected virtual  void OnDisable()
    {
        Instance = null;
    }
}
}
