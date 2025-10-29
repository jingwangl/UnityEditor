#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;

public class BulkRenameWindow : UnityEditor.EditorWindow
{
    string prefix = "Item_";
    string suffix = "";
    int startIndex = 1;
    string searchFolder = "Assets";
    UnityEngine.Vector2 scroll;
    System.Collections.Generic.List<string> targets = new();

    [UnityEditor.MenuItem("Tools/Project/Bulk Rename")]
    public static void Open()
    {
        var win = UnityEditor.EditorWindow.GetWindow<BulkRenameWindow>(true, "Bulk Rename", true);
        win.minSize = new UnityEngine.Vector2(520, 380);
        win.RefreshTargets();
    }

    void OnGUI()
    {
        UnityEditor.EditorGUILayout.LabelField("批量重命名", UnityEditor.EditorStyles.boldLabel);
        UnityEditor.EditorGUILayout.Space(6);

        using (new UnityEditor.EditorGUILayout.VerticalScope("box"))
        {
            prefix = UnityEditor.EditorGUILayout.TextField("前缀", prefix);
            suffix = UnityEditor.EditorGUILayout.TextField("后缀", suffix);
            startIndex = Mathf.Max(0, UnityEditor.EditorGUILayout.IntField("起始编号", startIndex));
            using (new UnityEditor.EditorGUILayout.HorizontalScope())
            {
                searchFolder = UnityEditor.EditorGUILayout.TextField("扫描目录", searchFolder);
                if (UnityEngine.GUILayout.Button("…", UnityEngine.GUILayout.Width(28)))
                {
                    var p = UnityEditor.EditorUtility.OpenFolderPanel("选择目录(Assets内)", Application.dataPath, "");
                    if (!string.IsNullOrEmpty(p))
                    {
                        if (p.StartsWith(Application.dataPath))
                            searchFolder = "Assets" + p.Substring(Application.dataPath.Length);
                        else UnityEditor.EditorUtility.DisplayDialog("提示", "必须选择 Assets 内的目录", "好的");
                    }
                }
            }
            if (UnityEngine.GUILayout.Button("刷新列表")) RefreshTargets();
        }

        UnityEditor.EditorGUILayout.LabelField($"找到 {targets.Count} 个对象（名称将被改动）");
        using (var sv = new UnityEditor.EditorGUILayout.ScrollViewScope(scroll, GUILayout.Height(160)))
        {
            scroll = sv.scrollPosition;
            for (int i = 0; i < targets.Count; i++)
            {
                var newName = $"{prefix}{startIndex + i}{suffix}";
                UnityEditor.EditorGUILayout.BeginHorizontal();
                UnityEditor.EditorGUILayout.ObjectField(UnityEditor.AssetDatabase.LoadAssetAtPath<Object>(targets[i]), typeof(Object), false);
                UnityEditor.EditorGUILayout.LabelField($"→ {newName}");
                UnityEditor.EditorGUILayout.EndHorizontal();
            }
        }

        using (new UnityEditor.EditorGUILayout.HorizontalScope())
        {
            if (UnityEngine.GUILayout.Button("全选 ScriptableObject")) SelectByType<ScriptableObject>();
            if (UnityEngine.GUILayout.Button("全选 Sprite/Texture")) SelectByType<Texture2D>();
            if (UnityEngine.GUILayout.Button("仅保留选中")) KeepOnlySelection();
        }

        UnityEditor.EditorGUILayout.Space();
        if (UnityEngine.GUILayout.Button("执行重命名 (可 Undo)")) DoRename();
    }

    void RefreshTargets()
    {
        targets.Clear();
        var guids = UnityEditor.AssetDatabase.FindAssets("t:Object", new[] { searchFolder });
        foreach (var g in guids)
        {
            var path = UnityEditor.AssetDatabase.GUIDToAssetPath(g);
            if (UnityEditor.AssetDatabase.IsValidFolder(path)) continue;
            targets.Add(path);
        }
    }

    void SelectByType<T>() where T : UnityEngine.Object
    {
        targets.Clear();
        var guids = UnityEditor.AssetDatabase.FindAssets("t:" + typeof(T).Name, new[] { searchFolder });
        foreach (var g in guids)
        {
            var path = UnityEditor.AssetDatabase.GUIDToAssetPath(g);
            if (UnityEditor.AssetDatabase.IsValidFolder(path)) continue;
            targets.Add(path);
        }
    }

    void KeepOnlySelection()
    {
        targets.Clear();
        foreach (var obj in UnityEditor.Selection.objects)
        {
            var path = UnityEditor.AssetDatabase.GetAssetPath(obj);
            if (!string.IsNullOrEmpty(path)) targets.Add(path);
        }
    }

    void DoRename()
    {
        if (targets.Count == 0) { ShowNotification(new GUIContent("没有对象可改")); return; }
        UnityEditor.Undo.IncrementCurrentGroup();
        var group = UnityEditor.Undo.GetCurrentGroup();

        try
        {
            int total = targets.Count;
            for (int i = 0; i < total; i++)
            {
                UnityEditor.EditorUtility.DisplayProgressBar("批量重命名", $"Renaming {i + 1}/{total}", (float)(i + 1) / total);
                var path = targets[i];
                var obj = UnityEditor.AssetDatabase.LoadAssetAtPath<Object>(path);
                if (!obj) continue;
                var newName = $"{prefix}{startIndex + i}{suffix}";
                UnityEditor.Undo.RecordObject(obj, "Rename Asset");
                obj.name = newName;
                UnityEditor.AssetDatabase.RenameAsset(path, newName);
            }
        }
        finally
        {
            UnityEditor.EditorUtility.ClearProgressBar();
            UnityEditor.Undo.CollapseUndoOperations(group);
            UnityEditor.AssetDatabase.SaveAssets();
            UnityEditor.AssetDatabase.Refresh();
        }
    }
}
#endif
