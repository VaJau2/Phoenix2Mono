using Godot;

public partial class AutosaveTrigger: TriggerBase
{
    private Messages messages;
    private SaveMenu saveMenu;
    
    public override async void _Ready()
    {
        messages = GetNode<Messages>("/root/Main/Scene/canvas/messages");
        saveMenu = GetNode<SaveMenu>("/root/Main/Menu/PauseMenu/Save");
        if (!IsActive) return;

        await ToSignal(GetTree(), "process_frame");
        OnActivateTrigger();
    }
    
    public override void SetActive(bool newActive)
    {
        base.SetActive(newActive);
        if (IsActive)
        {
            OnActivateTrigger();
        }
    }

    public override void OnActivateTrigger()
    {
        messages.ShowMessage("gameAutosaved");
        
        //добавляем триггер в удаленные объекты заранее, чтобы не сохранял второй раз
        Global.AddDeletedObject(Name);

        var saveName = Global.Get().autosaveName;
        saveMenu.SaveGame(saveName, GetTree());
        saveMenu.CreateTableLine(saveName);
        base.OnActivateTrigger();
    }
}