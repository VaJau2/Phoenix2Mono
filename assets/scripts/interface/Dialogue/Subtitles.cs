using System;
using Godot;
using Godot.Collections;
using Array = Godot.Collections.Array;

//Пример вызова субтитров с NPC:
//
// subtitles.SetTalker(npc)
//    .LoadSubtitlesFile(subtitlesCode)
//    .StartAnimatingText();
// 
//Может проигрывать субтитры без NPC, для этого нужно вызвать таким образом:
//
//  subtitles.SetTalker(audioPlayer3d, "configCode")
//    .LoadSubtitlesFile(subtitlesCode)
//    .StartAnimatingText();
//
public class Subtitles : Label, ISavable
{
    private const float DEFAULT_ANIMATING_COOLDOWN = 0.04f;
    private const float DEFAULT_FINISHING_TIMER = 0.8f;
    private const float VISIBLE_DISTANCE = 50;

    private static Player Player => Global.Get().player;
    
    // собеседник с которым в целом начат разговор
    public NPC Talker { get; private set; } 
    private string talkerCode;
    
    // тот кто говорит в данный момент через субтитры
    private Label tempSpeakerLabel;
    private string tempSpeakerCode;
    
    private DialogueAudio dialogueAudio;
    
    private string subtitlesCode;
    private Dictionary tempPhrases;
    private Array tempPhraseKeys;
    private int phraseIndex;
    
    private string animatingText;
    private float phraseCooldown;
    private float animatingCooldown;
    
    public bool IsAnimatingText { get; private set; }

    [Signal]
    public delegate void ChangePhrase();
    
    [Signal]
    public delegate void End();
    
    public override void _Ready()
    {
        tempSpeakerLabel = GetNode<Label>("speaker");
        dialogueAudio = GetNode<DialogueAudio>("dialogueAudio");
        MenuBase.LoadColorForChildren(this);
    }
    
    public Subtitles SetTalker(AudioStreamPlayer3D audioPlayer3D, string characterName)
    {
        talkerCode = characterName;
        dialogueAudio.SetAudioPlayer(audioPlayer3D);
        
        return this;
    }

    public Subtitles SetTalker(NPC talker)
    {
        var audioPlayer = talker.GetNode<AudioStreamPlayer3D>("audiVoice");
        if (audioPlayer == null) return this;

        Talker = talker;
        talkerCode = talker.Name;
        dialogueAudio.SetAudioPlayer(audioPlayer);
        
        return this;
    }
    
    public Subtitles LoadSubtitlesFile(string subCode, int phraseI = 0)
    {
        var lang = InterfaceLang.GetLang();
        var path = $"assets/dialogues/{lang}/{talkerCode}/subtitles.json";

        subtitlesCode = subCode;
        tempPhrases = Global.LoadJsonFile(path)[subtitlesCode] as Dictionary;
        if (tempPhrases == null) return this;
        
        tempPhraseKeys = new Array(tempPhrases.Keys);
        phraseIndex = phraseI;

        return this;
    }
    
    public void StartAnimatingText()
    {
        if (IsAnimatingText) return;
        if (tempPhrases == null) return;

        var phraseKey = tempPhraseKeys[phraseIndex].ToString();
        if (tempPhrases[phraseKey] is not Dictionary phraseData) return;

        ReadPhrase(phraseKey, phraseData);
        
        EmitSignal(nameof(ChangePhrase));
    }

