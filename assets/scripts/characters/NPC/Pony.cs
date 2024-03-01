using System;
using Godot;
using Godot.Collections;

//Класс для пней-неписей
//Также включает в себя единорогов, которые умеют только в левитацию
public partial class Pony: NpcWithWeapons
{
    const float RUN_DISTANCE = 12f;
    
    [Export]
    public int RunSpeed = 10;

    [Export]
    public bool IsUnicorn = false;
    public GpuParticles3D MagicParticles;

    public bool IsRunning = false;
    public float RotationToVictim = 0f;

    public NPCBody body;
    
    private bool RunToPoint;
    public bool stayInPoint;

    private RandomNumberGenerator rand = new RandomNumberGenerator();
    
    public override void SetState(NPCState newState)
    {
        if (Health <= 0) 
        {
            return;
        }

        if (stayInPoint)
        {
            state = newState == NPCState.Talk ? newState : NPCState.Idle;
            return;
        }

        base.SetState(newState);

        switch(newState) 
        {
            case NPCState.Idle:
                body.lookTarget = null;
                break;
            
            case NPCState.Attack:
                body.lookTarget = tempVictim;
                break;
            
            case NPCState.Search:
                body.lookTarget = null;
                break;
        }
    }

    protected override void AnimateDeath(Character killer, int shapeID)
    {
        PlayStopAnim();
        base.AnimateDeath(killer, shapeID);
    }
    
    public override Node3D GetWeaponParent(bool isPistol)
    {
        if (IsUnicorn) 
        {
            return GetNode<Node3D>("levitation/weapons");
        }
        if (isPistol) 
        {
            return GetNode<Node3D>("Armature/Skeleton3D/BoneAttachment3D/weapons");
        } 
        
        return GetNode<Node3D>("Armature/Skeleton3D/BoneAttachment3D 2/weapons");
    }

    public override void SetNewStartPos(Vector3 newPos, bool run = false)
    {
        cameToPlace = false;
        RunToPoint = run;
        stayInPoint = false;
        base.SetNewStartPos(newPos, run);
    }

    public override void SetFollowTarget(Character newTarget)
    {
        cameToPlace = false;
        stayInPoint = false;
        base.SetFollowTarget(newTarget);
    }
    
    protected override void FinishGoingTo()
    {
        RunToPoint = false;
        base.FinishGoingTo();
    }

    protected override void MoveToPoint(float tempDistance, bool mayRun)
    {
        if ((RunToPoint || mayRun) && tempDistance > RUN_DISTANCE) 
        {
            body.PlayAnim("Run");
            MoveTo(path[pathI], COME_DISTANCE, RunSpeed);
            IsRunning = true;
        } 
        else 
        {
            body.PlayAnim("Walk");
            MoveTo(path[pathI], COME_DISTANCE, WalkSpeed);
            IsRunning = false;
        }
    }

    protected override void LookAtTarget(bool rotateBody = true)
    {
        if (tempVictim == null) 
        {
            RotationToVictim = 0;
            return;
        }

        var npcForward = -GlobalTransform.Basis.Z;
        var npcDir = body.GetDirToTarget(tempVictim);
        RotationToVictim = npcForward.AngleTo(npcDir);

        if (weapons.isPistol || !rotateBody) 
        {
            if (Mathf.RadToDeg(RotationToVictim) < 80) 
            {
                return;
            }
        }

        Vector3 victimPos = tempVictim.GlobalTransform.Origin;
        victimPos.Y = GlobalTransform.Origin.Y;
        LookAt(victimPos, Vector3.Up);
    }

    protected override void PlayStopAnim()
    {
        body.PlayAnim("Idle1");
    }
    
    protected override void PlayIdleAnim()
    {
        if (!string.IsNullOrEmpty(IdleAnim))
        {
            body.PlayAnim(IdleAnim);
        } 
        else
        {
            body.StopAnim();
        }
    }

    public override Dictionary GetSaveData()
    {
        var saveData = base.GetSaveData();
        saveData["runSpeed"] = RunSpeed;
        saveData["runToPoint"] = RunToPoint;
        saveData["stayInPoint"] = stayInPoint;
        if (GetNodeOrNull<NPCFace>("Armature/Skeleton3D/Body") is NPCFace npcFace)
        {
            saveData["face"] = npcFace.GetSaveData();
        }
        
        return saveData;
    }

    public override void LoadData(Dictionary data)
    {
        base.LoadData(data);
        RunSpeed = Convert.ToInt16(data["runSpeed"]);
        RunToPoint = Convert.ToBoolean(data["runToPoint"]);
        stayInPoint = Convert.ToBoolean(data["stayInPoint"]);
        if (!data.ContainsKey("face")) return;
        
        Dictionary faceData = data["face"].AsGodotDictionary();
        GetNode<NPCFace>("Armature/Skeleton3D/Body").LoadData(faceData);
    }
    
    public void _on_lookArea_body_entered(Node body)
    {
        this.body._on_lookArea_body_entered(body);
    }

    public void _on_lookArea_body_exited(Node body)
    {
        this.body._on_lookArea_body_exited(body);
    }

    public void _on_stopArea_body_entered(Node body)
    {
        if (body is Player)
        {
            stopAreaEntered = true;
        }

        if (body is FurnDoor door && !door.IsOpen)
        {
            SetDoorWait(door.ClickFurn());
        }
    }
    
    public void _on_stopArea_body_exited(Node body)
    {
        if (body is Player)
        {
            stopAreaEntered = false;
        }
    }

    public override void _Ready()
    {
        if (IsUnicorn) 
        {
            MagicParticles = GetNode<GpuParticles3D>("Armature/Skeleton3D/BoneAttachment3D/HeadPos/Particles");
        }
        
        base._Ready();
        rand.Randomize();
        body = new NPCBody(this);
        PlayIdleAnim();
    }

    public override void _Process(double delta)
    {
        base._Process(delta);
        if (Health <= 0) 
        {
            return;
        }
        
        body.Update((float)delta);
        UpdatePath((float)delta);
        UpdateAI((float)delta);
    }
}
