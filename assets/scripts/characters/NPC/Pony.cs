using Godot;

//Класс для пней-неписей
//Также включает в себя единорогов, которые умеют только в левитацию
public class Pony: NpcWithWeapons
{
    const float RUN_DISTANCE = 12f;
    
    [Export]
    public int RunSpeed = 10;

    [Export]
    public bool IsUnicorn = false;
    public Particles MagicParticles;

    public bool IsRunning = false;
    public float RotationToVictim = 0f;

    public NPCBody body;
    
    private float doorWait = 0;

    private RandomNumberGenerator rand = new RandomNumberGenerator();
    
    public override void SetState(NPCState newState)
    {
        if (Health <= 0) {
            return;
        }

        base.SetState(newState);

        switch(newState) {
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
    
    public override Spatial GetWeaponParent(bool isPistol)
    {
        if (IsUnicorn) {
            return GetNode<Spatial>("levitation/weapons");
        }
        if (isPistol) {
            return GetNode<Spatial>("Armature/Skeleton/BoneAttachment/weapons");
        } 
        
        return GetNode<Spatial>("Armature/Skeleton/BoneAttachment 2/weapons");
    }
    
    private void FinishGoingTo()
    {
        Stop();
        cameToPlace = true;
        EmitSignal(nameof(IsCame));
    }

    protected override void MoveToPoint(float tempDistance, bool mayRun)
    {
        if (mayRun && tempDistance > RUN_DISTANCE) {
            body.PlayAnim("Run");
            MoveTo(path[pathI], COME_DISTANCE, RunSpeed);
            IsRunning = true;
        } else {
            body.PlayAnim("Walk");
            MoveTo(path[pathI], COME_DISTANCE, WalkSpeed);
            IsRunning = false;
        }
    }

    protected override void LookAtTarget(bool rotateBody = true)
    {
        if (tempVictim == null) {
            RotationToVictim = 0;
            return;
        }

        var npcForward = -GlobalTransform.basis.z;
        var npcDir = body.GetDirToTarget(tempVictim);
        RotationToVictim = npcForward.AngleTo(npcDir);

        if (weapons.isPistol || !rotateBody) {
            if (Mathf.Rad2Deg(RotationToVictim) < 80) {
                return;
            }
        }

        Vector3 victimPos = tempVictim.GlobalTransform.origin;
        victimPos.y = GlobalTransform.origin.y;
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
    }


    public void _on_lookArea_body_entered(Node body)
    {
        if (body is Player)
        {
            lookAreaEntered = true;
        }
        
        this.body._on_lookArea_body_entered(body);
    }

    public void _on_lookArea_body_exited(Node body)
    {
        if (body is Player)
        {
            lookAreaEntered = false;
        }
        
        this.body._on_lookArea_body_exited(body);
    }

 
    public override void _Ready()
    {
        if (IsUnicorn) {
            MagicParticles = GetNode<Particles>("Armature/Skeleton/BoneAttachment/HeadPos/Particles");
        }
        
        base._Ready();
        rand.Randomize();
        body = new NPCBody(this);
    }

    public override void _Process(float delta)
    {
        if (Health <= 0) {
            return;
        }

        base._Process(delta);
        body.Update(delta);
        UpdatePath(delta);
        UpdateAI(delta);
    }
}
