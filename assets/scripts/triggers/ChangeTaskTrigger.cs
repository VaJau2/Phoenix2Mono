using Godot;

//Меняет код текущей задачи при активации
//если активен при старте сцены, меняет задачу сразу
class ChangeTaskTrigger: ActivateOtherTrigger
{
    [Export] private ChangeMode changeMode;
    [Export] private string taskCode;
    [Export] private bool showMessage = true;
    [Export] private bool clearPreviousTasks = true;
    
    private Messages messages;
    
    private enum ChangeMode
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
        
        if (IsActive) _on_activate_trigger();
    }

    public override void _on_activate_trigger()
    {
        switch (changeMode)
        {
            case ChangeMode.New:
                messages.NewTask(taskCode, showMessage, clearPreviousTasks);
                break;
            
            case ChangeMode.Add:
                messages.AddTask(taskCode, showMessage);
                break;
            
            case ChangeMode.Done:
                messages.DoneTask(taskCode, showMessage);
                break;
        }
        
        base._on_activate_trigger();
    }
    
    public override void _on_body_entered(Node body)
    {
        if (body is Player)
        {
            SetActive(true);
        }
    }
}
