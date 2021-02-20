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

    Messages messages;

    Player player;
    uint rayLayer = 3;

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
        tempRay.CastTo = new Vector3(0, 0, -newDistance);
        mayUseRay = false;
        return tempRay;
    }

    public void ReturnRayBack()
    {
        float oldLength = player.ThirdView ? RAY_THIRD_LENGTH : RAY_LENGH;
        tempRay.CastTo = new Vector3(0, 0, -oldLength);
        mayUseRay = true;
    }
    
    private void showHint(string textLink) {
        var actions = InputMap.GetActionList("use");
        var action = (InputEventKey)actions[0];
        var key = OS.GetScancodeString(action.Scancode);
        label.Text = key;
        label.Text += InterfaceLang.GetPhrase("inGame", "cameraHints", textLink);
        labelBack.Visible = true;
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

        if (mayUseRay && tempObject != null) {
            if (tempObject is FurnBase) {
                var furn = tempObject as FurnBase;
                if (furn.IsOpen) {
                    showHint("close");
                } else {
                    showHint("open");
                }
            } else if (tempObject is ITrader) {
                showHint("trade");
            }
        } else {
            tempObject = null;
        }
    }

    public void UpdateInput() {
        if (labelBack.Visible && closedTimer <= 0 && tempObject != null) {
            if (tempObject is FurnDoor) {
                var furn = tempObject as FurnDoor;
                var keys = player.inventory.GetKeys();
                closedTimer = furn.ClickFurn(keys);
                onetimeHint = false;
            }
            else if (tempObject is FurnBase) {
                var furn = tempObject as FurnBase;
                furn.ClickFurn();
            } else if (tempObject is ITrader) {
                var trader = tempObject as ITrader;
                trader.StartTrading();
            }
        }
    }

    public override void _Ready()
    {
        messages = GetNode<Messages>("/root/Main/Scene/canvas/messages");
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
        UpdateFov(delta);
        UpdateInteracting(delta);
    }

    public override void _Input(InputEvent @event)
    {
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