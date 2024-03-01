using System.Linq;
using Godot;
using Godot.Collections;

public partial class DialogueMenu : Control, IMenu
{
    Global global => Global.Get();
    public bool mustBeClosed => false;
    
    private const int MAX_LINE_LENGTH = 50;
    private const int MAX_ANSWER_LENGTH = 65;
    private const float DEFAULT_ANIMATING_COOLDOWN = 0.04f;

    private const float MIN_NPC_DISTANCE_CHECK = 2f;
    private const float LOOK_POS_DELTA = 1.5f;

    public NPC npc {get; private set;}
    public Player player => Global.Get().player;
    public bool MenuOn => ((Control)GetParent()).Visible;
    
    private Dictionary nodes;
    private Dictionary tempNode;

    private RichTextLabel text;
    private Label skipLabel;
    private bool isAnimatingText;
    private int animatingAnswerNum = -1;
    private string animatingText;
    private float animatingCooldown;
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
    public delegate void FinishTalkingEventHandler();

    public void StartTalkingTo(NPC newNpc)
    {
        //диалог должен закрывать любое открытое меню
        //прим. - меню терминала, которое имеет mustBeClosed = false
        if (MenuManager.SomeMenuOpen)
        {
            if (MenuManager.openedMenu is GodotObject && IsInstanceValid(MenuManager.openedMenu as GodotObject))
            {
                MenuManager.CloseMenu(MenuManager.openedMenu);
            }
        }
        
        if (!MenuManager.TryToOpenMenu(this, true)) return;
        npc = newNpc;
        npc.SetState(NPCState.Talk);
        npc.tempVictim = player;
        text.Text = "";
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
        
        string lang = InterfaceLang.GetLang();
        string path = "assets/dialogues/" + lang + "/" + npc.Name + "/" + npc.dialogueCode + ".json";
        nodes = Global.LoadJsonFile(path)["nodes"].AsGodotDictionary();
        if (nodes != null)
        {
            MoveToNode(((Array)nodes.Keys)[0].ToString());
        }
    }

    private void InitDialogueScript(string scriptName, string parameter, string key = "")
    {
        var scriptType = System.Type.GetType("DialogueScripts." + scriptName);
        if (scriptType == null) return;
        var scriptObj = System.Activator.CreateInstance(scriptType) as DialogueScripts.IDialogueScript;
        scriptObj?.initiate(this, parameter, key);
    }

