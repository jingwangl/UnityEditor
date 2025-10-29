using UnityEngine;


public static class DamageCalculator
{
// 期望伤害（非暴+暴击的期望值）
public static float EvaluateExpected(SkillConfig skill, CombatStats stats, int level, int maxLevel)
{
if (!skill) return 0f; if (maxLevel < 1) maxLevel = 1; if (level < 1) level = 1; if (level > maxLevel) level = maxLevel;
float lvl01 = (maxLevel == 1) ? 1f : (level - 1f) / (maxLevel - 1f);
float levelMul = Mathf.Max(0f, skill.levelCurve.Evaluate(lvl01));


float basePart = skill.baseDamage + stats.ATK * skill.atkScale + stats.ATK * skill.extraScale;
float bonusMul = (1f + stats.damageBonus) * (1f + stats.elementBonus) * skill.finalMultiplier;
float resistMul = Mathf.Clamp01(1f - Mathf.Max(-0.8f, stats.targetResist)); // 允许最低 -80% 易伤
float defenseMul = 1f - Mathf.Clamp01(stats.defenseMitigation);


float nonCrit = basePart * levelMul * bonusMul * resistMul * defenseMul;
float crit = nonCrit * skill.critMultiplier;
float expected = nonCrit * (1f - skill.critChance) + crit * skill.critChance;
return Mathf.Max(0f, expected);
}


public static float EvaluateNonCrit(SkillConfig skill, CombatStats stats, int level, int maxLevel)
=> EvaluateExpected(skill, stats, level, maxLevel) / (1f - skill.critChance + skill.critChance * skill.critMultiplier);


public static float EvaluateCrit(SkillConfig skill, CombatStats stats, int level, int maxLevel)
=> EvaluateNonCrit(skill, stats, level, maxLevel) * skill.critMultiplier;
}