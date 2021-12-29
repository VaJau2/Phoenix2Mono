using Godot;

public class DemoEnd : Control
{
    public override void _Ready()
    {
        var label2 = GetNode<Label>("Label2");
        var label3 = GetNode<Label>("Label3");
        label2.Text = InterfaceLang.GetPhrase("inGame", "labels", "endLabel2");
        label3.Text = InterfaceLang.GetPhrase("inGame", "labels", "endLabel3");
    }

    public override void _Process(float delta)
    {
        if (Input.IsActionPressed("jump"))
        {
            GetNode<LevelsLoader>("/root/Main").LoadLevel(0);
        }
    }
}
