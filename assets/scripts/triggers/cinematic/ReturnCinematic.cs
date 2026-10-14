using Godot;
using Godot.Collections;

public partial class ReturnCinematic : AbstractMoveCinematic
{
    public override void Enable()
    {
        if (!smoothTransition)
        {
            InstantReturn();
            return;
        }
        
        var playerCamera = cutscene.GetPlayerCamera();
        cameraAngleRad = playerCamera.GlobalRotation;
        
        var playerLocalPos = playerCamera.GlobalPosition - GlobalPosition;
        Curve.AddPoint(playerLocalPos);
        
        base.Enable();
        
        if (Curve.GetPointCount() > 2)
        {
            Curve.RemovePoint(1);
        }
    }

    private async void InstantReturn()
    {
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        Disable();
    }

    public override void Disable()
    {
        SetPhysicsProcess(false);
        cutscene.ReturnPlayerCamera();
        
        if (smoothTransition) Curve.RemovePoint(0);
        Curve.RemovePoint(Curve.GetPointCount() - 1);
        
        EmitSignal(SignalName.Finished, this);
    }

    public override async void LoadData(Dictionary data)
    {
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        base.LoadData(data);
    }
}
