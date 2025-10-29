using UnityEngine;


[System.Serializable]
public class CombatStats
{
    [Header("Attacker")] public float ATK = 500f; // 攻击力
    [Range(0, 1)] public float damageBonus = 0.0f; // 伤害加成（同乘区）
    [Range(-1, 1)] public float elementBonus = 0.0f; // 元素加成


    [Header("Target")][Range(-1, 1)] public float targetResist = 0.1f; // 目标抗性（0.1=10%）
    [Range(0, 1)] public float defenseMitigation = 0.0f; // 防御减伤系数（如 0.2 代表最终乘 0.8）
}