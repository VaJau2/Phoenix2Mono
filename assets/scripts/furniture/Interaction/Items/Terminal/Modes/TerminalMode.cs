using Godot;

public class TerminalMode {

    protected Terminal terminal;
    protected Viewport viewport;
    protected MeshInstance3D screen;
    protected RichTextLabel textLabel;
    public bool startKeyPressed = false;

    public void ShowMessage(string message)
    {
        textLabel.Text += message + "\n\n";
        textLabel.Text += terminal.startCommand;
    }

    public void ClearOutput()
    {
        textLabel.Text = null;
    }

    public TerminalMode(Terminal terminal)
    {
        this.terminal = terminal;
        viewport  = terminal.GetNode<Viewport>("Viewport");
        screen    = terminal.GetNode<MeshInstance3D>("terminal");
        textLabel = terminal.GetNode<RichTextLabel>("Viewport/Control/text");
    }

    public virtual void LoadMode()
    {
        StandardMaterial3D material = screen.GetSurfaceOverrideMaterial(1) as StandardMaterial3D;
        material.AlbedoColor = Colors.White;
        material.AlbedoTexture = viewport.GetTexture();
    }

    public virtual void UpdateInput(InputEventKey keyEvent) {}
}