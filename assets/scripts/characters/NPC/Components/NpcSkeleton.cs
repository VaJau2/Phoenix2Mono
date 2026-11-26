using Godot;
using Godot.Collections;
using System.Collections.Generic;
using System.Text;

public partial class NpcSkeleton : Skeleton3D, ISavable
{
    [Export] private float RagdollImpulse = 500;

    //слой 7 «Corpse»: кости трупов; в маске только у игрока и снарядов
    public const uint CORPSE_LAYER = 64;
    private const float MAX_CORPSE_SPEED = 40f; //быстрее кости трупа не летают, м/с (импульс смерти — до ~35)
    [Export] private NodePath headBonePath, bodyBonePath;

    private readonly List<PhysicalBone3D> bones = [];
    private NPC npc;
    private PhysicalBone3D headBone;
    private PhysicalBone3D bodyBone;
    private bool tempShotgunShot; //для увеличения импульса при получении урона от дробовика

    //страховка от взрыва рэгдолла (B-31): взрыв — NaN или долгий/очень большой разрыв сустава
    //(при ударе о пол сустав на кадр расходится до ~1.3 м и сам стягивается)
    private const float BROKEN_JOINT_STRETCH = 1f;      //разрыв больше этого BROKEN_STRETCH_FRAMES кадров подряд — взрыв, м
    private const int BROKEN_STRETCH_FRAMES = 20;
    private const float HARD_BROKEN_JOINT_STRETCH = 3f; //разрыв больше этого — взрыв сразу, м
    private const float HEALTHY_JOINT_STRETCH = 0.4f;   //положение запоминается как нормальное только при разрыве меньше этого
    private const int MAX_RECOVERIES = 3;
    private const ulong RECOVERY_WINDOW = 120; //кадров физики: больше MAX_RECOVERIES восстановлений за это время — труп замораживается
    private const int HISTORY_SIZE = 30;

    private Transform3D[] lastGoodTransforms = [];
    private int[] jointParents = [];          //индекс родительской физической кости в bones, -1 — нет
    private Vector3[] jointAnchorsInParent = []; //точка сустава в системе родительской кости (как при создании сустава)
    private int recoveries;
    private ulong lastRecoveryFrame;
    private int stretchedFrames; //сколько кадров подряд разрыв больше BROKEN_JOINT_STRETCH
    private bool diagnosticsLogged;

    //история последних кадров физики для диагностики взрыва
    private struct HistoryEntry
    {
        public ulong Frame;
        public Vector3 Center;
        public float MaxSpeed;
        public int Fastest;
        public float Stretch;
    }
    private readonly HistoryEntry[] history = new HistoryEntry[HISTORY_SIZE];
    private int historyPos;

    public float MaxJointStretch { get; private set; }

    public override void _Ready()
    {
        npc = GetNode<NPC>("../../");

        headBone = headBonePath != null ? GetNodeOrNull<PhysicalBone3D>(headBonePath)
            : GetNodeOrNull<PhysicalBone3D>("Physical Bone neck");
        bodyBone = bodyBonePath != null ? GetNodeOrNull<PhysicalBone3D>(bodyBonePath)
            : GetNodeOrNull<PhysicalBone3D>("Physical Bone back_2");

        //кинематические кости живого NPC разгоняли кости трупов до NaN (B-31); слой и маска — в MakeDead (B-17)
        foreach (var boneObject in GetChildren())
        {
            if (boneObject is not PhysicalBone3D bone) continue;
            bone.CollisionLayer = 0;
            bone.CollisionMask = 0;
            bones.Add(bone);
        }
        lastGoodTransforms = new Transform3D[bones.Count];
        InitJoints();

        //проверка трупа — раньше остальных узлов (игрока), чтобы они не видели испорченный рэгдолл
        ProcessPhysicsPriority = -1000;
        SetPhysicsProcess(false);
    }

