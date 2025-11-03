#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

/// <summary>
/// 技能设计与伤害预览工具
/// 功能：实时预览、批量调整、等级曲线可视化
/// </summary>
public class SkillDesignerWindow : EditorWindow
{
    #region Fields & Properties
    
    private SkillConfig currentSkill;
    private CombatStats previewStats = new CombatStats();
    private int previewLevel = 1;
    private int maxLevel = 10;
    private float batchMultiplier = 1.0f;
    
    private Vector2 scrollPos;
    private bool showBatchTools = false;
    
    // 预设模板（便于一键套用典型数值）
    private static readonly Dictionary<string, SkillPreset> presets = new Dictionary<string, SkillPreset>()
    {
        { "Basic Attack", new SkillPreset(50f, 1.2f, 0.15f, 1.5f, "基础攻击") },
        { "Heavy Strike", new SkillPreset(150f, 1.8f, 0.25f, 2.0f, "重击技能") },
        { "Ultimate", new SkillPreset(300f, 2.5f, 0.30f, 2.5f, "终极技能") },
        { "DOT Skill", new SkillPreset(30f, 0.8f, 0.10f, 1.2f, "持续伤害") }
    };
    
    // UI 颜色配置
    private static readonly Color headerColor = new Color(0.3f, 0.5f, 0.8f, 0.2f);
    private static readonly Color warningColor = new Color(1f, 0.8f, 0f, 0.3f);
    private static readonly Color successColor = new Color(0.3f, 0.8f, 0.3f, 0.3f);
    
    #endregion
    
    #region 菜单与初始化
    
    [MenuItem("Tools/Balance/Skill Damage Designer")]
    public static void ShowWindow()
    {
        var window = GetWindow<SkillDesignerWindow>("Skill Designer");
        window.minSize = new Vector2(900, 600);
        window.Show();
    }
    
    private void OnEnable()
    {
        // 初始化预览用的战斗属性
        previewStats.ATK = 300f;
    }
    
    #endregion
    
    #region 主界面
    
    private void OnGUI()
    {
        DrawHeader();
        DrawSkillSelector();
        
        if (!currentSkill)
        {
            DrawEmptyState();
            return;
        }
        
        using (var scroll = new EditorGUILayout.ScrollViewScope(scrollPos))
        {
            scrollPos = scroll.scrollPosition;
            
            using (new EditorGUILayout.HorizontalScope())
            {
                // 左侧：技能编辑
                using (new EditorGUILayout.VerticalScope(GUILayout.Width(position.width * 0.48f)))
                {
                    DrawSkillEditor();
                    DrawQuickActions();
                }
                
                GUILayout.Space(10);
                
                // 右侧：预览与工具
                using (new EditorGUILayout.VerticalScope())
                {
                    DrawPreviewPanel();
                    DrawAdvancedTools();
                }
            }
        }
    }
    
    #endregion
    
    #region 顶部与技能选择
    
    private void DrawHeader()
    {
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            GUILayout.Space(4);
            
            var titleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 16,
                alignment = TextAnchor.MiddleCenter
            };
            
            EditorGUILayout.LabelField("⚔️ Skill Damage Designer", titleStyle);
            
