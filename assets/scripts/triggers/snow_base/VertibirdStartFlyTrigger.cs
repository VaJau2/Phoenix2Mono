using System;
using Godot;
using Godot.Collections;

public class VertibirdStartFlyTrigger : TriggerBase
{
    private float animTimer = 19f;
    [Export] private NodePath VertibirdPath;
    private Spatial Vertibird;
    private AnimationPlayer anim;
    private AudioStreamPlayer3D audi;
    private int step;

    public override void _Ready()
    {
        Vertibird = GetNode<Spatial>(VertibirdPath);
        anim = Vertibird.GetNode<AnimationPlayer>("anim");
        audi = Vertibird.GetNode<AudioStreamPlayer3D>("Box001/audi");
        SetProcess(false);
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
        saveData["animTimer"] = animTimer;
        saveData["animTime"] = anim.IsPlaying() ? anim.CurrentAnimationPosition : 0;
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

        if (data.Contains("animTimer"))
        {
            animTimer = Convert.ToSingle(data["animTimer"]);
        }
        
        _on_activate_trigger();
    }

    public override void _on_activate_trigger()
    {
        if (step == 1)
        {
            anim.Play("fly");
            audi.Play();
            step = 2;
        }
        
        SetProcess(true);
    }

    public override void _Process(float delta)
    {
        if (animTimer > 0)
        {
            animTimer -= delta;
        }
        else
        {
            Vertibird.QueueFree();
            base._on_activate_trigger();
        }
    }
}
