using Godot;

public partial class LoadingMenu : Control
{
    private const double TIME = 0.5f;
    private Label loadingLabel;
    private string loadText;

    private int pointsCount = 0;
    private double timer;

    public override void _Ready()
    {
        var loadingPage = GetNode<Label>("page_label");
        loadingPage.Text = InterfaceLang.GetPhrase("mainMenu", "load", "page");

        loadingLabel = GetNode<Label>("Label");
        loadText = InterfaceLang.GetPhrase("mainMenu", "load", "text");
        loadingLabel.Text = loadText;

        MenuBase.LoadColorForChildren(this);
    }

    public override void _Process(double delta)
    {
        if (timer < TIME)
        {
            timer += delta;
        }
        else
        {
            timer = 0;

            if (pointsCount < 3)
            {
                pointsCount += 1;
                loadingLabel.Text += ".";
            }
            else
            {
                loadingLabel.Text = loadText;
                pointsCount = 0;
            }
        }
    }
}
