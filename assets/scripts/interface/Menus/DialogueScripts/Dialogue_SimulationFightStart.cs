using Godot;
using Godot.Collections;

//Скрипт, который запускается после диалога с командиром в симуляции
//Отправляет боевых поней в атаку на лагерь зебр
//и меняет им диалоги после сражения
namespace DialogueScripts
{
    public class Dialogue_SimulationFightStart : IDialogueScript
    {
        Player player;
        Pony korporal;
        Pony private1;
        Pony privateMare;

        Array<NPC> zebras = new Array<NPC>();

        Array<Spatial> korporalPath = new Array<Spatial>();
        Array<Spatial> private1Path = new Array<Spatial>();
        Array<Spatial> privateMarePath = new Array<Spatial>();

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
                korporalPath.Add((temp as Spatial));
            }

            Node private1PathNode = player.GetNode("../terrain/private1Path");
            foreach (var temp in private1PathNode.GetChildren())
            {
                private1Path.Add((temp as Spatial));
            }

            Node privateMarePathNode = player.GetNode("../terrain/privateMarePath");
            foreach (var temp in privateMarePathNode.GetChildren())
            {
                privateMarePath.Add((temp as Spatial));
            }

            foreach (var temp in (korporal.GetParent().GetChildren()))
            {
                if (temp is NPC)
                {
                    NPC npc = temp as NPC;
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
                else
                {
                    pony.SetNewStartPos(tempPath.GlobalTransform.origin);
                    pony.IdleAnim = "Idle1";
                    await (pony.ToSignal(pony, nameof(Pony.IsCame)));
                }
            }

            while (pony.state != NPCState.Idle)
            {
                await (Global.Get().ToTimer(1f));
            }

            pony.dialogueCode = newDialogueCode;
        }

        public void initiate(DialogueMenu dialogueMenu, string parameter)
        {
            LoadParameters(dialogueMenu);
            SendPonyToPath(korporal, korporalPath, "win");
            SendPonyToPath(private1, private1Path, "win");
            SendPonyToPath(privateMare, privateMarePath, "win");
        }
    }
}