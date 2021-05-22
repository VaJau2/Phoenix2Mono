using Godot;

public class AutosaveTrigger: TriggerBase
{
    [Export] public string SaveName;
    private Messages messages;
    private SaveMenu saveMenu;
    
    public override async void _Ready()
    {
        messages = GetNode<Messages>("/root/Main/Scene/canvas/messages");
        saveMenu = GetNode<SaveMenu>("/root/Main/Menu/PauseMenu/Save");
        if (!IsActive) return;

        await ToSignal(GetTree(), "idle_frame");
        _on_activate_trigger();
    }
    
    public override void SetActive(bool newActive)
    {
        base.SetActive(newActive);
        if (IsActive)
        {
            _on_activate_trigger();
        }
    }

    public override void _on_activate_trigger()
    {
        messages.ShowMessage("gameAutosaved");
        
        //добавляем триггер в удаленные объекты заранее, чтобы не сохранял второй раз
        Global.AddDeletedObject(Name); 
        saveMenu.SaveGame(SaveName, GetTree());
        saveMenu.CreateTableLine(SaveName);
        base._on_activate_trigger();
    }
}