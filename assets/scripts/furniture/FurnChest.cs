using Godot;
using Godot.Collections;
using System;

public class FurnChest: FurnBase {

    const float ITEM_DROP_CHANCE = 0.65f;

    [Export]
    public Vector3 dropSide;
    [Export]
    public float itemHeight = 2f;

    [Export]
    public string keyName = "";
    [Export]
    public string pickKeyTextLink = "";

    Random rand = new Random();


    public override void ClickFurn(AudioStreamSample openSound = null, float timer = 0, string openAnim = null)
    {
        base.ClickFurn();
    }
}