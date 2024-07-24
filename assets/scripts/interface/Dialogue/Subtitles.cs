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
public class Subtitles : Label
{
    private const float DEFAULT_ANIMATING_COOLDOWN = 0.04f;
    private const float DEFAULT_FINISHING_TIMER = 0.8f;
    private const float VISIBLE_DISTANCE = 50;

    private static Player Player => Global.Get().player;
    private string tempTalkerName;
    private NPC tempTalker;
    private Label speakerLabel;
    private DialogueAudio dialogueAudio;
    private Dictionary tempPhrases;
    private Array tempPhraseKeys;
    private int phraseI;
    
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
        speakerLabel = GetNode<Label>("speaker");
        dialogueAudio = GetNode<DialogueAudio>("dialogueAudio");
        MenuBase.LoadColorForChildren(this);
    }
    
    public Subtitles SetTalker(AudioStreamPlayer3D audioPlayer3D, string characterName)
    {
        tempTalkerName = characterName;
        dialogueAudio.SetAudioPlayer(audioPlayer3D);
        dialogueAudio.LoadCharacter(tempTalkerName, "subtitles");
        
        return this;
    }

    public Subtitles SetTalker(NPC talker)
    {
        var audioPlayer = talker.GetNode("audiVoice");
        if (audioPlayer == null) return this;

        tempTalker = talker;
        tempTalkerName = talker.Name;
        dialogueAudio.SetAudioPlayer(audioPlayer);
        dialogueAudio.LoadCharacter(tempTalkerName, "subtitles");
        
        return this;
    }
    
    public Subtitles LoadSubtitlesFile(string subtitlesCode)
    {
        var lang = InterfaceLang.GetLang();
        var path = "assets/dialogues/" + lang + "/" + tempTalkerName + "/subtitles.json";
        tempPhrases = Global.LoadJsonFile(path)[subtitlesCode] as Dictionary;
        if (tempPhrases == null) return this;
        
        tempPhraseKeys = new Array(tempPhrases.Keys);
        phraseI = 0;

        return this;
    }
    
    public void StartAnimatingText()
    {
        if (IsAnimatingText) return;
        if (tempPhrases == null) return;

        var phraseKey = tempPhraseKeys[phraseI].ToString();
        if (tempPhrases[phraseKey] is not Dictionary phraseData) return;

        speakerLabel.Text = phraseData["name"].ToString();
        animatingText = phraseData["text"].ToString();
        phraseCooldown = phraseData.Contains("timer") ? Convert.ToSingle(phraseData["timer"]) : 0;
        IsAnimatingText = true;
        
        dialogueAudio.TryToPlayAudio(phraseKey);
        EmitSignal(nameof(ChangePhrase));
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
        speakerLabel.Text = "";
        Text = animatingText = "";
        IsAnimatingText = false;
        animatingCooldown = 0;

        phraseI++;
        if (tempPhrases.Count > phraseI)
        {
            StartAnimatingText();
        }
        else
        {
            EmitSignal(nameof(End));
        }
    }
    
    private void CheckTempTalker()
    {
        if (tempTalker == null) return;
        
        if (tempTalker.Health <= 0)
        {
            FinishAnimatingText();
        }

        var distance = Player.GlobalTransform.origin.DistanceTo(tempTalker.GlobalTransform.origin);
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
}