    //родитель сустава — ближайший предок в скелете, у которого есть физическая кость (как в PhysicalBone3D)
    private void InitJoints()
    {
        var byBoneId = new System.Collections.Generic.Dictionary<int, int>();
        for (int i = 0; i < bones.Count; i++) byBoneId[bones[i].GetBoneId()] = i;

        jointParents = new int[bones.Count];
        jointAnchorsInParent = new Vector3[bones.Count];
        for (int i = 0; i < bones.Count; i++)
        {
            jointParents[i] = -1;
            if (bones[i].JointType == PhysicalBone3D.JointTypeEnum.None) continue;

            int parentBone = GetBoneParent(bones[i].GetBoneId());
            while (parentBone >= 0 && !byBoneId.ContainsKey(parentBone)) parentBone = GetBoneParent(parentBone);
            if (parentBone < 0) continue;

            jointParents[i] = byBoneId[parentBone];
        }

        //кости одного скелета друг с другом не сталкиваются (B-31): формы соседних костей перекрываются,
        //и коллизия растягивала суставы; в Godot 3 (Bullet) связанные тела не сталкивались
        for (int i = 0; i < bones.Count; i++)
        {
            for (int j = i + 1; j < bones.Count; j++) bones[i].AddCollisionExceptionWith(bones[j]);
        }
    }

    //пересоздание суставов по текущему положению костей (B-31): PhysicalBone3D создаёт их по позе покоя,
    //и поза смерти рвала суставы с первого кадра; пределы углов — от позы в момент смерти
    private void RebuildJoints()
    {
        foreach (var bone in bones)
        {
            var type = bone.JointType;
            if (type == PhysicalBone3D.JointTypeEnum.None) continue;

            //при смене типа сустава параметры сбрасываются в значения по умолчанию — сохраняем и возвращаем
            var saved = new List<(string, Variant)>();
            foreach (var property in bone.GetPropertyList())
            {
                string name = property["name"].AsString();
                if (name.StartsWith("joint_constraints/")) saved.Add((name, bone.Get(name)));
            }

            bone.JointType = PhysicalBone3D.JointTypeEnum.None;
            bone.JointType = type;
            foreach (var (name, value) in saved) bone.Set(name, value);
        }

        //точки суставов для проверки разрыва: сразу после пересоздания суставы ничем не нарушены
        for (int i = 0; i < bones.Count; i++)
        {
            int parent = jointParents[i];
            if (parent < 0) continue;
            var jointPoint = bones[i].GlobalTransform * bones[i].JointOffset.Origin;
            jointAnchorsInParent[i] = bones[parent].GlobalTransform.AffineInverse() * jointPoint;
        }
        for (int i = 0; i < bones.Count; i++) lastGoodTransforms[i] = bones[i].GlobalTransform;
    }

    public override void _PhysicsProcess(double delta)
    {
        if (!CheckRagdoll()) return;

        for (int i = 0; i < bones.Count; i++)
        {
            //ограничение скорости костей трупа: разогнавшийся рэгдолл разлетается по уровню (B-31)
            var velocity = bones[i].LinearVelocity;
            if (velocity.LengthSquared() > MAX_CORPSE_SPEED * MAX_CORPSE_SPEED)
            {
                bones[i].LinearVelocity = velocity.LimitLength(MAX_CORPSE_SPEED);
            }
        }

        if (MaxJointStretch < HEALTHY_JOINT_STRETCH)
        {
            for (int i = 0; i < bones.Count; i++) lastGoodTransforms[i] = bones[i].GlobalTransform;
        }
    }

