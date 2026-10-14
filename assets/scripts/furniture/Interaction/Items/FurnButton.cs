using Godot;

public partial class FurnButton : ActivateOtherTrigger, IInteractable
{
    public bool MayInteract { get; } = true;
    public string InteractionHintCode { get; } = "pressButton";
    public void Interact(PlayerCamera interactor)
    {
        _on_activate_trigger();
    }
}
