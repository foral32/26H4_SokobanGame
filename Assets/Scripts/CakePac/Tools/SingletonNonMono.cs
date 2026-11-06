using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class SingletonNonMono<T> where T : class, new() //确保T是引用类型（class）且有一个无参构造函数（new()）
{
    private static T _instance;
    private static object _lock = new object(); //线程同步对象

    public static T Instance
    {
        get
        {
            if (_instance == null)
            {
                lock (_lock)
                {
                    _instance ??= new T();
                    //??= 是C#8.0引入的复合赋值运算符，表示如果左边的值为null，则将右边的值赋给左边
                }
            }
            return _instance;
        }
    }
}