    //страховка от взрыва рэгдолла (B-31): труп возвращается в последнее целое положение, при повторе — замирает;
    //первый случай подробно пишется в лог
    private bool CheckRagdoll()
    {
        bool finite = true;
        foreach (var bone in bones)
        {
            if (bone.GlobalTransform.IsFinite() && bone.LinearVelocity.IsFinite() && bone.AngularVelocity.IsFinite())
                continue;
            finite = false;
            break;
        }

        int worstJoint = -1;
        MaxJointStretch = finite ? GetMaxJointStretch(out worstJoint) : float.NaN;
        RecordHistory();
        stretchedFrames = finite && MaxJointStretch > BROKEN_JOINT_STRETCH ? stretchedFrames + 1 : 0;
        if (finite && MaxJointStretch < HARD_BROKEN_JOINT_STRETCH && stretchedFrames < BROKEN_STRETCH_FRAMES) return true;
        stretchedFrames = 0;

        if (!diagnosticsLogged)
        {
            diagnosticsLogged = true;
            GD.PushWarning(GetDiagnostics(finite, worstJoint));
        }

        ulong now = Engine.GetPhysicsFrames();
        recoveries = now - lastRecoveryFrame < RECOVERY_WINDOW ? recoveries + 1 : 1;
        lastRecoveryFrame = now;

        for (int i = 0; i < bones.Count; i++)
        {
            bones[i].GlobalTransform = lastGoodTransforms[i];
            bones[i].LinearVelocity = Vector3.Zero;
            bones[i].AngularVelocity = Vector3.Zero;
        }

        string reason = finite ? $"разрыв сустава {MaxJointStretch:0.00} м" : "NaN";
        if (recoveries > MAX_RECOVERIES)
        {
            FreezeCorpse();
            GD.PushWarning($"NpcSkeleton: рэгдолл {npc.Name} взрывается снова ({reason}) — труп заморожен");
        }
        else
        {
            GD.PushWarning($"NpcSkeleton: рэгдолл {npc.Name} взорвался ({reason}) — кости возвращены в последнее нормальное положение");
        }
        return false;
    }

    private float GetMaxJointStretch(out int worst)
    {
        float max = 0;
        worst = -1;
        for (int i = 0; i < bones.Count; i++)
        {
            int parent = jointParents[i];
            if (parent < 0) continue;
            var fromChild = bones[i].GlobalTransform * bones[i].JointOffset.Origin;
            var fromParent = bones[parent].GlobalTransform * jointAnchorsInParent[i];
            float stretch = fromChild.DistanceTo(fromParent);
            if (stretch <= max) continue;
            max = stretch;
            worst = i;
        }
        return max;
    }

    private void RecordHistory()
    {
        var entry = new HistoryEntry { Frame = Engine.GetPhysicsFrames(), Stretch = MaxJointStretch, Fastest = -1 };
        for (int i = 0; i < bones.Count; i++)
        {
            float speed = bones[i].LinearVelocity.Length();
            if (entry.Fastest < 0 || speed > entry.MaxSpeed)
            {
                entry.MaxSpeed = speed;
                entry.Fastest = i;
            }
            entry.Center += bones[i].GlobalPosition;
        }
        entry.Center /= Mathf.Max(bones.Count, 1);
        history[historyPos] = entry;
        historyPos = (historyPos + 1) % HISTORY_SIZE;
    }

    private string GetDiagnostics(bool finite, int worstJoint)
    {
        var text = new StringBuilder();
        text.Append($"NpcSkeleton: диагностика взрыва рэгдолла {npc.Name} ({(finite ? "разрыв сустава" : "NaN")}");
        if (worstJoint >= 0) text.Append($", хуже всего {bones[worstJoint].Name} — {bones[jointParents[worstJoint]].Name}");
        text.Append("). Последние кадры:");
        for (int k = 0; k < HISTORY_SIZE; k++)
        {
            var e = history[(historyPos + k) % HISTORY_SIZE];
            if (e.Fastest < 0) continue;
            text.Append($"\n  кадр {e.Frame}: центр {e.Center.ToString("F2")}, макс. скорость {e.MaxSpeed:0.0} ({bones[e.Fastest].Name}), разрыв {e.Stretch:0.00}");
        }
        text.Append("\n  Кости сейчас (положение, скорость, касания):");
        foreach (var bone in bones)
        {
            text.Append($"\n  {bone.Name}: {bone.GlobalPosition.ToString("F2")} v={bone.LinearVelocity.Length():0.0} w={bone.AngularVelocity.Length():0.0}");
            var state = PhysicsServer3D.BodyGetDirectState(bone.GetRid());
            if (state == null) continue;
            for (int c = 0; c < state.GetContactCount(); c++)
            {
                var other = state.GetContactColliderObject(c);
                string otherName = other is Node node ? (node.IsInsideTree() ? node.GetPath().ToString() : node.Name.ToString()) : other?.ToString() ?? "?";
                text.Append($" [{otherName} {state.GetContactLocalPosition(c).ToString("F2")}]");
            }
        }
        return text.ToString();
    }

