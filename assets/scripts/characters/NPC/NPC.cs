using System;
using System.Linq;
using Godot;
using Godot.Collections;

//класс отвечает за поведение НПЦ
public abstract partial class NPC : Character, IInteractable
{
    const int RAGDOLL_IMPULSE = 1000;
    const float SEARCH_TIMER = 12f;
    readonly string[] SKIP_SIGNALS = {"tree_entered", "tree_exiting"};

    protected float ROTATION_SPEED = 0.15f;
    protected float GRAVITY = 0;
    protected float PATROL_WAIT = 4f;
    
    [Export] public Array<NodePath> patrolArray;
    protected Node3D[] patrolPoints;
    protected int patrolI = 0;
    protected float patrolWaitTimer = 0;
    [Export] public string IdleAnim = "";

    [Export] public bool IsImmortal;
    [Export] public int StartHealth = 100;
    [Export] public Relation relation;
    [Export] public string dialogueCode = "";
    [Export] public int WalkSpeed = 5;

    [Export] public float lookHeightFactor = 1;
    public bool aggressiveAgainstPlayer;
    public bool ignoreDamager;
    public NPCState state;
    public SeekArea seekArea {get; private set;}
    
    public Vector3 myStartPos, myStartRot;

    public virtual bool MayInteract => !dialogueMenu.MenuOn && !string.IsNullOrEmpty(dialogueCode);
    public virtual string InteractionHintCode => "talk";

    protected AudioStreamPlayer3D audi;
    [Export] protected Array<AudioStreamWav> hittedSounds;
    [Export] protected Array<AudioStreamWav> dieSounds;
    
    [Export] protected bool hasSkeleton = true;
    private Skeleton3D skeleton;
    [Export] private NodePath headBonePath, bodyBonePath;
    
    private Dictionary<string, bool> objectsChangeActive = new Dictionary<string, bool>();
    private PhysicalBone3D headBone;
    private PhysicalBone3D bodyBone;
    private bool tempShotgunShot; //для увеличения импульса при получении урона от дробовика

    private Player player => Global.Get().player;
    private DialogueMenu dialogueMenu;
    public Character tempVictim;
    protected Vector3 lastSeePos;
    protected float searchTimer;

    protected bool CloseToPoint = false;

    public virtual void Interact(PlayerCamera interactor)
    {
        dialogueMenu.StartTalkingTo(this);
    }
    
    public virtual void SetState(NPCState newState)
    {
        if (Health <= 0) return;

        switch(newState) 
        {
            case NPCState.Idle:
                if (tempVictim == player) 
                {
                    player.Stealth.RemoveSeekEnemy(this);
                }
                tempVictim = null;
                break;
            
            case NPCState.Attack:
                if (state == NPCState.Talk) return;
                
                if (tempVictim == player) 
                {
                    if (!(this is NpcWithWeapons npcWeapons) || !string.IsNullOrEmpty(npcWeapons.weaponCode))
                    {
                        player.Stealth.AddAttackEnemy(this);
                    }
                }
                break;
            
            case NPCState.Search:
                searchTimer = SEARCH_TIMER;
                
                if (!IsInstanceValid(tempVictim))
                {
                    if (lastSeePos == Vector3.Zero)
                    {
                        //врага нет, позиции нет, искать нечего
                        SetState(NPCState.Idle);
                        return;
                    }

                    break;
                }
                
                if (tempVictim == player) 
                {
                    player.Stealth.AddSeekEnemy(this);
                }
                lastSeePos = tempVictim.GlobalTransform.Origin;
                
                break;
        }
        state = newState;
    }

    public void SetLastSeePos(Vector3 newPos)
    {
        lastSeePos = newPos;
    }

    public override int GetDamage()
    {
        float tempDamage = base.GetDamage();
        tempDamage *= Global.Get().Settings.npcDamage;
        return (int) tempDamage;
    }

    public override void TakeDamage(Character damager, int damage, int shapeID = 0)
    {
        EmitSignal(nameof(TakenDamageEventHandler));
        
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
        
        PlayRandomSound(hittedSounds);

        if (shapeID != 0) 
        {
            damage = (int)(damage * 1.5f);
        }

        base.TakeDamage(damager, damage, shapeID);

        if (Health <= 0) 
        {
            if (tempVictim == player) 
            {
                player.Stealth.RemoveSeekEnemy(this);
            }

            MakeDead();
            AnimateDeath(damager, shapeID);
        }
    }

    public override void CheckShotgunShot(bool isShotgun)
    {
        tempShotgunShot = isShotgun;
    }

