using System.Linq;
using Godot;
using Godot.Collections;

public class DialogueMenu : Control, IMenu, ISavable
{
    public bool mustBeClosed => false;
    
    private const int MAX_LINE_LENGTH = 50;
    private const int MAX_ANSWER_LENGTH = 65;

    public NPC npc {get; private set;}
    private Player player => Global.Get().player;
    public bool MenuOn => ((Control)GetParent()).Visible;
    
    private Dictionary nodes;
    private Dictionary tempNode;
    private string currentCode;

    private RichTextLabel text;
    private Label skipLabel;
    private bool isAnimatingText;
    private int animatingAnswerNum = -1;
    private string animatingText;
    private string cleanAnimatingText;
    private float symbolTimer;
    private float symbolDelay;
    private string tempTextBlock;
    
    private Button[] answers;
    private string[] answerCodes;
    private Array answerText;
    private string tempAnswer = "";
    private int tempAnswerI = -1;
    private float tempAnswerCooldown;
    private bool mayAnswer = true;
    private bool updateTempAnswer = true;
    
    private Label leftName, rightName;
    private bool isContinue;
    
    private DialogueAudio dialogueAudio;
    
    [Signal]
    public delegate void FinishTalking();

    public override void _Ready()
    {
        MenuBase.LoadColorForChildren(this);

        dialogueAudio = GetNode<DialogueAudio>("../dialogueAudio");
        text      = GetNode<RichTextLabel>("text");
        skipLabel = GetNode<Label>("skipLabel");
        leftName  = GetNode<Label>("leftName");
        rightName = GetNode<Label>("rightName");
        answers   =
        [
            GetNode<Button>("answer1"),
            GetNode<Button>("answer2"),
            GetNode<Button>("answer3"),
            GetNode<Button>("answer4")
        ];
        answerCodes = ["", "", "", ""];
        answerText = new Array();
    }

    public override void _Process(float delta)
    {
        if (!MenuOn) return;
        
        if (npc == null || npc.GetState() != SetStateEnum.Talk) 
        {
            MenuManager.CloseMenu(this);
            return;
        }
        
        UpdateAnswerCooldown(delta);
        UpdateAnimatingText(delta);
    }
    
    public void StartTalkingTo(NPC newNpc)
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
        
        npc = newNpc;
        npc.tempVictim = player;
        npc.SetState(SetStateEnum.Talk);
        player.SetTalking(true, npc);
        
