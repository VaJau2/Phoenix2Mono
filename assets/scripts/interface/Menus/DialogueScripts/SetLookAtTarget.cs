using Godot;

namespace DialogueScripts
{
    public class SetLookAtTarget : IDialogueScript
    {
        public void initiate(DialogueMenu dialogueMenu, string parameter, string key = "")
        {
            dialogueMenu.SetLookAtTarget(
                string.IsNullOrEmpty(parameter)
                    ? Vector3.Zero
                    : dialogueMenu.GetNodeOrNull<Spatial>("/root/Main/Scene/dialogueTargets/" + parameter)
                        .GlobalTranslation
            );
        }
    }
}