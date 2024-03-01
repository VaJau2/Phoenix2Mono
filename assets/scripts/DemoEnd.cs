using Godot;

public partial class DemoEnd : Control
{
    public override void _Ready()
    {
        var title = GetNode<Label>("Title");
        var press = GetNode<Label>("Press");
        var label = GetNode<Label>("Label");
        title.Text = InterfaceLang.GetPhrase("inGame", "labels", "toBeContinueTitle");
        press.Text = InterfaceLang.GetPhrase("inGame", "labels", "pressLabel");
        label.Text = InterfaceLang.GetPhrase("inGame", "labels", "toExitToMainMenu");
        
        MenuBase.LoadColorForChildren(this);
    }

    public override void _Process(double delta)
    {
        if (Input.IsActionPressed("jump"))
        {
            GetNode<LevelsLoader>("/root/Main").LoadLevel(0);
        }
    }
}