    private void ReadPhrase(string phraseKey, Dictionary phraseData)
    {
        if (phraseData.Contains("speakerCode") && phraseData.Contains("text"))
        {
            var lang = InterfaceLang.GetLang();
            var namesPath = $"assets/lang/{lang}/names.json";
            
            tempSpeakerCode = phraseData["speakerCode"].ToString();
            tempSpeakerLabel.Text = Global.LoadJsonFile(namesPath)[tempSpeakerCode].ToString();
            
            animatingText = phraseData["text"].ToString();
            phraseCooldown = phraseData.Contains("timer") ? Convert.ToSingle(phraseData["timer"]) : 0;
            IsAnimatingText = true;
        
            dialogueAudio.LoadCharacter(tempSpeakerCode, "subtitles");
            dialogueAudio.TryToPlayAudio(phraseKey);
        }
        
        if (phraseData.Contains("class"))
        {
            var scriptName = phraseData["class"].ToString();
            var scriptType = Type.GetType("DialogueScripts." + scriptName);
            var parameter = "";
            
            if (scriptType == null) return;
            
            if (phraseData.Contains("value"))
            {
                parameter = phraseData["value"].ToString();
            }
            
            var scriptObj = Activator.CreateInstance(scriptType) as DialogueScripts.IDialogueScript;
            scriptObj?.initiate(this, parameter);
        }
    }
    
    private void UpdateAnimatingText()
    {
        var nextSymbol = animatingText[0];
        Text += nextSymbol.ToString();
        dialogueAudio.UpdateDynamicPlaying(nextSymbol);
        animatingText = animatingText.Substring(1);
        if (string.IsNullOrEmpty(animatingText))
        {
            animatingCooldown = DEFAULT_FINISHING_TIMER;
        }
        else
        {
            animatingCooldown = phraseCooldown > 0 ? phraseCooldown : DEFAULT_ANIMATING_COOLDOWN;
        }
    }

    private void FinishAnimatingText()
    {
        tempSpeakerLabel.Text = "";
        Text = animatingText = "";
        IsAnimatingText = false;
        animatingCooldown = 0;

        phraseIndex++;
        if (tempPhrases.Count > phraseIndex)
        {
            StartAnimatingText();
        }
        else
        {
            ClearSubtitles();
            EmitSignal(nameof(End));
        }
    }

    private void ClearSubtitles()
    {
        if (Talker != null)
        {
            Talker.subtitlesCode = null;
        }
        
        subtitlesCode = null;
        Talker = null;
        talkerCode = null;
    }
    
    private void CheckTempTalker()
    {
        if (Talker == null) return;
        
        if (Talker.Health <= 0)
        {
            FinishAnimatingText();
        }

        var distance = Player.GlobalTransform.origin.DistanceTo(Talker.GlobalTransform.origin);
        Visible = distance < VISIBLE_DISTANCE;
    }

    public override void _Process(float delta)
    {
        if (!IsAnimatingText) return;
        
        if (animatingCooldown > 0)
        {
            animatingCooldown -= delta;
            return;
        }

        if (string.IsNullOrEmpty(animatingText))
        {
            FinishAnimatingText();
            return;
        }

        CheckTempTalker();
        UpdateAnimatingText();
    }

    public Dictionary GetSaveData()
    {
        return new Dictionary()
        {
            {"talkerPath", Talker?.GetPath()},
            {"audioPath", dialogueAudio.GetAudioPlayerPath()},
            {"talkerName", talkerCode},
            {"code", subtitlesCode},
            {"phrase", phraseIndex}
        };
    }

    public void LoadData(Dictionary data)
    {
        subtitlesCode = Convert.ToString(data["code"]);
        phraseIndex = Convert.ToInt32(data["phrase"]);
        
        var talkerPath = Convert.ToString(data["talkerPath"]);
        var talker = GetNodeOrNull<NPC>(talkerPath);
        
        if (talker != null)
        {
            SetTalker(talker)
                .LoadSubtitlesFile(subtitlesCode, phraseIndex)
                .StartAnimatingText();
            
            return;
        }
        
        var talkerName = Convert.ToString(data["talkerName"]);
        var audioPath = Convert.ToString(data["audioPath"]);
        
        if (string.IsNullOrEmpty(talkerName)) return;
        if (string.IsNullOrEmpty(audioPath)) return;
        
        var audioPlayer3D = GetNode<AudioStreamPlayer3D>(audioPath);
        
        SetTalker(audioPlayer3D, talkerName)
            .LoadSubtitlesFile(subtitlesCode, phraseIndex)
            .StartAnimatingText();
    }
}
