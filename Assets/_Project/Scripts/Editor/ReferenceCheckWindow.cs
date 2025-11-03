#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// 缺失引用检测器：扫描工程中的资源与场景，定位丢失引用
/// </summary>
public class MissingReferenceDetector : EditorWindow
{
    [System.Serializable]
    public class MissingRefInfo
    {
        public Object asset;
        public string assetPath;
        public string propertyPath;
        public string componentType;
        
        public MissingRefInfo(Object obj, string path, string prop, string type)
        {
            asset = obj;
            assetPath = path;
            propertyPath = prop;
            componentType = type;
        }
    }

    private List<MissingRefInfo> missingRefs = new List<MissingRefInfo>();
    private Vector2 scrollPosition;
    private bool scanPrefabs = true;
    private bool scanScenes = true;
    private bool scanScriptableObjects = true;
    private bool isScanning = false;
    private string searchFolder = "Assets";
    private int totalScanned = 0;
    private int totalMissing = 0;

    [MenuItem("Tools/Project/Missing Reference Detector")]
    public static void ShowWindow()
    {
        var window = GetWindow<MissingReferenceDetector>("Missing Ref Detector");
        window.minSize = new Vector2(600, 400);
        window.Show();
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Missing Reference 检测工具", EditorStyles.boldLabel);
        EditorGUILayout.Space(6);

        // 扫描选项
        using (new EditorGUILayout.VerticalScope("box"))
        {
            EditorGUILayout.LabelField("扫描选项", EditorStyles.boldLabel);
            
            using (new EditorGUILayout.HorizontalScope())
            {
                searchFolder = EditorGUILayout.TextField("扫描目录", searchFolder);
                if (GUILayout.Button("...", GUILayout.Width(30)))
                {
                    string path = EditorUtility.OpenFolderPanel("选择扫描目录", Application.dataPath, "");
                    if (!string.IsNullOrEmpty(path))
                    {
                        if (path.StartsWith(Application.dataPath))
                        {
                            searchFolder = "Assets" + path.Substring(Application.dataPath.Length);
                        }
                        else
                        {
                            EditorUtility.DisplayDialog("错误", "必须选择 Assets 目录下的文件夹", "确定");
                        }
                    }
                }
            }
            
            scanPrefabs = EditorGUILayout.ToggleLeft("扫描 Prefabs", scanPrefabs);
            scanScenes = EditorGUILayout.ToggleLeft("扫描 Scenes", scanScenes);
            scanScriptableObjects = EditorGUILayout.ToggleLeft("扫描 ScriptableObjects", scanScriptableObjects);
        }

        EditorGUILayout.Space(4);

        // 扫描按钮
        GUI.enabled = !isScanning;
        if (GUILayout.Button(isScanning ? "扫描中..." : "开始扫描", GUILayout.Height(30)))
        {
            ScanForMissingReferences();
        }
        GUI.enabled = true;

        EditorGUILayout.Space(6);

        // 结果摘要
        using (new EditorGUILayout.HorizontalScope("box"))
        {
            EditorGUILayout.LabelField($"扫描对象数: {totalScanned}", EditorStyles.miniLabel);
            EditorGUILayout.LabelField($"发现问题数: {totalMissing}", EditorStyles.miniLabel);
        }

        if (missingRefs.Count > 0)
        {
            EditorGUILayout.Space(4);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("清空列表"))
                {
                    missingRefs.Clear();
                    totalMissing = 0;
                }
                if (GUILayout.Button("导出报告"))
                {
                    ExportReport();
                }
            }
        }

        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("缺失引用列表:", EditorStyles.boldLabel);

        // 结果列表
        using (var scrollView = new EditorGUILayout.ScrollViewScope(scrollPosition))
        {
            scrollPosition = scrollView.scrollPosition;

            if (missingRefs.Count == 0)
            {
                EditorGUILayout.HelpBox("点击 '开始扫描' 来检测项目中的缺失引用", MessageType.Info);
            }
            else
            {
                foreach (var info in missingRefs)
                {
                    DrawMissingRefItem(info);
                }
            }
        }
    }

    private void DrawMissingRefItem(MissingRefInfo info)
    {
        using (new EditorGUILayout.VerticalScope("box"))
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.ObjectField(info.asset, typeof(Object), false, GUILayout.Width(200));
                
                using (new EditorGUILayout.VerticalScope())
                {
                    EditorGUILayout.LabelField($"路径: {info.assetPath}", EditorStyles.miniLabel);
                    EditorGUILayout.LabelField($"组件: {info.componentType}", EditorStyles.miniLabel);
                    EditorGUILayout.LabelField($"属性: {info.propertyPath}", EditorStyles.miniLabel);
                }
                
                if (GUILayout.Button("定位", GUILayout.Width(60)))
                {
                    Selection.activeObject = info.asset;
                    EditorGUIUtility.PingObject(info.asset);
                }
            }
        }
    }

    private void ScanForMissingReferences()
    {
        isScanning = true;
        missingRefs.Clear();
        totalScanned = 0;
        totalMissing = 0;

        try
        {
            if (scanPrefabs)
            {
                ScanAssets("t:Prefab");
            }

            if (scanScriptableObjects)
            {
                ScanAssets("t:ScriptableObject");
            }

            if (scanScenes)
            {
                ScanScenes();
            }

            Debug.Log($"<color=cyan>[Missing Ref Detector]</color> Scan completed. Scanned: {totalScanned}, Missing: {totalMissing}");
            
            if (totalMissing > 0)
            {
                EditorUtility.DisplayDialog("扫描完成", 
                    $"扫描了 {totalScanned} 个对象\n发现 {totalMissing} 个缺失引用", "确定");
            }
            else
            {
                EditorUtility.DisplayDialog("扫描完成", 
                    $"扫描了 {totalScanned} 个对象\n未发现缺失引用！", "确定");
            }
        }
        finally
        {
            isScanning = false;
            EditorUtility.ClearProgressBar();
            Repaint();
        }
    }

    private void ScanAssets(string filter)
    {
        string[] guids = AssetDatabase.FindAssets(filter, new[] { searchFolder });
        
        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            EditorUtility.DisplayProgressBar("扫描资源", $"检查: {path}", (float)i / guids.Length);
            
            Object asset = AssetDatabase.LoadAssetAtPath<Object>(path);
            if (asset != null)
            {
                totalScanned++;
                CheckForMissingReferences(asset, path);
            }
        }
    }

    private void ScanScenes()
    {
        string[] sceneGuids = AssetDatabase.FindAssets("t:Scene", new[] { searchFolder });
        
        // 记录当前场景，结束后还原
        string currentScene = EditorSceneManager.GetActiveScene().path;
        
        for (int i = 0; i < sceneGuids.Length; i++)
        {
            string scenePath = AssetDatabase.GUIDToAssetPath(sceneGuids[i]);
            EditorUtility.DisplayProgressBar("扫描场景", $"检查: {scenePath}", (float)i / sceneGuids.Length);
            
            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            GameObject[] rootObjects = scene.GetRootGameObjects();
            
            foreach (GameObject root in rootObjects)
            {
                CheckGameObjectHierarchy(root, scenePath);
            }
        }
        
        // 还原原始场景
        if (!string.IsNullOrEmpty(currentScene))
        {
            EditorSceneManager.OpenScene(currentScene, OpenSceneMode.Single);
        }
    }

    private void CheckGameObjectHierarchy(GameObject go, string assetPath)
    {
        totalScanned++;
        
        Component[] components = go.GetComponents<Component>();
        foreach (Component comp in components)
        {
            if (comp == null)
            {
                // 缺失脚本组件
                missingRefs.Add(new MissingRefInfo(go, assetPath, "Component", "Missing Script"));
                totalMissing++;
                continue;
            }
            
            CheckComponentForMissingRefs(comp, assetPath);
        }
        
        // 遍历子节点
        foreach (Transform child in go.transform)
        {
            CheckGameObjectHierarchy(child.gameObject, assetPath);
        }
    }

    private void CheckForMissingReferences(Object obj, string assetPath)
    {
        if (obj is GameObject go)
        {
            CheckGameObjectHierarchy(go, assetPath);
        }
        else
        {
            // ScriptableObject 与其他资源的序列化字段检测
            SerializedObject so = new SerializedObject(obj);
            SerializedProperty prop = so.GetIterator();
            
            while (prop.NextVisible(true))
            {
                if (prop.propertyType == SerializedPropertyType.ObjectReference)
                {
                    if (prop.objectReferenceValue == null && 
                        prop.objectReferenceInstanceIDValue != 0)
                    {
                        missingRefs.Add(new MissingRefInfo(
                            obj, 
                            assetPath, 
                            prop.propertyPath, 
                            obj.GetType().Name
                        ));
                        totalMissing++;
                    }
                }
            }
        }
    }

    private void CheckComponentForMissingRefs(Component comp, string assetPath)
    {
        SerializedObject so = new SerializedObject(comp);
        SerializedProperty prop = so.GetIterator();
        
        while (prop.NextVisible(true))
        {
            if (prop.propertyType == SerializedPropertyType.ObjectReference)
            {
                // 判断是否为缺失引用（object 为 null 但 instanceID 非 0）
                if (prop.objectReferenceValue == null && 
                    prop.objectReferenceInstanceIDValue != 0)
                {
                    missingRefs.Add(new MissingRefInfo(
                        comp.gameObject, 
                        assetPath, 
                        $"{comp.GetType().Name}.{prop.propertyPath}", 
                        comp.GetType().Name
                    ));
                    totalMissing++;
                }
            }
        }
    }

    private void ExportReport()
    {
        string path = EditorUtility.SaveFilePanel("导出报告", "", "MissingReferences.txt", "txt");
        if (string.IsNullOrEmpty(path)) return;

        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.AppendLine("=== Missing Reference Report ===");
        sb.AppendLine($"Generated: {System.DateTime.Now}");
        sb.AppendLine($"Total Scanned: {totalScanned}");
        sb.AppendLine($"Total Missing: {totalMissing}");
        sb.AppendLine();
        
        foreach (var info in missingRefs)
        {
            sb.AppendLine($"Asset: {info.assetPath}");
            sb.AppendLine($"  Component: {info.componentType}");
            sb.AppendLine($"  Property: {info.propertyPath}");
            sb.AppendLine();
        }
        
        System.IO.File.WriteAllText(path, sb.ToString());
        EditorUtility.DisplayDialog("导出成功", $"报告已保存至:\n{path}", "确定");
    }
}
#endif

