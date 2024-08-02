using Godot;

namespace DialogueScripts
{
    public class SetSavableValue: IDialogueScript
    {
        public void initiate(Node node, string parameter, string key = "")
        {
            if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(parameter)) return;
            
            var saveNode = node.GetNode<SaveNode>("/root/Main/SaveNode");
            if (saveNode == null) return;
            
            saveNode.SavedVariables[key] = parameter;
        }
    }
}