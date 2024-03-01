using Godot;
using System.Collections.Generic;

public partial class ControlHintsManager : Control
{
    List<ControlHint> controlHints = new();

    public void LoadHits(ControlText[] controlTexts)
    {
        foreach (ControlHint controlHint in controlHints)
        {
            controlHint.QueueFree();
        }

        controlHints.Clear();

        foreach(ControlText controlText in controlTexts)
        {
            var scene = GD.Load<PackedScene>("res://objects/interface/controlHints/" + controlText + ".tscn");
            var instance = scene.Instantiate();
            AddChild(instance);

            ControlHint controlHint = (ControlHint)instance;
            controlHints.Add(controlHint);
            controlHint.Initialize();
            controlHint.Position = new Vector2(0, 25 * (controlHints.Count - 1));
        }
    }
}
