using UnityEngine;

/// <summary>
/// 技能伤害配置（为求职 Demo 精简版）
/// </summary>
public enum ElementType { Physical, Fire, Ice, Lightning, Wind }

[CreateAssetMenu(menuName = "Demo/SkillConfig", fileName = "SkillConfig")]
public class SkillConfig : ScriptableObject
{
    [Header("基础信息")]
    public string skillId = "skill_001";
    public string displayName = "New Skill";
    public ElementType element = ElementType.Physical;

    [Header("伤害参数")]
    [Min(0)] public float baseDamage = 100f;
    [Tooltip("该技能对攻击力的倍率")]
    public float atkScale = 1.0f;

    [Header("等级系数")]
    [Tooltip("随等级缩放的曲线（X:0-1，Y:缩放系数）")]
    public AnimationCurve levelCurve = AnimationCurve.Linear(0, 1, 1, 1.5f);

    [Header("暴击")]
    [Range(0, 1)] public float critChance = 0.2f;
    [Min(1)] public float critMultiplier = 1.5f;
}