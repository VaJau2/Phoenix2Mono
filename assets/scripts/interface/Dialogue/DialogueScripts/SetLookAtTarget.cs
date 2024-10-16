using Godot;

namespace DialogueScripts
{
    public class SetLookAtTarget : IDialogueScript
    {
        public void initiate(Node node, string parameter, string key = "")
        {
            var player = Global.Get().player;
            var dialogueMenu = node.GetNode<DialogueMenu>("/root/Main/Scene/canvas/DialogueMenu/Menu");
            var target = Vector3.Zero;

            if (!string.IsNullOrEmpty(parameter))
            {
                target = node
                    .GetNodeOrNull<Spatial>($"/root/Main/Scene/dialogueTargets/{parameter}")
                    .GlobalTranslation;
            }
            
            player.DialogueCheck.SetLookAtTarget(target);
        }
    }
}