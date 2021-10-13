using Godot;

public class RoboEye : NPC
{
    [Export] public float COME_DISTANCE = 3.5f;
    [Export] public AudioStreamSample walkSound;

    public bool IsActive { get; private set; } = true;
    
    private AnimationPlayer anim;
    private Navigation navigation;
    private bool cameToPlace;
    private Vector3[] path;
    private int pathI;

    private string tempMaterial;
    
    [Signal]
    public delegate void FoundEnemy();

    public void MakeActive(bool active)
    {
        SetState(NPCState.Idle);
        if (!active)
        {
            anim.CurrentAnimation = null;
        }
        
        ChangeMaterial(active ? "" : "dead");
        IsActive = active;
    }

    public override void SetState(NPCState newState)
    {
        if (!IsActive) return;
        
        base.SetState(newState);

        switch (newState)
        {
            case NPCState.Attack:
                ChangeMaterial("red");
                break;
            case NPCState.Search:
                ChangeMaterial("orange");
                break;
            default:
                ChangeMaterial();
                break;
        }
    }

    private void ChangeMaterial(string materialName = "")
    {
        if (tempMaterial == materialName) return;
        
        string path = "res://assets/models/characters/robots/roboEye/roboEye";
        path += (materialName != "") ? "-" + materialName : "";
        path += ".material";
        SpatialMaterial newMaterial = GD.Load<SpatialMaterial>(path);
        
        GetNode<MeshInstance>("corpus/monitor/screen").SetSurfaceMaterial(0, newMaterial);
        GetNode<MeshInstance>("corpus/monitor/lamp").SetSurfaceMaterial(0, newMaterial);
        tempMaterial = materialName;
    }
    
    protected override void AnimateDealth(Character killer, int shapeID)
    {
        anim.Play("Die");
        base.AnimateDealth(killer, shapeID);
    }
    
    private void Stop(bool MoveDown = false)
    {
        if (!(Velocity.Length() > 0)) return;
        anim.Play(IdleAnim);
        path = null;
        pathI = 0;
        Velocity = new Vector3(0, MoveDown ? -GRAVITY : 0, 0);
    }

    private void FinishGoingTo()
    {
        Stop();
        cameToPlace = true;
    }
    
    private void GoTo(Vector3 place, float distance, bool mayRun = true)
    {
        cameToPlace = false;
        var pos = GlobalTransform.origin;

        var tempDistance = pos.DistanceTo(place);
        if (tempDistance < distance)
        {
            FinishGoingTo();
            return;
        }

        if (path == null)
        {
            path = navigation.GetSimplePath(pos, place, true);
            pathI = 0;
        }

        if (path.Length == 0)
        {
            Stop();
            return;
        }

        MoveTo(path[pathI], COME_DISTANCE, WalkSpeed);
        anim.Play("walk");
        if (!audi.Playing)
        {
            audi.Stream = walkSound;
            audi.Play();
        }

        if (CloseToPoint)
        {
            if (pathI < path.Length - 1)
            {
                pathI += 1;
            }
            else
            {
                FinishGoingTo();
            }
        }
    }
    
    private void UpdateAI(float delta)
    {
        switch (state) {
            case NPCState.Idle:
                if (patrolPoints == null || patrolPoints.Length == 0) {
                    if (!cameToPlace) {
                        GoTo(myStartPos, COME_DISTANCE, false);
                        
                    } else {
                        GlobalTransform = Global.setNewOrigin(GlobalTransform, myStartPos);
                        Rotation = new Vector3(
                            Rotation.x,
                            myStartRot.y,
                            Rotation.z
                        );
                        anim.Play(IdleAnim);
                    }
                } else {
                    if (patrolWaitTimer > 0) {
                        patrolWaitTimer -= delta;
                    } else {
                        GoTo(patrolPoints[patrolI].GlobalTransform.origin, COME_DISTANCE, false);
                        
                        if (cameToPlace) {
                            if (patrolI < patrolPoints.Length - 1) {
                                patrolI += 1;
                            } else {
                                patrolI = 0;
                            }
                            patrolWaitTimer = PATROL_WAIT;
                        }
                    }
                }

                break;
            case NPCState.Attack:
                if (tempVictim.Health <= 0) {
                    SetState(NPCState.Idle);
                    return;
                }
                EmitSignal(nameof(FoundEnemy));
                Stop();
                
                break;
            case NPCState.Search:
                if (cameToPlace) {
                    if (searchTimer > 0) {
                        searchTimer -= delta;
                    } else {
                        SetState(NPCState.Idle);
                    }
                } else {
                    GoTo(lastSeePos, COME_DISTANCE);
                }
                break;
        }
    }
    
    public override void _Ready()
    {
        base._Ready();
        
        navigation = GetNode<Navigation>("/root/Main/Scene/Navigation");
        anim = GetNode<AnimationPlayer>("anim");
        anim.Play(IdleAnim);
    }
    
    public override void _Process(float delta)
    {
        if (Health <= 0)
        {
            return;
        }

        if (!IsActive)
        {
            Stop();
            return;
        }

        base._Process(delta);
        UpdateAI(delta);
    }
}
