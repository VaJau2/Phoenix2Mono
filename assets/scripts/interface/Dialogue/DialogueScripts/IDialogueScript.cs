using Godot;

namespace DialogueScripts
{
    public interface IDialogueScript 
    {
        void initiate(Node node, string parameter, string key = "");
    }
}