using System;
using Godot;
using Godot.Collections;

/**
 * Проверяет, заходит ли игрок на какой-либо объект гридмапа
 * если заходит, играет звуки
 */
public class GridmapStepSound : GridMap
{
    [Export] 
    private Array<AudioStream> enterSounds = new Array<AudioStream>();

    [Export] private float cooldown = 1f;

    private float cooldownTimer;
    private bool isInGridItem;

    private readonly Random rand = new Random();
    private static Player Player => Global.Get().player;
    private AudioStream RandomEnterSound => enterSounds[rand.Next(enterSounds.Count - 1)];

    public override void _Process(float delta)
    {
        if (enterSounds.Count == 0)
        {
            GD.PrintErr($"There is no any enter sound in {Name}!");
            SetProcess(false);
            return;
        }

        if (cooldownTimer > 0)
        {
            cooldownTimer -= delta;
            return;
        }
        
        var tempIsOnGridItem = playerIsOnGridItem();
        if (isInGridItem == tempIsOnGridItem) return;
        isInGridItem = tempIsOnGridItem;
        if (!isInGridItem) return;
        Player.GetAudi(true).Stream = RandomEnterSound;
        Player.GetAudi(true).Play();
        cooldownTimer = cooldown;
    }

    private bool playerIsOnGridItem()
    {
        if (Global.Get().player == null) return false;
        var playerGlobalPos = Global.Get().player.GlobalTransform.origin;
        var playerLocalPos = playerGlobalPos - GlobalTransform.origin;
        var mapPos = WorldToMap(playerLocalPos);
        mapPos.y -= 1;
        var cellItem = GetCellItem((int)mapPos.x, (int)mapPos.y, (int)mapPos.z);
        return cellItem > -1;
    }
}