    private void MoveToNode(string code)
    {
        if (code == "") 
        {
            MenuManager.CloseMenu(this);
            return;
        }

        tempNode = nodes[code].AsGodotDictionary();
        
        switch (tempNode["kind"].ToString()) 
        {
            case "dialogue":
                //грузим текст фразы
                if (!isContinue || rightName.Text != tempNode["speaker"].ToString()) 
                {
                    //имя персонажа
                    string uName = GetBlockText(tempNode["speaker"].ToString(), "u");
                    text.Text += GetBlockText(uName + ":", "right") + "\n";
                }
                
                var spacedText = GetSpacedText(tempNode["body"].ToString()) + "\n";
                StartAnimatingText(spacedText, "right");
                isContinue = false;
                
                //грузим имена персонажей
                leftName.Text  = tempNode["opposite"].ToString();
                rightName.Text = tempNode["speaker"].ToString();

                dialogueAudio.LoadCharacter(
                    npc.Name, npc.dialogueCode, 
                    tempNode.TryGetValue("config", out var configValue) 
                        ? configValue.ToString() 
                        : null
                );
                dialogueAudio.TryToPlayAudio(code);
                break;
            
            case "combat":
                npc.SetState(NPCState.Idle);
                npc.aggressiveAgainstPlayer = true;
                npc.relation = Relation.Enemy;
                npc.seekArea.AddEnemyInArea(player);
                npc = null;
                break;
            
            case "narration":
                string scriptName = tempNode["body"].ToString();
                
                string parameter = null;
                string key = null;
                if (tempNode.ContainsKey("set")) 
                {
                    if (tempNode["set"].Obj is Array paramsArray)
                    {
                        if (paramsArray[0].Obj is Dictionary paramsDict)
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
                if (tempNode.ContainsKey("next"))
                {
                    MoveToNode(tempNode["next"].ToString());
                    return;
                }

                MenuManager.CloseMenu(this);
                return;
        }
    }

    public void OpenMenu()
    {
        player.Camera3D.HideInteractionSquare();

        if (!string.IsNullOrEmpty(player.Inventory.weapon))
        {
            var useHandler = player.Inventory.UseHandler;
            useHandler.UnwearItem(useHandler.weaponButton);
        }
        
        player.MayRotateHead = player.MayMove = false;
        Input.MouseMode = Input.MouseModeEnum.Visible;
        ((Control)GetParent()).Visible = true;
    }

    public void CloseMenu()
    {
        dialogueAudio.Stop();
        player.Inventory.SetBindsCooldown(0.5f);
        Input.MouseMode = Input.MouseModeEnum.Captured;
        
        player.SetMayMove(true);
        player.MayRotateHead = true;
        
        if (npc != null) 
        {
            npc.SetState(NPCState.Idle);
            npc = null;
        }

        text.Text = "";
        ((Control)GetParent()).Visible = false;
        EmitSignal(nameof(FinishTalkingEventHandler));
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
            text.Text += GetBlockText(leftName.Text, "u") + ":\n";
            dialogueAudio.LoadCharacter("strikely", "");

            var spacedAnswerText = GetSpacedText(answerText[i].ToString()) + "\n";
            StartAnimatingText(spacedAnswerText);
        }
    }

    public override void _Ready()
    {
        MenuBase.LoadColorForChildren(this);

        dialogueAudio = GetNode<DialogueAudio>("../audi");
        text      = GetNode<RichTextLabel>("text");
        skipLabel = GetNode<Label>("skipLabel");
        leftName  = GetNode<Label>("leftName");
        rightName = GetNode<Label>("rightName");
        answers   = new[] 
        {
            GetNode<Button>("answer1"),
            GetNode<Button>("answer2"),
            GetNode<Button>("answer3"),
            GetNode<Button>("answer4")
        };
        answerCodes = new[] {"", "", "", ""};
        answerText = new Array();
    }

    public override void _Process(double delta)
    {
        if (!MenuOn) return;
        
        if (npc is not { state: NPCState.Talk }) 
        {
            MenuManager.CloseMenu(this);
            return;
        }

        player?.LookAt(GetNpcLookPosition());
        UpdateAnswerCooldown((float)delta);
        UpdateAnimatingText((float)delta);
    }

    private Vector3 GetNpcLookPosition()
    {
        var npcPos = npc.GlobalTransform.Origin;
        var playerRelativePos = player.GlobalTransform.Origin - npcPos;

        npcPos.X += GetLookPosDelta(playerRelativePos.Z);
        npcPos.Z += GetLookPosDelta(-playerRelativePos.X);
        
        return npcPos;
    }

    private float GetLookPosDelta(float sideRelativePos)
    {
        return sideRelativePos switch
        {
            > MIN_NPC_DISTANCE_CHECK => LOOK_POS_DELTA,
            < MIN_NPC_DISTANCE_CHECK => -LOOK_POS_DELTA,
            _ => 0
        };
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
        isAnimatingText = true;
        skipLabel.Visible = true;
        
        tempTextBlock = block;
        if (block != null)
        {
            text.Text += "[" + block + "]";
        }

        text.Text += MakeSpacesString(textValue);
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
            text.Text += "[/" + tempTextBlock + "]";
        }
        skipLabel.Visible = false;
        isAnimatingText = false;
        animatingCooldown = 0;

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
        
            if (tempNode.TryGetValue("options", out var optionsValue)) 
            {
                if (optionsValue.Obj is not Array options) return;
                for (int i = 0; i < answers.Length; i++) 
                {
                    if (i < options.Count) 
                    {
                        var option = options[i].AsGodotDictionary();
                        var optionCode = option["next"].ToString();

                        if (nodes[optionCode].Obj is not Dictionary optionNode) continue;
                        var optionText = optionNode["body"].ToString();

                        answerText.Add(optionText);
                        answers[i].Text = (i + 1) + ": " + optionText;
                        answers[i].Visible = true;

                        if (optionNode.TryGetValue("next", out var nextValue)) 
                        {
                            var optionNext = nextValue.ToString();
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
                answerCodes[0] = tempNode.TryGetValue("next", out var nextValue) ? nextValue.ToString() : "";
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
            AddStringToText(animatingText);
            FinishAnimatingText();
            return;
        }

        if (animatingCooldown > 0)
        {
            animatingCooldown -= delta;
            return;
        }

        var nextSymbol = animatingText[0];
        AddStringToText(nextSymbol.ToString());
        dialogueAudio.UpdateDynamicPlaying(nextSymbol);
        animatingCooldown = GetAnimatingCooldown(nextSymbol);
        animatingText = animatingText.Substring(1);
    }

    private void AddStringToText(string value)
    {
        var replacePos = text.Text.Length - animatingText.Length;
        if (value == text.Text[replacePos].ToString())
        {
            return;
        }
        text.Text = text.Text.Remove(replacePos, value.Length).Insert(replacePos, value);
    }

    private float GetAnimatingCooldown(char newSymbol)
    {
        var nodeTimer = animatingAnswerNum == -1 && tempNode.TryGetValue("timer", out var timerValue)
            ? Global.ParseFloat(timerValue.ToString())
            : DEFAULT_ANIMATING_COOLDOWN;
        
        switch (newSymbol)
        {
            case '…':
            case '.':
            case '!':
            case '?':
                return nodeTimer + 0.4f;
            case ',':
                return nodeTimer + 0.1f;
            default:
                return nodeTimer;
        }
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

        Array<string> tempLines = [text];
        tempLines = Global.ClumpLineLength(tempLines, MAX_LINE_LENGTH);

        return tempLines.Aggregate(
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

        switch (eventKey.Keycode)
        {
            case Key.Key1:
                _on_answer_pressed(0);
                break;
            case Key.Key2:
                _on_answer_pressed(1);
                break;
            case Key.Key3:
                _on_answer_pressed(2);
                break;
            case Key.Key4:
                _on_answer_pressed(3);
                break;
        }
    }
}
