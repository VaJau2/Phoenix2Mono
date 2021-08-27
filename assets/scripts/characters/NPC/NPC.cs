using System;
using Godot;
using Godot.Collections;

//класс отвечает за поведение НПЦ
public class NPC : Character
{
    const int RAGDOLL_IMPULSE = 700;
    const float SEARCH_TIMER = 5f;
    private float ROTATION_SPEED = 0.15f;
    protected float GRAVITY = 6;
    protected float PATROL_WAIT = 4f;
    [Export]
    public Array<NodePath> patrolArray;
    protected Spatial[] patrolPoints;
    protected int patrolI = 0;
    protected float patrolWaitTimer = 0;
    [Export]
    public string IdleAnim = "";

    [Export] 
    public bool IsImmortal;
    [Export]
    public int StartHealth = 100;
    [Export]
    public Relation relation;
    [Export]
    public string dialogueCode = "";
    [Export]
    public int WalkSpeed = 5;

    [Export] public float lookHeightFactor = 1;
    public bool aggressiveAgainstPlayer;
    public bool ignoreDamager;
    public NPCState state;
    public SeekArea seekArea {get; private set;}
    protected AudioStreamPlayer3D audi;
    private Skeleton skeleton;
    [Export] private NodePath headBonePath, bodyBonePath;
    private Dictionary<string, bool> objectsChangeActive = new Dictionary<string, bool>();
    private PhysicalBone headBone;
    private PhysicalBone bodyBone;
    private bool tempShotgunShot; //для увеличения импульса при получении урона от дробовика

    private Player player => Global.Get().player;
    public Character tempVictim;
    protected Vector3 lastSeePos;
    protected float searchTimer = 0;

    protected bool CloseToPoint = false;

    public Vector3 myStartPos, myStartRot;


    public virtual void SetState(NPCState newState)
    {
        if (Health <= 0) {
            return;
        }

        switch(newState) {
            case NPCState.Idle:
                if (tempVictim == player) {
                    player.Stealth.RemoveSeekEnemy(this);
                }
                tempVictim = null;
                break;
            
            case NPCState.Attack:
                if (tempVictim == player) {
                    player.Stealth.AddAttackEnemy(this);
                }
                break;
            
            case NPCState.Search:
                if (tempVictim == player) {
                    player.Stealth.AddSeekEnemy(this);
                }

                lastSeePos = tempVictim.GlobalTransform.origin;
                searchTimer = SEARCH_TIMER;
                break;
        }
        state = newState;
    }

    public override void TakeDamage(Character damager, int damage, int shapeID = 0)
    {
        EmitSignal(nameof(TakenDamage));
        
        if (IsImmortal) return;

        if (!ignoreDamager)
        {
            if (damager == player && !aggressiveAgainstPlayer && state == NPCState.Idle)
            {
                aggressiveAgainstPlayer = true;
                seekArea.AddEnemyInArea(player);
            }

            if (state != NPCState.Attack)
            {
                tempVictim = damager;
                SetState(NPCState.Attack);
            }
        }
        

        if (shapeID != 0) {
            damage = (int)(damage * 1.5f);
        }

        base.TakeDamage(damager, damage, shapeID);

        if (Health <= 0) {
            if (tempVictim == player) {
                player.Stealth.RemoveAttackEnemy(this);
                player.Stealth.RemoveSeekEnemy(this);
            }

            AnimateDealth(damager, shapeID);
        }
    }

    public override void CheckShotgunShot(bool isShotgun)
    {
        tempShotgunShot = isShotgun;
    }

    public void SetObjectActive(string objectPath, bool active)
    {
        var showobject = GetNode<Spatial>(objectPath);
        if (showobject == null) return;
        showobject.Visible = active;
        if (objectsChangeActive.ContainsKey(objectPath))
        {
            objectsChangeActive[objectPath] = active;
        }
        else
        {
            objectsChangeActive.Add(objectPath, active);
        }
    }

