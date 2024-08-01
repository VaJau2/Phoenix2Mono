using Godot;

namespace DialogueScripts;

// адаптер для скриптов что-то меняющих в npc с которым общаеться игрок
public abstract class BaseChangeInNPC : IDialogueScript
{
    protected DialogueMenu DialogueMenu;
    protected Subtitles Subtitles;
    
    public abstract void initiate(Node node, string parameter, string key = "");

    protected NPC GetNPC(Node node)
    {
        DialogueMenu = node.GetNodeOrNull<DialogueMenu>("/root/Main/Scene/canvas/DialogueMenu/Menu");
        Subtitles = node.GetNodeOrNull<Subtitles>("/root/Main/Scene/canvas/subtitles");
        
        return DialogueMenu.npc ?? Subtitles.Talker;
    }
}