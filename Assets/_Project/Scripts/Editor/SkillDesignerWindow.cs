#if UNITY_EDITOR
using UnityEngine;

public class SkillDesignerWindow : UnityEditor.EditorWindow
{
    // 当前编辑的技能
    SkillConfig skill;
    // 右侧预览参数
    CombatStats previewStats = new CombatStats();
    int level = 1, maxLevel = 10;

    // 批量平衡
    float batchMultiplier = 1.0f; // 同比缩放 baseDamage & atkScale

    [UnityEditor.MenuItem("Tools/Balance/Skill Damage Designer")]
    public static void Open()
    {
        var win = GetWindow<SkillDesignerWindow>(true, "Skill Damage Designer", true);
        win.minSize = new Vector2(820, 520);
    }

    void OnGUI()
    {
        UnityEditor.EditorGUILayout.LabelField("技能伤害编辑与预览", UnityEditor.EditorStyles.boldLabel);
        UnityEditor.EditorGUILayout.Space(6);

        using (new UnityEditor.EditorGUILayout.HorizontalScope())
        {
            skill = (SkillConfig)UnityEditor.EditorGUILayout.ObjectField("Skill", skill, typeof(SkillConfig), false);
            if (GUILayout.Button("新建", GUILayout.Width(80))) CreateNewSkill();
        }

        if (!skill)
        {
            UnityEditor.EditorGUILayout.HelpBox("请先选择或创建一个 SkillConfig", UnityEditor.MessageType.Info);
            return;
        }

        using (new UnityEditor.EditorGUILayout.HorizontalScope())
        {
            using (new UnityEditor.EditorGUILayout.VerticalScope("box", GUILayout.MinWidth(360)))
            {
                DrawSkillFields();
                UnityEditor.EditorGUILayout.Space(4);
                DrawBatchTools();
            }

            using (new UnityEditor.EditorGUILayout.VerticalScope("box"))
            {
                DrawPreviewPanel();
            }
        }
    }

    // 左侧：技能字段编辑
    void DrawSkillFields()
    {
        UnityEditor.EditorGUI.BeginChangeCheck();

        skill.skillId        = UnityEditor.EditorGUILayout.TextField("Skill ID",      skill.skillId);
        skill.displayName    = UnityEditor.EditorGUILayout.TextField("Display Name",  skill.displayName);
        skill.element        = (ElementType)UnityEditor.EditorGUILayout.EnumPopup("Element", skill.element);
        skill.baseDamage     = UnityEditor.EditorGUILayout.FloatField("Base Damage",  skill.baseDamage);
        skill.atkScale       = UnityEditor.EditorGUILayout.FloatField("ATK Scale",    skill.atkScale);
        skill.extraScale     = UnityEditor.EditorGUILayout.FloatField("Extra Scale",  skill.extraScale);
        skill.finalMultiplier= UnityEditor.EditorGUILayout.FloatField("Final Mult.",  skill.finalMultiplier);
        skill.critChance     = UnityEditor.EditorGUILayout.Slider("Crit Chance",      skill.critChance, 0f, 1f);
        skill.critMultiplier = Mathf.Max(1f, UnityEditor.EditorGUILayout.FloatField("Crit Mult", skill.critMultiplier));

        // 等级曲线：x:0..1, y:0..2 的视图窗口
        skill.levelCurve     = UnityEditor.EditorGUILayout.CurveField("Level Curve", skill.levelCurve, Color.green, new Rect(0, 0, 1, 2));

        // tags 用 SerializedObject 画，可增删
        var so = new UnityEditor.SerializedObject(skill);
        var propTags = so.FindProperty("tags");
        UnityEditor.EditorGUILayout.PropertyField(propTags, new GUIContent("Tags"), true);
        so.ApplyModifiedProperties();

        // 备注
        skill.note = UnityEditor.EditorGUILayout.TextArea(skill.note, GUILayout.MinHeight(50));

        if (UnityEditor.EditorGUI.EndChangeCheck())
        {
            UnityEditor.EditorUtility.SetDirty(skill);
        }
    }