    protected virtual async void AnimateDealth(Character killer, int shapeID)
    {
        CollisionLayer = 0;
        CollisionMask = 0;
        skeleton.PhysicalBonesStartSimulation();
        Vector3 dir = Translation.DirectionTo(killer.Translation);
        float force = tempShotgunShot ? RAGDOLL_IMPULSE * 2 : RAGDOLL_IMPULSE;

        if (shapeID == 0) {
            bodyBone?.ApplyCentralImpulse(-dir * force);
        } else {
            headBone?.ApplyCentralImpulse(-dir * force);
        }
        
        await Global.Get().ToTimer(5f);
        Global.AddDeletedObject(Name);
        QueueFree();
    }

    protected void RotateTo(Vector3 place)
    {
        var rotA = Transform.basis.Quat().Normalized();
        var rotB = Transform.LookingAt(place, Vector3.Up).basis.Quat().Normalized();
        var tempRotation = rotA.Slerp(rotB, ROTATION_SPEED);

        Transform tempTransform = Transform;
        tempTransform.basis = new Basis(tempRotation);
        Transform = tempTransform;
    }

    protected void MoveTo(Vector3 place, float distance, float speed = 1)
    {
        var pos = GlobalTransform.origin;
        place.y = pos.y;

        RotateTo(place);

        speed += BaseSpeed;

        Rotation = new Vector3(0, Rotation.y, 0);
        Velocity = new Vector3(0, -GRAVITY, -speed).Rotated(Vector3.Up, Rotation.y);

        var temp_distance = pos.DistanceTo(place);
        CloseToPoint = temp_distance <= distance;
    }

    public override void LoadData(Dictionary data)
    {
        string tempVictimName = data["tempVictim"].ToString();
        if (tempVictimName != "")
        {
            var scene = GetNode("/root/Main/Scene");
            tempVictim = Global.FindNodeInScene(scene, tempVictimName) as Character;
        }
        
        var newState = (NPCState) Enum.Parse(typeof(NPCState), data["state"].ToString());
        SetState(newState);
        lastSeePos = data["lastSeePos"] as Vector3? ?? default;
        relation = (Relation)Enum.Parse(typeof(Relation), data["relation"].ToString());
        aggressiveAgainstPlayer = Convert.ToBoolean(data["aggressiveAgainstPlayer"]);
        myStartPos = new Vector3(
            Convert.ToSingle(data["myStartPos_x"]), 
            Convert.ToSingle(data["myStartPos_y"]), 
            Convert.ToSingle(data["myStartPos_z"])
        );
        myStartRot = new Vector3(
            Convert.ToSingle(data["myStartRot_x"]), 
            Convert.ToSingle(data["myStartRot_y"]), 
            Convert.ToSingle(data["myStartRot_z"])
        );
        IdleAnim = data["idleAnim"].ToString();
        dialogueCode = data["dialogueCode"].ToString();
        WalkSpeed = Convert.ToInt16(data["walkSpeed"]);
        ignoreDamager = Convert.ToBoolean(data["ignoreDamager"]);

        if (data["signals"] is Godot.Collections.Array signals)
        {
            foreach (Dictionary signalData in signals)
            {
                var signalName = signalData["signal"].ToString();
                var method = signalData["method"].ToString();
                var binds = signalData["binds"] as Godot.Collections.Array;
                
                var targetPath = signalData["target_path"].ToString();
                var target = GetNodeOrNull(targetPath);
                if (target == null) continue;

                if (!IsConnected(signalName, target, method))
                {
                    Connect(signalName, target, method, binds);
                }
            }
        }

        if (data.Contains("showObjects") && data["showObjects"] is Dictionary showObjects)
        {
            foreach (string objectPath in showObjects.Keys)
            {
                bool showObject = Convert.ToBoolean(showObjects[objectPath]);
                SetObjectActive(objectPath, showObject);
            }
        }
        
        base.LoadData(data);

        if (Health <= 0)
        {
            AnimateDealth(this, 0);
        }
    }