    public void SetObjectActive(string objectPath, bool active)
    {
        var showobject = GetNode<Node3D>(objectPath);
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

    protected void CleanPatrolArray()
    {
        patrolArray?.Clear();
        patrolPoints = null;
    }

    protected void PlayRandomSound(Array<AudioStreamWav> array)
    {
        if (array == null || array.Count <= 0) return;
        var rand = new RandomNumberGenerator();
        rand.Randomize();
        int randomNum = rand.RandiRange(0, array.Count - 1);
        audi.Stream = array[randomNum];
        audi.Play();
    }

    private void MakeDead()
    {
        if (GetParent() is EnemiesManager manager)
        {
            if (manager.enemies.Contains(this))
            {
                manager.enemies.Remove(this);
            }
        }
        
        CollisionLayer = 0;
        CollisionMask = 0;
        
        if (hasSkeleton)
        {
            skeleton.PhysicalBonesStartSimulation();
            
            foreach (Node node in GetChildren())
            {
                if (node.Name == "Armature") continue;
                node.QueueFree();
            }
        }
    }

    protected virtual void AnimateDeath(Character killer, int shapeID)
    {
        PlayRandomSound(dieSounds);
        
        if (hasSkeleton)
        {
            Vector3 dir = Position.DirectionTo(killer.Position);
            float force = tempShotgunShot ? RAGDOLL_IMPULSE * 1.5f : RAGDOLL_IMPULSE;

            if (shapeID == 0)
            {
                bodyBone?.ApplyCentralImpulse(-dir * force);
            }
            else
            {
                headBone?.ApplyCentralImpulse(-dir * force);
            }
        }
    }

    protected void RotateTo(Vector3 place)
    {
        var rotA = Transform.Basis.GetRotationQuaternion().Normalized();
        var rotB = Transform.LookingAt(place, Vector3.Up).Basis.GetRotationQuaternion().Normalized();
        var tempRotation = rotA.Slerp(rotB, ROTATION_SPEED);

        var tempTransform = Transform;
        tempTransform.Basis = new Basis(tempRotation);
        Transform = tempTransform;
    }

    protected void MoveTo(Vector3 place, float distance, float speed = 1)
    {
        var pos = GlobalTransform.Origin;
        place.Y = pos.Y;

        RotateTo(place);

        speed += BaseSpeed;

        Rotation = new Vector3(0, Rotation.Y, 0);
        Velocity = new Vector3(0, -GRAVITY, -speed).Rotated(Vector3.Up, Rotation.Y);

        var tempDistance = pos.DistanceTo(place);
        CloseToPoint = tempDistance <= distance;
    }

    private void HandleGravity()
    {
        if (!IsOnFloor())
        {
            if (GRAVITY < 30)
            {
                GRAVITY += 0.5f;
            }
        }
        else
        {
            GRAVITY = 0;
        }
    }
    
    public void Test(int test) {}

    public override void LoadData(Dictionary data)
    {
        base.LoadData(data);
        string tempVictimName = data["tempVictim"].ToString();
        if (tempVictimName != "")
        {
            var scene = GetNode("/root/Main/Scene");
            tempVictim = Global.FindNodeInScene(scene, tempVictimName) as Character;
        }
        
        var newState = (NPCState) Enum.Parse(typeof(NPCState), data["state"].ToString());
        SetState(newState);
        lastSeePos = data["lastSeePos"].AsVector3();
        relation = (Relation)Enum.Parse(typeof(Relation), data["relation"].ToString());
        aggressiveAgainstPlayer = Convert.ToBoolean(data["aggressiveAgainstPlayer"]);
        myStartPos = SaveToVector3(data, "myStartPos");
        myStartRot = SaveToVector3(data, "myStartRot");
        IdleAnim = data["idleAnim"].ToString();
        dialogueCode = data["dialogueCode"].ToString();
        WalkSpeed = Convert.ToInt16(data["walkSpeed"]);
        ignoreDamager = Convert.ToBoolean(data["ignoreDamager"]);

        if (hasSkeleton && Health <= 0)
        {
            foreach (var node in skeleton.GetChildren())
            {
                var bone = (Node3D)node;
                if (!(bone is PhysicalBone3D)) continue;

                Vector3 newPos = SaveToVector3(data, $"rb_{bone.Name}_pos");
                Vector3 newRot = SaveToVector3(data, $"rb_{bone.Name}_rot");
                Vector3 oldScale = bone.Scale;

                Basis newBasis = Basis.FromEuler(newRot);
                Transform3D newTransform = new Transform3D(newBasis, newPos);
                bone.GlobalTransform = newTransform;
                bone.Scale = oldScale;
            }
        }

        if (data["signals"].AsGodotArray() is { } signals)
        {
            foreach (Dictionary signalData in signals)
            {
                var signalName = signalData["signal"].ToString();
                var method = signalData["method"].ToString();
                
                var targetPath = signalData["target_path"].ToString();
                var target = GetNodeOrNull(targetPath);
                if (target == null) continue;

                if (!IsConnected(signalName, new Callable(target, method)))
                {
                    Connect(signalName, new Callable(target, method));
                }
            }
        }

        if (data.ContainsKey("showObjects") && data["showObjects"].AsGodotDictionary() is { } showObjects)
        {
            foreach (string objectPath in showObjects.Keys)
            {
                bool showObject = Convert.ToBoolean(showObjects[objectPath]);
                SetObjectActive(objectPath, showObject);
            }
        }

        if (data.ContainsKey("patrolPaths") && data["patrolPaths"].AsGodotArray() is { } patrolPaths)
        {
            patrolPoints = new Node3D[patrolPaths.Count];
            for (int i = 0; i < patrolPaths.Count; i++)
            {
                patrolPoints[i] = GetNode<Node3D>(patrolPaths[i].ToString());
            }
        }
        else
        {
            CleanPatrolArray();
        }

        if (Health <= 0)
        {
            MakeDead();
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

        DictionaryHelper.Merge(ref saveData, Vector3ToSave(myStartPos, "myStartPos"));
        DictionaryHelper.Merge(ref saveData, Vector3ToSave(myStartRot, "myStartRot"));

        saveData["dialogueCode"] = dialogueCode;
        saveData["showObjects"] = objectsChangeActive;
        saveData["ignoreDamager"] = ignoreDamager;

        if (patrolPoints != null)
        {
            string[] patrolPaths = new string[patrolPoints.Length];
            for (int i = 0; i < patrolPaths.Length; i++)
            {
                patrolPaths[i] = patrolPoints[i].GetPath().ToString();
            }

            saveData["patrolPaths"] = patrolPaths;
        }

        if (hasSkeleton && Health <= 0)
        {
            foreach (Node3D bone in skeleton.GetChildren())
            {
                if (!(bone is PhysicalBone3D)) continue;
                DictionaryHelper.Merge(ref saveData, Vector3ToSave(bone.GlobalTransform.Origin, $"rb_{bone.Name}_pos"));
                DictionaryHelper.Merge(ref saveData, Vector3ToSave(bone.GlobalTransform.Basis.GetEuler(), $"rb_{bone.Name}_rot"));
            }
        }

        var signals = new Godot.Collections.Array();
        foreach (var signal in GetSignalList())
        {
            if (!(signal is Dictionary signalDict)) continue;

            var connectionList = GetSignalConnectionList(signalDict["name"].ToString());
            if (connectionList == null || connectionList.Count == 0) continue;

            foreach (var connectionData in connectionList)
            {
                if (!(connectionData is Dictionary connectionDict)) continue;
                if (connectionDict["target"].As<Node>() is not { } target) continue;
                var signalName = signalDict["name"].ToString();
                if (SKIP_SIGNALS.Contains(signalName)) continue;

                signals.Add(new Dictionary
                {
                    {"signal", signalName},
                    {"method", connectionDict["method"].ToString()},
                    {"target_path", target.GetPath()},
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
        seekArea = GetNode<SeekArea>("seekArea");
        dialogueMenu = GetNode<DialogueMenu>("/root/Main/Scene/canvas/DialogueMenu/Menu");
        if (hasSkeleton)
        {
            skeleton = GetNode<Skeleton3D>("Armature/Skeleton3D");

            headBone = !string.IsNullOrEmpty(headBonePath) ? GetNode<PhysicalBone3D>(headBonePath) 
                : GetNodeOrNull<PhysicalBone3D>("Armature/Skeleton3D/Physical Bone neck");
            bodyBone = !string.IsNullOrEmpty(bodyBonePath) ? GetNode<PhysicalBone3D>(bodyBonePath) 
                : GetNodeOrNull<PhysicalBone3D>("Armature/Skeleton3D/Physical Bone back_2");
        }

        SetStartHealth(StartHealth);
        BaseSpeed = WalkSpeed;

        if (patrolArray == null || patrolArray.Count == 0)
        {
            if (myStartPos != Vector3.Zero || myStartRot != Vector3.Zero) return;
            
            myStartPos = GlobalTransform.Origin;
            myStartRot = Rotation;
        } 
        else if (patrolPoints == null) 
        {
            patrolPoints = new Node3D[patrolArray.Count];
            for (int i = 0; i < patrolArray.Count; i++) 
            {
                patrolPoints[i] = GetNode<Node3D>(patrolArray[i]);
            }
        }
    }

    public override void _Process(double delta)
    {
        if (Health <= 0) 
        {
            return;
        }

        HandleImpulse();
        HandleGravity();
        
        if (Velocity.Length() > 0) 
        {
            MoveAndSlide();
        }
    }
}

public enum Relation {
    Friend,
    Enemy,
    Neitral
}

public enum NPCState {
    Idle,
    Attack,
    Search,
    Talk
}