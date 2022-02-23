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
    public bool MenuOn => (GetParent() as Control).Visible;
    Dictionary nodes = null;

    RichTextLabel text;
    Button[] answers;
    string[] answerCodes;
    Array answerText;
    Label leftName, rightName;
    bool isContinue = false;
    string tempAnswer = "";
    int tempAnswerI = -1;
    float tempAnswerCooldown;
    bool signalConnected = false;
    private bool mayAnswer = true;
    private bool updateTempAnswer = true;

    private DialogueAudio dialogueAudio;
    
    [Signal]
    public delegate void FinishTalking();

    public void StartTalkingTo(NPC npc)
    {
        //диалог должен закрывать любое открытое меню
        //прим. - меню терминала, которое имеет mustBeClosed = false
        if (MenuManager.SomeMenuOpen)
        {
            if (MenuManager.openedMenu is Object && IsInstanceValid(MenuManager.openedMenu as Object))
            {
                MenuManager.CloseMenu(MenuManager.openedMenu);
            }
        }
        
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
        dialogueAudio.LoadNpc(npcName, code);
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

    private void initDialogueScript(string scriptName, string parameter, string key = "")
    {
        var scriptType = System.Type.GetType("DialogueScripts." + scriptName);
        if (scriptType == null) return;
        var scriptObj = System.Activator.CreateInstance(scriptType) as DialogueScripts.IDialogueScript;
        scriptObj?.initiate(this, parameter, key);
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

                dialogueAudio.TryToPlayAudio(code);
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
                string key = null;
                if (tempNode.Contains("set")) {
                    if (tempNode["set"] is Array paramsArray)
                    {
                        if (paramsArray[0] is Dictionary paramsDict)
                        {
                            parameter = paramsDict["value"].ToString();
                            key = paramsDict["key"].ToString();
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
                        initDialogueScript(tempScriptName, parameter, key);
                    }
                }
                //если указан только один скрипт
                else
                {
                    initDialogueScript(scriptName, parameter, key);
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
        answerText.Clear();
        mayAnswer = true;
        if (tempNode.Contains("options")) {
            if (!(tempNode["options"] is Array options)) return;
            for(int i = 0; i < answers.Length; i++) {
                if (i < options.Count) {
                    var option = options[i] as Dictionary;
                    if (option == null) continue;
                    var optionCode = option["next"].ToString();

                    var optionNode = nodes[optionCode] as Dictionary;
                    if (optionNode == null) continue;
                    var optionText = optionNode["body"].ToString();

                    answerText.Add(optionText);
                    answers[i].Text = (i + 1) + ": " + optionText;
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
            answerText.Add(GetContinueText());
            answers[0].Text = "1: " + GetContinueText();
            answerCodes[0] = tempNode.Contains("next") ? tempNode["next"].ToString() : "";
        } 
    }

    public void OpenMenu()
    {
        global.SetPause(this, true, false);
        (GetParent() as Control).Visible = true;
    }

    public void CloseMenu()
    {
        dialogueAudio.Stop();
        global.player.inventory.SetBindsCooldown(0.5f);
        global.SetPause(this, false, false);
        if (npc != null) {
            npc.SetState(NPCState.Idle);
            npc = null;
        }

        text.BbcodeText = "";
        (GetParent() as Control).Visible = false;
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
        if (answerText.Count <= i) return;
        mayAnswer = false;
        
        if (updateTempAnswer)
        {
            tempAnswer = answerText[i].ToString();
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

        dialogueAudio = GetNode<DialogueAudio>("../audi");
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
        answerText = new Array();

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

    public override void _Input(InputEvent @event)
    {
        if (!MenuOn) return;
        if (!mayAnswer) return;
        if (!(@event is InputEventKey eventKey)) return;
        if (!eventKey.Pressed) return;

        switch (eventKey.Scancode)
        {
            case (uint)KeyList.Key1:
                _on_answer_pressed(0);
                break;
            case (uint)KeyList.Key2:
                _on_answer_pressed(1);
                break;
            case (uint)KeyList.Key3:
                _on_answer_pressed(2);
                break;
            case (uint)KeyList.Key4:
                _on_answer_pressed(3);
                break;
        }
    }
}