        text.BbcodeText = "";
        skipLabel.Text = InterfaceLang.GetPhrase("inGame", "dialogue", "skip");
        LoadDialogueFile();
    }

    private void LoadDialogueFile()
    {
        if (string.IsNullOrEmpty(npc.dialogueCode))
        {
            GD.PrintErr($"{npc.Name} dialogue code is empty!");
            return;
        }
        
        var lang = InterfaceLang.GetLang();
        var path = "assets/dialogues/" + lang + "/" + GetNpcName(npc) + "/" + npc.dialogueCode + ".json";
        
        nodes = Global.LoadJsonFile(path)["nodes"] as Dictionary;
        
        if (nodes == null) return;
        
        if (string.IsNullOrEmpty(currentCode))
        {
            currentCode = ((Array)nodes.Keys)[0].ToString();
        }
            
        MoveToNode(currentCode);
    }
    
    private static string GetNpcName(NPC npc) => !string.IsNullOrEmpty(npc.npcCustomDialogueName) 
        ? npc.npcCustomDialogueName
        : npc.Name;

    private void InitDialogueScript(string scriptName, string parameter, string key = "")
    {
        var scriptType = System.Type.GetType("DialogueScripts." + scriptName);
        
        if (scriptType == null) return;
        
        var scriptObj = System.Activator.CreateInstance(scriptType) as DialogueScripts.IDialogueScript;
        scriptObj?.initiate(this, parameter, key);
    }

    private void MoveToNode(string code)
    {
        currentCode = code;
        
        if (code == "") 
        {
            MenuManager.CloseMenu(this);
            return;
        }

        tempNode = nodes[code] as Dictionary;
        if (tempNode == null) return;
        
        switch (tempNode["kind"].ToString()) 
        {
            case "dialogue":
                //грузим текст фразы
                if (!isContinue || rightName.Text != tempNode["speaker"].ToString()) 
                {
                    //имя персонажа
                    string uName = GetBlockText(tempNode["speaker"].ToString(), "u");
                    text.BbcodeText += GetBlockText(uName + ":", "right") + "\n";
                }
                
                var spacedText = GetSpacedText(tempNode["body"].ToString()) + "\n";
                StartAnimatingText(spacedText, "right");
                isContinue = false;
                
                //грузим имена персонажей
                leftName.Text  = tempNode["opposite"].ToString();
                rightName.Text = tempNode["speaker"].ToString();

                dialogueAudio.LoadCharacter(
                    GetNpcName(npc), npc.dialogueCode, 
                    tempNode.Contains("config") ? tempNode["config"].ToString() : null
                );
                dialogueAudio.TryToPlayAudio(code);
                break;
            
            case "combat":
                npc.SetState(SetStateEnum.Idle);
                npc.aggressiveAgainstPlayer = true;
                npc.relation = Relation.Enemy;
                npc.SeekArea.AddEnemyInArea(player);
                npc = null;
                break;
            
            case "narration":
                string scriptName = tempNode["body"].ToString();
                
                string parameter = null;
                string key = null;
                if (tempNode.Contains("set")) 
                {
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
                        InitDialogueScript(tempScriptName, parameter, key);
                    }
                }
                //если указан только один скрипт
                else
                {
                    InitDialogueScript(scriptName, parameter, key);
                }

                //после выполнения скрипта сразу отправляемся на следующий нод
                if (tempNode.Contains("next"))
                {
                    MoveToNode(tempNode["next"].ToString());
                    return;
                }

                currentCode = null;
                MenuManager.CloseMenu(this);
                return;
        }
    }

    public void OpenMenu()
    {
        player.Connect(nameof(Character.TakenDamage), this, nameof(CloseUsingManager));
        
        Input.MouseMode = Input.MouseModeEnum.Visible;
        ((Control)GetParent()).Visible = true;
    }

    public void CloseUsingManager()
    {
        MenuManager.CloseMenu(this);
    }

    public void CloseMenu()
    {
        if (!MenuOn) return;
        
        dialogueAudio.Stop();
        Input.MouseMode = Input.MouseModeEnum.Captured;
        
        player.SetTalking(false);
        player.Disconnect(nameof(Character.TakenDamage), this, nameof(CloseUsingManager));
        
        if (npc != null) 
        {
            npc.SetState(SetStateEnum.Idle);
            npc = null;
        }

        text.BbcodeText = "";
        ((Control)GetParent()).Visible = false;
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

    public void _on_answer_pressed(int i)
    {
        if (answerText.Count <= i) return;
        mayAnswer = false;
        
        if (updateTempAnswer)
        {
            tempAnswer = answerText[i].ToString();
        }

        updateTempAnswer = true;
        tempAnswerI = -1;
        
        foreach (var answerButton in answers) 
        {
            answerButton.Visible = false;
        }

        if (answerText[i].ToString() == GetContinueText()) 
        {
            isContinue = true;
            MoveToNode(answerCodes[i]);
        } 
        else 
        {
            animatingAnswerNum = i;
            text.BbcodeText += GetBlockText(leftName.Text, "u") + ":\n";

            var customEffect = DialogueEffectsManager.GetCustomAudioConfig("strikely");
            dialogueAudio.LoadCharacter("strikely", "", customEffect);

            var textWithEffects = DialogueEffectsManager.GetTextWithEffects(
                answerText[i].ToString(),
                "strikely"
            );
            var spacedAnswerText = GetSpacedText(textWithEffects) + "\n";
            StartAnimatingText(spacedAnswerText);
        }
    }

    //прокручивает текст ответа при наведении на него, если он не влезает
    private void UpdateAnswerCooldown(float delta)
    {
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
    
    private void StartAnimatingText(string textValue, string block = null)
    {
        animatingText = textValue;
        cleanAnimatingText = DialogueDelay.ClearDelaySymbols(animatingText);
        isAnimatingText = true;
        skipLabel.Visible = true;
        
        tempTextBlock = block;
        if (block != null)
        {
            text.BbcodeText += "[" + block + "]";
        }

        text.BbcodeText += MakeSpacesString(cleanAnimatingText);
        
        symbolDelay = animatingAnswerNum == -1 && tempNode.Contains("timer")
            ? Global.ParseFloat(tempNode["timer"].ToString())
            : DialogueDelay.DEFAULT_SYMBOL_DELAY;
    }

    private static string MakeSpacesString(string origin)
    {
        return origin.Aggregate(
            "", 
            (current, originLetter) => current + (originLetter == '\n' ? '\n' : ' ')
        );
    }

    private void FinishAnimatingText()
    {
        if (tempTextBlock != null)
        {
            text.BbcodeText += "[/" + tempTextBlock + "]";
        }
        skipLabel.Visible = false;
        isAnimatingText = false;
        symbolTimer = 0;

        if (animatingAnswerNum > -1)
        {
            MoveToNode(answerCodes[animatingAnswerNum]);
            animatingAnswerNum = -1;
        }
        else
        {
            //грузим варианты ответов
            answerText.Clear();
            mayAnswer = true;
        
            if (tempNode.Contains("options")) 
            {
                if (!(tempNode["options"] is Array options)) return;
                for (int i = 0; i < answers.Length; i++) 
                {
                    if (i < options.Count) 
                    {
                        var option = options[i] as Dictionary;
                        if (option == null) continue;
                        var optionCode = option["next"].ToString();

                        if (!(nodes[optionCode] is Dictionary optionNode)) continue;
                        var optionText = optionNode["body"].ToString();

                        answerText.Add(optionText);
                        answers[i].Text = (i + 1) + ": " + optionText;
                        answers[i].Visible = true;

                        if (optionNode.Contains("next")) 
                        {
                            var optionNext = optionNode["next"].ToString();
                            answerCodes[i] = optionNext;
                        } 
                        else 
                        {
                            answerCodes[i] = "";
                        }
                    } 
                }
            } 
            else 
            {
                answers[0].Visible = true;
                answerText.Add(GetContinueText());
                answers[0].Text = "1: " + GetContinueText();
                answerCodes[0] = tempNode.Contains("next") ? tempNode["next"].ToString() : "";
            }
        }
    }
    
    private void UpdateAnimatingText(float delta)
    {
        if (!isAnimatingText) return;

        if (string.IsNullOrEmpty(animatingText))
        {
            FinishAnimatingText();
            return;
        }
        
        if (Input.IsActionJustPressed("jump"))
        {
            AddStringToText(cleanAnimatingText);
            FinishAnimatingText();
            return;
        }

        if (symbolTimer > 0)
        {
            symbolTimer -= delta;
            return;
        }

        var nextSymbol = animatingText[0];

        symbolTimer = DialogueDelay.Get(ref animatingText, DialogueDelay.DEFAULT_PHRASE_DELAY, symbolDelay);

        if (nextSymbol == DialogueDelay.DELAY_SYMBOL) return;
        
        AddStringToText(nextSymbol.ToString());
        dialogueAudio.UpdateDynamicPlaying(nextSymbol);
        animatingText = animatingText.Substring(1);
        cleanAnimatingText = cleanAnimatingText.Substring(1);
    }

    private void AddStringToText(string value)
    {
        var replacePos = text.BbcodeText.Length - cleanAnimatingText.Length;
        
        if (value == text.BbcodeText[replacePos].ToString()) return;
        
        text.BbcodeText = text.BbcodeText
            .Remove(replacePos, value.Length)
            .Insert(replacePos, value);
    }
    
    private static string GetBlockText(string blockText, string block)
    {
        return "[" + block + "]" + blockText + "[/" + block + "]";
    }

    private static string GetContinueText()
    {
        return InterfaceLang.GetPhrase("inGame", "dialogue", "continue");
    }

    //ограничивает длину строки на MAX_LINE_LENGTH символов
    private static string GetSpacedText(string text)
    {
        if (text.Length <= MAX_LINE_LENGTH) 
        {
            return text + "\n";
        }

        Array tempLines = new Array {text};
        tempLines = Global.ClumpLineLength(tempLines, MAX_LINE_LENGTH);

        return tempLines.Cast<string>().Aggregate(
            "", 
            (current, temp) => current + (temp + "\n")
        );
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

    public Dictionary GetSaveData()
    {
        return new Dictionary
        {
            {"currentCode", currentCode},
            {"npcPath", npc?.GetPath().ToString()}
        };
    }

    public void LoadData(Dictionary data)
    {
        if (!data.Contains("currentCode")) return;
        
        currentCode = (string) data["currentCode"];
        
        if (string.IsNullOrEmpty(currentCode)) return;

        var npcPath = (string) data["npcPath"];

        npc = GetNodeOrNull<NPC>(npcPath);

        if (npc == null) return;
        
        OnSaveDataLoaded();
    }

    private async  void OnSaveDataLoaded()
    {
        await ToSignal(GetTree(), "idle_frame");
        StartTalkingTo(npc);
    }
}
