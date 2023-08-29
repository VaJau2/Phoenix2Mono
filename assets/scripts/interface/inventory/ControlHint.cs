using Godot;
using Godot.Collections;

public class ControlHint : Control
{
    [Export] private string actionKey;
    [Export] private bool holdDelay = true;
    private bool isMouseMoving;
    private float deadZone = 1;

    [Export] private bool isMove;
    private bool reverse;

    [Export] private Array<Texture> icons = new Array<Texture>();
    private TextureRect icon;
    private int indexIcon;
    private float changeTimer = 1f;
    private float time = 0;

    private AnimationPlayer anim;

    private Label label;
    [Export] private ControlText textKey;

    private InventoryMenu menu;
    
    public override void _Ready()
    {
        icon = GetNode<TextureRect>("Icon");
        label = GetNode<Label>("Label");
        anim = GetNodeOrNull<AnimationPlayer>("anim");

        menu = GetNode<InventoryMenu>("/root/Main/Scene/canvas/inventory");

        if (isMove) SetProcessInput(true);
        if (icons.Count > 0 || isMove || anim != null) SetProcess(true);
    }

    public override void _Process(float delta)
    {
        if (icons.Count > 0) Iconshow(delta);
        if (isMove) MoveIcon(delta);
        if (anim != null) UpdateHoldIcon();
    }

    public void Initialize()
    {
        LoadIcon();
        LoadText();
    }

    private void LoadIcon()
    {
        if (!InputMap.HasAction(actionKey)) return;

        var actions = InputMap.GetActionList(actionKey);

        if (actions[0] is InputEventKey inputKey)
        {
            icon.Texture = GD.Load<Texture>("res://assets/textures/interface/icons/buttons/" + inputKey + ".png");
        }
    }

    private void LoadText()
    {
        label.Text = InterfaceLang.GetPhrase(
            "inventory",
            "inventoryControlHints",
            textKey.ToString()
        );
    }

    private void Iconshow(float delta)
    {
        time += delta;
        if (time >= changeTimer)
        {
            time = 0;

            if (indexIcon == icons.Count - 1) indexIcon = 0;
            else indexIcon++;

            icon.Texture = icons[indexIcon];
        }
    }

    private void MoveIcon(float delta)
    {
        if (!Input.IsActionPressed("ui_click")) return;
        delta *= 30;

        if (reverse)
        {
            if (icon.RectPosition.x < 5) icon.RectPosition += new Vector2(delta, 0);
            else reverse = false;
        }
        else
        {
            if (icon.RectPosition.x > -5) icon.RectPosition -= new Vector2(delta, 0);
            else reverse = true;
        }
    }

    private void UpdateHoldIcon()
    {
        if (Input.IsActionJustPressed("ui_click"))
        {
            anim.Play("hold");
        }

        if (Input.IsActionJustReleased("ui_click") || menu.mode.isDragging)
        {
            anim.Play("RESET");
        }
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventMouseMotion mouseMotion)
        {
            if (mouseMotion.Relative.Length() > deadZone)
            {
                isMouseMoving = true;
            }
        }
        else isMouseMoving = false;
    }
}

public enum ControlText
{
    equip,
    bind,
    move,
    drop,
    use,
    eat,
    read,
    take,
    put,
    buy,
    sell
}
