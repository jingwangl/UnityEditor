using UnityEngine;
public enum Rarity { Common, Rare, Epic, Legendary }
[CreateAssetMenu(fileName = "ItemConfig", menuName = "Scriptable Objects/ItemConfig")]
public class ItemConfig : ScriptableObject
{
    [Header("Base")] public string itemId; // 唯一 ID（必填）
    public string displayName = "New Item"; // 展示名
    public Rarity rarity = Rarity.Common; // 稀有度


    [Header("Visual")] public Sprite icon; // 图标（建议 256x256）


    [Header("Economy")][Min(0)] public int price; // 价格 >= 0
}
