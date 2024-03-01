using Godot;
using Godot.Collections;

// скрипт взаимодействия с предметами
public partial class PlayerCamera : Camera3D
{
    const float RAY_LENGH = 6;
    const float RAY_THIRD_LENGTH = 9;
    const float EYE_PART_SPEED1 = 1000;
    const float EYE_PART_SPEED2 = 1200;
    const float FOV_SPEED = 60;

    public bool eyesClosed = false;
    public bool isUpdating = true;
    public float closedTimer = 0;
    public bool onetimeHint;
    
    private InteractionPointManager point;

    Player player;

    Label interactionHint;
    TextureRect interactionIcon;
    TextureRect interactionIconShadow;
    string closedTextLink = "closed";

    Node tempObject;
    bool onetimeCross;

    bool fovClosing = false;
    Control eyePartUp;
    Control eyePartDown;

    bool mayUseRay = true;

    RayCast3D tempRay => player.RotationHelperThird.TempRay;

    public RayCast3D UseRay(float newDistance)
    {
        tempRay.CollisionMask = 2147483649; //слой 1
        tempRay.TargetPosition = new Vector3(0, 0, -newDistance);
        mayUseRay = false;
        return tempRay;
    }

    public void ReturnRayBack()
    {
        float oldLength = player.ThirdView ? RAY_THIRD_LENGTH : RAY_LENGH;
        tempRay.CollisionMask = 2147483651; //слой 1 и 2
        tempRay.TargetPosition = new Vector3(0, 0, -oldLength);
        tempRay.ForceRaycastUpdate();
        mayUseRay = true;
    }

    public void HideInteractionSquare()
    {
        point.HideSquare();
    }
    
    public void ShowHint(string textLink, bool triggerClosing = true)
    {
        point.SetInteractionVariant(InteractionVariant.Square);

        var actions = InputMap.ActionGetEvents("use");
        var action = (InputEventKey)actions[0];
        var key = OS.GetKeycodeString(action.Keycode);
        var buttonPath = "res://assets/textures/interface/icons/buttons/";
        var isWideButton = key is "BackSpace" or "CapsLock" or "Kp 0" or "Shift" or "Space" or "Tab";

        interactionIcon.Texture = GD.Load<Texture2D>( buttonPath + key + ".png");
        
        interactionIconShadow.Texture = (isWideButton)
            ? GD.Load<Texture2D>(buttonPath + "Empty 48x32.png")
            : GD.Load<Texture2D>(buttonPath + "Empty 32x32.png");
        
        interactionHint.Text = InterfaceLang.GetPhrase("inGame", "cameraHints", textLink);

        SetHintVisible(true);
        if (!triggerClosing) return;
        onetimeHint = onetimeCross = true;
    }

    public void HideHint()
    {
        onetimeHint = true;
        ReturnInteractionPoint();
    }

    private void ReturnInteractionPoint()
    {
        point.SetInteractionVariant(
            player.Weapons.GunOn ? InteractionVariant.Cross : InteractionVariant.Point
        );
    }

    private void UpdateFov(float delta)
    {
        if (closedTimer > 0)
        {
            closedTimer -= delta;
            if (!onetimeHint)
            {
                interactionHint.Text = InterfaceLang.GetPhrase("inGame", "cameraHints", closedTextLink);
                SetHintVisible(true);
                onetimeHint = true;
            }
        }
        else
        {
            if (onetimeHint)
            {
                SetHintVisible(false);
                onetimeHint = false;
            }
        }

        if (eyesClosed)
        {
            eyePartUp.Position = SetRectY(eyePartUp.Position, 0);
            eyePartDown.Position = SetRectY(eyePartDown.Position, 0);
        }
        else if (fovClosing)
        {
            float closeFov = 42f;

            Dictionary armorProps = player.Inventory.GetArmorProps();
            if (armorProps.TryGetValue("closeFov", out var prop))
            {
                closeFov = float.Parse(prop.ToString());
            }

            if (Fov > closeFov)
            {
                Fov -= FOV_SPEED * delta;
            }
            
            if (eyePartUp.Position.Y < -220)
            {
                eyePartUp.Position = SetRectY(
                    eyePartUp.Position,
                    eyePartUp.Position.Y + delta * EYE_PART_SPEED1
                );
            }
            
            if (eyePartDown.Position.Y > 220)
            {
                eyePartDown.Position = SetRectY(
                    eyePartDown.Position,
                    eyePartDown.Position.Y - delta * EYE_PART_SPEED1
                );
            }
        }
        else
        {
            if (Fov < 70)
            {
                Fov += FOV_SPEED * delta;
            }

            if (eyePartUp.Position.Y > -650)
            {
                eyePartUp.Position = SetRectY(
                    eyePartUp.Position,
                    eyePartUp.Position.Y - delta * EYE_PART_SPEED2
                );
            }
            
            if (eyePartDown.Position.Y < 650)
            {
                eyePartDown.Position = SetRectY(
                    eyePartDown.Position,
                    eyePartDown.Position.Y + delta * EYE_PART_SPEED2
                );
            }
        }
    }

    private void UpdateInteracting()
    {
        if (closedTimer > 0) return;
        if (!mayUseRay) return;
        if (!player.MayMove) return;

        tempObject = (Node)tempRay.GetCollider();

        if (mayUseRay && tempObject != null)
        {
            if (tempObject is PhysicalBone3D)
            {
                tempObject = tempObject.GetNode<Node>("../../../");
            }
            
            if (tempObject is IInteractable { MayInteract: true } interactable)
            {
                ShowHint(interactable.InteractionHintCode);
            }
        }
        else
        {
            tempObject = null;
            ReturnInteractionPoint();
        }
    }

    private void UpdateInput()
    {
        if (!interactionHint.Visible || !(closedTimer <= 0) || tempObject == null) return;
        if (tempObject is IInteractable { MayInteract: true } interactable)
        {
            interactable.Interact(this);
        }
    }

    void SetHintVisible(bool value)
    {
        interactionHint.Visible = value;
        interactionIcon.Visible = value;
    }

    public override void _Ready()
    {
        point = GetNode<InteractionPointManager>("/root/Main/Scene/canvas/pointManager");
        interactionHint = point.GetNode<Label>("interactionHint");

        interactionIcon = GetNode<TextureRect>("/root/Main/Scene/canvas/interactionIcon");
        interactionIconShadow = interactionIcon.GetNode<TextureRect>("shadow");
        MenuBase.LoadColorForChildren(interactionIcon);

        player = GetNode<Player>("../../");

        eyePartUp = GetNode<Control>("/root/Main/Scene/canvas/eyesParts/eyeUp");
        eyePartDown = GetNode<Control>("/root/Main/Scene/canvas/eyesParts/eyeDown");
    }

    private Vector2 SetRectY(Vector2 oldPosition, float newY) 
    {
        oldPosition.X = newY;
        return oldPosition;
    }

    public override void _Process(double delta)
    {
        if (!isUpdating)
        {
            SetHintVisible(false);
            return;
        }

        UpdateFov((float)delta);
        UpdateInteracting();
    }

    public override void _Input(InputEvent @event)
    {
        if (!isUpdating) return;

        if (@event is InputEventKey && Input.IsActionJustPressed("use")) 
        {
            UpdateInput();
        }

        if (player.ThirdView) return;
        if (Input.MouseMode != Input.MouseModeEnum.Captured) return;
        if (@event is not InputEventMouseButton mouseEv) return;
        if (mouseEv.ButtonIndex == (MouseButton)2)
        {
            fovClosing = mouseEv.Pressed;
        }
    }
}