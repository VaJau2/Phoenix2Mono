using Godot;
using Godot.Collections;

public class RoboEye : NPC
{
    [Export] public float COME_DISTANCE = 3.5f;
    [Export] public AudioStreamSample walkSound;

    public bool IsActive { get; private set; } = true;
    
    private AnimationPlayer anim;
    private bool cameToPlace;
    private Vector3[] path;
    private int pathI;

    private string tempMaterial;
    
    [Signal]
    public delegate void FoundEnemy();

    public override Dictionary GetSaveData()
    {
        var data = base.GetSaveData();
        data["IsActive"] = IsActive;
        return data;
    }

    public override void LoadData(Dictionary data)
    {
        base.LoadData(data);
        var active = (bool) data["IsActive"];
        if (!active)
        {
            MakeActive(false);
        }
    }

    public void MakeActive(bool active)
    {
        var newState = active ? NPCState.Search : NPCState.Idle;
        SetState(newState);
        
        if (active)
        {
            if (Health == 0) Resurrect();
        }
        else
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
    
    protected override void AnimateDeath(Character killer, int shapeID)
    {
        anim.Play("Die");
        base.AnimateDeath(killer, shapeID);
    }

    private async void Resurrect()
    {
        CollisionLayer = 1;
        CollisionMask = 1;
        
        anim.PlayBackwards("Die");

        await ToSignal(anim, "animation_finished");

        Health = HealthMax;
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
            path = NavigationServer.MapGetPath(GetWorld().NavigationMap, pos, place, true);
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

    private void OnCameToPlace(Vector3 pos, float angle)
    {
        GlobalTransform = Global.SetNewOrigin(GlobalTransform, pos);
        Rotation = new Vector3
        (
            Rotation.x,
            angle,
            Rotation.z
        );
    }
    
    private void UpdateAI(float delta)
    {
        switch (state) 
        {
            case NPCState.Idle:
                if (patrolPoints == null || patrolPoints.Length == 0) 
                {
                    if (!cameToPlace)
                    {
                        GoTo(myStartPos, COME_DISTANCE, false);
                        
                    } 
                    else 
                    {
                        OnCameToPlace(myStartPos, myStartRot.y);
                        anim.Play(IdleAnim);
                    }
                } 
                else 
                {
                    if (patrolWaitTimer > 0)
                    {
                        patrolWaitTimer -= delta;
                    } 
                    else 
                    {
                        GoTo(patrolPoints[patrolI].GlobalTransform.origin, COME_DISTANCE, false);
                        
                        if (cameToPlace) 
                        {
                            OnCameToPlace
                            (
                                patrolPoints[patrolI].GlobalTransform.origin, 
                                patrolPoints[patrolI].GlobalRotation.y
                            );

                            patrolI = patrolI < patrolPoints.Length - 1 
                                ? patrolI + 1 
                                : 0;
                            
                            patrolWaitTimer = PATROL_WAIT;
                        }
                    }
                }

                break;
            case NPCState.Attack:
                if (tempVictim.Health <= 0) 
                {
                    SetState(NPCState.Idle);
                    return;
                }
                EmitSignal(nameof(FoundEnemy));
                Stop();
                
                break;
            case NPCState.Search:
                if (cameToPlace) 
                {
                    if (searchTimer > 0) 
                    {
                        searchTimer -= delta;
                    } 
                    else 
                    {
                        SetState(NPCState.Idle);
                    }
                }
                else 
                {
                    GoTo(lastSeePos, COME_DISTANCE);
                }
                break;
        }
    }
    
    public override void _Ready()
    {
        base._Ready();
        
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