    //остановка симуляции с сохранением позы: кости становятся кинематическими и следуют за позой скелета,
    //поэтому поза скелета выставляется по последнему нормальному положению костей
    private void FreezeCorpse()
    {
        var skeletonInverse = GlobalTransform.AffineInverse();
        var order = new List<int>();
        for (int i = 0; i < bones.Count; i++) order.Add(i);
        //родительские кости раньше дочерних: глобальная поза дочерней считается от позы родителя
        order.Sort((a, b) => bones[a].GetBoneId().CompareTo(bones[b].GetBoneId()));
        foreach (int i in order)
        {
            var pose = skeletonInverse * lastGoodTransforms[i] * bones[i].BodyOffset.AffineInverse();
            SetBoneGlobalPose(bones[i].GetBoneId(), pose);
        }

        PhysicalBonesStopSimulation();
        SetPhysicsProcess(false);
    }

    public void CheckShotgunShot(bool isShotgun)
    {
        tempShotgunShot = isShotgun;
    }

    // настройки рэгдолла
    // вызывается во время загрузки трупика
    public void MakeDead()
    {
        PhysicalBonesStartSimulation();
        RebuildJoints();

        for (int i = 0; i < bones.Count; i++)
        {
            var bone = bones[i];
            //слой 3 (попадания выстрелов, другие трупы) + слой трупов (7): с ним сталкивается только игрок (B-17).
            //Слоя 2 нет: предметы (маска 3) не должны задевать трупы, иначе NPC вдавливает их в рэгдолл (B-31)
            bone.CollisionLayer = 4 | CORPSE_LAYER;
            bone.CollisionMask = 6; // слои 2 (пол) и 3
            //касания нужны только для диагностики взрыва (GetDiagnostics)
            PhysicsServer3D.BodySetMaxContactsReported(bone.GetRid(), 4);
        }

        SetPhysicsProcess(true);
    }

    // даёт рэгдоллу импульс
    public void AnimateDeath(Character killer, int shapeID)
    {
        Vector3 dir = GlobalPosition.DirectionTo(killer.GlobalPosition);
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

    public void LoadData(Dictionary data)
    {
        if (npc.Health > 0) return;

        foreach (Node3D bone in GetChildren())
        {
            if (bone is not PhysicalBone3D) continue;

            var newPos = data[$"rb_{bone.Name}_pos"].ToString().ParseToVector3();
            var newRot = data[$"rb_{bone.Name}_rot"].ToString().ParseToVector3();
            var oldScale = bone.Scale;

            var newBasis = Basis.FromEuler(newRot);
            var newTransform = new Transform3D(newBasis, newPos);
            bone.GlobalTransform = newTransform;
            bone.Scale = oldScale;
        }

        //кости перенесены в сохранённую позу — суставы по ней (иначе они нарушены с первого кадра)
        RebuildJoints();
    }

    public Dictionary GetSaveData()
    {
        var saveData = new Dictionary();
        if (npc.Health > 0) return saveData;

        foreach (Node3D bone in GetChildren())
        {
            if (bone is not PhysicalBone3D) continue;
            saveData[$"rb_{bone.Name}_pos"] = bone.GlobalTransform.Origin;
            saveData[$"rb_{bone.Name}_rot"] = bone.GlobalTransform.Basis.GetEuler();
        }

        return saveData;
    }
}
