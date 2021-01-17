using Godot;
using Godot.Collections;

/// <summary>
/// скрипт взаимодействия с предметами
/// внезапно управляет перемещением на локацию базы из локации обучения через предмет карты
/// </summary>
public class PlayerCamera: Camera {
    //TODO
    //дописать взаимодействие для DialogueMenu
    //ну и все остальное внизу тоже дописать, да

    const float RAY_LENGH = 6;
    const float EYE_PART_SPEED1 = 1000;
    const float EYE_PART_SPEED2 = 1200;
    const float FOV_SPEED = 60;

    Messages messages;

    Player player;
    float tempLength;
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

    public bool eyesClosed = false;

    
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
                closeFov = (float)armorProps["closeFov"];
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

        var pos = OS.WindowSize / 2;
        var spaceState = GetWorld().DirectSpaceState;
        var from = ProjectRayOrigin(pos);
        var to = from + ProjectRayNormal(pos) * tempLength;
        var result = spaceState.IntersectRay(from, to,
            new Array() {player}, rayLayer);

        if (result.Count > 0) {
            tempObject = (Spatial)result["collider"];
            if (tempObject is FurnBase) {
                var furn = tempObject as FurnBase;
                if (furn.IsOpen) {
                    showHint("close");
                } else {
                    showHint("open");
                }
                //TODO
                //дописать сюда интерфейс для взаимодействия с:
                //терминалами
                //картой
                //персонажами
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
            }
            //TODO
                //дописать сюда взаимодействие с:
                //терминалами
                //картой
                //персонажами
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
        tempLength = RAY_LENGH;
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