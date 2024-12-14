using Godot;

// Объект, который пропадает при интеракции
public class InteractableHoldDisappearFurn : 
    StaticBody, 
    IInteractable, 
    IInteractableHold, 
    IInteractableHoldSound,
    IInteractableUseSound
{
    [Export] private string interactionHintCode = "use";

    [Export] private float holdingAnimSpeed = 2;
    
    [Export] private AudioStream holdingSound;
    
    [Export] private AudioStream useSound;
    
    public bool MayInteract => true;
    public string InteractionHintCode => interactionHintCode;

    public float HoldingAnimSpeed => holdingAnimSpeed;
    
    public AudioStream HoldingSound => holdingSound;
    public AudioStream UseSound => useSound;

    public void Interact(PlayerCamera interactor)
    {
        QueueFree();
    }
}
