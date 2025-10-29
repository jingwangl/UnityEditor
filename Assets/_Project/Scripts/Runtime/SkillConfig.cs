using UnityEngine;


public enum ElementType { Physical, Fire, Ice, Lightning, Wind, Earth, Water, Holy, Dark }


[CreateAssetMenu(menuName = "Demo/SkillConfig", fileName = "SkillConfig")]
public class SkillConfig : ScriptableObject
{
    [Header("Identity")]
    public string skillId; // 唯一 ID（必填）
    public string displayName = "New Skill";
    public ElementType element = ElementType.Physical;
    [Tooltip("可选标签（如 单体、群体、DOT、爆发、穿透 等）")] public string[] tags;


    [Header("Base Damage")]
    [Min(0)] public float baseDamage = 100f; // 基础伤害（固定值）
    [Tooltip("攻击力系数：最终伤害中 ATK 的乘数")] public float atkScale = 1.0f; // 攻击力系数
    [Tooltip("额外面板系数，如元素精通/法强等，可先留 0")]
    public float extraScale = 0f;


    [Header("Level Curve (0..1)")]
    [Tooltip("等级曲线：输入 normalizedLevel= (level-1)/(maxLevel-1)")]
    public AnimationCurve levelCurve = AnimationCurve.Linear(0, 1, 1, 1);


    [Header("Crit")]
    [Range(0, 1)] public float critChance = 0.2f;
    [Min(1)] public float critMultiplier = 1.5f; // 暴击伤害倍率


    [Header("Multipliers")]
    [Tooltip("最终乘区：如技能系数、增伤、易伤等，多个叠乘前可先合并为一个")] public float finalMultiplier = 1.0f;


    [TextArea]
    [Tooltip("备注/公式说明，用于文档化（可写：damage=(base+ATK*scale)*curve*final 等)")]
    public string note;
}