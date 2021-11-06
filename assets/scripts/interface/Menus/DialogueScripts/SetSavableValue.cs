namespace DialogueScripts
{
    public class SetSavableValue: IDialogueScript
    {
        public void initiate(DialogueMenu dialogueMenu, string parameter, string key = "")
        {
            if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(parameter)) return;
            
            var saveNode = dialogueMenu.GetNode<SaveNode>("/root/Main/SaveNode");
            if (saveNode == null) return;
            
            saveNode.SavedVariables[key] = parameter;
        }
    }
}