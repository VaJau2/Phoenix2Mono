using System;
using Godot;

// Триггер мониторит текущее время проигрывания музыки
// И меняет его на другое, когда оно достигает нужного значения
public class LoopingMusicPartTrigger : TriggerBase
{
    [Export] public float CheckTime;
    [Export] public float ChangeTime;
    [Export] public NodePath audiPath;
    public AudioPlayerCommon audi;
    [Export] public bool isAudi3D;

    public override void _Ready()
    {
        base._Ready();
        audi = new AudioPlayerCommon(isAudi3D, audiPath, this);
        SetProcess(IsActive);
    }

    public override void _on_activate_trigger()
    {
        audi.PlayTime = ChangeTime;
        base._on_activate_trigger();
    }

    public override void _Process(float delta)
    {
        if (CheckTime == ChangeTime)
        {
            SetProcess(false);
        }
        
        //ждем указанной секунды
        if (Math.Abs(audi.PlayTime - CheckTime) > 0.05f)
        {
            return;
        }
        _on_activate_trigger();
    }
}
