using UnityEngine;

public class ManagedSingleton<T> : ManagedBehaviour where T : ManagedSingleton<T>
{
    private static T instance;
    private static object _lock = new object();

    public static T Instance
    {
        get
        {
            if (instance == null)
            {
                //lock确保在多线程环境下，同一时间只有一个线程可以执行被锁定的代码块
                lock (_lock)
                {
                    instance = FindObjectOfType<T>() as T;
                }
            }
            return instance;
        }
    }


    protected override void ManagedInitialize()
    {
        if (instance == null)
        {
            instance = (T)this;
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }
    protected virtual void OnDestroy()
    {
        if (instance == this)
            instance = null;
    }
    private void OnApplicationQuit()
    {
        instance = null;
    }
}