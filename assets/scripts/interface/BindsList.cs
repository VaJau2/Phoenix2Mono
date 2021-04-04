using System;
using Godot;
using System.Collections.Generic;
using System.Linq;

//класс, выводящий список биндов кнопок на канвасе
public class BindsList : Node
{
    private List<ItemIcon> bindIcons = new List<ItemIcon>();

    private PackedScene iconPrefab;

    public override void _Ready()
    {
        iconPrefab = GD.Load<PackedScene>("res://objects/interface/itemIcon.tscn");
    }

    //сортирует бинды в порядке возрастания клавиш
    private void SortBinds()
    {
        foreach (var itemIcon in bindIcons.Where(itemIcon => !IsInstanceValid(itemIcon)))
        {
            bindIcons.Remove(itemIcon);
            break;
        }
        
        bindIcons.Sort(delegate(ItemIcon x, ItemIcon y)
        {
            int bindX = Convert.ToInt16(x.GetBindKey());
            int bindY = Convert.ToInt16(y.GetBindKey());
            return bindX > bindY ? 1 : -1;
        });
        
        for (int i = 0; i < bindIcons.Count; i++)
        {
            MoveChild(bindIcons[i], i);
        }
    }

    //возвращеет копию иконки из массива биндов
    private ItemIcon GetIcon(ItemIcon sameIcon)
    {
        return bindIcons.FirstOrDefault(tempIcon => tempIcon.GetIcon() == sameIcon.GetIcon());
    }

    public void AddIcon(ItemIcon icon)
    {
        var sameIcon = GetIcon(icon);
        if (sameIcon != null)
        {
            //если той же кнопке ставится тот же бинд, игнорируем
            if (icon.GetBindKey() == sameIcon.GetBindKey())
            {
                return;
            }
        }
        
        if (!(iconPrefab.Instance() is ItemIcon newIcon)) return;
        MenuBase.LoadColorForChildren(newIcon);
        AddChild(newIcon);
        newIcon.SetBindKey(icon.GetBindKey());
        newIcon.SetIcon(icon.GetIcon());
        bindIcons.Add(newIcon);
        
        SortBinds();
    }

    public void RemoveIcon(ItemIcon icon)
    {
        foreach (var tempIcon 
            in bindIcons.Where(tempIcon => tempIcon.GetIcon() == icon.GetIcon()))
        {
            tempIcon.QueueFree();
            bindIcons.Remove(tempIcon);
            SortBinds();
            return;
        }
    }
}
