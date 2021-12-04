using Godot;
using Godot.Collections;

public abstract class TrainingTriggerWithButton: TriggerBase
{
    [Export] public string HintCode = "clickButton";
    [Export] public AudioStreamSample clickButtonSound;
    [Export] public NodePath audiPath;
    
    protected AudioStreamPlayer3D audi;
    protected static Player player => Global.Get().player;
    
    protected bool checkButton;
    protected bool playerHere;

    public override void _Ready()
    {
        base._Ready();
        SetProcess(false);
        audi = GetNode<AudioStreamPlayer3D>(audiPath);
        audi.Stream = clickButtonSound;
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
    
    public void _on_body_entered(Node body)
    {
        if (!IsActive) return;
        if (!(body is Player)) return;
        playerHere = true;
        if (!checkButton) return;
        SetProcess(true);
    }
    
    public void _on_body_exited(Node body)
    {
        if (!IsActive) return;
        if (!(body is Player)) return;
        playerHere = false;
        if (!checkButton) return;
        player?.Camera.HideHint();
        SetProcess(false);
    }
    
    public override void _Process(float delta)
    {
        player.Camera.ShowHint(HintCode, false);

        if (!Input.IsActionJustPressed("use")) return;
        player?.Camera.HideHint();
        SetProcess(false);
        checkButton = false;
        audi.Stream = clickButtonSound;
        audi.Play();
        PressButton();
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