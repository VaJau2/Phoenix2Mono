using Godot;
using Godot.Collections;

public class DialogueMenu : Control, IMenu
{
    public bool mustBeClosed {get => false;}
    const int MAX_LINE_LENGTH = 50;
    const int MAX_ANSWER_LENGTH = 65;
    NPC npc;
    Player player => Global.Get().player;
    public bool MenuOn = false;
    Dictionary nodes = null;

    RichTextLabel text;
    Button[] answers;
    string[] answerCodes;
    Label leftName, rightName;
    bool isContinue = false;
    string tempAnswer = "";
    int tempAnswerI = -1;
    float tempAnswerCooldown;

    public void StartTalkingTo(NPC npc)
    {
        this.npc = npc;
        npc.SetState(NPCState.Talk);
        npc.tempVictim = player;
        LoadDialogueFile(npc.Name, npc.dialogueCode);
        MenuManager.TryToOpenMenu(this);
    }

    private void LoadDialogueFile(string npcName, string code)
    {
        string lang = InterfaceLang.GetLang();
        string path = "assets/dialogues/" + lang + "/" + npcName + "/" + code + ".json";
        nodes = Global.loadJsonFile(path)["nodes"] as Dictionary;
        MoveToNode((nodes.Keys as Array)[0].ToString());
    }

    private string GetBlockText(string text, string block)
    {
        return "[" + block + "]" + text + "[/" + block + "]";
    }

    private string GetContinueText()
    {
        return "[продолжить]";
    }

    //метод добавляет переносы в строку между пробелами, точками или запятыми
    //ограничивая длину строки на MAX_LINE_LENGTH символов
    private string GetSpacedText(string text)
    {
        if (text.Length <= MAX_LINE_LENGTH) {
            return text;
        }

        Array<char> charsForEOL = new Array<char>() {'.', ',', ' '};
        var resultString = "";
        var sourceString = text;
        do
        {
            for (int i = MAX_LINE_LENGTH; i >= 1; i--) {
                if (charsForEOL.Contains(sourceString[i])) {
                    resultString += sourceString.Substring(0, i) + "\n";
                    sourceString = sourceString.Substring(i + 1);

                    if (sourceString.Length <= MAX_LINE_LENGTH) {
                        resultString += sourceString;
                    }
                    break;
                }
                if (i == 1) {
                    resultString += sourceString.Substring(0, MAX_LINE_LENGTH) + "\n";
                    sourceString = sourceString.Substring(MAX_LINE_LENGTH + 1);

                }
            }
        } while(sourceString.Length > MAX_LINE_LENGTH);

        return resultString;
    }

    private void MoveToNode(string code)
    {
        var tempNode = nodes[code] as Dictionary;
        
        switch (tempNode["kind"].ToString()) {
            case "dialogue":
                //грузим имена персонажей
                leftName.Text  = tempNode["opposite"].ToString();
                rightName.Text = tempNode["speaker"].ToString();

                //грузим текст фразы
                if (!isContinue) {
                    //имя персонажа
                    string uName = GetBlockText(rightName.Text, "u");
                    text.BbcodeText += GetBlockText(uName + ":", "right") + "\n";
                }
                
                var spacedText = GetSpacedText(tempNode["body"].ToString());
                text.BbcodeText += GetBlockText(spacedText, "right") + "\n\n";
                isContinue = false;
                break;
            case "combat":
                npc.aggressiveAgainstPlayer = true;
                npc.SetState(NPCState.Attack);
                npc = null;
                break;
            case "narration":
                GD.Print("start script = " + tempNode["body"].ToString());
                break;
        }

        //грузим варианты ответов
        if (tempNode.Contains("options")) {
            var options = tempNode["options"] as Array;
            for(int i = 0; i < answers.Length; i++) {
                if (i < options.Count) {
                    var option = options[i] as Dictionary;
                    var optionCode = option["next"].ToString();

                    var optionNode = nodes[optionCode] as Dictionary;
                    var optionText = optionNode["body"].ToString();
                    var optionNext = optionNode["next"].ToString();

                    answers[i].Text = optionText;
                    answers[i].Visible = true;
                    answerCodes[i] = optionNext;
                } 
            }
        } else if (tempNode.Contains("next")) {
            answers[0].Visible = true;
            answers[0].Text = GetContinueText();
            answerCodes[0] = tempNode["next"].ToString();
        } else {
            MenuManager.CloseMenu(this);
        }
    }

    public void OpenMenu()
    {
        player.MayMove = false;
        Input.SetMouseMode(Input.MouseMode.Visible);
        Visible = true;
        MenuOn = true;
    }

    public void CloseMenu()
    {
        if (npc != null) {
            npc.SetState(NPCState.Idle);
            npc = null;
        }

        player.MayMove = true;
        Input.SetMouseMode(Input.MouseMode.Captured);
        Visible = false;
        MenuOn = false;
    }

    public void onPlayerTakeDamage()
    {
        CloseMenu();
    }

    public void _on_answer_mouse_entered(int i)
    {
        tempAnswer = answers[i].Text;
        tempAnswerI = i;
    }

    public void _on_answer_mouse_exited(int i)
    {
        answers[i].Text = tempAnswer;
        tempAnswer = "";
        tempAnswerI = -1;
    }

    public async void _on_answer_pressed(int i)
    {
        foreach(Button temp in answers) {
            temp.Visible = false;
        }

        if (tempAnswer != GetContinueText()) {
            text.BbcodeText += GetBlockText(leftName.Text, "u") + ":\n";
            text.BbcodeText += GetSpacedText(tempAnswer) + "\n\n";
            await(Global.Get().ToTimer(0.5f));
        } else {
            isContinue = true;
        }
        
        MoveToNode(answerCodes[i]);
    }

    public override void _Ready()
    {
        MenuBase.LoadColorForChildren(this);
        this.Connect("TakenDamage", player, nameof(onPlayerTakeDamage));

        text      = GetNode<RichTextLabel>("text");
        leftName  = GetNode<Label>("leftName");
        rightName = GetNode<Label>("rightName");
        answers   = new Button[] {
            GetNode<Button>("answer1"),
            GetNode<Button>("answer2"),
            GetNode<Button>("answer3"),
            GetNode<Button>("answer4")
        };
        answerCodes = new string[] {"", "", "", ""};
    }

    public override void _Process(float delta)
    {
        if (MenuOn) {
            if (npc.state != NPCState.Talk) {
                CloseMenu();
            } else {
                player.LookAt(npc.GlobalTransform.origin);
            }

            if (tempAnswerI != -1)
            {
                if (answers[tempAnswerI].Text.Length > MAX_ANSWER_LENGTH) {
                    if (tempAnswerCooldown > 0) {
                        tempAnswerCooldown -= delta;
                    } else {
                        answers[tempAnswerI].Text = answers[tempAnswerI].Text.Substring(1);
                        tempAnswerCooldown = 0.05f;
                    }
                }
            }
        }
    }
}
