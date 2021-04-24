using Godot;

//Меняет код текущей задачи при активации
//если активен при старте сцены, меняет задачу сразу
class ChangeTaskTrigger: TriggerBase
{
    [Export] public string NewTaskCode;
    Messages messages;

    public override void _Ready()
    {
        messages = GetNode<Messages>("/root/Main/Scene/canvas/messages");
        CheckChange();
    }

    public override void SetActive(bool newActive)
    {
        base.SetActive(newActive);
        CheckChange();
    }

    private void CheckChange()
    {
        if (IsActive)
        {
            messages.currentTaskLink = NewTaskCode;
            if (DeleteAfterTrigger)
            {
                QueueFree();
            }
        }
    }
}