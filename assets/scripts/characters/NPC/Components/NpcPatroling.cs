using Godot;
using Godot.Collections;

public partial class NpcPatroling : Node, ISavable
{
    private const float PATROL_WAIT = 4f;

    private Node3D[] patrolPoints;
    private float patrolWaitTimer;
    private int patrolI;
    
    private NPC npc;
    
    public override void _Ready()
    {
        npc = GetParent<NPC>();
        
        if (patrolPoints != null) return;

        patrolPoints = new Node3D[npc.patrolArray.Count];
        for (var i = 0; i < npc.patrolArray.Count; i++)
        {
            patrolPoints[i] = npc.GetNode<Node3D>(npc.patrolArray[i]);
        }
    }

    public void SetPatrolPoints(Node3D[] newPatrolPoints)
    {
        patrolPoints = newPatrolPoints;
    }

    public bool IsEmpty => patrolPoints == null || patrolPoints.Length == 0;

    public Vector3 CurrentPatrolPoint => patrolPoints[patrolI].GlobalTranslation;
    public Vector3 CurrentPatrolRotation => patrolPoints[patrolI].GlobalRotation;

    public void ClearPoints()
    {
        patrolI = 0;
        patrolPoints = null;
    }

    public void NextPatrolPoint()
    {
        if (patrolI < patrolPoints.Length - 1)
        {
            patrolI += 1;
        }
        else
        {
            patrolI = 0;
        }

        patrolWaitTimer = PATROL_WAIT;
    }

    public bool IsWaiting(float delta)
    {
        if (patrolWaitTimer > 0)
        {
            patrolWaitTimer -= delta;
            return true;
        }
        
        return false;
    }
    
    public Dictionary GetSaveData()
    {
        var saveData = new Dictionary();

        if (patrolPoints != null)
        {
            var patrolPaths = new string[patrolPoints.Length];
            for (var i = 0; i < patrolPaths.Length; i++)
            {
                patrolPaths[i] = patrolPoints[i].GetPath().ToString();
            }

            saveData["patrolPaths"] = patrolPaths;
        }

        return saveData;
    }

    public void LoadData(Dictionary data)
    {
        if (data.Contains("patrolPaths") && data["patrolPaths"] is Array patrolPaths)
        {
            patrolPoints = new Node3D[patrolPaths.Count];
            for (int i = 0; i < patrolPaths.Count; i++)
            {
                patrolPoints[i] = npc.GetNode<Node3D>(patrolPaths[i].ToString());
            }
        }
        else
        {
            npc.CleanPatrolArray();
        }
    }
}
