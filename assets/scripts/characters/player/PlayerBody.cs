using Godot;
using Godot.Collections;

public class PlayerBody : Spatial
{
    //---------------------------------------------------+
    //скрипт анимирует тело и не дает ему поворачиваться,
    //пока угол между ним и головой не больше MAX_ANGLE
    //---------------------------------------------------+

    const float MAX_ANGLE = 90;
    const int MAX_MOUSE_SPEED = 450;
    const float OFFSET_SPEED = 3f;

    const float HEAD_ROT_SPEED = 5f;
    const float BODY_ROT_SPEED = 26f;

    const float CROUCH_COOLDOWN = 5f;
    const float JUMP_COOLDOWN = 0.7f;

    public PlayerHead Head {get; private set;}
    public PlayerLegs Legs;
    public SoundSteps SoundSteps;
    private Player player;
    private Race playerRace;

    private AnimationTree animTree;
    private AnimationNodeStateMachinePlayback playback;
    private Vector2 headBlend;

    private float walkOffset;
    private float jumpingCooldown;
    private float crouchingCooldown;
    private float smileCooldown;
    private float shyCooldown = 1.5f;

    public float bodyRot {get; private set;} = 0;
    private bool onetimeBodyRotBack;

    public bool RotClumpsMin { get  => bodyRot < MAX_ANGLE + 1; }

    public bool RotClumpsMax { get => bodyRot > -MAX_ANGLE - 1; }

    public void SetRotZero() 
    {
        bodyRot = 0;
        Vector3 rot = RotationDegrees;
        rot.y = 0;
        RotationDegrees = rot;
    }

    private bool isWalking {
        get {
            return player.MayMove && (
                Input.IsActionPressed("ui_left") ||
                Input.IsActionPressed("ui_right") ||
                Input.IsActionPressed("ui_up") ||
                Input.IsActionPressed("ui_down")
            );
        }
    }

    private bool playerMakingShy {
        get {
            if (player.IsHitting && !Legs.getTempFront) {
                if (bodyRot > 17f && bodyRot < 44f && headBlend.y > 0.4f) {
                    return true;
                }
            } else {
                if (bodyRot < 61 && bodyRot > 27 && headBlend.y > 1) {
                    return true;
                }
            }
            return false;
        }
    }

    private bool checkPegasusFlying {
        get {
            if(player is Player_Pegasus) {
                var pegasus = player as Player_Pegasus;
                return !pegasus.IsFlying || pegasus.IsFlyingFast;
            }
            return true;
        }
    }

    private bool checkPegasusFlyingFast {
        get {
            if(player is Player_Pegasus) {
                var pegasus = player as Player_Pegasus;
                return pegasus.IsFlyingFast;
            }
            return false;
        }
    }

    private void updateHeadRotation(float delta) 
    {
        var lookYAngle = (player.GetVerticalLook() / 60f - 0.1f) + walkOffset;
        //обрасываем нули, чтоб вращение головы не подрагивало
        string stringYAngle = System.String.Format("{0:0.00}", lookYAngle);  
        headBlend.y = float.Parse(stringYAngle);

        if (isWalking || jumpingCooldown > 0) {
            if (checkPegasusFlyingFast) {
                walkOffset = Mathf.MoveToward(walkOffset, 0.8f, 2 * delta);
            } else {
                walkOffset = Mathf.MoveToward(walkOffset, 0.4f, 2 * delta);
            }
        } else {
            if (player.IsCrouching && crouchingCooldown > 0) {
                walkOffset = Mathf.MoveToward(walkOffset, 0.4f, 2 * delta);
            }  else {
                walkOffset = Mathf.MoveToward(walkOffset, 0.2f, 2 * delta);
            }
        }

        float rotX = 0;
        if (!player.BodyFollowsCamera) {
            if (bodyRot > 130f) {
                headBlend.y *= -1;
                rotX = (bodyRot - 200f) / 90f;
            } else if (bodyRot < -105f) {
                headBlend.y *= -1;
                rotX = (bodyRot + 159f) / 90f;
            } else {
                rotX = bodyRot / 90f;
            }
        }

        float speed = 0;
        if (isWalking) {
            speed = (BODY_ROT_SPEED / 90f) * player.Velocity.Length();
            //обрасываем нули, чтоб вращение головы не подрагивало
            string parsedSpeed = System.String.Format("{0:0.00}", speed);  
            speed = float.Parse(parsedSpeed);
        } else {
            speed = HEAD_ROT_SPEED;
        }

        headBlend.x = Mathf.MoveToward(headBlend.x, rotX, speed * delta);
        animTree.Set("parameters/BlendSpace2D/blend_position", headBlend);
    }

    
    private void AnimateWalkEarthpony(Player_Earthpony earthpony) 
    {
        if (Input.IsActionJustPressed("jump") && jumpingCooldown <= 0) {
            if (earthpony.IsRunning) {
                playback.Start("Jump-Run");
            } else {
                playback.Start("Jump");
            }
            jumpingCooldown = JUMP_COOLDOWN;
        } else {
            if (earthpony.IsRunning) {
                playback.Travel("Run");
            } else {
                playback.Travel("Walk");
            }
        }
    }

