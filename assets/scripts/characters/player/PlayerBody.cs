using Godot;
using Godot.Collections;

public class PlayerBody : Spatial
{
    //---------------------------------------------------+
    //скрипт анимирует тело и не дает ему поворачиваться,
    //пока угол между ним и головой не больше MAX_ANGLE
    //---------------------------------------------------+

    private const float MAX_ANGLE = 90;
    private const int MAX_MOUSE_SPEED = 450;
    private const float OFFSET_SPEED = 3f;

    private const float HEAD_ROT_SPEED = 5f;
    private const float BODY_ROT_SPEED = 26f;

    private const float CROUCH_COOLDOWN = 5f;
    private const float JUMP_COOLDOWN = 0.7f;

    private const int RAGDOLL_IMPULSE = 700;
    
    private const string WALK_FORWARD = "Walk";
    private const string WALK_BACKWARD = "WalkBackwards";

    public PlayerHead Head { get; private set; }
    private Player player;
    private Race playerRace;

    private Skeleton playerSkeleton;
    private PhysicalBone headBone;
    private AnimationTree animTree;
    private AnimationNodeStateMachinePlayback playback;
    private Vector2 headBlend;

    private float walkOffset;
    private float notJumpingCooldown; //чтоб гг не прыгала сразу после того, как встает
    private float jumpingCooldown;
    private float crouchingCooldown;
    private float smileCooldown;

    public float bodyRot;
    private bool onetimeBodyRotBack;

    public bool RotClumpsMin => bodyRot > -MAX_ANGLE + 1;

    public bool RotClumpsMax => bodyRot < MAX_ANGLE - 1;

    private bool IsVelocityMoving => new Vector2(player.Velocity.x, player.Velocity.z).Length() > 1f;

    private bool IsMovementInput => Input.IsActionPressed("ui_up") || Input.IsActionPressed("ui_down") ||
                                    Input.IsActionPressed("ui_left") || Input.IsActionPressed("ui_right");

    private bool checkPegasusFlying
    {
        get
        {
            if (player is Player_Pegasus pegasus)
            {
                return !pegasus.IsFlying || pegasus.IsFlyingFast;
            }

            return true;
        }
    }

    private bool checkPegasusFlyingFast
    {
        get
        {
            if (player is Player_Pegasus pegasus)
            {
                return pegasus.IsFlyingFast;
            }

            return false;
        }
    }

    public override void _Ready()
    {
        player = GetNode<Player>("../");
        playerRace = Global.Get().playerRace;
        playerSkeleton = GetNode<Skeleton>("Armature/Skeleton");
        headBone = playerSkeleton.GetNode<PhysicalBone>("Physical Bone neck");

        animTree = GetNode<AnimationTree>("animTree");
        playback = (AnimationNodeStateMachinePlayback)animTree.Get("parameters/StateMachine/playback");
        headBlend = (Vector2)animTree.Get("parameters/BlendSpace2D/blend_position");
        playback.Start(Character.IDLE_ANIM1);
    }

