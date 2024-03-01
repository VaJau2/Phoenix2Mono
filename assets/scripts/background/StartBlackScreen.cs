using Godot;
using Godot.Collections;

public partial class StartBlackScreen : CanvasLayer, ISavable
{
    private const float SPEED = 2.0f;
    private const float TIMER_1 = 1.5f;
    private float TIMER_2 = 1.5f;
    
    private ColorRect blackScreen;

    private bool done;
    
    public override async void _Ready()
    {
        blackScreen = GetNode<ColorRect>("black");
        Label label = GetNode<Label>("Label");

        if (done) return;
        
        blackScreen.Color = new Color(
            blackScreen.Color
        ); //делаем alpha в 1
        
        SetProcess(false);
        if (!done)
        {
            await Global.Get().ToTimer(TIMER_1, this);
        }
        
        label.Visible = true;
        
        if (!done)
        {
            await Global.Get().ToTimer(TIMER_2, this);
        }
        
        label.Visible = false;
        SetProcess(true);
        done = true;
    }

    public override void _Process(double delta)
    {
        if (blackScreen.Color.A > 0 && !done)
        {
            blackScreen.Color = new Color(blackScreen.Color, blackScreen.Color.A - SPEED * (float)delta);
        }
        else
        {
            blackScreen.Color = new Color(blackScreen.Color, 0);
            SetProcess(false);
        }
    }

    public Dictionary GetSaveData()
    {
        return new Dictionary()
        {
            {"done", done}
        };
    }

    public void LoadData(Dictionary data)
    {
        done = (bool)data["done"];
    }
}
