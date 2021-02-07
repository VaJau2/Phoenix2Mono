using Godot;
using Godot.Collections;

//класс управляет инвентарным меню
//имеет несколько разных режимов работы
//режимы устанавливаются переменной mode
public class InventoryMenu : Control
{
    public InventoryMode mode;
    public bool isOpen = false;

    [Signal]
    public delegate void MenuIsClosed();

    public void SetTempButton(ItemIcon newButton, bool showInfo = true)
    {
        mode.SetTempButton(newButton, showInfo);
    }

    public void OpenMenu() 
    {
        mode.OpenMenu();
    }

    public void CloseMenu()
    {
        mode.CloseMenu();
    }

    public void ChangeMode(NewInventoryMode newMode = NewInventoryMode.Usual)
    {
        switch (newMode) {
            case NewInventoryMode.Usual:
                if (!(mode is UsualMode)) {
                    mode = new UsualMode(this);
                }
                break;
            case NewInventoryMode.Chest:
                if (!(mode is ChestMode)) {
                    mode = new ChestMode(this);
                }
                break;
            case NewInventoryMode.Trade:
                if (!(mode is TradeMode)) {
                    mode = new TradeMode(this);
                }
                break;
        }
    }
    
    public void LoadItemButtons(Array<string> newItems, Dictionary<string, int> ammo)
    {
        mode = (mode == null) ? new UsualMode(this) : mode;
        mode.LoadItemButtons(newItems, ammo);
    }

    public override void _Ready()
    {
        mode = new UsualMode(this);
    }

    public override void _Input(InputEvent @event)
    {
        if (mode != null) {
            mode.UpdateOpen(@event);
            mode.UpdateInput(@event);
        }
    }

    public void _on_modal_no_pressed()
    {
        mode._on_modal_no_pressed();
    }

    public void _on_modal_yes_pressed()
    {
        mode._on_modal_yes_pressed();
    }

    public void _on_count_value_changed(float newCount)
    {
        mode._on_count_value_changed(newCount);
    }

    public void _on_takeAll_pressed()
    {
        mode._on_takeAll_pressed();
    }
}

public enum NewInventoryMode
{
    Usual,
    Chest,
    Trade
}