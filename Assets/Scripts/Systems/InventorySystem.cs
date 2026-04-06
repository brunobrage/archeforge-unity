using System.Collections.Generic;
using UnityEngine;

public class InventoryItem
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string SpriteKey { get; set; }
    public int Quantity { get; set; }
    public int MaxStack { get; set; }

    public InventoryItem(string id, string name, string spriteKey, int quantity, int maxStack = 64)
    {
        Id = id;
        Name = name;
        SpriteKey = spriteKey;
        Quantity = quantity;
        MaxStack = maxStack;
    }
}

public class InventorySystem : MonoBehaviour
{
    private Dictionary<string, InventoryItem> items = new Dictionary<string, InventoryItem>();

    public void AddItem(string id, string name, string spriteKey, int quantity, int maxStack = 64)
    {
        if (items.ContainsKey(id))
        {
            items[id].Quantity += quantity;
            if (items[id].Quantity > items[id].MaxStack)
            {
                items[id].Quantity = items[id].MaxStack;
            }
        }
        else
        {
            items[id] = new InventoryItem(id, name, spriteKey, quantity, maxStack);
        }

        Debug.Log($"[Inventory] Added {quantity}x {name}. Total: {items[id].Quantity}");
    }

    public bool RemoveItem(string id, int quantity)
    {
        if (items.ContainsKey(id) && items[id].Quantity >= quantity)
        {
            items[id].Quantity -= quantity;
            if (items[id].Quantity <= 0)
            {
                items.Remove(id);
            }
            return true;
        }
        return false;
    }

    public bool HasItem(string id, int quantity)
    {
        return items.ContainsKey(id) && items[id].Quantity >= quantity;
    }

    public int GetItemCount(string id)
    {
        return items.ContainsKey(id) ? items[id].Quantity : 0;
    }

    public string GetInventoryText()
    {
        if (items.Count == 0)
            return "Inventory: Empty";

        string text = "Inventory: ";
        foreach (var item in items.Values)
        {
            text += $"{item.Name} x{item.Quantity}, ";
        }
        return text.TrimEnd(',', ' ');
    }

    public Dictionary<string, InventoryItem> GetAllItems()
    {
        return items;
    }
}
