using Godot;
using Godot.Collections;

// скрипт взаимодействия с предметами
public class PlayerCamera : Camera
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

    RayCast tempRay => player.RotationHelperThird.TempRay;

    public RayCast UseRay(float newDistance)
    {
        tempRay.CollisionMask = 2147483649; //слой 1
        tempRay.CastTo = new Vector3(0, 0, -newDistance);
        mayUseRay = false;
        return tempRay;
    }

    public void ReturnRayBack()
    {
        float oldLength = player.ThirdView ? RAY_THIRD_LENGTH : RAY_LENGH;
        tempRay.CollisionMask = 2147483651; //слой 1 и 2
        tempRay.CastTo = new Vector3(0, 0, -oldLength);
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

        var actions = InputMap.GetActionList("use");
        var action = (InputEventKey)actions[0];
        var key = OS.GetScancodeString(action.Scancode);
        var buttonPath = "res://assets/textures/interface/icons/buttons/";
        var isWideButton = key is "BackSpace" or "CapsLock" or "Kp 0" or "Shift" or "Space" or "Tab";

        interactionIcon.Texture = GD.Load<Texture>( buttonPath + key + ".png");
        
        interactionIconShadow.Texture = (isWideButton)
            ? GD.Load<Texture>(buttonPath + "Empty 48x32.png")
            : GD.Load<Texture>(buttonPath + "Empty 32x32.png");
        
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
            eyePartUp.RectPosition = SetRectY(eyePartUp.RectPosition, 0);
            eyePartDown.RectPosition = SetRectY(eyePartDown.RectPosition, 0);
        }
        else if (fovClosing)
        {
            float closeFov = 42f;

            Dictionary armorProps = player.inventory.GetArmorProps();
            if (armorProps.Contains("closeFov"))
            {
                closeFov = float.Parse(armorProps["closeFov"].ToString());
            }

            if (Fov > closeFov)
            {
                Fov -= FOV_SPEED * delta;
            }
            
            if (eyePartUp.RectPosition.y < -220)
            {
                eyePartUp.RectPosition = SetRectY(
                    eyePartUp.RectPosition,
                    eyePartUp.RectPosition.y + delta * EYE_PART_SPEED1
                );
            }
            
            if (eyePartDown.RectPosition.y > 220)
            {
                eyePartDown.RectPosition = SetRectY(
                    eyePartDown.RectPosition,
                    eyePartDown.RectPosition.y - delta * EYE_PART_SPEED1
                );
            }
        }
        else
        {
            if (Fov < 70)
            {
                Fov += FOV_SPEED * delta;
            }

            if (eyePartUp.RectPosition.y > -650)
            {
                eyePartUp.RectPosition = SetRectY(
                    eyePartUp.RectPosition,
                    eyePartUp.RectPosition.y - delta * EYE_PART_SPEED2
                );
            }
            
            if (eyePartDown.RectPosition.y < 650)
            {
                eyePartDown.RectPosition = SetRectY(
                    eyePartDown.RectPosition,
                    eyePartDown.RectPosition.y + delta * EYE_PART_SPEED2
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
            if (tempObject is PhysicalBone)
            {
                tempObject = tempObject.GetNode<Node>("../../../");
            }
            
            if (tempObject is IInteractable interactable && interactable.MayInteract)
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
        if (tempObject is IInteractable interactable && interactable.MayInteract)
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
        oldPosition.y = newY;
        return oldPosition;
    }

    public override void _Process(float delta)
    {
        if (!isUpdating)
        {
            SetHintVisible(false);
            return;
        }

        UpdateFov(delta);
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
        if (!(@event is InputEventMouseButton mouseEv)) return;
        if (mouseEv.ButtonIndex == 2)
        {
            fovClosing = mouseEv.Pressed;
        }
    }
}