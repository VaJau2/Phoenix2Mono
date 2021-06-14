using Godot;
using Godot.Collections;

// скрипт взаимодействия с предметами
public class PlayerCamera: Camera 
{
    const float RAY_LENGH = 6;
    const float RAY_THIRD_LENGTH = 9;
    const float EYE_PART_SPEED1 = 1000;
    const float EYE_PART_SPEED2 = 1200;
    const float FOV_SPEED = 60;

    public bool eyesClosed = false;
    public bool isUpdating = true;

    Messages messages;
    DialogueMenu dialogueMenu;

    Player player;

    Control labelBack;
    Label label;
    float closedTimer = 0;
    string closedTextLink = "closed";

    Spatial tempObject;
    bool onetimeHint = false;

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
        mayUseRay = true;
    }
    
    public void ShowHint(string textLink, bool triggerClosing = true) {
        var actions = InputMap.GetActionList("use");
        var action = (InputEventKey)actions[0];
        var key = OS.GetScancodeString(action.Scancode);
        label.Text = key;
        label.Text += InterfaceLang.GetPhrase("inGame", "cameraHints", textLink);
        labelBack.Visible = true;
        if (!triggerClosing) return;
        onetimeHint = true;
    }

    public void HideHint()
    {
        onetimeHint = true;
    }

    private void UpdateFov(float delta) {
        if (closedTimer > 0) {
            closedTimer -= delta;
            if (!onetimeHint) {
                label.Text = InterfaceLang.GetPhrase("inGame", "cameraHints", closedTextLink);
                labelBack.Visible = true;
                onetimeHint = true;
            }
        } else {
            if (onetimeHint) {
                labelBack.Visible = false;
                onetimeHint = false;
            }
        }

        if (eyesClosed) {
            eyePartUp.RectPosition = setRectY(eyePartUp.RectPosition, -200);
            eyePartDown.RectPosition = setRectY(eyePartDown.RectPosition, -200);
        } else if(fovClosing) {
            float closeFov = 42f;

            Dictionary armorProps = player.inventory.GetArmorProps();
            if (armorProps.Contains("closeFov")) {
                closeFov = float.Parse(armorProps["closeFov"].ToString());
            }

            if (Fov > closeFov) {
                Fov -= FOV_SPEED * delta;
            }
            if (eyePartUp.RectPosition.y < -220) {
                eyePartUp.RectPosition = setRectY(
                    eyePartUp.RectPosition, 
                    eyePartUp.RectPosition.y + delta * EYE_PART_SPEED1
                );
            }
            if (eyePartDown.RectPosition.y > 220) {
                eyePartDown.RectPosition = setRectY(
                    eyePartDown.RectPosition, 
                    eyePartDown.RectPosition.y - delta * EYE_PART_SPEED1
                );
            }
        } else {
            if (Fov < 70) {
                Fov += FOV_SPEED * delta;
            }

            if (eyePartUp.RectPosition.y > -650) {
                eyePartUp.RectPosition = setRectY(
                    eyePartUp.RectPosition, 
                    eyePartUp.RectPosition.y - delta * EYE_PART_SPEED2
                );
            }
            if (eyePartDown.RectPosition.y < 650) {
                eyePartDown.RectPosition = setRectY(
                    eyePartDown.RectPosition, 
                    eyePartDown.RectPosition.y + delta * EYE_PART_SPEED2
                );
            }
        }
    }

    private void UpdateInteracting(float delta) {
        if (closedTimer > 0) return;
        if (!mayUseRay) return;
        
        tempObject = (Spatial)tempRay.GetCollider();

        if (mayUseRay && tempObject != null)
        {
            switch (tempObject)
            {
                case TheaterChair chair when chair.isActive && !player.IsSitting:
                    ShowHint("sit");
                    break;
                case FurnBase furn when furn.IsOpen:
                    ShowHint("close");
                    break;
                case FurnBase furn:
                    ShowHint("open");
                    break;
                case Terminal _:
                    ShowHint("terminal");
                    break;
                default:
                {
                    if (!dialogueMenu.MenuOn && tempObject is NPC npc) {
                        if (npc.state == NPCState.Idle && npc.dialogueCode != "") {
                            ShowHint("talk");
                        }
                    }

                    break;
                }
            }
        } else {
            tempObject = null;
        }
    }

    public void UpdateInput()
    {
        if (!labelBack.Visible || !(closedTimer <= 0) || tempObject == null) return;
        switch (tempObject)
        {
            case TheaterChair chair when chair.isActive && !player.IsSitting:
            {
                chair.Sit(player);
                break;
            }
            case FurnDoor furn1:
            {
                var keys = player.inventory.GetKeys();
                closedTimer = furn1.ClickFurn(keys);
                onetimeHint = false;
                break;
            }
            case FurnBase furn:
            {
                furn.ClickFurn();
                break;
            }
            case Terminal tempTerminal:
            {
                MenuManager.TryToOpenMenu(tempTerminal);
                break;
            }
            case NPC npc:
            {
                if (npc.state == NPCState.Idle && npc.dialogueCode != "") {
                    dialogueMenu.StartTalkingTo(npc);
                }

                break;
            }
        }
    }

    public override void _Ready()
    {
        messages = GetNode<Messages>("/root/Main/Scene/canvas/messages");
        dialogueMenu = GetNode<DialogueMenu>("/root/Main/Scene/canvas/DialogueMenu/Menu");
        player = GetNode<Player>("../../");
        labelBack = GetNode<Control>("/root/Main/Scene/canvas/openBack");
        eyePartUp = GetNode<Control>("/root/Main/Scene/canvas/eyesParts/eyeUp");
        eyePartDown = GetNode<Control>("/root/Main/Scene/canvas/eyesParts/eyeDown");

        label = labelBack.GetNode<Label>("label");
    }

    public Vector2 setRectY(Vector2 oldPosition, float newY) {
        oldPosition.y = newY;
        return oldPosition;
    }

    public override void _Process(float delta)
    {
        if (!isUpdating) {
            labelBack.Visible = false;
            return;
        }
        
        UpdateFov(delta);
        UpdateInteracting(delta);
    }

    public override void _Input(InputEvent @event)
    {
        if (!isUpdating) {
            return;
        }
        if (@event is InputEventKey && Input.IsActionJustPressed("use")) {
            UpdateInput();
        }

        if (!player.ThirdView && @event is InputEventMouseButton 
                && Input.GetMouseMode() == Input.MouseMode.Captured) {
            var mouseEv = @event as InputEventMouseButton;
            if (mouseEv.ButtonIndex == 2) {
                if (mouseEv.Pressed) {
                    fovClosing = true;
                } else {
                    fovClosing = false;
                }
            }
        }
    }
}