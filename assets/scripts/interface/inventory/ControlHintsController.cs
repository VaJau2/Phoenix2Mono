using Godot;
using System.Collections.Generic;

public class ControlHintsController : Control
{
    List<ControlHint> controlHints = new List<ControlHint>();

    public void LoadHits(ControlText[] controlTexts)
    {
        foreach (ControlHint controlHint in controlHints)
        {
            controlHint.QueueFree();
        }

        controlHints.Clear();

        foreach(ControlText controlText in controlTexts)
        {
            var scene = GD.Load<PackedScene>("res://objects/interface/controlHints/" + controlText.ToString() + ".tscn");
            var instance = scene.Instance();
            AddChild(instance);

            ControlHint controlHint = (ControlHint)instance;
            controlHints.Add(controlHint);
            controlHint.Initialize();
            controlHint.RectPosition = new Vector2(0, 25 * (controlHints.Count - 1));
        }
    }
}