            var subtitleStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.gray }
            };
            
            EditorGUILayout.LabelField("Real-time damage calculation & balance tool", subtitleStyle);
            GUILayout.Space(4);
        }
        
        GUILayout.Space(6);
    }
    
    private void DrawSkillSelector()
    {
        using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
        {
            EditorGUILayout.LabelField("Current Skill:", GUILayout.Width(85));
            
            EditorGUI.BeginChangeCheck();
            currentSkill = (SkillConfig)EditorGUILayout.ObjectField(currentSkill, typeof(SkillConfig), false);
            if (EditorGUI.EndChangeCheck() && currentSkill != null)
            {
                Selection.activeObject = currentSkill;
            }
            
            if (GUILayout.Button("New", EditorStyles.toolbarButton, GUILayout.Width(50)))
                CreateNewSkill();
            
            if (GUILayout.Button("Duplicate", EditorStyles.toolbarButton, GUILayout.Width(70)))
                DuplicateCurrentSkill();
            
            GUI.enabled = currentSkill != null;
            if (GUILayout.Button("Ping", EditorStyles.toolbarButton, GUILayout.Width(50)))
                EditorGUIUtility.PingObject(currentSkill);
            GUI.enabled = true;
        }
        
        GUILayout.Space(4);
    }
    
    private void DrawEmptyState()
    {
        GUILayout.FlexibleSpace();
        
        using (new EditorGUILayout.VerticalScope())
        {
            GUILayout.FlexibleSpace();
            
            var style = new GUIStyle(EditorStyles.label)
            {
                fontSize = 14,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.gray }
            };
            
            EditorGUILayout.LabelField("No Skill Selected", style);
            EditorGUILayout.Space(10);
            
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Create New Skill", GUILayout.Width(150), GUILayout.Height(30)))
                    CreateNewSkill();
                GUILayout.FlexibleSpace();
            }
            
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Or select an existing SkillConfig asset", style);
            
            GUILayout.FlexibleSpace();
        }
        
        GUILayout.FlexibleSpace();
    }

    #endregion
    
    #region Skill Editor Panel
    
    private void DrawSkillEditor()
    {
        DrawSectionHeader("📝 Skill Properties");
        
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            // 使用 SerializedObject 以获得自动的撤销/重做支持
            var so = new SerializedObject(currentSkill);
            so.Update();
            
            EditorGUI.BeginChangeCheck();
            
            // 基本信息
            DrawSubSection("Basic Information");
            
            var propSkillId = so.FindProperty("skillId");
            var propDisplayName = so.FindProperty("displayName");
            var propElement = so.FindProperty("element");
            
            EditorGUILayout.PropertyField(propSkillId, new GUIContent("Skill ID"));
            
            // 校验 ID 是否为空
            if (string.IsNullOrEmpty(propSkillId.stringValue))
            {
                DrawWarningBox("⚠️ Skill ID is required!");
            }
            
            EditorGUILayout.PropertyField(propDisplayName, new GUIContent("Display Name"));
            EditorGUILayout.PropertyField(propElement, new GUIContent("Element Type"));
            
            // 元素颜色指示条
            DrawElementIndicator(currentSkill.element);
            
            EditorGUILayout.Space(8);
            
            // 伤害参数
            DrawSubSection("Damage Parameters");
            
            var propBaseDamage = so.FindProperty("baseDamage");
            var propAtkScale = so.FindProperty("atkScale");
            
            EditorGUILayout.PropertyField(propBaseDamage, new GUIContent("Base Damage"));
            propBaseDamage.floatValue = Mathf.Max(0, propBaseDamage.floatValue);
            
            EditorGUILayout.PropertyField(propAtkScale, new GUIContent("ATK Scale"));
            
            EditorGUILayout.Space(8);
            
            // 暴击相关
            DrawSubSection("Critical Hit");
            
            var propCritChance = so.FindProperty("critChance");
            var propCritMultiplier = so.FindProperty("critMultiplier");
            
            EditorGUILayout.Slider(propCritChance, 0f, 1f, new GUIContent("Crit Chance"));
            
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.PrefixLabel("Crit Rate");
                var style = new GUIStyle(EditorStyles.miniLabel) { normal = { textColor = Color.cyan } };
                EditorGUILayout.LabelField($"{propCritChance.floatValue:P0}", style);
            }
            
            EditorGUILayout.PropertyField(propCritMultiplier, new GUIContent("Crit Multiplier"));
            propCritMultiplier.floatValue = Mathf.Max(1f, propCritMultiplier.floatValue);
            
            EditorGUILayout.Space(8);
            
            // 等级曲线
            DrawSubSection("Level Scaling Curve");
            
            var propLevelCurve = so.FindProperty("levelCurve");
            EditorGUILayout.PropertyField(propLevelCurve, new GUIContent("Level Curve"), GUILayout.Height(60));
            
            EditorGUILayout.HelpBox("X: Normalized Level (0-1), Y: Scaling Factor", MessageType.None);
            
            if (EditorGUI.EndChangeCheck())
            {
                so.ApplyModifiedProperties();
            }
        }
    }
    
    private void DrawQuickActions()
    {
        GUILayout.Space(8);
        DrawSectionHeader("⚡ Quick Actions");
        
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            EditorGUILayout.LabelField("Skill Presets", EditorStyles.miniBoldLabel);
            
            using (new EditorGUILayout.HorizontalScope())
            {
                foreach (var preset in presets)
                {
                    if (GUILayout.Button(preset.Key, GUILayout.Height(25)))
                    {
                        ApplyPreset(preset.Value);
                    }
                }
            }
            
            GUILayout.Space(6);
            
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("📋 Copy Values", GUILayout.Height(25)))
                {
                    CopySkillToClipboard();
                }
                
                if (GUILayout.Button("🔄 Reset to Default", GUILayout.Height(25)))
                {
                    ResetSkillToDefault();
                }
            }
        }
    }

    #endregion
    
    #region Preview Panel
    
    private void DrawPreviewPanel()
    {
        DrawSectionHeader("📊 Damage Preview");
        
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            DrawSubSection("Preview Parameters");
            
            previewStats.ATK = EditorGUILayout.FloatField("ATK (Attack Power)", previewStats.ATK);
            
            EditorGUILayout.Space(6);
            
            using (new EditorGUILayout.HorizontalScope())
            {
                previewLevel = EditorGUILayout.IntSlider("Current Level", previewLevel, 1, Mathf.Max(1, maxLevel));
                EditorGUILayout.LabelField("/", GUILayout.Width(10));
                maxLevel = Mathf.Max(1, EditorGUILayout.IntField(maxLevel, GUILayout.Width(50)));
            }
        }
        
        // Calculate damage values
        float expected = DamageCalculator.EvaluateExpected(currentSkill, previewStats, previewLevel, maxLevel);
        float nonCrit = DamageCalculator.EvaluateNonCrit(currentSkill, previewStats, previewLevel, maxLevel);
        float crit = DamageCalculator.EvaluateCrit(currentSkill, previewStats, previewLevel, maxLevel);
        
        GUILayout.Space(8);
        DrawDamageResults(nonCrit, crit, expected);
        
        GUILayout.Space(8);
        DrawDamageCurveVisualization();
    }
    
    private void DrawDamageResults(float nonCrit, float crit, float expected)
    {
        DrawSectionHeader("💥 Damage Output");
        
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            // Calculate breakdown for display
            float lvl01 = maxLevel > 1 ? (previewLevel - 1f) / (maxLevel - 1f) : 1f;
            float levelMul = currentSkill.levelCurve.Evaluate(lvl01);
            float basePart = currentSkill.baseDamage + previewStats.ATK * currentSkill.atkScale;
            
            // Formula breakdown
            EditorGUILayout.LabelField("Formula:", EditorStyles.miniBoldLabel);
            
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                var miniStyle = new GUIStyle(EditorStyles.miniLabel) { normal = { textColor = Color.gray } };
                
                EditorGUILayout.LabelField($"Base = {currentSkill.baseDamage:F0} + ({previewStats.ATK:F0} × {currentSkill.atkScale:F2}) = {basePart:F1}", miniStyle);
                EditorGUILayout.LabelField($"Level Multiplier (Lv{previewLevel}/{maxLevel}) = ×{levelMul:F2}", miniStyle);
                EditorGUILayout.LabelField($"Final = {basePart:F1} × {levelMul:F2} = {nonCrit:F1}", miniStyle);
            }
            
            EditorGUILayout.Space(8);
            
            // 伤害结果
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("Non-Crit", GUILayout.Width(100));
                var style = new GUIStyle(EditorStyles.boldLabel) { fontSize = 14, normal = { textColor = Color.white } };
                EditorGUILayout.LabelField($"{nonCrit:F0}", style);
            }
            
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("Critical Hit", GUILayout.Width(100));
                var style = new GUIStyle(EditorStyles.boldLabel) { fontSize = 14, normal = { textColor = Color.yellow } };
                EditorGUILayout.LabelField($"{crit:F0}", style);
                EditorGUILayout.LabelField($"(×{currentSkill.critMultiplier:F1})", EditorStyles.miniLabel);
            }
            
            EditorGUILayout.Space(4);
            DrawSeparator();
            EditorGUILayout.Space(4);
            
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("Expected Avg", GUILayout.Width(100));
                var style = new GUIStyle(EditorStyles.boldLabel) 
                { 
                    fontSize = 16,
                    normal = { textColor = Color.cyan } 
                };
                EditorGUILayout.LabelField($"{expected:F0}", style);
            }
            
            EditorGUILayout.LabelField($"With {currentSkill.critChance:P0} crit rate", EditorStyles.miniLabel);
        }
    }
    
    private void DrawDamageCurveVisualization()
    {
        DrawSectionHeader("📈 Level Scaling");
        
        int graphHeight = 140;
        var rect = EditorGUILayout.GetControlRect(false, graphHeight);
        
        // 背景
        Handles.DrawSolidRectangleWithOutline(rect, new Color(0.1f, 0.1f, 0.1f, 0.2f), new Color(0.3f, 0.3f, 0.3f, 0.5f));
        
        if (Event.current.type == EventType.Repaint)
        {
            // 网格线
            Handles.color = new Color(1f, 1f, 1f, 0.1f);
            for (int i = 0; i <= 4; i++)
            {
                float y = Mathf.Lerp(rect.yMin, rect.yMax, i / 4f);
                Handles.DrawLine(new Vector3(rect.xMin, y), new Vector3(rect.xMax, y));
            }
            
            // 曲线
            int steps = Mathf.Max(2, maxLevel);
            Vector3 prevPoint = Vector3.zero;
            bool hasPrev = false;
            
            Handles.color = Color.green;
            for (int i = 1; i <= steps; i++)
            {
                float t = (steps == 1) ? 1f : (i - 1f) / (steps - 1f);
                float y = currentSkill.levelCurve.Evaluate(t);
                
                float xPos = Mathf.Lerp(rect.xMin + 10, rect.xMax - 10, t);
                float yPos = Mathf.Lerp(rect.yMax - 10, rect.yMin + 10, Mathf.InverseLerp(0, 2f, y));
                
                var currentPoint = new Vector3(xPos, yPos, 0);
                
                if (hasPrev)
                {
                    Handles.DrawLine(prevPoint, currentPoint, 2f);
                }
                
                // Draw point
                Handles.DrawSolidDisc(currentPoint, Vector3.forward, 3f);
                
                prevPoint = currentPoint;
                hasPrev = true;
            }
            
            // 当前等级指示
            float currentT = maxLevel > 1 ? (previewLevel - 1f) / (maxLevel - 1f) : 1f;
            float currentY = currentSkill.levelCurve.Evaluate(currentT);
            float currentX = Mathf.Lerp(rect.xMin + 10, rect.xMax - 10, currentT);
            float currentYPos = Mathf.Lerp(rect.yMax - 10, rect.yMin + 10, Mathf.InverseLerp(0, 2f, currentY));
            
            Handles.color = Color.cyan;
            Handles.DrawSolidDisc(new Vector3(currentX, currentYPos, 0), Vector3.forward, 4f);
        }
    }
    
    private void DrawAdvancedTools()
    {
        GUILayout.Space(8);
        
        showBatchTools = EditorGUILayout.Foldout(showBatchTools, "🛠️ 批量操作", true, EditorStyles.foldoutHeader);
        
        if (showBatchTools)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("批量平衡倍率", EditorStyles.miniBoldLabel);
                batchMultiplier = EditorGUILayout.Slider("倍率", batchMultiplier, 0.5f, 2.0f);
                
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField("缩放：", GUILayout.Width(80));
                    var style = new GUIStyle(EditorStyles.boldLabel);
                    style.normal.textColor = batchMultiplier > 1f ? Color.green : (batchMultiplier < 1f ? Color.red : Color.white);
                    EditorGUILayout.LabelField($"×{batchMultiplier:F2}", style);
                }
                
                GUILayout.Space(6);
                
                if (GUILayout.Button("应用到选中的技能", GUILayout.Height(30)))
                {
                    ApplyBatchBalance();
                }
                
                EditorGUILayout.HelpBox("在 Project 窗口多选 SkillConfig 资源，点击按钮对 BaseDamage 与 ATKScale 一并缩放。", MessageType.Info);
            }
        }
    }
    
    #endregion
    
    #region 工具方法
    
    private void DrawSectionHeader(string title)
    {
        GUILayout.Space(4);
        var style = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 12,
            normal = { textColor = new Color(0.7f, 0.9f, 1f) }
        };
        EditorGUILayout.LabelField(title, style);
        DrawSeparator();
        GUILayout.Space(4);
    }
    
    private void DrawSubSection(string title)
    {
        var style = new GUIStyle(EditorStyles.miniBoldLabel)
        {
            normal = { textColor = Color.gray }
        };
        EditorGUILayout.LabelField(title, style);
        GUILayout.Space(2);
    }
    
    private void DrawSeparator()
    {
        var rect = EditorGUILayout.GetControlRect(false, 1);
        EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 0.3f));
    }
    
    private void DrawWarningBox(string message)
    {
        var rect = EditorGUILayout.GetControlRect(false, 24);
        EditorGUI.DrawRect(rect, warningColor);
        EditorGUI.LabelField(rect, message, EditorStyles.miniLabel);
    }
    
    private void DrawElementIndicator(ElementType element)
    {
        var elementColors = new Dictionary<ElementType, Color>
        {
            { ElementType.Physical, new Color(0.7f, 0.7f, 0.7f) },
            { ElementType.Fire, new Color(1f, 0.3f, 0f) },
            { ElementType.Ice, new Color(0.3f, 0.8f, 1f) },
            { ElementType.Lightning, new Color(0.9f, 0.9f, 0.3f) },
            { ElementType.Wind, new Color(0.5f, 1f, 0.7f) }
        };
        
        if (elementColors.TryGetValue(element, out var color))
        {
            var rect = EditorGUILayout.GetControlRect(false, 4);
            EditorGUI.DrawRect(rect, color);
        }
    }
    
    #endregion
    
    #region Operations
    
    private void CreateNewSkill()
    {
        var path = EditorUtility.SaveFilePanelInProject("Create SkillConfig", "Skill_New", "asset", "Choose save location");
        if (string.IsNullOrEmpty(path)) return;
        
        var newSkill = ScriptableObject.CreateInstance<SkillConfig>();
        newSkill.skillId = "skill_" + System.Guid.NewGuid().ToString().Substring(0, 8);
        newSkill.displayName = "New Skill";
        
        AssetDatabase.CreateAsset(newSkill, path);
        AssetDatabase.SaveAssets();
        
        currentSkill = newSkill;
        Selection.activeObject = newSkill;
        
        Debug.Log($"<color=green>[Skill Designer]</color> Created new skill: {path}");
    }
    
    private void DuplicateCurrentSkill()
    {
        if (!currentSkill) return;
        
        var path = AssetDatabase.GetAssetPath(currentSkill);
        var newPath = AssetDatabase.GenerateUniqueAssetPath(path);
        
        AssetDatabase.CopyAsset(path, newPath);
        var duplicated = AssetDatabase.LoadAssetAtPath<SkillConfig>(newPath);
        
        currentSkill = duplicated;
        Selection.activeObject = duplicated;
        
        Debug.Log($"<color=green>[Skill Designer]</color> Duplicated skill: {newPath}");
    }
    
    private void ApplyPreset(SkillPreset preset)
    {
        if (!currentSkill) return;
        
        Undo.RecordObject(currentSkill, "Apply Preset");
        
        currentSkill.baseDamage = preset.baseDamage;
        currentSkill.atkScale = preset.atkScale;
        currentSkill.critChance = preset.critChance;
        currentSkill.critMultiplier = preset.critMultiplier;
        
        EditorUtility.SetDirty(currentSkill);
        
        Debug.Log($"<color=green>[Skill Designer]</color> Applied preset to {currentSkill.displayName}");
    }
    
    private void ResetSkillToDefault()
    {
        if (!currentSkill) return;
        
        if (EditorUtility.DisplayDialog("Reset Skill", 
            "Reset all values to default?", "Yes", "Cancel"))
        {
            Undo.RecordObject(currentSkill, "Reset Skill");
            
            currentSkill.baseDamage = 100f;
            currentSkill.atkScale = 1.0f;
            currentSkill.critChance = 0.2f;
            currentSkill.critMultiplier = 1.5f;
            currentSkill.levelCurve = AnimationCurve.Linear(0, 1, 1, 1.5f);
            
            EditorUtility.SetDirty(currentSkill);
        }
    }
    
    private void CopySkillToClipboard()
    {
        if (!currentSkill) return;
        
        var data = $"Skill: {currentSkill.displayName}\n" +
                   $"Base Damage: {currentSkill.baseDamage}\n" +
                   $"ATK Scale: {currentSkill.atkScale}\n" +
                   $"Crit: {currentSkill.critChance:P0} × {currentSkill.critMultiplier:F2}";
        
        EditorGUIUtility.systemCopyBuffer = data;
        Debug.Log($"<color=green>[Skill Designer]</color> Copied skill data to clipboard");
    }
    
    private void ApplyBatchBalance()
    {
        var skills = Selection.GetFiltered(typeof(SkillConfig), SelectionMode.Assets);
        if (skills.Length == 0)
        {
            EditorUtility.DisplayDialog("No Skills Selected", 
                "Please select SkillConfig assets in the Project window.", "OK");
            return;
        }
        
        Undo.IncrementCurrentGroup();
        var group = Undo.GetCurrentGroup();
        
        int count = 0;
        foreach (var obj in skills)
        {
            var skill = obj as SkillConfig;
            if (!skill) continue;
            
            Undo.RecordObject(skill, "Batch Balance");
            skill.baseDamage *= batchMultiplier;
            skill.atkScale *= batchMultiplier;
            EditorUtility.SetDirty(skill);
            count++;
        }
        
        Undo.CollapseUndoOperations(group);
        AssetDatabase.SaveAssets();
        
        EditorUtility.DisplayDialog("Batch Complete", 
            $"Applied ×{batchMultiplier:F2} multiplier to {count} skills", "OK");
        
        Debug.Log($"<color=green>[Skill Designer]</color> Batch balanced {count} skills with ×{batchMultiplier:F2}");
    }
    
    #endregion
}

#region Helper Classes

[System.Serializable]
public struct SkillPreset
{
    public float baseDamage;
    public float atkScale;
    public float critChance;
    public float critMultiplier;
    public string description;
    
    public SkillPreset(float dmg, float atk, float crit, float critMult, string desc)
    {
        baseDamage = dmg;
        atkScale = atk;
        critChance = crit;
        critMultiplier = critMult;
        description = desc;
    }
}

#endregion

#endif
