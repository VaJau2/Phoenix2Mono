using Godot;

//Меняет код текущей задачи при активации
//если активен при старте сцены, меняет задачу сразу
class ChangeTaskTrigger: TriggerBase
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
            _on_activate_trigger();
        }
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
        messages.ChangeTaskCode(NewTaskCode, showMessage);
        base._on_activate_trigger();
    }
}