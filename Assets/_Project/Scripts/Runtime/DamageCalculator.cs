using UnityEngine;

/// <summary>
/// 伤害计算模块（求职 Demo 精简公式）
/// 公式：Damage = (BaseDamage + ATK × ATKScale) × LevelMultiplier
/// </summary>
public static class DamageCalculator
{
    /// <summary>
    /// 计算期望伤害（非暴击与暴击的加权平均）
    /// </summary>
    public static float EvaluateExpected(SkillConfig skill, CombatStats stats, int level, int maxLevel)
    {
        if (!skill) return 0f;
        
        // 等级取合法范围
        maxLevel = Mathf.Max(1, maxLevel);
        level = Mathf.Clamp(level, 1, maxLevel);
        
        // 归一化等级（0-1）
        float normalizedLevel = maxLevel == 1 ? 1f : (level - 1f) / (maxLevel - 1f);
        float levelMultiplier = Mathf.Max(0f, skill.levelCurve.Evaluate(normalizedLevel));
        
        // 基础部分：Base + ATK × Scale
        float baseDamage = skill.baseDamage + stats.ATK * skill.atkScale;
        
        // 应用等级系数
        float nonCritDamage = baseDamage * levelMultiplier;
        float critDamage = nonCritDamage * skill.critMultiplier;
        
        // 期望伤害：按暴击率加权
        float expectedDamage = nonCritDamage * (1f - skill.critChance) + critDamage * skill.critChance;
        
        return Mathf.Max(0f, expectedDamage);
    }
    
    /// <summary>
    /// 计算非暴击伤害
    /// </summary>
    public static float EvaluateNonCrit(SkillConfig skill, CombatStats stats, int level, int maxLevel)
    {
        float expected = EvaluateExpected(skill, stats, level, maxLevel);
        float avgMultiplier = 1f - skill.critChance + skill.critChance * skill.critMultiplier;
        return expected / avgMultiplier;
    }
    
    /// <summary>
    /// 计算暴击伤害
    /// </summary>
    public static float EvaluateCrit(SkillConfig skill, CombatStats stats, int level, int maxLevel)
    {
        return EvaluateNonCrit(skill, stats, level, maxLevel) * skill.critMultiplier;
    }
}