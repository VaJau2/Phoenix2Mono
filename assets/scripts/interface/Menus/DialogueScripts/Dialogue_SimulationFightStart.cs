using Godot;
using Godot.Collections;

//Скрипт, который запускается после диалога с командиром в симуляции
//Отправляет боевых поней в атаку на лагерь зебр
//и меняет им диалоги после сражения
namespace DialogueScripts
{
    public partial class Dialogue_SimulationFightStart : IDialogueScript
    {
        Player player;
        Pony korporal;
        Pony private1;
        Pony privateMare;

        Array<NPC> zebras = new Array<NPC>();

        Array<Node3D> korporalPath = new Array<Node3D>();
        Array<Node3D> private1Path = new Array<Node3D>();
        Array<Node3D> privateMarePath = new Array<Node3D>();

        bool fightIsOver = false;

        //загружаем всех нужных пней
        private void LoadParameters(DialogueMenu dialogueMenu)
        {
            player = dialogueMenu.player;
            korporal = dialogueMenu.npc as Pony;
            private1 = korporal.GetNode<Pony>("../private1");
            privateMare = korporal.GetNode<Pony>("../private_mare");

            Node korporalPathNode = player.GetNode("../terrain/korporalPath");
            foreach (var temp in korporalPathNode.GetChildren())
            {
                korporalPath.Add((temp as Node3D));
            }

            Node private1PathNode = player.GetNode("../terrain/private1Path");
            foreach (var temp in private1PathNode.GetChildren())
            {
                private1Path.Add((temp as Node3D));
            }

            Node privateMarePathNode = player.GetNode("../terrain/privateMarePath");
            foreach (var temp in privateMarePathNode.GetChildren())
            {
                privateMarePath.Add((temp as Node3D));
            }

            foreach (var temp in (korporal.GetParent().GetChildren()))
            {
                if (temp is NPC npc)
                {
                    if (npc.Name.ToString().StartsWith("zebra"))
                    {
                        zebras.Add(npc);
                    }
                }
            }
        }

        //отправляем пня в атаку на лагерь
        private async void SendPonyToPath(Pony pony, Array<Node3D> path, string newDialogueCode)
        {
            pony.dialogueCode = "";


            foreach (Node3D tempPath in path)
            {
                if (pony.state == NPCState.Attack)
                {
                    break;
                }
                else
                {
                    pony.SetNewStartPos(tempPath.GlobalTransform.Origin);
                    pony.IdleAnim = "Idle1";
                    await pony.ToSignal(pony, nameof(Pony.IsCame));
                }
            }

            while (pony.state != NPCState.Idle)
            {
                await (Global.Get().ToTimer(1f));
            }

            pony.dialogueCode = newDialogueCode;
        }

        public void initiate(DialogueMenu dialogueMenu, string parameter, string key = "")
        {
            LoadParameters(dialogueMenu);
            SendPonyToPath(korporal, korporalPath, "win");
            SendPonyToPath(private1, private1Path, "win");
            SendPonyToPath(privateMare, privateMarePath, "win");
        }
    }
}