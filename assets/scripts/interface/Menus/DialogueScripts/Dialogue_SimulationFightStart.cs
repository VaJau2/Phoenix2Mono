using Godot;
using Godot.Collections;

//Скрипт, который запускается после диалога с командиром в симуляции
//Отправляет боевых поней в атаку на лагерь зебр
//и меняет им диалоги после сражения
namespace DialogueScripts;

public class Dialogue_SimulationFightStart : IDialogueScript
{
    private Player player;
    private Pony korporal;
    private Pony private1;
    private Pony privateMare;

    private Array<NPC> zebras = [];

    private readonly Array<Spatial> korporalPath = [];
    private readonly Array<Spatial> private1Path = [];
    private readonly Array<Spatial> privateMarePath = [];

    //загружаем всех нужных пней
    private void LoadParameters(Node node)
    {
        player = Global.Get().player;
        korporal = node.GetNode<Pony>("/root/Main/Scene/npc/korporal");
        private1 = node.GetNode<Pony>("/root/Main/Scene/npc/private1");
        privateMare = node.GetNode<Pony>("/root/Main/Scene/npc/private_mare");

        var korporalPathNode = player.GetNode("../terrain/korporalPath");
        foreach (var temp in korporalPathNode.GetChildren())
        {
            korporalPath.Add((temp as Spatial));
        }

        var private1PathNode = player.GetNode("../terrain/private1Path");
        foreach (var temp in private1PathNode.GetChildren())
        {
            private1Path.Add((temp as Spatial));
        }

        var privateMarePathNode = player.GetNode("../terrain/privateMarePath");
        foreach (var temp in privateMarePathNode.GetChildren())
        {
            privateMarePath.Add((temp as Spatial));
        }

        foreach (var temp in (korporal.GetParent().GetChildren()))
        {
            if (temp is NPC npc)
            {
                if (npc.Name.BeginsWith("zebra"))
                {
                    zebras.Add(npc);
                }
            }
        }
    }

    //отправляем пня в атаку на лагерь
    private async void SendPonyToPath(Pony pony, Array<Spatial> path, string newDialogueCode)
    {
        pony.dialogueCode = "";
        
        foreach (Spatial tempPath in path)
        {
            if (pony.state == NPCState.Attack)
            {
                break;
            }
            
            pony.SetNewStartPos(tempPath.GlobalTransform.origin);
            pony.IdleAnim = "Idle1";
            await pony.ToSignal(pony, nameof(Pony.IsCame));
        }

        while (pony.state != NPCState.Idle)
        {
            await (Global.Get().ToTimer(1f));
        }

        pony.dialogueCode = newDialogueCode;
    }

    public void initiate(Node node, string parameter, string key = "")
    {
        LoadParameters(node);
        SendPonyToPath(korporal, korporalPath, "win");
        SendPonyToPath(private1, private1Path, "win");
        SendPonyToPath(privateMare, privateMarePath, "win");
    }
}