    public override void _Process(float delta)
    {
        if (player.Health > 0)
        {
            if (player.MayRotateHead) UpdateHeadRotation(delta);

            //update smiling
            if (bodyRot > 130 || bodyRot < -105)
            {
                if (smileCooldown < 5)
                {
                    smileCooldown += delta;
                }
                else
                {
                    Head.SmileOn();
                }
            }
            else
            {
                if (smileCooldown != 0)
                {
                    smileCooldown = 0;
                    Head.SmileOff();
                }
            }

            if (jumpingCooldown > 0)
            {
                jumpingCooldown -= delta;
            }

            if (player.IsCrouching)
            {
                notJumpingCooldown = 0.1f;
            }
            else if (notJumpingCooldown > 0)
            {
                notJumpingCooldown -= delta;
            }

            if (crouchingCooldown > 0)
            {
                crouchingCooldown -= delta;
            }

            AnimateMoving();
            UpdateBodyRotValue();
            SetRotationByBodyRot(delta);
        }
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventMouseMotion &&
            Input.MouseMode == Input.MouseModeEnum.Captured
            && player.MayRotateHead)
        {
            var mouseEvent = @event as InputEventMouseMotion;
            float mouseSensivity = player.MouseSensivity;
            float speedX = Mathf.Clamp(mouseEvent.Relative.x, -MAX_MOUSE_SPEED, MAX_MOUSE_SPEED) * -mouseSensivity;

            bool mayRotate = (player.ThirdView && !player.IsSitting && !player.Weapons.GunOn) ||
                             (mouseEvent.Relative.x < 0 && RotClumpsMin) ||
                             (mouseEvent.Relative.x > 0 && RotClumpsMax);
            if (mayRotate)
            {
                bodyRot -= speedX;
            }
        }
    }

    public void SetRotZero()
    {
        bodyRot = 0;
        Vector3 rot = RotationDegrees;
        rot.y = 0;
        RotationDegrees = rot;
    }

    private void UpdateHeadRotation(float delta)
    {
        var lookYAngle = (player.GetVerticalLook() / 60f - 0.1f) + walkOffset;
        //обрасываем нули, чтоб вращение головы не подрагивало
        string stringYAngle = System.String.Format("{0:0.00}", lookYAngle);
        headBlend.y = float.Parse(stringYAngle);

        if (IsMovementInput || jumpingCooldown > 0)
        {
            if (checkPegasusFlyingFast)
            {
                walkOffset = Mathf.MoveToward(walkOffset, 0.8f, 2 * delta);
            }
            else
            {
                walkOffset = Mathf.MoveToward(walkOffset, 0.4f, 2 * delta);
            }
        }
        else
        {
            if (player.IsCrouching && crouchingCooldown > 0)
            {
                walkOffset = Mathf.MoveToward(walkOffset, 0.4f, 2 * delta);
            }
            else
            {
                walkOffset = Mathf.MoveToward(walkOffset, 0.2f, 2 * delta);
            }
        }

        float rotX = 0;
        if (!player.BodyFollowsCamera)
        {
            if (bodyRot > 130f)
            {
                headBlend.y *= -1;
                rotX = (bodyRot - 200f) / 90f;
            }
            else if (bodyRot < -105f)
            {
                headBlend.y *= -1;
                rotX = (bodyRot + 159f) / 90f;
            }
            else
            {
                rotX = bodyRot / 90f;
            }
        }

        float speed = 0;
        if (IsMovementInput)
        {
            speed = (BODY_ROT_SPEED / 90f) * player.Velocity.Length();
            //обрасываем нули, чтоб вращение головы не подрагивало
            string parsedSpeed = System.String.Format("{0:0.00}", speed);
            speed = float.Parse(parsedSpeed);
        }
        else
        {
            speed = HEAD_ROT_SPEED;
        }

        headBlend.x = Mathf.MoveToward(headBlend.x, rotX, speed * delta);
        animTree.Set("parameters/BlendSpace2D/blend_position", headBlend);
    }

    private bool checkJumpKey => Input.IsActionJustPressed("jump") && player.MayMove && notJumpingCooldown <= 0f;


    private void AnimateWalkEarthpony(Player_Earthpony earthpony)
    {
        if (checkJumpKey && jumpingCooldown <= 0)
        {
            if (earthpony.IsRunning)
            {
                playback.Start("Jump-Run");
            }
            else
            {
                playback.Start("Jump");
            }

            jumpingCooldown = JUMP_COOLDOWN;
        }
        else
        {
            if (earthpony.IsRunning)
            {
                playback.Travel("Run");
            }
            else
            {
                AnimateWalking();
            }
        }
    }

    private void AnimateWalkPegasus(Player_Pegasus pegasus)
    {
        if (pegasus.IsFlying)
        {
            if (pegasus.IsFlyingFast)
            {
                playback.Travel("Fly");
            }
            else
            {
                if (Input.IsActionPressed("ui_left"))
                {
                    playback.Travel("Fly-Left");
                }
                else if (Input.IsActionPressed("ui_right"))
                {
                    playback.Travel("Fly-Right");
                }
                else
                {
                    playback.Travel("Fly-OnPlace");
                }
            }
        }
        else
        {
            if (checkJumpKey && jumpingCooldown <= 0)
            {
                playback.Start("Jump");
                jumpingCooldown = JUMP_COOLDOWN;
            }
            else
            {
                if (jumpingCooldown <= 0)
                {
                    AnimateWalking();
                }
            }
        }
    }

    private void AnimateWalkUnicorn()
    {
        if (checkJumpKey && jumpingCooldown <= 0)
        {
            playback.Start("Jump");
            jumpingCooldown = JUMP_COOLDOWN;
        }
        else
        {
            AnimateWalking();
        }
    }

    private void AnimateWalking()
    {
        if (Input.IsActionPressed("ui_up"))
        {
            playback.Travel(WALK_FORWARD);
        }
        else if (Input.IsActionPressed("ui_down"))
        {
            playback.Travel(WALK_BACKWARD);
        }
        else if (Input.IsActionPressed("ui_left") || Input.IsActionPressed("ui_right"))
        {
            playback.Travel(WALK_FORWARD);
        }
        else
        {
            var playerDir = player.Velocity * -player.Transform.basis.z;
            var playerMovingForward = playerDir.x + playerDir.z > 0;

            playback.Travel(playerMovingForward ? WALK_FORWARD : WALK_BACKWARD);
        }
    }

    private void AnimateIdlePegasus(Player_Pegasus pegasus)
    {
        if (pegasus.IsFlying)
        {
            playback.Travel("Fly-OnPlace");
        }
        else if (jumpingCooldown <= 0)
        {
            if (checkJumpKey)
            {
                playback.Start("Jump");
                jumpingCooldown = JUMP_COOLDOWN;
            }
            else
            {
                playback.Travel(Character.IDLE_ANIM1);
            }
        }
    }

    private void AnimateIdleEarthpony()
    {
        if (jumpingCooldown <= 0)
        {
            if (checkJumpKey)
            {
                playback.Start("Jump");
                jumpingCooldown = JUMP_COOLDOWN;
            }
            else
            {
                playback.Travel(Character.IDLE_ANIM1);
            }
        }
    }

    private void ClumpBodyRot()
    {
        float rotBeyond = MAX_ANGLE * 2; //180

        if (bodyRot < -rotBeyond)
        {
            bodyRot = rotBeyond;
        }
        else if (bodyRot > rotBeyond)
        {
            bodyRot = -rotBeyond;
        }
    }

    public void AnimateHitting(bool front)
    {
        if (front)
        {
            playback.Travel("HitFront");
        }
        else
        {
            playback.Travel("HitBack");
        }
    }

    public void MakeLying(bool lying)
    {
        if (lying)
        {
            playback.Travel("Lying");
        }
        else
        {
            playback.Travel("GetUp");
        }
    }

    public void MakeSitting(bool sitting)
    {
        if (sitting)
        {
            SetRotZero();
            playback.Start("Sit");
        }
        else
        {
            playback.Travel(Character.IDLE_ANIM1);
        }
    }

    public void SetHead(PlayerHead head)
    {
        Head = head;
    }

    public void AnimateDeath(Character killer)
    {
        playback.Travel(Character.IDLE_ANIM1);
        bodyRot = 0;
        playerSkeleton.PhysicalBonesStartSimulation();

        foreach (var boneObject in playerSkeleton.GetChildren())
        {
            if (boneObject is not PhysicalBone bone) continue;
            bone.CollisionLayer = 6; // слои 2 и 3
            bone.CollisionMask = 6; // слои 2 и 3
        }

        var dir = Translation.DirectionTo(killer.Translation);
        headBone.ApplyCentralImpulse(-dir * RAGDOLL_IMPULSE);
        SetProcess(false);
    }

    public void DetachFromPlayer()
    {
        foreach (Node node in GetChildren())
        {
            if (node.Name == "Armature") continue;
            node.SetProcess(false);
            node.QueueFree();
        }

        playerSkeleton.GetNode<MeshInstance>("Body").QueueFree();

        var thirdBody = playerSkeleton.GetNode<MeshInstance>("Body_third");
        thirdBody.Layers = 1;
        thirdBody.SetScript(null);
        SetScript(null);
    }

    private void AnimateMoving()
    {
        if (onetimeBodyRotBack)
        {
            bodyRot = RotationDegrees.y;
            onetimeBodyRotBack = false;
        }
        
        //Для анимирования сползания по лестнице проверяем через велосити
        if (IsVelocityMoving)
        {
            if (player.IsCrouching)
            {
                playback.Travel("Crouch");
                crouchingCooldown = CROUCH_COOLDOWN;
            }
            else if (player.MayMove)
            {
                crouchingCooldown = 0;

                switch (playerRace)
                {
                    case Race.Pegasus:
                        AnimateWalkPegasus(player as Player_Pegasus);
                        break;
                    case Race.Earthpony:
                        AnimateWalkEarthpony(player as Player_Earthpony);
                        break;
                    case Race.Unicorn:
                        AnimateWalkUnicorn();
                        break;
                }
            }
        }
        else if (!player.IsHitting && !player.IsSitting)
        {
            if (player.IsCrouching)
            {
                if (!player.BodyFollowsCamera && crouchingCooldown <= 0)
                {
                    playback.Travel("Sit");
                }
                else if (player.MayMove)
                {
                    playback.Travel("Crouch-idle");
                }
            }
            else
            {
                crouchingCooldown = 0;

                switch (playerRace)
                {
                    case Race.Pegasus:
                        AnimateIdlePegasus(player as Player_Pegasus);
                        break;
                    case Race.Earthpony:
                        AnimateIdleEarthpony();
                        break;
                    case Race.Unicorn:
                        AnimateIdleEarthpony();
                        break;
                }
            }
        }
    }

    private void UpdateBodyRotValue()
    {
        if (!player.MayMove || !IsMovementInput) return;

        bodyRot = 0;
        onetimeBodyRotBack = true;
        
        if (Input.IsActionPressed("ui_left") && checkPegasusFlying)
        {
            bodyRot = 90f;
            if (Input.IsActionPressed("ui_up"))
            {
                bodyRot = 45f;
            }
            else if (Input.IsActionPressed("ui_down"))
            {
                bodyRot = -45f;
            }
        }

        if (Input.IsActionPressed("ui_right") && checkPegasusFlying)
        {
            bodyRot = -90f;
            if (Input.IsActionPressed("ui_up"))
            {
                bodyRot = -45f;
            }
            else if (Input.IsActionPressed("ui_down"))
            {
                bodyRot = 45f;
            }
        }
    }

    private void SetRotationByBodyRot(float delta)
    {
        if (player.BodyFollowsCamera && player.MayMove)
        {
            SetRotZero();
        }
        else if (IsMovementInput && player.MayMove)
        {
            Vector3 rot = RotationDegrees;

            float speed = 0;
            if (checkPegasusFlyingFast)
            {
                speed = BODY_ROT_SPEED * 10f;
            }
            else
            {
                speed = BODY_ROT_SPEED * player.Velocity.Length();
            }

            rot.y = Mathf.MoveToward(rot.y, bodyRot, speed * delta);
            RotationDegrees = rot;
        }
        else
        {
            Vector3 rot = RotationDegrees;
            rot.y = bodyRot;

            RotationDegrees = rot;

            ClumpBodyRot();
        }
    }
}