using System;
using Godot;
using System.Collections.Generic;
using System.Linq;

//класс, выводящий список биндов кнопок на канвасе
public partial class BindsList : Node
{
    public BindIcon TempDeletingIcon;
    
    private readonly List<BindIcon> bindIcons = [];
    
    private PackedScene iconPrefab;

    public override void _Ready()
    {
        iconPrefab = GD.Load<PackedScene>("res://objects/interface/BindIcon.tscn");
    }

    //сортирует бинды в порядке возрастания клавиш
    private void SortBinds()
    {
        foreach (var itemIcon in bindIcons.Where(itemIcon => !IsInstanceValid(itemIcon)))
        {
            bindIcons.Remove(itemIcon);
            break;
        }
        
        bindIcons.Sort(delegate(BindIcon x, BindIcon y)
        {
            int bindX = Convert.ToInt16(x.GetBindKey());
            if (bindX == 0) bindX = 10;

            int bindY = Convert.ToInt16(y.GetBindKey());
            if (bindY == 0) bindY = 10;

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
        
        if (iconPrefab.Instantiate<BindIcon>() is not { } newIcon) return;
        MenuBase.LoadColorForChildren(newIcon);
        AddChild(newIcon);
        newIcon.SetBindKey(icon.GetBindKey());
        newIcon.SetIcon(icon.GetIcon());
        newIcon.SetItemCode(icon.myItemCode);
        bindIcons.Add(newIcon);
        
        SortBinds();
    }

    public async void RemoveIcon(ItemIcon icon)
    {
        foreach (var tempIcon in bindIcons.Where(tempIcon => tempIcon.GetIcon() == icon.GetIcon()))
        {
            TempDeletingIcon = tempIcon.IsNeedDelay() ? tempIcon : null;
            
            tempIcon.DeleteWithDelay();
            bindIcons.Remove(tempIcon);

            if (tempIcon.IsNeedDelay())
            {
                await ToSignal(tempIcon, nameof(BindIcon.IsDeleting));
            }
            
            SortBinds();
            return;
        }
    }
}