    private void AnimateWalkPegasus(Player_Pegasus pegasus) 
    {
        if (pegasus.IsFlying) {
            if (pegasus.IsFlyingFast) {
                playback.Travel("Fly");
            } else {
                if (Input.IsActionPressed("ui_left")) {
                    playback.Travel("Fly-Left");
                } else if (Input.IsActionPressed("ui_right")) {
                    playback.Travel("Fly-Right");
                } else {
                    playback.Travel("Fly-OnPlace");
                }
            }
        } else {
            if (Input.IsActionJustPressed("jump") && jumpingCooldown <= 0) {
                playback.Start("Jump");
                jumpingCooldown = JUMP_COOLDOWN;
            } else {
                if (jumpingCooldown <= 0) {
                    playback.Travel("Walk");
                }
            }
        }
    }

    private void AnimateIdlePegasus(Player_Pegasus pegasus) 
    {
        if (pegasus.IsFlying) {
            playback.Travel("Fly-OnPlace");
        } else if (jumpingCooldown <= 0) {
            if(Input.IsActionJustPressed("jump") && !player.BlockJump) {
                playback.Start("Jump");
			    jumpingCooldown = JUMP_COOLDOWN;
            } else {
                playback.Travel("Idle1");
            }
        }
    }

    private void AnimateIdleEarthpony(Player_Earthpony earthpony) 
    {
        if (jumpingCooldown <= 0) {
            if(Input.IsActionJustPressed("jump") && !player.BlockJump) {
                playback.Start("Jump");
                jumpingCooldown = JUMP_COOLDOWN;
            } else {
                playback.Travel("Idle1");
            }
        }
    }

    private void ClumpBodyRot() 
    {
        float rotBeyond = MAX_ANGLE * 2; //180

        if (bodyRot < -rotBeyond) {
            bodyRot = rotBeyond;
        } else if (bodyRot > rotBeyond) {
            bodyRot = -rotBeyond;
        }
    }

    public void AnimateHitting(bool front)
    {
        if (front) {
            playback.Travel("HitFront");
        } else {
            playback.Travel("HitBack");
        }
    }

    public void MakeLying(bool lying) 
    {
        if (lying) {
            playback.Travel("Lying");
        } else {
            playback.Travel("GetUp");
        }
    }

    public void SetHead(PlayerHead head) 
    {
        Head = head;
    }

    public override void _Ready()
    {
        player = GetNode<Player>("../");
        playerRace = Global.Get().playerRace;

        Legs = GetNode<PlayerLegs>("frontArea");

        SoundSteps = GetNode<SoundSteps>("floorRay");

        animTree = GetNode<AnimationTree>("animTree");
        playback = (AnimationNodeStateMachinePlayback)animTree.Get("parameters/StateMachine/playback");
        headBlend = (Vector2)animTree.Get("parameters/BlendSpace2D/blend_position");
        playback.Start("Idle1");
    }

