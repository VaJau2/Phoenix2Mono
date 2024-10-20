using Godot;
using Godot.Collections;

public class NpcPatroling : Node, ISavable
{
    private const float PATROL_WAIT = 4f;

    private Spatial[] patrolPoints;
    private float patrolWaitTimer;
    private int patrolI;
    
    private NPC npc;
    
    public override void _Ready()
    {
        npc = GetParent<NPC>();
        
        if (patrolPoints != null) return;

        patrolPoints = new Spatial[npc.patrolArray.Count];
        for (var i = 0; i < npc.patrolArray.Count; i++)
        {
            patrolPoints[i] = npc.GetNode<Spatial>(npc.patrolArray[i]);
        }
    }

    public bool IsEmpty => patrolPoints == null || patrolPoints.Length == 0;

    public Vector3 CurrentPatrolPoint => patrolPoints[patrolI].GlobalTranslation;

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
            patrolPoints = new Spatial[patrolPaths.Count];
            for (int i = 0; i < patrolPaths.Count; i++)
            {
                patrolPoints[i] = npc.GetNode<Spatial>(patrolPaths[i].ToString());
            }
        }
        else
        {
            npc.CleanPatrolArray();
        }
    }
}
