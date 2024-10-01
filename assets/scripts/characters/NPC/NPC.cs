using System;
using System.Linq;
using Godot;
using Godot.Collections;

//класс отвечает за поведение НПЦ
public abstract class NPC : Character, IInteractable
{
    protected int RagdollImpulse = 1000;
    private const float SEARCH_TIMER = 12f;
    private readonly string[] skipSignals = {"tree_entered", "tree_exiting"};

    protected float RotationSpeed = 0.15f;
    protected float Gravity = 0;
    protected const float PATROL_WAIT = 4f;

    [Export] public Array<NodePath> patrolArray;
    protected Spatial[] patrolPoints;
    protected int patrolI = 0;
    protected float patrolWaitTimer = 0;
    [Export] public string IdleAnim = "";

    [Export] public bool IsImmortal;
    [Export] public int StartHealth = 100;
    [Export] public Relation relation;
    [Export] public string subtitlesCode = "";
    [Export] public string dialogueCode = "";
    [Export] public int WalkSpeed = 5;

    [Export] public float lookHeightFactor = 1;
    public bool aggressiveAgainstPlayer;
    [Export] public bool ignoreDamager;
    public NPCState state;
    public SeekArea seekArea {get; private set;}
    
    public Vector3 myStartPos, myStartRot;

    public virtual bool MayInteract => GetMayInteract();
    public virtual string InteractionHintCode => "talk";
    
    protected AudioStreamPlayer3D audi;
    [Export] protected Array<AudioStreamSample> hittedSounds;
    [Export] protected Array<AudioStreamSample> dieSounds;
    
    [Export] protected bool hasSkeleton = true;
    private Skeleton skeleton;
    [Export] private NodePath headBonePath, bodyBonePath;
    
    private Dictionary<string, bool> objectsChangeActive = new();
    private Spatial headAttachment;
    private PhysicalBone headBone;
    private PhysicalBone bodyBone;
    private bool tempShotgunShot; //для увеличения импульса при получении урона от дробовика

    private Player player => Global.Get().player;
    private DialogueMenu dialogueMenu;
    private Subtitles subtitles;
    public Character tempVictim;
    protected Vector3 lastSeePos;
    protected float searchTimer;
    private float deadTimer = 5f;

    protected bool CloseToPoint;

    private bool GetMayInteract()
    {
        if (!string.IsNullOrEmpty(dialogueCode))
        {
            return !dialogueMenu.MenuOn;
        }

        if (!string.IsNullOrEmpty(subtitlesCode))
        {
            return !subtitles.IsAnimatingText;
        }

        return false;
    }

    public virtual void Interact(PlayerCamera interactor)
    {
        if (!string.IsNullOrEmpty(dialogueCode))
        {
            dialogueMenu.StartTalkingTo(this);
        }

        if (!string.IsNullOrEmpty(subtitlesCode))
        {
            subtitles.SetTalker(this)
                .LoadSubtitlesFile(subtitlesCode)
                .StartAnimatingText();
        }
    }
    
