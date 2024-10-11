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
    private const float VISIBLE_DISTANCE = 50;

    private static Player Player => Global.Get().player;

    public bool IsAnimatingText { get; private set; }
    public bool MayClearCode = true;
    
    // собеседник с которым в целом начат разговор
    public NPC Talker { get; private set; } 
    private string talkerCode;
    
    // тот кто говорит в данный момент через субтитры
    private Label tempSpeakerLabel;
    private string tempSpeakerCode;
    
    private DialogueAudio dialogueAudio;
    
    private string subtitlesCode;
    private Dictionary phrases;
    private Array phrasesKeys;
    private int tempPhraseIndex;
    private string tempPhraseKey;
    private Dictionary tempPhraseData;
    
    private string animatingText;
    private float phraseDelay;
    private float symbolDelay;
    private float symbolTimer;
    
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
    
    public override void _Process(float delta)
    {
        if (!IsAnimatingText) return;
        
        if (symbolTimer > 0)
        {
            symbolTimer -= delta;
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
        phrases = Global.LoadJsonFile(path)[subtitlesCode] as Dictionary;
        if (phrases == null) return this;
        
        phrasesKeys = new Array(phrases.Keys);
        tempPhraseIndex = phraseI;

        return this;
    }
    
    public void StartAnimatingText()
    {
        if (IsAnimatingText) return;
        if (phrases == null) return;

        MayClearCode = true;
        tempPhraseKey = phrasesKeys[tempPhraseIndex].ToString();
        tempPhraseData = phrases[tempPhraseKey] as Dictionary;

        ReadPhraseText();
        
        EmitSignal(nameof(ChangePhrase));
    }

    private void ReadPhraseText()
    {
        if (tempPhraseData.Contains("speakerCode") && tempPhraseData.Contains("text"))
        {
            var lang = InterfaceLang.GetLang();
            var namesPath = $"assets/lang/{lang}/names.json";
            
            tempSpeakerCode = tempPhraseData["speakerCode"].ToString();
            tempSpeakerLabel.Text = Global.LoadJsonFile(namesPath)[tempSpeakerCode].ToString();
            
            animatingText = tempPhraseData["text"].ToString();
            
            symbolDelay = tempPhraseData.Contains("timer") 
                ? Convert.ToSingle(tempPhraseData["timer"]) 
                : DialogueDelay.DEFAULT_SYMBOL_DELAY; 
            
            phraseDelay = tempPhraseData.Contains("delay") 
                ? Convert.ToSingle(tempPhraseData["delay"]) 
                : DialogueDelay.DEFAULT_PHRASE_DELAY;
            
            IsAnimatingText = true;
        
            dialogueAudio.LoadCharacter(tempSpeakerCode, "subtitles");
            dialogueAudio.TryToPlayAudio(tempPhraseKey);
        }
        else FinishAnimatingText();
    }
    
    private void UpdateAnimatingText()
    {
        var nextSymbol = animatingText[0];
        
        symbolTimer = DialogueDelay.Get(ref animatingText, phraseDelay, symbolDelay);

        if (nextSymbol == DialogueDelay.DELAY_SYMBOL) return;
        
        Text += nextSymbol.ToString();
        dialogueAudio.UpdateDynamicPlaying(nextSymbol);
        animatingText = animatingText.Substring(1);
    }

    private void FinishAnimatingText()
    {
        tempSpeakerLabel.Text = "";
        Text = animatingText = "";
        IsAnimatingText = false;
        symbolTimer = 0;
        
        ReadPhraseScript();
        
        tempPhraseIndex++;
        if (phrases.Count > tempPhraseIndex)
        {
            StartAnimatingText();
        }
        else
        {
            ClearSubtitles();
            EmitSignal(nameof(End));
        }
    }

    private void ReadPhraseScript()
    {
        if (tempPhraseData.Contains("class"))
        {
            var scriptName = tempPhraseData["class"].ToString();
            var scriptType = Type.GetType("DialogueScripts." + scriptName);
            var parameter = "";
            var key = "";
            
            if (scriptType == null) return;
            
            if (tempPhraseData.Contains("value"))
            {
                parameter = tempPhraseData["value"].ToString();
            }
            
            if (tempPhraseData.Contains("key"))
            {
                key = tempPhraseData["key"].ToString();
            }
            
            var scriptObj = Activator.CreateInstance(scriptType) as DialogueScripts.IDialogueScript;
            scriptObj?.initiate(this, parameter, key);
        }
    }

    private void ClearSubtitles()
    {
        if (MayClearCode && Talker != null)
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

        var distance = Player.GlobalTranslation.DistanceTo(Talker.GlobalTranslation);
        Visible = distance < VISIBLE_DISTANCE;
    }

    public Dictionary GetSaveData()
    {
        return new Dictionary()
        {
            {"talkerPath", Talker?.GetPath()},
            {"audioPath", dialogueAudio.GetAudioPlayerPath()},
            {"talkerName", talkerCode},
            {"code", subtitlesCode},
            {"phrase", tempPhraseIndex}
        };
    }

    public void LoadData(Dictionary data)
    {
        subtitlesCode = Convert.ToString(data["code"]);
        tempPhraseIndex = Convert.ToInt32(data["phrase"]);
        
        var talkerPath = Convert.ToString(data["talkerPath"]);
        var talker = GetNodeOrNull<NPC>(talkerPath);
        
        if (talker != null)
        {
            SetTalker(talker)
                .LoadSubtitlesFile(subtitlesCode, tempPhraseIndex)
                .StartAnimatingText();
            
            return;
        }
        
        var talkerName = Convert.ToString(data["talkerName"]);
        var audioPath = Convert.ToString(data["audioPath"]);
        
        if (string.IsNullOrEmpty(talkerName)) return;
        if (string.IsNullOrEmpty(audioPath)) return;
        
        var audioPlayer3D = GetNode<AudioStreamPlayer3D>(audioPath);
        
        SetTalker(audioPlayer3D, talkerName)
            .LoadSubtitlesFile(subtitlesCode, tempPhraseIndex)
            .StartAnimatingText();
    }
}
