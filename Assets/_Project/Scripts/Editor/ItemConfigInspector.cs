using UnityEditor;
using UnityEngine;


[CustomEditor(typeof(ItemConfig))]
public class ItemConfigInspector : Editor
{
SerializedProperty propItemId, propDisplayName, propRarity, propIcon, propPrice;
bool showValidate;


void OnEnable()
{
propItemId = serializedObject.FindProperty("itemId");
propDisplayName = serializedObject.FindProperty("displayName");
propRarity = serializedObject.FindProperty("rarity");
propIcon = serializedObject.FindProperty("icon");
propPrice = serializedObject.FindProperty("price");
}


public override void OnInspectorGUI()
{
serializedObject.Update();


EditorGUILayout.LabelField("Item Config", EditorStyles.boldLabel);
EditorGUILayout.Space(4);


using (new EditorGUILayout.VerticalScope("box"))
{
DrawRequiredText(propItemId, "Item ID");
EditorGUILayout.PropertyField(propDisplayName);
EditorGUILayout.PropertyField(propRarity);
EditorGUILayout.PropertyField(propIcon);
EditorGUILayout.PropertyField(propPrice);
}


EditorGUILayout.Space(6);
if (GUILayout.Button("Validate This Item"))
{
var msg = Validate((ItemConfig)target, out var ok);
if (ok) EditorUtility.DisplayDialog("Validate", "OK: " + msg, "Close");
else EditorUtility.DisplayDialog("Validate", "ERROR: " + msg, "Close");
}


showValidate = EditorGUILayout.Foldout(showValidate, "Live Validation");
if (showValidate)
{
var message = Validate((ItemConfig)target, out var ok);
var type = ok ? MessageType.Info : MessageType.Error;
EditorGUILayout.HelpBox(message, type);
}


serializedObject.ApplyModifiedProperties();
}


void DrawRequiredText(SerializedProperty prop, string label)
{
using (new EditorGUILayout.HorizontalScope())
{
var wasEmpty = string.IsNullOrWhiteSpace(prop.stringValue);
var bg = GUI.backgroundColor;
if (wasEmpty) GUI.backgroundColor = new Color(1f, 0.8f, 0.8f);
EditorGUILayout.PropertyField(prop, new GUIContent(label));
GUI.backgroundColor = bg;
}
}


string Validate(ItemConfig cfg, out bool ok)
{
if (string.IsNullOrWhiteSpace(cfg.itemId)) { ok = false; return "itemId 必填"; }
if (cfg.price < 0) { ok = false; return "price 不能为负"; }
if (!cfg.icon) { ok = false; return "icon 未设置（建议设置 256x256 Sprite）"; }
ok = true; return "所有字段检查通过";
}
}