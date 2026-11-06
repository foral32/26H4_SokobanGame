using UnityEngine;

public class Lifetime : MonoBehaviour
{
  [Header("生命周期设置")]
  [SerializeField] private float lifeTime = 1f;
  public float LifeTime { get => lifeTime; set => lifeTime = value; }
  [SerializeField] private bool destroyOnComplete = true;

  void Start()
  {
    if (lifeTime > 0 && destroyOnComplete)
    {
      Destroy(gameObject, lifeTime);
    }
  }
}