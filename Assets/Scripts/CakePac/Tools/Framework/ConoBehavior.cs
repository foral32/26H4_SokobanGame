using Aya.Events;
using UnityEngine;

public class ConoBehavior : MonoListener
{
    private bool initialized = false;
    protected virtual void ManagedInitialize() { }
    protected virtual void ManagedDestroy() { }
    protected virtual void ManagedOnEnable() { }
    protected virtual void ManagedOnDisable() { }

    protected sealed override void Awake()
    {
        base.Awake();
        if (!initialized)
        {
            initialized = true;
            ManagedInitialize();
        }
    }

    protected sealed override void OnEnable()
    {
        base.OnEnable();
        ManagedOnEnable();
    }

    protected sealed override void OnDisable()
    {
        base.OnDisable();
        ManagedOnDisable();
    }
    protected sealed override void OnDestroy()
    {
        base.OnDestroy();
        ManagedDestroy();
    }

}