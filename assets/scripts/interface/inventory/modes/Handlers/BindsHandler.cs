using System;
using Godot;

public class BindsHandler
{
    public float useCooldown;
    
    public readonly InventoryMenu menu;
    public readonly BindsList bindsList;
    private ItemIcon tempButton => mode.tempButton;

    private readonly InventoryMode mode;

    public BindIcon TempDeletingIcon => bindsList.TempDeletingIcon;

    public BindsHandler(InventoryMenu menu, InventoryMode mode)
    {
        this.menu = menu;
        this.mode = mode;
        bindsList = menu.GetNode<BindsList>("/root/Main/Scene/canvas/binds");
    }

    public void UpdateUseCooldown(float delta)
    {
        if (useCooldown > 0)
        {
            useCooldown -= delta;
        }
    }
    
    public void ClearBind(ItemIcon button)
    {
        if (button.GetBindKey() == "") return;
        menu.bindedButtons.Remove(int.Parse(button.GetBindKey()));
        bindsList.RemoveIcon(button);
        button.SetBindKey(null);
    }
    
    public void BindTheSameItem(string bind, string itemCode)
    {
        if (string.IsNullOrEmpty(bind)) return;
        int bindKey = Convert.ToInt16(bind);
        var otherButton = mode.FindButtonWithItem(itemCode);
        if (otherButton == null) return;
        BindButtonWithKey(otherButton, bindKey);
    }
    
    public void BindHotkeys(ItemType itemType)
    {
        if (mode.isDragging) return;
        if (tempButton.myItemCode == null) return;
        if (!ItemIsBindable(itemType)) return;
        
        for (int i = 0; i < 10; i++)
        {
            if (!Input.IsKeyPressed(48 + i)) continue;
            BindButtonWithKey(tempButton, i);
        }
    }
    
    private static bool ItemIsBindable(ItemType itemType) 
    {
        return itemType == ItemType.weapon || itemType == ItemType.food || itemType == ItemType.meds;
    }
    
    private void BindButtonWithKey(ItemIcon button, int key)
    {
        //если нажали ту же кнопку на той же клавише
        if (button.GetBindKey() == key.ToString())
        {
            ClearBind(tempButton);
            return;
        }
            
        //если кнопка уже забиндена на какую-то клавишу
        if (menu.bindedButtons.Values.Contains(button))
        {
            ClearBind(button);
        }
            
        //если на кнопку забита другая клавиша
        if (menu.bindedButtons.Keys.Contains(key))
        {
            ClearBind(menu.bindedButtons[key]);
        }

        //бинд кнопки
        menu.bindedButtons[key] = button;
        button.SetBindKey(key.ToString());
        bindsList.AddIcon(button);
    }
}
