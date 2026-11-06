using UnityEngine;

public class LifetimeBehavior : MonoBehaviour
{
  protected Lifetime lifetime_Comp;
  protected float lifeTime => lifetime_Comp.LifeTime;

  protected virtual void Awake()
  {
    lifetime_Comp = this.GetOrAddComponent<Lifetime>();
  }
}