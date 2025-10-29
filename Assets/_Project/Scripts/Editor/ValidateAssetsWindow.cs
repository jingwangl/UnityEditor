using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public class ValidateAssetsWindow : EditorWindow
{
    class Issue { public string path; public string message; public string fix; }

    List<Issue> issues = new();
    Vector2 scroll;
    string searchFolder = "Assets";

    [MenuItem("Tools/Project/Validate Assets")]
    public static void Open()
    {
        var win = GetWindow<ValidateAssetsWindow>(true, "Validate Assets", true);
        win.minSize = new Vector2(640, 420);
    }

    void OnGUI()
    {
        EditorGUILayout.LabelField("贴图导入设置校验", EditorStyles.boldLabel);
        using (new EditorGUILayout.HorizontalScope())
        {
            searchFolder = EditorGUILayout.TextField("扫描目录", searchFolder);
            if (GUILayout.Button("扫描")) Scan();
            if (GUILayout.Button("修复全部")) FixAll();
            if (GUILayout.Button("导出 JSON 报告")) ExportJson();
        }

        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField($"发现问题：{issues.Count} 条");
        using (var sv = new EditorGUILayout.ScrollViewScope(scroll))
        {
            scroll = sv.scrollPosition;
            foreach (var it in issues)
            {
                using (new EditorGUILayout.VerticalScope("box"))
                {
                    EditorGUILayout.ObjectField(AssetDatabase.LoadAssetAtPath<Object>(it.path), typeof(Object), false);
                    EditorGUILayout.LabelField(it.message);
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        GUILayout.FlexibleSpace();
                        if (GUILayout.Button("修复")) FixOne(it);
                    }
                }
            }
        }
    }

    void Scan()
    {
        issues.Clear();
        var guids = AssetDatabase.FindAssets("t:Texture2D", new[] { searchFolder });
        int total = guids.Length;
        for (int i = 0; i < total; i++)
        {
            EditorUtility.DisplayProgressBar("扫描贴图", $"{i+1}/{total}", (float)(i+1)/total);
            var path = AssetDatabase.GUIDToAssetPath(guids[i]);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            if (importer == null) continue;

            // 规则示例
            if (importer.maxTextureSize > 1024)
                AddIssue(path, $"Max Size 太大：{importer.maxTextureSize}", "设为 ≤ 1024");
            if (importer.textureCompression == TextureImporterCompression.Uncompressed)
                AddIssue(path, "未压缩", "改为 Compressed");
            if (!importer.sRGBTexture)
                AddIssue(path, "sRGB 关闭", "开启 sRGB");
        }
        EditorUtility.ClearProgressBar();
    }

    void AddIssue(string path, string msg, string fix)
    {
        issues.Add(new Issue { path = path, message = msg, fix = fix });
    }

    void FixOne(Issue it)
    {
        var importer = (TextureImporter)AssetImporter.GetAtPath(it.path);
        if (importer == null) return;
        bool changed = false;

        if (importer.maxTextureSize > 1024) { importer.maxTextureSize = 1024; changed = true; }
        if (importer.textureCompression == TextureImporterCompression.Uncompressed) { importer.textureCompression = TextureImporterCompression.Compressed; changed = true; }
        if (!importer.sRGBTexture) { importer.sRGBTexture = true; changed = true; }

        if (changed)
        {
            AssetDatabase.StartAssetEditing();
            try { importer.SaveAndReimport(); }
            finally { AssetDatabase.StopAssetEditing(); }
        }
        Scan(); // 重新扫描刷新列表
    }

    void FixAll()
    {
        int total = issues.Count;
        for (int i = 0; i < total; i++)
        {
            EditorUtility.DisplayProgressBar("修复中", $"{i+1}/{total}", (float)(i+1)/total);
            FixOne(issues[i]);
        }
        EditorUtility.ClearProgressBar();
        Scan();
    }

    void ExportJson()
    {
        var list = new List<Dictionary<string, string>>();
        foreach (var it in issues)
        {
            list.Add(new Dictionary<string, string>
            {
                {"path", it.path},
                {"message", it.message},
                {"suggestedFix", it.fix}
            });
        }
        var json = JsonUtility.ToJson(new Wrapper { items = list }, true);
        var dir = "Assets/_Project/Reports";
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
        var file = Path.Combine(dir, $"validate_{System.DateTime.Now:yyyyMMdd_HHmmss}.json");
        File.WriteAllText(file, json);
        AssetDatabase.ImportAsset(file);
        EditorUtility.RevealInFinder(file);
        EditorUtility.DisplayDialog("导出完成", file, "好的");
    }

    [System.Serializable]
    class Wrapper { public List<Dictionary<string, string>> items; }
}