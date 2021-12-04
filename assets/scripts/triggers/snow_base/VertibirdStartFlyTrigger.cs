using System;
using Godot;
using Godot.Collections;

public class VertibirdStartFlyTrigger : TriggerBase
{
    private const float animTimer = 19f;
    [Export] private NodePath VertibirdPath;
    private Spatial Vertibird;
    private AnimationPlayer anim;
    private AudioStreamPlayer3D audi;
    private SavableTimers timers;
    private int step;

    public override void _Ready()
    {
        Vertibird = GetNode<Spatial>(VertibirdPath);
        anim = Vertibird.GetNode<AnimationPlayer>("anim");
        audi = Vertibird.GetNode<AudioStreamPlayer3D>("Box001/audi");
        timers = GetNode<SavableTimers>("/root/Main/Scene/timers");
        base._Ready();
    }

    public void _on_body_entered(Node body)
    {
        if (!(body is Player)) return;
        if (step > 0) return;
        step = 1;
        _on_activate_trigger();
    }

    public override Dictionary GetSaveData()
    {
        Dictionary saveData = base.GetSaveData();
        saveData["animTime"] = anim.CurrentAnimationPosition;
        saveData["audiTime"] = audi.GetPlaybackPosition();
        saveData["step"] = step;
        return saveData;
    }

    public override void LoadData(Dictionary data)
    {
        base.LoadData(data);
        step = Convert.ToInt32(data["step"]);
        if (step <= 1) return;
        
        float animTime = Convert.ToSingle(data["animTime"]);
        anim.Play("fly");
        anim.Seek(animTime);

        float audiTime = Convert.ToSingle(data["audiTime"]);
        audi.Play(audiTime);
        
        _on_activate_trigger();
    }

    public override async void _on_activate_trigger()
    {
        if (step == 1)
        {
            anim.Play("fly");
            audi.Play();
            step = 2;
        }
        
        while (timers.CheckTimer(Name + "_timer", animTimer))
        {
            await ToSignal(GetTree(), "idle_frame");
        }
    
        Vertibird.QueueFree();
        base._on_activate_trigger();
    }
}
