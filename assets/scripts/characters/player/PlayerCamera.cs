using Godot;
using Godot.Collections;

// скрипт взаимодействия с предметами
//
// TODO отрефакторить (выделить fov в отдельный класс)
public class PlayerCamera : Camera
{
    private const float RAY_LENGTH = 8;
    private const float RAY_THIRD_LENGTH = 9;
    private const float EYE_PART_SPEED1 = 1000;
    private const float EYE_PART_SPEED2 = 1200;
    private const float FOV_SPEED = 60;

    public bool eyesClosed = false;
    public float closedTimer;
    public bool onetimeHint;
    
    private InteractionPointManager point;

    private Player player;

    private Label interactionHint;
    private string closedTextLink = "closed";
    
    private Control loadingIcon;
    private AnimationPlayer loadingAnim;

    private Node tempObject;

    private bool fovClosing;
    private Control eyePartUp;
    private Control eyePartDown;

    private bool isHoldingSound;
    private bool isUpdating = true;
    private bool mayUseRay = true;

    private RayCast tempRay => player.RotationHelperThird.TempRay;
    
    private bool MayInteract => !interactionHint.Visible || !(closedTimer <= 0) ||
                                tempObject is IInteractable { MayInteract: true };
    
    public override void _Ready()
    {
        player = GetNode<Player>("../../");

        eyePartUp = GetNode<Control>("/root/Main/Scene/canvas/eyesParts/eyeUp");
        eyePartDown = GetNode<Control>("/root/Main/Scene/canvas/eyesParts/eyeDown");
        
        point = GetNode<InteractionPointManager>("/root/Main/Scene/canvas/pointManager");
        interactionHint = point.GetNode<Label>("interactionHint");
        
        loadingIcon = point.GetNode<Control>("loadingIcon");
        loadingAnim = loadingIcon.GetNode<AnimationPlayer>("anim");
        if (!loadingAnim.IsConnected("animation_finished", this, nameof(HoldAnimationFinished)))
        {
            loadingAnim.Connect("animation_finished", this, nameof(HoldAnimationFinished));
        }
    }
    
    public override void _Process(float delta)
    {
        if (!isUpdating) return;

        UpdateFov(delta);
        UpdateInteractionObject();
    }

    public override void _Input(InputEvent @event)
    {
        if (!isUpdating) return;

        if (MayInteract && @event is InputEventKey)
        {
            UpdateInteractionInput();
        }

        if (player.ThirdView) return;
        if (Input.MouseMode != Input.MouseModeEnum.Captured) return;
        if (@event is not InputEventMouseButton mouseEv) return;
        if (mouseEv.ButtonIndex == 2)
        {
            fovClosing = mouseEv.Pressed;
        }
    }

    public void HoldAnimationFinished(string animation)
    {
        if (animation != "load") return;
        
        if (tempObject is IInteractableHoldSound)
        {
            player.GetAudi(true).Stop();
        }
        
        InteractWithItem();
        HideLoadingIcon();
    }

    public void SetUpdating(bool value)
    {
        isUpdating = value;

        if (value)
        {
            ReturnInteractionPoint();
        }
        else
        {
            SetHintVisible(false);
            point.SetInteractionVariant(InteractionVariant.Point);
        }
    }
    
    public RayCast UseRay(float newDistance)
    {
        tempRay.CollisionMask = 21; //слой 1, 3 и 5
        tempRay.CastTo = new Vector3(0, 0, -newDistance);
        mayUseRay = false;
        return tempRay;
    }

    public void ReturnRayBack()
    {
        float oldLength = player.ThirdView ? RAY_THIRD_LENGTH : RAY_LENGTH;
        tempRay.CollisionMask = 21; //слой 1, 3 и 5
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
        interactionHint.Text = InterfaceLang.GetPhrase("inGame", "cameraHints", textLink);

        SetHintVisible(true);
        if (!triggerClosing) return;
        onetimeHint = true;
    }

    public void HideHint()
    {
        onetimeHint = true;
        ReturnInteractionPoint();
    }
    
    private void UpdateInteractionObject()
    {
        if (closedTimer > 0) return;
        if (!mayUseRay) return;

        tempObject = (Node)tempRay.GetCollider();

        if (mayUseRay && tempObject != null)
        {
            if (tempObject is PhysicalBone)
            {
                tempObject = tempObject.GetNode<Node>("../../../");
            }
            
            if (tempObject is IInteractable { MayInteract: true } interactable)
            {
                ShowHint(interactable.InteractionHintCode);
                return;
            }
        }
        
        ReturnInteractionPoint();
        HideLoadingIcon();
        
        if (isHoldingSound)
        {
            player.GetAudi(true).Stop();
        }
    }
    
    private void UpdateFov(float delta)
    {
        if (closedTimer > 0)
        {
            closedTimer -= delta;
            
            if (!onetimeHint)
            {
                interactionHint.Text = InterfaceLang.GetPhrase(
                    "inGame", 
                    "cameraHints", 
                    closedTextLink
                );
                
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

            Dictionary armorProps = player.Inventory.GetArmorProps();
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
    
    private void UpdateInteractionInput()
    {
        if (Input.IsActionJustPressed("use"))
        {
            if (tempObject is IInteractableHold holdable)
            {
                ShowLoadingIcon(holdable);
                        
                if (tempObject is IInteractableHoldSound soundable)
                {
                    PlayInteractionAudio(soundable.HoldingSound, true);
                }

                return;
            }
           
            InteractWithItem();
        }

        if (Input.IsActionJustReleased("use"))
        {
            if (tempObject is IInteractableHold)
            {
                HideLoadingIcon();
            }
            
            if (tempObject is IInteractableHoldSound)
            {
                player.GetAudi(true).Stop();
            }
        }
    }

    private void InteractWithItem()
    {
        if (tempObject is IInteractableUseSound soundable)
        {
            PlayInteractionAudio(soundable.UseSound, false);
        }
        
        if (tempObject is IInteractable interactable)
        {
            interactable.Interact(this);
        }
    }
    
    private void PlayInteractionAudio(AudioStream stream, bool holdingSound)
    {
        if (stream == null) return;
        isHoldingSound = holdingSound;
        
        player.GetAudi(true).Stream = stream;
        player.GetAudi(true).Play();
    }

    private void ReturnInteractionPoint()
    {
        point.SetInteractionVariant(
            player.Weapons.GunOn && player.Weapons.IsShootingWeapon 
                ? InteractionVariant.Cross 
                : InteractionVariant.Point
        );
    }

    private void SetHintVisible(bool value)
    {
        interactionHint.Visible = value;
    }

    private Vector2 SetRectY(Vector2 oldPosition, float newY) 
    {
        oldPosition.y = newY;
        return oldPosition;
    }
    
    private void ShowLoadingIcon(IInteractableHold holdable)
    {
        loadingIcon.Visible = true;
        loadingAnim.PlaybackSpeed = holdable.HoldingAnimSpeed;
        loadingAnim.Play("load");
    }

    private void HideLoadingIcon()
    {
        loadingIcon.Visible = false;
        loadingAnim.Stop();
    }
}
