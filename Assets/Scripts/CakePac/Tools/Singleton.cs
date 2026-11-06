using Aya.Events;
using UnityEngine;

public abstract class Singleton<T> : ConoBehavior where T : Singleton<T>
{
    private static T instance;
    private static object _lock = new object();
    protected static bool isQuitting = false;

    public static T Instance
    {
        get
        {
            if (isQuitting)
            {
                if (instance != null) instance = null;
                return null;
            }
            if (instance == null)
            {
                //lock确保在多线程环境下，同一时间只有一个线程可以执行被锁定的代码块
                lock (_lock)
                {
                    instance = FindObjectOfType<T>();
                }
            }

            return instance;
        }
    }

    [SerializeField] protected bool dontDestroyOnLoad = false;


    protected override void ManagedInitialize()
    {
        isQuitting = false;
        if (instance == null)
        {
            instance = (T)this;
            if (dontDestroyOnLoad)
                DontDestroyOnLoad(gameObject);
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }
    protected override void ManagedDestroy()
    {
#if UNITY_EDITOR
        if (instance == this)
            instance = null;
        isQuitting = true;
#endif
    }
    private void OnApplicationQuit()
    {
        instance = null;
    }
}

