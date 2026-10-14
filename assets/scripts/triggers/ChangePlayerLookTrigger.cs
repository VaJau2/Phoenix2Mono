using System;
using System.CodeDom.Compiler;
using Godot;
using Godot.Collections;

public partial class ChangePlayerLookTrigger : ActivateOtherTrigger
{ 
    [Export] private NodePath targetPath;
    private Node3D target;

    private Player player => Global.Get().player;
    
    public override void _Ready()
    {
        target = GetNode<Node3D>(targetPath);
        SetProcess(false);
    }

    public override void _Process(double delta)
    {
        if (player == null) return;
        
        var headRotation = GetHeadRotationToTarget();
        var animTree = player.GetNode<AnimationTree>("player_body/animTree");
        var headBlend = (Vector2) animTree.Get("parameters/BlendSpace2D/blend_position");
        headBlend = headBlend.MoveToward(headRotation, delta * 2);
        animTree.Set("parameters/BlendSpace2D/blend_position", headBlend);
    }
    
    public override void _on_activate_trigger()
    {
        SetActive(true);
        base._on_activate_trigger();
    }

    public override void SetActive(bool value)
    {
        base.SetActive(value);
        SetProcess(value);
        if (!value) DeleteTrigger();
    }
    
    private Vector2 GetHeadRotationToTarget()
    {
        var forward = -player.GlobalTransform.Basis.Z;
        var dir = GetDirToTarget();
        var angle = forward.AngleTo(dir);
        var diffY = target.GlobalPosition.Y - player.GlobalPosition.Y;
        
        if (player.GlobalTransform.Basis.X.Dot(dir) < 0)
        {
            angle = -angle;
        }
        
        return new Vector2(angle / 1.5f, diffY / 5);
    }
    
    private Vector3 GetDirToTarget()
    {
        var targetDirPos = target.GlobalPosition;
        targetDirPos.Y = player.GlobalPosition.Y;
        return targetDirPos - player.GlobalPosition;
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
        
        if (Convert.ToBoolean(data["isProcessing"]))
        {
            SetActive(true);
        }
    }

    protected override void DeleteTrigger()
    {
        if (IsProcessing()) return;
        base.DeleteTrigger();
    }
}
