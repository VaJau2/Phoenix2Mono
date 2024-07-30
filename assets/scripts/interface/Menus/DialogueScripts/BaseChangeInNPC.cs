using Godot;

namespace DialogueScripts;

// адаптер для скриптов что-то меняющих в npc с которым общаеться игрок
public abstract class BaseChangeInNPC : IDialogueScript
{
    public abstract void initiate(Node node, string parameter, string key = "");

    protected NPC GetNPC(Node node)
    {
        var dialogueMenu = node.GetNodeOrNull<DialogueMenu>("/root/Main/Scene/canvas/DialogueMenu/Menu");
        var subtitles = node.GetNodeOrNull<Subtitles>("/root/Main/Scene/canvas/subtitles");
        
        return dialogueMenu.npc ?? subtitles.tempTalker;
    }
}