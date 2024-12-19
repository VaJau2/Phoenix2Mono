using Godot;

//Меняет код текущей задачи при активации
//если активен при старте сцены, меняет задачу сразу
class ChangeTaskTrigger: ActivateOtherTrigger
{
    [Export] private Type type;
    [Export] private string NewTaskCode;
    [Export] private bool showMessage = true;
    
    private Messages messages;
    
    private enum Type
    {
        New,
        Add,
        Done
    }
    
    public override void _Ready()
    {
        if (IsQueuedForDeletion()) return;
        
        base._Ready();
        messages = GetNode<Messages>("/root/Main/Scene/canvas/messages");
        
        if (IsActive) _on_activate_trigger();
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
        switch (type)
        {
            case Type.New:
                messages.NewTask(NewTaskCode, showMessage);
                break;
            
            case Type.Add:
                messages.AddTask(NewTaskCode, showMessage);
                break;
            
            case Type.Done:
                messages.DoneTask(NewTaskCode, showMessage);
                break;
        }
        
        base._on_activate_trigger();
    }
}