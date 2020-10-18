using Godot;

public class FPSShow : Label
{
    public override void _Process(float delta)
    {
        this.Text = Performance.GetMonitor(Performance.Monitor.TimeFps).ToString();
    }
}