    public override Dictionary GetSaveData()
    {
        Dictionary saveData = base.GetSaveData();
        saveData["tempVictim"] = IsInstanceValid(tempVictim) ? tempVictim.Name : "";
        saveData["state"] = state.ToString();
        saveData["lastSeePos"] = lastSeePos;
        saveData["relation"] = relation.ToString();
        saveData["aggressiveAgainstPlayer"] = aggressiveAgainstPlayer;
        saveData["idleAnim"] = IdleAnim;
        saveData["walkSpeed"] = WalkSpeed;

        saveData["myStartPos_x"] = myStartPos.x;
        saveData["myStartPos_y"] = myStartPos.y;
        saveData["myStartPos_z"] = myStartPos.z;
        
        saveData["myStartRot_x"] = myStartRot.x;
        saveData["myStartRot_y"] = myStartRot.y;
        saveData["myStartRot_z"] = myStartRot.z;
        
        saveData["dialogueCode"] = dialogueCode;
        saveData["showObjects"] = objectsChangeActive;
        saveData["ignoreDamager"] = ignoreDamager;

        var signals = new Godot.Collections.Array();
        foreach (var signal in GetSignalList())
        {
            if (!(signal is Dictionary signalDict)) continue;

            var connectionList = GetSignalConnectionList(signalDict["name"].ToString());
            if (connectionList == null || connectionList.Count == 0) continue;

            foreach (var connectionData in connectionList)
            {
                if (!(connectionData is Dictionary connectionDict)) continue;
                if (!(connectionDict["target"] is Node)) continue;

                signals.Add(new Dictionary
                {
                    {"signal", signalDict["name"].ToString()},
                    {"method", connectionDict["method"].ToString()},
                    {"target_path", (connectionDict["target"] as Node)?.GetPath()},
                    {"binds", connectionDict["binds"]}
                });
            }
        }

        saveData["signals"] = signals;
        
        return saveData;
    }

    public override void _Ready()
    {
        audi = GetNode<AudioStreamPlayer3D>("audi");
        skeleton = GetNode<Skeleton>("Armature/Skeleton");
        seekArea = GetNode<SeekArea>("seekArea");

        headBone = !string.IsNullOrEmpty(headBonePath) ? GetNode<PhysicalBone>(headBonePath) 
            : GetNodeOrNull<PhysicalBone>("Armature/Skeleton/Physical Bone neck");
        bodyBone = !string.IsNullOrEmpty(bodyBonePath) ? GetNode<PhysicalBone>(bodyBonePath) 
            : GetNodeOrNull<PhysicalBone>("Armature/Skeleton/Physical Bone back_2");

        SetStartHealth(StartHealth);
        BaseSpeed = WalkSpeed;

        if (patrolArray == null || patrolArray.Count == 0)
        {
            if (myStartPos != Vector3.Zero || myStartRot != Vector3.Zero) return;
            
            myStartPos = GlobalTransform.origin;
            myStartRot = Rotation;
        } else {
            patrolPoints = new Spatial[patrolArray.Count];
            for(int i = 0; i < patrolArray.Count; i++) {
                patrolPoints[i] = GetNode<Spatial>(patrolArray[i]);
            }
        }
    }

    public override void _Process(float delta)
    {
        if (Health <= 0) {
            return;
        }

        if (state == NPCState.Search) {
            if (searchTimer > 0) {
                searchTimer -= delta;
            } else {
                SetState(NPCState.Idle);
            }
        }

        if (Velocity.Length() > 0) {
            MoveAndSlide(Velocity);
        }
    }
}

public enum Relation {
    Friend,
    Enemy
}

public enum NPCState {
    Idle,
    Attack,
    Search,
    Talk
}