    public override void _Process(float delta)
    {
        if (player.Health > 0) {
            updateHeadRotation(delta);

            //update smiling
            if(bodyRot > 130 || bodyRot < -105) {
                if(smileCooldown < 5) {
                    smileCooldown += delta;
                } else {
                    Head.SmileOn();
                }
            } else {
                if(smileCooldown != 0) {
                    smileCooldown = 0;
                    Head.SmileOff();
                }
            }

            //update shy when in coat
            if(player.inventory.GetArmorProps().Contains("makeShy") && playerMakingShy) {
                if (shyCooldown > 0) {
                    shyCooldown -= delta;
                } else {
                    shyCooldown = 1.5f;
                    Head.ShyOn();
                }
            }

            if(jumpingCooldown > 0) {
                jumpingCooldown -= delta;
            }

            if(crouchingCooldown > 0) {
                crouchingCooldown -= delta;
            }

            if (isWalking) {
                if (player.IsCrouching) {
                    playback.Travel("Crouch");
                    crouchingCooldown = CROUCH_COOLDOWN;
                } else {
                    crouchingCooldown = 0;

                    switch (playerRace) {
                        case Race.Pegasus:
                            AnimateWalkPegasus(player as Player_Pegasus);
                            break;
                        case Race.Earthpony:
                            AnimateWalkEarthpony(player as Player_Earthpony);
                            break;
                        case Race.Unicorn:
                            playback.Travel("Walk");
                            break;
                    }
                }
                bodyRot = 0;
                onetimeBodyRotBack = true;
            }
            else if (!player.IsHitting) {
                if(player.IsCrouching) {
                    if(!player.BodyFollowsCamera && crouchingCooldown <= 0) {
                        playback.Travel("Sit");
                    } else {
                        playback.Travel("Crouch-idle");
                    }
                } else {
                    crouchingCooldown = 0;

                    switch(playerRace) {
                        case Race.Pegasus:
                            AnimateIdlePegasus(player as Player_Pegasus);
                            break;
                        case Race.Earthpony:
                            AnimateIdleEarthpony(player as Player_Earthpony);
                            break;
                        case Race.Unicorn:
                            playback.Travel("Idle1");
                            break;
                    }
                }

                if (onetimeBodyRotBack) {
                    bodyRot = RotationDegrees.y;
                    onetimeBodyRotBack = false;
                }    
            }

            if (player.MayMove) {
                if (Input.IsActionPressed("ui_left") && checkPegasusFlying) {
                    bodyRot = 90f;
                    if (Input.IsActionPressed("ui_up")) {
                        bodyRot = 45f;
                    } else if (Input.IsActionPressed("ui_down")) {
                        bodyRot = -45f;
                    }
                }
                if (Input.IsActionPressed("ui_right") && checkPegasusFlying) {
                    bodyRot = -90f;
                    if (Input.IsActionPressed("ui_up")) {
                        bodyRot = -45f;
                    } else if (Input.IsActionPressed("ui_down")) {
                        bodyRot = 45f;
                    }
                }
            }
            

            if (player.BodyFollowsCamera) {
                SetRotZero();
            } else if (isWalking) {

                Vector3 rot = RotationDegrees;

                float speed = 0;
                if (checkPegasusFlyingFast) {
                    speed = BODY_ROT_SPEED * 10f;
                } else {
                    speed = BODY_ROT_SPEED * player.Velocity.Length();
                }
               
                rot.y = Mathf.MoveToward(rot.y, bodyRot, speed * delta);
                RotationDegrees = rot;
            } else {

                Vector3 rot = RotationDegrees;
                rot.y = bodyRot;
                RotationDegrees = rot;

                ClumpBodyRot();

            }
        } else { //Health <= 0
            bodyRot = 0;
            playback.Travel("Die1");
        }
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventMouseMotion && 
            Input.GetMouseMode() == Input.MouseMode.Captured) {
                var mouseEvent = @event as InputEventMouseMotion;
                float mouseSensivity = player.MouseSensivity;
                float speedX = Mathf.Clamp(mouseEvent.Relative.x, -MAX_MOUSE_SPEED, MAX_MOUSE_SPEED) * -mouseSensivity;

                bool mayRotate = ((player.ThirdView && !player.IsLying && !player.Weapons.GunOn) ||
                                 mouseEvent.Relative.x < 0 && RotClumpsMax) ||
                                 (mouseEvent.Relative.x > 0 && RotClumpsMin);
                
                if (mayRotate) {
                    bodyRot -= speedX;
                }
            }
    }
}
