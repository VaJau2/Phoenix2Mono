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
        //в Godot 3 текстура вьюпорта была перевёрнута по вертикали, и UV экрана это компенсировали;
        //в Godot 4 переворота нет — отражаем UV обратно
        material.Uv1Scale = new Vector3(1, -1, 1);
        material.Uv1Offset = new Vector3(0, 1, 0);
    }

    public virtual void UpdateInput(InputEventKey keyEvent) {}
}