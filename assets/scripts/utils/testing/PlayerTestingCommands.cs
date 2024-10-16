using System;
using Godot;

/// <summary>
/// Тестовые команды для управления игроком и его статами
/// F1 - лечение игрока
/// F2 - убийство всех врагов
/// F3 - убийство игрока
/// 
/// </summary>
public class PlayerTestingCommands : Node
{
    private const int KILL_DAMAGE = 9999;
    
    private enum CommandType
    {
        Heal, KillEnemies, KillPlayer, 
    }

    private CommandType? tempCommand;
    
    public override void _Ready()
    {
        if (!OS.IsDebugBuild()) QueueFree();
    }

    public override void _Process(float delta)
    {
        if (tempCommand == null) return;
        
        var player = Global.Get().player;
        if (player == null) return;
        
        var messages = GetNode<Messages>("/root/Main/Scene/canvas/messages");

        switch (tempCommand)
        {
            case CommandType.Heal:
                player.HealHealth(200);
                messages.ShowMessage("heal", "testing", Messages.HINT_TIMER);
                
                break;
            case CommandType.KillEnemies:
                var npcParent = GetNode<Node>("/root/Main/Scene/npc");
                foreach (var child in npcParent.GetChildren())
                {
                    if (child is not NPC npc) continue;
                    if (npc.relation != Relation.Enemy && !npc.Name.Contains("dragon")) continue;
                    if (npc.Health <= 0) continue;
                    npc.TakeDamage(npc, KILL_DAMAGE);
                }
                messages.ShowMessage("killEnemies", "testing", Messages.HINT_TIMER);
                
                break;
            case CommandType.KillPlayer:
                player.TakeDamage(player, KILL_DAMAGE);
                messages.ShowMessage("killPlayer", "testing", Messages.HINT_TIMER);
                
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        tempCommand = null;
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is not InputEventKey eventKey) return;
        if (!eventKey.IsPressed()) return;
        
        tempCommand = eventKey.Scancode switch
        {
            (uint)KeyList.F1 => CommandType.Heal,
            (uint)KeyList.F2 => CommandType.KillEnemies,
            (uint)KeyList.F3 => CommandType.KillPlayer,
            _ => tempCommand
        };
    }
}
