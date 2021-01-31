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

    public void OpenMenu(InventoryMode newMode = null) 
    {
        if (newMode != null) {
            mode = newMode;
        }
        mode.OpenMenu();
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
}