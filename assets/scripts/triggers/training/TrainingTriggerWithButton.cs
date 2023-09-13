using Godot;
using Godot.Collections;

public abstract class TrainingTriggerWithButton: TriggerBase, IInteractable
{
    [Export] public string HintCode = "pressButton";
    [Export] public AudioStreamSample clickButtonSound;
    [Export] public NodePath audiPath;
    
    protected AudioStreamPlayer3D audi;
    protected static Player player => Global.Get().player;
    
    protected bool checkButton;
    protected bool trainingIsDone;

    public bool MayInteract => IsActive;
    public string InteractionHintCode => HintCode;

    public override void _Ready()
    {
        base._Ready();
        SetProcess(false);
        audi = GetNode<AudioStreamPlayer3D>(audiPath);
        audi.Stream = clickButtonSound;
    }

    public void Interact(PlayerCamera interactor)
    {
        checkButton = false;
        audi.Stream = clickButtonSound;
        audi.Play();
        PressButton();
    }
    
    public override void _on_activate_trigger()
    {
        if (!IsActive) return;
        if (!checkButton)
        {
            checkButton = true;
            return;
        }
        
        base._on_activate_trigger();
    }

    protected virtual void PressButton() {}

    public override Dictionary GetSaveData()
    {
        var data = base.GetSaveData();
        data["checkButton"] = checkButton;
        return data;
    }

    public override void LoadData(Dictionary data)
    {
        base.LoadData(data);
        checkButton = (bool)data["checkButton"];
    }
}