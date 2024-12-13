using System;
using Godot;
using Godot.Collections;

public class MovePlayerTrigger : ActivateOtherTrigger
{
    [Export] private bool isTeleport;
    [Export] private bool changeMayMove;
    [Export] private float speed = 5;
    [Export] private NodePath pointPath;
    
    private Spatial point;
    private float speedCache;

    private Player player => Global.Get().player;

    public override void _Ready()
    {
        base._Ready();
        point = GetNode<Spatial>(pointPath);
        SetProcess(false);
    }

    public override void _Process(float delta)
    {
        if (player?.MovingController is NavigationMovingController navigationMovingController)
        {
            navigationMovingController.GoTo(point.GlobalTranslation);
        }
        else FinishMoving();
        
        base._Process(delta);
    }

    public override void _on_activate_trigger()
    {
        StartMoving();
        base._on_activate_trigger();
    }

    public override void SetActive(bool value)
    {
        base.SetActive(value);
        
        if (IsActive) _on_activate_trigger();
        else if (IsProcessing())
        {
            SetProcess(false);
            
            if (player == null) return;
            
            player.BaseSpeed = speedCache;
            player.Disconnect(nameof(Character.IsCame), this, nameof(FinishMoving));
            player.MovingController.SetProcess(false);
            if (player.MovingController is NavigationMovingController navigationMovingController)
            {
                navigationMovingController.Stop();
            }
        }
    }

    private void StartMoving()
    {
        if (IsProcessing()) return;
        if (player == null) return;
        
        if (isTeleport)
        {
            player.GlobalTranslation = point.GlobalTranslation;
            player.GlobalRotation = point.GlobalRotation;
        }
        else
        {
            if (changeMayMove)
            {
                player.SetTotalMayMove(false);
            }
            
            speedCache = player.BaseSpeed;
            player.BaseSpeed = speed;
            player.Connect(nameof(Character.IsCame), this, nameof(FinishMoving));
            player.MovingController.SetProcess(true);
            SetProcess(true);
        }
    }

    private void FinishMoving()
    {
        SetProcess(false);
        
        if (player == null) return;
        
        player.MovingController.SetProcess(false);
        player.Disconnect(nameof(Character.IsCame), this, nameof(FinishMoving));
        player.GlobalRotation = point.GlobalRotation;

        if (changeMayMove)
        {
            player.SetTotalMayMove(true);
        }
        
        player.BaseSpeed = speedCache;
        DeleteTrigger();
    }

    public override Dictionary GetSaveData()
    {
        var saveData = base.GetSaveData();
        saveData["isProcessing"] = IsProcessing();
        return saveData;
    }

    public override void LoadData(Dictionary data)
    {
        base.LoadData(data);

        if (!data.Contains("isProcessing")) return;
        
        var isProcessing = Convert.ToBoolean(data["isProcessing"]);
        SetActive(isProcessing);
    }

    protected override void DeleteTrigger()
    {
        if (player.GlobalTranslation != point.GlobalTranslation) return;
        if (player.GlobalRotation != point.GlobalRotation) return;
        base.DeleteTrigger();
    }
}