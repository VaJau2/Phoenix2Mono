using Godot;
using Godot.Collections;

public class DialogueMenu : Control, IMenu
{
    Global global => Global.Get();
    public bool mustBeClosed => false;
    const int MAX_LINE_LENGTH = 50;
    const int MAX_ANSWER_LENGTH = 65;
    public NPC npc {get; private set;}
    public Player player => Global.Get().player;
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
    bool signalConnected = false;
    private bool updateTempAnswer = true;
    
    [Signal]
    public delegate void FinishTalking();

    public void StartTalkingTo(NPC npc)
    {
        if (!MenuManager.TryToOpenMenu(this, true)) return;
        this.npc = npc;
        npc.SetState(NPCState.Talk);
        npc.tempVictim = player;
        text.BbcodeText = "";
        LoadDialogueFile(npc.Name, npc.dialogueCode);
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
        return InterfaceLang.GetPhrase("inGame", "dialogue", "continue");
    }

    //ограничивает длину строки на MAX_LINE_LENGTH символов
    private string GetSpacedText(string text)
    {
        if (text.Length <= MAX_LINE_LENGTH) {
            return text + "\n";
        }

        Array tempLines = new Array() {text};
        tempLines = Global.ClumpLineLength(tempLines, MAX_LINE_LENGTH);

        string result = "";
        foreach(string temp in tempLines) {
            result += temp + "\n";
        }
        return result;

    }

    private void initDialogueScript(string scriptName, string parameter)
    {
        var scriptType = System.Type.GetType("DialogueScripts." + scriptName);
        if (scriptType == null) return;
        var scriptObj = System.Activator.CreateInstance(scriptType) as DialogueScripts.IDialogueScript;
        scriptObj?.initiate(this, parameter);
    }

    private void MoveToNode(string code)
    {
        if (code == "") {
            MenuManager.CloseMenu(this);
            return;
        }

        var tempNode = nodes[code] as Dictionary;
        
        switch (tempNode["kind"].ToString()) {
            case "dialogue":
                //грузим текст фразы
                if (!isContinue || rightName.Text != tempNode["speaker"].ToString()) {
                    //имя персонажа
                    string uName = GetBlockText(tempNode["speaker"].ToString(), "u");
                    text.BbcodeText += GetBlockText(uName + ":", "right") + "\n";
                }
                
                var spacedText = GetSpacedText(tempNode["body"].ToString());
                text.BbcodeText += GetBlockText(spacedText, "right") + "\n";
                isContinue = false;
                
                //грузим имена персонажей
                leftName.Text  = tempNode["opposite"].ToString();
                rightName.Text = tempNode["speaker"].ToString();
                break;
            case "combat":
                npc.aggressiveAgainstPlayer = true;
                npc.seekArea.AddEnemyInArea(player);
                npc.SetState(NPCState.Attack);
                npc = null;
                break;
            case "narration":
                string scriptName = tempNode["body"].ToString();
                
                string parameter = null;
                if (tempNode.Contains("set")) {
                    if (tempNode["set"] is Array paramsArray)
                    {
                        if (paramsArray[0] is Dictionary paramsDict)
                        {
                            parameter = paramsDict["value"].ToString();
                        }
                    }
                    else
                    {
                        parameter = tempNode["set"].ToString();
                    }
                }
                //если указано несколько скриптов в несколько строк
                if (scriptName.Contains("\n"))
                {
                    //выполняем их все по очереди
                    string[] scriptNames = scriptName.Split('\n');
                    foreach (var tempScriptName in scriptNames)
                    {
                        initDialogueScript(tempScriptName, parameter);
                    }
                }
                //если указан только один скрипт
                else
                {
                    initDialogueScript(scriptName, parameter);
                }

                //после выполнения скрипта сразу отправляемся на следующий нод
                if (tempNode.Contains("next"))
                {
                    MoveToNode(tempNode["next"].ToString());
                    return;
                }
                else
                {
                    MenuManager.CloseMenu(this);
                    return;
                }
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

                    answers[i].Text = optionText;
                    answers[i].Visible = true;

                    if (optionNode.Contains("next")) {
                        var optionNext = optionNode["next"].ToString();
                        answerCodes[i] = optionNext;
                    } else {
                        answerCodes[i] = "";
                    }
                } 
            }
        } else {
            answers[0].Visible = true;
            answers[0].Text = GetContinueText();
            answerCodes[0] = tempNode.Contains("next") ? tempNode["next"].ToString() : "";
        } 
    }

    public void OpenMenu()
    {
        global.SetPause(this, true, false);
        (GetParent() as Control).Visible = true;
        MenuOn = true;
    }

    public void CloseMenu()
    {
        global.SetPause(this, false, false);
        if (npc != null) {
            npc.SetState(NPCState.Idle);
            npc = null;
        }

        text.BbcodeText = "";
        (GetParent() as Control).Visible = false;
        MenuOn = false;
        EmitSignal(nameof(FinishTalking));
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
        if (updateTempAnswer)
        {
            tempAnswer = answers[i].Text;
        }

        updateTempAnswer = true;
        tempAnswerI = -1;
        
        foreach(Button temp in answers) {
            temp.Visible = false;
        }

        if (tempAnswer != GetContinueText()) {
            text.BbcodeText += GetBlockText(leftName.Text, "u") + ":\n";
            text.BbcodeText += GetSpacedText(tempAnswer) + "\n\n";
            await Global.Get().ToTimer(0.5f, null, true);
        } else {
            isContinue = true;
        }
        
        MoveToNode(answerCodes[i]);
    }

    public override void _Ready()
    {
        MenuBase.LoadColorForChildren(this);

        text      = GetNode<RichTextLabel>("text");
        leftName  = GetNode<Label>("leftName");
        rightName = GetNode<Label>("rightName");
        answers   = new Button[] {
            GetNode<Button>("answer1"),
            GetNode<Button>("answer2"),
            GetNode<Button>("answer3"),
            GetNode<Button>("answer4")
        };
        answerCodes = new[] {"", "", "", ""};
    }

    public override void _Process(float delta)
    {
        if (!MenuOn) return;
        
        if (npc == null || npc.state != NPCState.Talk) {
            MenuManager.CloseMenu(this);
        } else {
            player.LookAt(npc.GlobalTransform.origin);
        }

        if (tempAnswerI == -1) return;
        if (answers[tempAnswerI].Text.Length <= MAX_ANSWER_LENGTH) return;
            
        if (tempAnswerCooldown > 0) 
        {
            tempAnswerCooldown -= delta;
        } 
        else
        {
            updateTempAnswer = false;
            answers[tempAnswerI].Text = answers[tempAnswerI].Text.Substring(1);
            tempAnswerCooldown = 0.05f;
        }
    }
}
