using Godot;

[Tool]
public partial class Lamp : BreakableObject
{
    [Export] private float range = 42f;
    [Export] private float energy = 1f;
    [Export] private float bias = 0.95f;

    private Light3D light;
    
    public override void _Ready()
    {
        light = GetNode<Light3D>("light");

        switch (light)
        {
            case OmniLight3D omniLight:
                omniLight.OmniRange = range;
                break;
            
            case SpotLight3D spotLight:
                spotLight.SpotRange = range;
                break;
        }

        light.LightEnergy = energy;
        light.ShadowBias = bias;
        
        base._Ready();
    }

    public override void _Process(double delta)
    {
        if (Engine.IsEditorHint())
        {
            _Ready();
        }
        
        base._Process(delta);
    }
}
