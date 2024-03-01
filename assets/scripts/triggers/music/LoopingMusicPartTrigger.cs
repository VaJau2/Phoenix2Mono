using System;
using Godot;

// Триггер мониторит текущее время проигрывания музыки
// И меняет его на другое, когда оно достигает нужного значения
public partial class LoopingMusicPartTrigger : TriggerBase
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

    public override void OnActivateTrigger()
    {
        audi.PlayTime = ChangeTime;
        base.OnActivateTrigger();
    }

    public override void _Process(double delta)
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
        OnActivateTrigger();
    }
}