    public virtual void SetState(NPCState newState)
    {
        if (Health <= 0) return;

        switch(newState) 
        {
            case NPCState.Idle:
                if (tempVictim == player) 
                {
                    player?.Stealth.RemoveSeekEnemy(this);
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
                lastSeePos = tempVictim.GlobalTransform.origin;
                
                break;
        }
        state = newState;
    }

    public void SetLastSeePos(Vector3 newPos)
    {
        lastSeePos = newPos;
    }

    public Vector3 GetHeadPosition()
    {
        return headAttachment.GlobalTranslation;
    }

    public override int GetDamage()
    {
        float tempDamage = base.GetDamage();
        tempDamage *= Global.Get().Settings.npcDamage;
        return (int) tempDamage;
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
        var showObject = GetNode<Spatial>(objectPath);
        if (showObject == null) return;
        showObject.Visible = active;
        objectsChangeActive[objectPath] = active;
    }

    protected void CleanPatrolArray()
    {
        patrolArray?.Clear();
        patrolPoints = null;
    }

    protected void PlayRandomSound(Array<AudioStreamSample> array)
    {
        if (array is not { Count: > 0 }) return;
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
        
        CollisionLayer = 2;
        CollisionMask = 2;

        if (!hasSkeleton) return;
        
        skeleton.PhysicalBonesStartSimulation();
            
        foreach (Node node in GetChildren())
        {
            if (node.Name == "Armature") continue;
            node.QueueFree();
        }
            
        foreach (var boneObject in skeleton.GetChildren())
        {
            if (boneObject is not PhysicalBone bone) continue;
            bone.CollisionLayer = 6;
            bone.CollisionMask = 6;
        }
    }

    protected virtual void AnimateDeath(Character killer, int shapeID)
    {
        PlayRandomSound(dieSounds);
        
        if (hasSkeleton)
        {
            Vector3 dir = Translation.DirectionTo(killer.Translation);
            float force = tempShotgunShot ? RagdollImpulse * 1.5f : RagdollImpulse;

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
        var rotA = Transform.basis.Quat().Normalized();
        var rotB = Transform.LookingAt(place, Vector3.Up).basis.Quat().Normalized();
        var tempRotation = rotA.Slerp(rotB, RotationSpeed);

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
        Velocity = new Vector3(0, -Gravity, -speed).Rotated(Vector3.Up, Rotation.y);

        var temp_distance = pos.DistanceTo(place);
        CloseToPoint = temp_distance <= distance;
    }

    private void HandleGravity()
    {
        if (!IsOnFloor())
        {
            if (Gravity < 30)
            {
                Gravity += 0.5f;
            }
        }
        else
        {
            Gravity = 0;
        }
    }

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
        lastSeePos = data["lastSeePos"] as Vector3? ?? default;
        relation = (Relation)Enum.Parse(typeof(Relation), data["relation"].ToString());
        aggressiveAgainstPlayer = Convert.ToBoolean(data["aggressiveAgainstPlayer"]);
        myStartPos = data["myStartPos"].ToString().ParseToVector3();
        myStartRot = data["myStartRot"].ToString().ParseToVector3();
        IdleAnim = data["idleAnim"].ToString();
        dialogueCode = data["dialogueCode"].ToString();
        WalkSpeed = Convert.ToInt16(data["walkSpeed"]);
        ignoreDamager = Convert.ToBoolean(data["ignoreDamager"]);

        if (hasSkeleton && Health <= 0)
        {
            foreach (Spatial bone in skeleton.GetChildren())
            {
                if (bone is not PhysicalBone) continue;

                var newPos = data[$"rb_{bone.Name}_pos"].ToString().ParseToVector3();
                var newRot = data[$"rb_{bone.Name}_rot"].ToString().ParseToVector3();
                var oldScale = bone.Scale;

                var newBasis = new Basis(newRot);
                var newTransform = new Transform(newBasis, newPos);
                bone.GlobalTransform = newTransform;
                bone.Scale = oldScale;
            }
        }

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

        if (data.Contains("patrolPaths") && data["patrolPaths"] is Godot.Collections.Array patrolPaths)
        {
            patrolPoints = new Spatial[patrolPaths.Count];
            for (int i = 0; i < patrolPaths.Count; i++)
            {
                patrolPoints[i] = GetNode<Spatial>(patrolPaths[i].ToString());
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
        var saveData = base.GetSaveData();
        saveData["tempVictim"] = IsInstanceValid(tempVictim) ? tempVictim.Name : "";
        saveData["state"] = state.ToString();
        saveData["lastSeePos"] = lastSeePos;
        saveData["relation"] = relation.ToString();
        saveData["aggressiveAgainstPlayer"] = aggressiveAgainstPlayer;
        saveData["idleAnim"] = IdleAnim;
        saveData["walkSpeed"] = WalkSpeed;
        saveData["myStartPos"] = myStartPos;
        saveData["myStartRot"] = myStartRot;
        saveData["dialogueCode"] = dialogueCode;
        saveData["showObjects"] = objectsChangeActive;
        saveData["ignoreDamager"] = ignoreDamager;

        if (patrolPoints != null)
        {
            var patrolPaths = new string[patrolPoints.Length];
            for (var i = 0; i < patrolPaths.Length; i++)
            {
                patrolPaths[i] = patrolPoints[i].GetPath().ToString();
            }

            saveData["patrolPaths"] = patrolPaths;
        }

        if (hasSkeleton && Health <= 0)
        {
            foreach (Spatial bone in skeleton.GetChildren())
            {
                if (bone is not PhysicalBone) continue;
                saveData[$"rb_{bone.Name}_pos"] = bone.GlobalTransform.origin;
                saveData[$"rb_{bone.Name}_rot"] = bone.GlobalTransform.basis.GetEuler();
            }
        }

        var signals = new Godot.Collections.Array();
        foreach (var signal in GetSignalList())
        {
            if (signal is not Dictionary signalDict) continue;

            var connectionList = GetSignalConnectionList(signalDict["name"].ToString());
            if (connectionList == null || connectionList.Count == 0) continue;

            foreach (var connectionData in connectionList)
            {
                if (connectionData is not Dictionary connectionDict) continue;
                if (connectionDict["target"] is not Node target) continue;
                var signalName = signalDict["name"].ToString();
                if (skipSignals.Contains(signalName)) continue;

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
        subtitles = GetNode<Subtitles>("/root/Main/Scene/canvas/subtitles");
        if (hasSkeleton)
        {
            skeleton = GetNode<Skeleton>("Armature/Skeleton");

            headAttachment = skeleton.GetNode<Spatial>("BoneAttachment");
            
            headBone = !string.IsNullOrEmpty(headBonePath) ? GetNode<PhysicalBone>(headBonePath) 
                : skeleton.GetNodeOrNull<PhysicalBone>("Physical Bone neck");
            bodyBone = !string.IsNullOrEmpty(bodyBonePath) ? GetNode<PhysicalBone>(bodyBonePath) 
                : skeleton.GetNodeOrNull<PhysicalBone>("Physical Bone back_2");
        }

        SetStartHealth(StartHealth);
        BaseSpeed = WalkSpeed;

        if (patrolArray == null || patrolArray.Count == 0)
        {
            if (myStartPos != Vector3.Zero || myStartRot != Vector3.Zero) return;
            
            myStartPos = GlobalTransform.origin;
            myStartRot = Rotation;
        } 
        else if (patrolPoints == null) 
        {
            patrolPoints = new Spatial[patrolArray.Count];
            for (int i = 0; i < patrolArray.Count; i++) 
            {
                patrolPoints[i] = GetNode<Spatial>(patrolArray[i]);
            }
        }
    }

    public override void _Process(float delta)
    {
        if (Health <= 0) 
        {
            return;
        }

        HandleImpulse();
        HandleGravity();
        
        if (Velocity.Length() > 0) 
        {
            MoveAndSlide(Velocity, new Vector3(0, 1, 0), true);
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
