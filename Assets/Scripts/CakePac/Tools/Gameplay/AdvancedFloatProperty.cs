using System;
using System.Diagnostics;
using UnityEngine;
[Serializable]

public class AdvancedFloatProperty
{
    private float m_baseValue = 0f; //基础白值
    private float m_addBonus = 0f; // 与base相加，再加到结果上
    private float m_percentBonus = 0f; // 与base相乘，再加到结果上

    private float m_realValue = 0f; //实际值
    public float BaseValue { get => m_baseValue; set { m_baseValue = value; RefreshNewRealValue(); } }
    public float AddBonus { get => m_addBonus; set { m_addBonus = value; RefreshNewRealValue(); } }
    public float PercentBonus { get => m_percentBonus; set { m_percentBonus = value; RefreshNewRealValue(); } }
    public float RealValue { get => m_realValue; }

    public Action<float> OnRealValueChanged;
    public void RefreshNewRealValue()
    {
        var oldValue = m_realValue;
        m_realValue = m_baseValue + m_addBonus + (m_baseValue * (m_percentBonus / 100f));
        if (!Mathf.Approximately(oldValue, m_realValue)) OnRealValueChanged?.Invoke(m_realValue);
    }

    public AdvancedFloatProperty() { }
    public AdvancedFloatProperty(float initialValue)
    {
        m_baseValue = initialValue;
        m_realValue = initialValue;
    }

    // 重载隐式转换运算符，允许直接读取当前实际值
    public static implicit operator float(AdvancedFloatProperty property)
    {
        return property.RealValue;
    }

}
