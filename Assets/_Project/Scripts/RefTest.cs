using UnityEngine;

/// <summary>
/// 用于缺失引用检测器的简单测试脚本
/// </summary>
public class RefTest : MonoBehaviour
{
    [Header("GameObject 引用")]
    public GameObject targetObject;
    public Transform targetTransform;
    
    [Header("资源引用（Sprite/Material/Audio）")]
    public Sprite icon;
    public Material material;
    public AudioClip sound;
    
    [Header("ScriptableObject 引用")]
    public ScriptableObject data;
}
