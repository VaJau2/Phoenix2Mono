using Godot;

namespace DialogueScripts
{
    public partial class SetLookAtTarget : IDialogueScript
    {
        public void initiate(Node node, string parameter, string key = "")
        {
            var player = Global.Get().player;
            var dialogueMenu = node.GetNode<DialogueMenu>("/root/Main/Scene/canvas/DialogueMenu/Menu");
            var target = Vector3.Zero;

            if (!string.IsNullOrEmpty(parameter))
            {
                target = node
                    .GetNodeOrNull<Node3D>($"/root/Main/Scene/dialogueTargets/{parameter}")
                    .GlobalPosition;
            }
            
            player.DialogueCheck.SetLookAtTarget(target);
        }
    }
}