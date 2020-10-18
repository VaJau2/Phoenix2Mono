using Godot;
using System;

public class PlayerBody : Spatial
{
    //---------------------------------------------------+
    //скрипт анимирует тело и не дает ему поворачиваться,
    //пока угол между ним и головой не больше MAX_ANGLE
    //---------------------------------------------------+

    const float MAX_ANGLE = 90;
    const int MAX_MOUSE_SPEED = 450;
    const float OFFSET_SPEED = 3f;

    const float HEAD_ROT_SPEED = 4f;
    const float BODY_ROT_SPEED = 20f;

    const float CROUCH_COOLDOWN = 5f;
    const float JUMP_COOLDOWN = 0.6f;

    public PlayerHead Head;

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

    public bool RotClumpsMin { get  => bodyRot < MAX_ANGLE; }

    public bool RotClumpsMax { get => bodyRot > -MAX_ANGLE; }

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
            if (player.IsHitting && !player.LegsHit.TempFront) {
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
            return true;
        }
    }

    private void updateHeadRotation(float delta) 
    {
        headBlend.y = (player.GetVerticalLook() / 60f) + walkOffset;
        if (isWalking || jumpingCooldown > 0) {
            if (checkPegasusFlyingFast) {
                if (walkOffset < 0.7) {
                    walkOffset += OFFSET_SPEED * delta;
                }
            }
            if (walkOffset < 0.3f) {
                walkOffset += (OFFSET_SPEED - 1f) * delta;
            } else if (walkOffset > 0.4f) {
                walkOffset -= OFFSET_SPEED * delta;
            }
        } else {
            if (player.IsCrouching && crouchingCooldown > 0) {
                if (walkOffset < 0.3f) {
                    walkOffset += (OFFSET_SPEED - 1f) * delta;
                } else if (walkOffset > 0.4f) {
                    walkOffset -= OFFSET_SPEED * delta;
                }
            }  else {
                if (walkOffset > 0.1f) {
                    walkOffset -= OFFSET_SPEED * delta;
                }
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
        headBlend.x = Mathf.MoveToward(headBlend.x, rotX, HEAD_ROT_SPEED * delta);
        animTree.Set("parameters/BlendSpace2D/blend_position", headBlend);
    }

    
    private void AnimateWalkEarthpony(Player_Earthpony earthpony) 
    {
        if (Input.IsActionJustPressed("jump")) {
            if (earthpony.IsRunning) {
                playback.Travel("Jump-Run");
            } else {
                playback.Travel("Jump");
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
            if (Input.IsActionJustPressed("jump")) {
                playback.Travel("Jump");
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
        } else {
            if(Input.IsActionJustPressed("jump") && !player.BlockJump) {
                playback.Travel("Jump");
			    jumpingCooldown = JUMP_COOLDOWN;
            } else if(jumpingCooldown <= 0) {
                playback.Travel("Idle1");
            }
        }
    }

    private void AnimateIdleEarthpony(Player_Earthpony earthpony) 
    {
        if(Input.IsActionJustPressed("jump") && !player.BlockJump) {
            playback.Travel("Jump");
            jumpingCooldown = JUMP_COOLDOWN;
        } else if(jumpingCooldown <= 0) {
            playback.Travel("Idle1");
        }
    }

    private void ClumpBodyRot() 
    {
        if (bodyRot < -180) {
            bodyRot = 180;
        } else if (bodyRot > 180) {
            bodyRot = -180;
        }
    }

    private void LoadBodyRopedMesh() 
    {
        string race = "0";
        if (playerRace == Race.Pegasus) {
            race = "1";
        }

        MeshInstance bodyMesh = GetNode<MeshInstance>("player_body/Armature/Skeleton/Body");
        string path = "res://assets/models/player_variants/body/roped" + race + ".res";
        Mesh loadedMesh = GD.Load<Mesh>(path);
        bodyMesh.Mesh = loadedMesh;
    }

    public void AnimateUnroping(bool unroping) 
    {
        if (unroping) {
            playback.Travel("Unroping");
        } else {
            playback.Travel("Roped");
        }
        
    }

    public void MakeRoped(bool roped) 
    {
        if(roped) {
            playback.Travel("Roped");
            LoadBodyRopedMesh();
        } else {
            playback.Travel("Lying");
            player.LoadBodyMesh(playerRace);
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

    public override void _Ready()
    {
        player = GetNode<Player>("../");
        playerRace = Global.Get().playerRace;
        Head = GetNode<PlayerHead>("Armature/Skeleton/BoneAttachment/Head");

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
            if(player.HaveCoat && playerMakingShy) {
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
            else if (player.MayMove) {
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

                bodyRot = 0;
                Vector3 rot = RotationDegrees;
                rot.y = 0;
                RotationDegrees = rot;
            } else if (isWalking && !player.IsRoped) {

                Vector3 rot = RotationDegrees;

                float speed = 0;
                if (checkPegasusFlyingFast) {
                    speed = BODY_ROT_SPEED * 10f;
                } else {
                    speed = BODY_ROT_SPEED * player.GetSpeed();
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
                float mouseSensivity = player.GetSensivity();
                float speedX = Mathf.Clamp(mouseEvent.Relative.x, -MAX_MOUSE_SPEED, MAX_MOUSE_SPEED) * -mouseSensivity;

                bool mayRotate = ((player.ThirdView && !player.IsLying) ||
                                 mouseEvent.Relative.x < 0 && RotClumpsMax) ||
                                 (mouseEvent.Relative.x > 0 && RotClumpsMin);
                
                if (mayRotate) {
                    bodyRot -= speedX;
                }
            }
    }
}
