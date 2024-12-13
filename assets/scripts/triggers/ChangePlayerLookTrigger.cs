using System;
using Godot;
using Godot.Collections;

public class ChangePlayerLookTrigger : ActivateOtherTrigger
{ 
    [Export] private NodePath targetPath;
    private Spatial target;

    private Player player => Global.Get().player;
    
    public override void _Ready()
    {
        target = GetNode<Spatial>(targetPath);
        SetProcess(false);
    }

    public override void _Process(float delta)
    {
        if (player == null) return;
        
        var headRotation = GetHeadRotationToTarget();
        var animTree = player.GetNode<AnimationTree>("player_body/animTree");
        var headBlend = (Vector2) animTree.Get("parameters/BlendSpace2D/blend_position");
        
        SetValueTo(ref headBlend.x, headRotation.x, delta * 4);
        SetValueTo(ref headBlend.y, headRotation.y, delta * 4);
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
        var forward = -player.GlobalTransform.basis.z;
        var dir = GetDirToTarget();
        var angle = forward.AngleTo(dir);
        var diffY = target.GlobalTranslation.y - player.GlobalTranslation.y;
        
        if (player.GlobalTransform.basis.x.Dot(dir) < 0)
        {
            angle = -angle;
        }
        
        return new Vector2(angle / 1.5f, diffY / 5);
    }
    
    private Vector3 GetDirToTarget()
    {
        var targetDirPos = target.GlobalTranslation;
        targetDirPos.y = player.GlobalTranslation.y;
        return targetDirPos - player.GlobalTranslation;
    }
    
    private void SetValueTo(ref float value, float to, float delta)
    {
        if (value > to + 0.05f)
        {
            value -= delta;
        }
        else if (value < to - 0.05f)
        {
            value += delta;
        }
        else
        {
            value = to;
        }
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
