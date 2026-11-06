using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Animations;
using Random = UnityEngine.Random;

public static class ExpandedClass
{
  #region 组件
  public static T GetOrAddComponent<T>(this Component go) where T : Component
  {
    T comp = go.GetComponent<T>();
    if (comp == null)
      comp = go.gameObject.AddComponent<T>();
    return comp;
  }

  public static void SetActive(this Component component, bool value)
  {
    if (component != null)
      component.gameObject.SetActive(value);
  }

  public static T GetComponentInChildren<T>(this Component component, string childName) where T : Component
  {
    var child = component.transform.Find(childName);
    return child != null ? child.GetComponent<T>() : null;
  }
  #endregion

  #region 颜色
  public static Color OverrideAlphaColor(this Color color, float alpha) =>
      new Color(color.r, color.g, color.b, alpha);

  public static Color WithAlpha(this Color color, float alpha) =>
      new Color(color.r, color.g, color.b, alpha);

  public static Color RandomColor(this Color color, float saturation = 1f, float value = 1f) =>
      Color.HSVToRGB(Random.value, saturation, value);

  // 不同格式的颜色互相转换
  public static Color HexToColor(this string hex)
  {
    if (ColorUtility.TryParseHtmlString(hex, out Color color))
      return color;
    return Color.white; // 默认颜色
  }
  public static string ColorToHex(this Color color)
  {
    return ColorUtility.ToHtmlStringRGBA(color);
  }
  public static Color V3ToColor(this Vector3 vector)
  {
    return new Color(vector.x, vector.y, vector.z);
  }
  #endregion

  #region 数据结构
  public static bool IsValidIndexFor(this int index, int length) =>
      index >= 0 && index < length;

  public static List<T> CleanList<T>(this List<T> list)
  {
    list.RemoveAll(x => x == null || (x is UnityEngine.Object obj && obj == null));
    return list;
  }

  public static void AddUnique<T>(this List<T> list, T item)
  {
    if (!list.Contains(item))
      list.Add(item);
  }

  public static void RemoveIfHave<T>(this List<T> list, T item)
  {
    if (list.Contains(item))
      list.Remove(item);
  }

  public static T RandomElement<T>(this IList<T> list)
  {
    if (list == null || list.Count == 0) return default;
    return list[Random.Range(0, list.Count)];
  }

  /// <summary>
  /// 随机打乱列表中的元素顺序
  /// </summary>
  public static void Shuffle<T>(this IList<T> list)
  {
    for (int i = list.Count - 1; i > 0; i--)
    {
      int j = Random.Range(0, i + 1);
      (list[j], list[i]) = (list[i], list[j]);
    }
  }

  public static void StopIfNoNull(this Coroutine coroutine, MonoBehaviour monoBehaviour)
  {
    if (coroutine != null)
    {
      monoBehaviour.StopCoroutine(coroutine);
    }
  }
  #endregion

  #region 向量
  public static Vector3 OverrideZ(this Vector3 vector3, float z) =>
      new Vector3(vector3.x, vector3.y, z);

  public static void Flatten(this ref Vector3 v) => v.y = 0;

  public static Vector2 ToXZ(this Vector3 vector3) =>
      new Vector2(vector3.x, vector3.z);

  public static Vector3 IgnoreY(this Vector3 vector3) =>
      new Vector3(vector3.x, 0, vector3.z);

  public static Vector3 RandomPointInBounds(this Bounds bounds) =>
      new Vector3(
          Random.Range(bounds.min.x, bounds.max.x),
          Random.Range(bounds.min.y, bounds.max.y),
          Random.Range(bounds.min.z, bounds.max.z)
      );

  public static Vector3 Add(this Vector3 v, float x = 0, float y = 0, float z = 0) =>
      new Vector3(v.x + x, v.y + y, v.z + z);

  public static Vector3 With(this Vector3 v, float? x = null, float? y = null, float? z = null)
  {
    if (x.HasValue) v.x = x.Value;
    if (y.HasValue) v.y = y.Value;
    if (z.HasValue) v.z = z.Value;
    return v;
  }
  #endregion

  #region 数学
  public static float RemapTo(this float value, float from1, float to1, float from2, float to2)
  {
    if (to1 == from1)
      throw new ArgumentException("from1 和 to1 不能相等", nameof(from1));
    if (to2 == from2)
      throw new ArgumentException("from2 和 to2 不能相等", nameof(from2));
    return (value - from1) / (to1 - from1) * (to2 - from2) + from2;
  }

  public static float Clamp01(this float value) => Mathf.Clamp01(value);

  /// <summary>
  /// 将值限制在指定范围内
  /// </summary>
  public static int Wrap(this int value, int min, int max)
  {
    if (min == max) return min;
    int range = max - min + 1;
    if (value < min)
      value = max - ((min - value) % range);
    else if (value > max)
      value = min + ((value - max) % range);
    return value;
  }
  #endregion

  #region Transform
  public static void Reset(this Transform transform)
  {
    transform.localPosition = Vector3.zero;
    transform.localRotation = Quaternion.identity;
    transform.localScale = Vector3.one;
  }

  public static void CopyTransformFrom(this Transform to, Transform from)
  {
    to.position = from.position;
    to.rotation = from.rotation;
    to.localScale = from.localScale;
  }

  /// <summary>
  /// 设置世界缩放
  /// </summary>
  public static void SetGlobalScale(this Transform transform, Vector3 globalScale)
  {
    if (transform == null) return;
    Transform parent = transform.parent;
    Vector3 parentScale = parent == null ? Vector3.one : parent.lossyScale;
    const float epsilon = 0.000001f;
    transform.localScale = new Vector3(
        Mathf.Abs(parentScale.x) < epsilon ? 0f : globalScale.x / parentScale.x,
        Mathf.Abs(parentScale.y) < epsilon ? 0f : globalScale.y / parentScale.y,
        Mathf.Abs(parentScale.z) < epsilon ? 0f : globalScale.z / parentScale.z);
  }

  public static void DestroyChildren(this Transform transform)
  {
    for (int i = transform.childCount - 1; i >= 0; i--)
    {
      UnityEngine.Object.Destroy(transform.GetChild(i).gameObject);
    }
  }
  #endregion

  #region 字符串
  public static string ToTitleCase(this string text)
  {
    if (string.IsNullOrEmpty(text)) return text;
    return char.ToUpper(text[0]) + text.Substring(1);
  }
  #endregion
  #region 枚举
  // 循环递增方法
  public static T CycleNext<T>(this T value) where T : Enum
  {
    T[] values = (T[])Enum.GetValues(typeof(T));
    int index = Array.IndexOf(values, value);
    return values[(index + 1) % values.Length];
  }
  // 循环递减方法
  public static T CyclePrevious<T>(this T value) where T : Enum
  {
    T[] values = (T[])Enum.GetValues(typeof(T));
    int index = Array.IndexOf(values, value);
    return values[(index - 1 + values.Length) % values.Length];
  }
  #endregion
  #region Gameplay
  public static void TimerTick(this ref float timer)
  {
    if (timer > 0)
      timer -= Time.deltaTime;
  }
  #endregion 
}
