using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]
public class PlayerInventory : MonoBehaviour
{
    [SerializeField, Min(1)] int capacity = 5;
    [SerializeField] List<ItemDefinition> items = new();

    [Header("Events")]
    public UnityEvent OnInventoryChanged;

    public int Capacity => capacity;
    public IReadOnlyList<ItemDefinition> Items => items;

    public bool TryAdd(ItemDefinition item)
    {
        if (!item) return false;
        if (items.Count >= capacity) return false;

        items.Add(item);
        OnInventoryChanged?.Invoke();
        return true;
    }

    public bool TryRemove(ItemDefinition item)
    {
        if (!item) return false;
        bool removed = items.Remove(item);
        if (removed) OnInventoryChanged?.Invoke();
        return removed;
    }

    public bool TryRemoveAt(int slot)
    {
        if (slot < 0 || slot >= items.Count) return false;
        items.RemoveAt(slot);
        OnInventoryChanged?.Invoke();
        return true;
    }
}