    // 左下：批量平衡工具
    void DrawBatchTools()
    {
        UnityEditor.EditorGUILayout.LabelField("批量平衡", UnityEditor.EditorStyles.boldLabel);
        batchMultiplier = UnityEditor.EditorGUILayout.Slider("倍率", batchMultiplier, 0.5f, 2.0f);

        if (GUILayout.Button("对选中的 SkillConfig 批量应用倍率（BaseDamage & ATKScale）"))
        {
            var objs = UnityEditor.Selection.GetFiltered(typeof(SkillConfig), UnityEditor.SelectionMode.Assets);
            int n = 0;
            UnityEditor.Undo.IncrementCurrentGroup();
            var group = UnityEditor.Undo.GetCurrentGroup();

            foreach (var o in objs)
            {
                var s = (SkillConfig)o;
                if (!s) continue;
                n++;

                UnityEditor.Undo.RecordObject(s, "Batch Balance Skill");
                s.baseDamage *= batchMultiplier;
                s.atkScale   *= batchMultiplier;
                UnityEditor.EditorUtility.SetDirty(s);
            }

            UnityEditor.Undo.CollapseUndoOperations(group);
            UnityEditor.AssetDatabase.SaveAssets();
            UnityEditor.EditorUtility.DisplayDialog("完成", $"批量调整 {n} 个技能", "好的");
        }
    }

    // 右侧：预览参数 + 结果
    void DrawPreviewPanel()
    {
        UnityEditor.EditorGUILayout.LabelField("预览参数", UnityEditor.EditorStyles.boldLabel);

        using (new UnityEditor.EditorGUILayout.VerticalScope("box"))
        {
            previewStats.ATK                = UnityEditor.EditorGUILayout.FloatField("ATK", previewStats.ATK);
            previewStats.damageBonus        = UnityEditor.EditorGUILayout.Slider("Damage Bonus",  previewStats.damageBonus, -0.5f, 1.0f);
            previewStats.elementBonus       = UnityEditor.EditorGUILayout.Slider("Element Bonus", previewStats.elementBonus, -0.5f, 1.0f);
            previewStats.targetResist       = UnityEditor.EditorGUILayout.Slider("Target Resist", previewStats.targetResist, -0.8f, 0.9f);
            previewStats.defenseMitigation  = UnityEditor.EditorGUILayout.Slider("Defense Mitig", previewStats.defenseMitigation, 0f, 0.8f);

            using (new UnityEditor.EditorGUILayout.HorizontalScope())
            {
                level    = UnityEditor.EditorGUILayout.IntSlider("Level", level, 1, Mathf.Max(1, maxLevel));
                maxLevel = Mathf.Max(1, UnityEditor.EditorGUILayout.IntField("Max", maxLevel));
            }
        }

        float expected = DamageCalculator.EvaluateExpected (skill, previewStats, level, maxLevel);
        float nonCrit  = DamageCalculator.EvaluateNonCrit   (skill, previewStats, level, maxLevel);
        float crit     = DamageCalculator.EvaluateCrit      (skill, previewStats, level, maxLevel);

        UnityEditor.EditorGUILayout.Space(4);
        using (new UnityEditor.EditorGUILayout.VerticalScope("box"))
        {
            UnityEditor.EditorGUILayout.LabelField("结果预览", UnityEditor.EditorStyles.boldLabel);
            UnityEditor.EditorGUILayout.LabelField($"Non-Crit: {nonCrit:F1}");
            UnityEditor.EditorGUILayout.LabelField($"Crit:     {crit:F1}");
            UnityEditor.EditorGUILayout.LabelField($"Expected: {expected:F1}  (pCrit={skill.critChance:P0})");
        }

        // 简易等级曲线示意图
        int h = 160;
        var r = UnityEditor.EditorGUILayout.GetControlRect(false, h);
        UnityEditor.Handles.DrawSolidRectangleWithOutline(r, new Color(0,0,0,0.04f), new Color(0,0,0,0.2f));

        if (Event.current.type == EventType.Repaint)
        {
            int steps = Mathf.Max(2, maxLevel);
            Vector3 prev = Vector3.zero; bool hasPrev = false;

            for (int i = 1; i <= steps; i++)
            {
                float t = (steps == 1) ? 1f : (i - 1f) / (steps - 1f);
                float y = skill.levelCurve.Evaluate(t);                   // 0..2
                float xPix = Mathf.Lerp(r.xMin, r.xMax, t);
                float yPix = Mathf.Lerp(r.yMax, r.yMin, Mathf.InverseLerp(0, 2f, y));
                var cur = new Vector3(xPix, yPix, 0);

                if (hasPrev) UnityEditor.Handles.DrawLine(prev, cur);
                prev = cur; hasPrev = true;
            }
        }
    }

    // 新建一个 SkillConfig 资产
    void CreateNewSkill()
    {
        var path = UnityEditor.EditorUtility.SaveFilePanelInProject("Create SkillConfig", "Skill_New", "asset", "选择保存路径");
        if (string.IsNullOrEmpty(path)) return;

        var so = ScriptableObject.CreateInstance<SkillConfig>();
        UnityEditor.AssetDatabase.CreateAsset(so, path);
        UnityEditor.AssetDatabase.SaveAssets();

        UnityEditor.Selection.activeObject = so;
        skill = so;
    }
}
#endif
