using Godot;
using Godot.Collections;

public class MoveNpcTrigger: ActivateOtherTrigger
{
    [Export] public Array<NodePath> NpcPaths; 
    [Export] public Array<NodePath> pointPaths;
    [Export] public Array<string> idleAnims;
    [Export] private Array<bool> stayThere;
    [Export] public float timer = 1f;
    [Export] public float lastTimer = 1f;
    [Export] public bool runToPoint;

    private Array<NpcWithWeapons> npc = new Array<NpcWithWeapons>();
    private Array<Spatial> points = new Array<Spatial>();
    private bool activated;

    public override void _Ready()
    {
        base._Ready();
        
        if (NpcPaths == null || pointPaths == null) return;
        
        for (int i = 0; i < NpcPaths.Count; i++)
        {
            npc.Add(GetNode<NpcWithWeapons>(NpcPaths[i]));
            points.Add(GetNode<Spatial>(pointPaths[i]));
        }
    }

    public override void SetActive(bool newActive)
    {
        base.SetActive(newActive);
        if (activated) return;
        _on_activate_trigger();
    }
    
    public override async void _on_activate_trigger()
    {
        if (IsActive)
        {
            activated = true;

            for (int i = 0; i < npc.Count; i++)
            {
                npc[i].SetNewStartPos(points[i].GlobalTransform.origin, runToPoint);
                npc[i].myStartRot = points[i].Rotation;
                if (idleAnims.Count > i) npc[i].IdleAnim = idleAnims[i];
                
                if (npc[i] is Pony pony && stayThere.Count == npc.Count)
                {
                    pony.stayInPoint = stayThere[i];
                }
                await Global.Get().ToTimer(timer);
            }
            
            await Global.Get().ToTimer(lastTimer);
            
            base._on_activate_trigger();
        }
    }
    
    public void _on_body_entered(Node body)
    {
        if (activated) return;
        if (!IsActive) return;
        if (!(body is Player)) return;
        _on_activate_trigger();
    }
}