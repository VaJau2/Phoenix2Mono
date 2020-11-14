using Godot;

public class LoadingMenu : Control
{
    private const float TIME = 0.5f;
    private Label loadingLabel;
    private string loadText;

    private int pointsCount = 0;
    private float timer;

    public override void _Ready()
    {
        var loadingPage = GetNode<Label>("page_label");
        loadingPage.Text = InterfaceLang.GetLang("mainMenu", "load", "page");

        loadingLabel = GetNode<Label>("Label");
        loadText = InterfaceLang.GetLang("mainMenu", "load", "text");
        loadingLabel.Text = loadText;
    }

    public override void _Process(float delta)
    {
        if (timer < TIME) {
            timer += delta;
        } else {
            timer = 0;

            if (pointsCount < 3) {
                pointsCount += 1;
                loadingLabel.Text += ".";
            } else {
                loadingLabel.Text = loadText;
                pointsCount = 0;
            }
        }
    }
}
