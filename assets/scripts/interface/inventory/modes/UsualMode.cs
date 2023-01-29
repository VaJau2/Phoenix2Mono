using Godot;
using Object = Godot.Object;

public class UsualMode: InventoryMode 
{
    private Control wearBack;

    private bool countClickTimer;
    private float clickTimer;

    public UsualMode (InventoryMenu menu)
    : base(menu)
    {
        wearBack = back.GetNode<Control>("wearBack");
    }

    public void SetUseBindsCooldown(float cooldown)
    {
        bindsHandler.useCooldown = cooldown;
    }

    public override void OpenMenu()
    {
        base.OpenMenu();
        wearBack.Visible = true;
    }

    public override void CloseMenu()
    {
        CloseModal();
        wearBack.Visible = false;
        base.CloseMenu();
    }

    private bool IsUnwearingItem(string itemType)
    {
        return (itemType == "weapon" && tempButton == useHandler.weaponButton)
        || (itemType == "armor" && tempButton == useHandler.armorButton)
        || (itemType == "artifact" && tempButton == useHandler.artifactButton);
    }

    protected override void CheckDragItem()
    {
        string itemType = tempItemData["type"].ToString();
        
        switch (itemType)
        {
            case "weapon" when CheckMouseInButton(useHandler.weaponButton):
                useHandler.WearTempItem(useHandler.weaponButton); return;
            case "armor" when CheckMouseInButton(useHandler.armorButton):
                useHandler.WearTempItem(useHandler.armorButton); return;
            case "artifact" when CheckMouseInButton(useHandler.artifactButton):
                useHandler.WearTempItem(useHandler.artifactButton); return;
        }

        foreach (var otherButton in itemButtons) 
        {
            var buttonControl = (Control) otherButton;
            if (tempButton == otherButton || !CheckMouseInButton(buttonControl)) continue;
            if (IsUnwearingItem(itemType)) 
            {
                if (!useHandler.CanTakeItemOff()) return;
                inventory.UnwearItem(tempButton.myItemCode);
            }

            ChangeItemButtons(tempButton, otherButton);
            SetTempButton(otherButton, false);
            dragIcon.Texture = null;
        }
    }

    private void UseHotkeys() 
    {
        if (bindsHandler.useCooldown > 0) return;

        for (var i = 0; i < 10; i++)
        {
            if (!Input.IsKeyPressed(48 + i) || !menu.bindedButtons.Keys.Contains(i)) continue;
            SetTempButton(menu.bindedButtons[i], false);
            useHandler.UseTempItem();
        }
    }

    private void CheckAutoheal()
    {
        if (!Object.IsInstanceValid(player)) return;
        if (!Input.IsActionJustPressed("autoheal") || !player.MayMove) return;
        if (player.Health == player.HealthMax) 
        {
            inventory.ItemsMessage("youAreHealthy");
            return;
        }

        foreach (var newTempButton in itemButtons)
        {
            if (newTempButton.myItemCode == null) continue;
            SetTempButton(newTempButton);
            if (tempButton.myItemCode != "heal-potion" && tempItemData["type"].ToString() != "food") continue;
            useHandler.UseTempItem();
            return;
        }
        inventory.ItemsMessage("cantFindHeal");
        CheckTempIcon();
    }

    public override void Process(float delta)
    {
        countClickTimer = false;
        if (menu.isOpen && tempButton != null)
        {
            if (Input.IsActionPressed("ui_click"))
            {
                countClickTimer = true;
            }
        }

        if (countClickTimer)
        {
            if (clickTimer < 0.5f)
            {
                clickTimer += delta;
            }
        }
        else
        {
            clickTimer = 0;
        }

        bindsHandler.UpdateUseCooldown(delta);
    }

    public override void UpdateInput(InputEvent @event)
    {
        if (menu.isOpen && tempButton != null) 
        {
            if (UpdateDragging(@event)) return;

            if (Input.IsMouseButtonPressed(2) && !isDragging && tempButton != null) 
            {
                useHandler.DropTempItem();
            }

            if (@event is InputEventKey) 
            {
                bindsHandler.BindHotkeys(tempItemData["type"].ToString());
            }
        }
        
        if (@event is InputEventKey && tempButton == null) 
        {
            UseHotkeys();
            CheckAutoheal();
        }
    }
}
