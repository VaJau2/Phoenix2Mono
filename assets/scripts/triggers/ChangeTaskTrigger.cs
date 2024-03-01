using Godot;

//Меняет код текущей задачи при активации
//если активен при старте сцены, меняет задачу сразу
partial class ChangeTaskTrigger: TriggerBase
{
    [Export] public string NewTaskCode;
    [Export] public bool showMessage = true;
    Messages messages;

    public override void _Ready()
    {
        if (IsQueuedForDeletion()) return;
        messages = GetNode<Messages>("/root/Main/Scene/canvas/messages");
        if (IsActive)
        {
            OnActivateTrigger();
        }
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
        messages.ChangeTaskCode(NewTaskCode, showMessage);
        base.OnActivateTrigger();
    }
    
    public void _on_body_entered(Node body)
    {
        if (!(body is Player)) return;
        OnActivateTrigger();
    }
}