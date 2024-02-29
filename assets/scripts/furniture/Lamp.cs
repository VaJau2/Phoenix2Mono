using Godot;

[Tool]
public class Lamp : BreakableObject
{
    [Export] private float range = 42f;
    [Export] private float energy = 1f;
    [Export] private float bias = 0.95f;

    private Light light;
    
    public override void _Ready()
    {
        light = GetNode<Light>("light");

        switch (light)
        {
            case OmniLight omniLight:
                omniLight.OmniRange = range;
                break;
            
            case SpotLight spotLight:
                spotLight.SpotRange = range;
                break;
        }

        light.LightEnergy = energy;
        light.ShadowBias = bias;
        
        base._Ready();
    }

    public override void _Process(float delta)
    {
        if (Engine.EditorHint)
        {
            _Ready();
        }
        
        base._Process(delta);
    }
}
