using System;
using System.Linq;
using Godot;
using Godot.Collections;

public class SaveMenu : Control
{
    [Signal]
    public delegate void BackPressed();

    private MenuBase parentMenu;

    private Label pageLabel;

    private Button backButton;
    private Control tableBack;
    private Table table;
    private Button rewriteButton;
    private Button deleteButton;
    private Button saveButton;
    private LineEdit lineEdit;

    private Control existButtons;
    private Control newButtons;

    private string tempText = "";

    public void UpdateTable()
    {
        table.RedrawAllButtons();
    }
    public void LoadInterfaceLanguage()
    {
        pageLabel.Text = InterfaceLang.GetPhrase("saveloadMenu", "main", "page_save");
        backButton.Text = InterfaceLang.GetPhrase("saveloadMenu", "main", "back");
        rewriteButton.Text = InterfaceLang.GetPhrase("saveloadMenu", "main", "rewrite");
        deleteButton.Text = InterfaceLang.GetPhrase("saveloadMenu", "main", "delete");
        saveButton.Text = InterfaceLang.GetPhrase("saveloadMenu", "main", "save");
        lineEdit.PlaceholderText = InterfaceLang.GetPhrase("saveloadMenu", "main", "input_placeholder");
        
        table.HeaderName = InterfaceLang.GetPhrase("saveloadMenu", "table", "name");
        table.HeaderDate = InterfaceLang.GetPhrase("saveloadMenu", "table", "date");
        table.HeaderLevel = InterfaceLang.GetPhrase("saveloadMenu", "table", "level");
        table.UpdateHeader();
    }

    public override void _Ready()
    {
        parentMenu = GetParent<MenuBase>();
        pageLabel = GetNode<Label>("page_label");
        backButton = GetNode<Button>("back");
        tableBack = GetNode<Control>("Table");
        table = tableBack.GetNode<Table>("table");
        rewriteButton = GetNode<Button>("Exist/Rewrite");
        deleteButton = GetNode<Button>("Exist/Delete");
        saveButton = GetNode<Button>("New/Save");
        lineEdit = GetNode<LineEdit>("LineEdit");
        existButtons = GetNode<Control>("Exist");
        newButtons = GetNode<Control>("New");
        LoadInterfaceLanguage();
    }

    public override void _Process(float delta)
    {
        if (!MenuManager.SomeMenuOpen && Input.IsActionJustPressed("ui_quicksave"))
        {
            SaveGame("quicksave", GetTree());
            Messages messages = GetNode<Messages>("/root/Main/Scene/canvas/messages");
            messages.ShowMessage("gameQuicksaved");
        }
    }

    public void _on_back_pressed()
    {
        parentMenu.SoundClick();
        EmitSignal(nameof(BackPressed));
    }

    public void _on_mouse_entered(string section, string messageLink)
    {
        parentMenu._on_mouse_entered(section, messageLink, "saveloadMenu");
    }
    
    public void _on_mouse_exited()
    {
        parentMenu._on_mouse_exited();
    }

    public void _on_TableButtonPressed(string fileName)
    {
        parentMenu.SoundClick();
        lineEdit.Text = fileName;
        UpdateControls(true, true);
        lineEdit.EmitSignal("text_changed", fileName);
    }

    private static bool IsValidFilename(string line)
    {
        var forbiddenChars = new[] {':', '/', '\\', '?', '*', '\"', '|', '%', '<', '>'};
        var lineArray = line.ToCharArray();
        return !forbiddenChars.Any(tempLineChar => lineArray.Any(tempCheckChar => tempLineChar == tempCheckChar));
    }

    public void _on_LineEdit_text_changed(string newText)
    {
        var textNotEmpty = newText.Length > 0;
        if (textNotEmpty)
        {
            if (!IsValidFilename(newText))
            {
                int oldCaretPosition = lineEdit.CaretPosition;
                lineEdit.Text = tempText;
                lineEdit.CaretPosition = oldCaretPosition - 1;
                return;
            }

            tempText = newText;
            var lineExists = table.LineExists(newText);
            UpdateControls(true, lineExists);
            return;
        }
        
        UpdateControls(false);
    }

    public void _on_Save_pressed()
    {
        SaveGame(lineEdit.Text, GetTree());
        CreateTableLine(lineEdit.Text);
    }

    public void _on_Rewrite_pressed()
    {
        parentMenu.SoundClick();
        SaveGame(lineEdit.Text, GetTree());
        table.UpdateButton(createNewTableLine(lineEdit.Text));
    }

    public void _on_Delete_pressed()
    {
        parentMenu.SoundClick();
        string fileName = GetLikeLatinString(lineEdit.Text) + ".sav";
        Global.DeleteSaveFile(fileName);
        table.DeleteButton(lineEdit.Text);
        
        UpdateControls(true);
    }
    
    private FileTableLine createNewTableLine(string saveName)
    {
        return new FileTableLine(
            saveName,
            DateTime.Now.ToShortDateString(),
            InterfaceLang.GetPhrase("levels", "levelNames", LevelsLoader.tempLevelNum.ToString())
        );
    }

    private void UpdateControls(bool textNotEmpty, bool lineExists = false)
    {
        if (textNotEmpty)
        {
            newButtons.Visible = !lineExists;
            existButtons.Visible = lineExists;
        }
        
        rewriteButton.Disabled = !textNotEmpty;
        saveButton.Disabled    = !textNotEmpty;
        deleteButton.Disabled  = !textNotEmpty;
    }

    //создает и выделяет запись в таблице
    private void CreateTableLine(string fileName)
    {
        var fileTableLine = createNewTableLine(fileName); 
        Global.saveFilesArray.Add(fileTableLine);
        Button newButton = table.SpawnButton(fileTableLine);
        newButton.Pressed = true;
        table._on_table_button_click(newButton.Name);
    }

    public static void SaveGame(string fileName, SceneTree tree)
    {
        var saveFile = new File();
        
        var filePath = $"res://saves/{GetLikeLatinString(fileName)}.sav";
        //saveFile.OpenCompressed(filePath, File.ModeFlags.Write);
        saveFile.Open(filePath, File.ModeFlags.Write);
        
        saveFile.StoreLine(fileName);                            //название сохранения
        saveFile.StoreLine(DateTime.Now.ToShortDateString());             //дата
        saveFile.StoreLine(LevelsLoader.tempLevelNum.ToString());         //номер текущего уровня
        saveFile.StoreLine(Global.RaceToString(Global.Get().playerRace)); //раса
        
        //данные удаленных объектов
        saveFile.StoreLine(JSON.Print(Global.deletedObjects));
        
        //данные игровых объектов
        var objectsData = new Dictionary<string, Dictionary>();
        foreach (Node tempNode in tree.GetNodesInGroup("savable"))
        {
            if (!(tempNode is ISavable savableNode)) continue;
            Dictionary tempData = savableNode.GetSaveData();
            objectsData.Add(tempNode.Name, tempData);
        }
        saveFile.StoreLine(JSON.Print(objectsData));
        saveFile.Close();
    }
    
    public static string GetLikeLatinString(string cyrillicLine)
    {
        var DicVer = new System.Collections.Generic.Dictionary<char, char>()
        {
            {'а', 'а'}, {'А', 'A'},
            {'б', 'b'}, {'Б', 'B'},
            {'в', 'v'}, {'В', 'V'},
            {'г', 'g'}, {'Г', 'G'},
            {'д', 'd'}, {'Д', 'D'},
            {'е', 'e'}, {'Е', 'E'},
            {'ё', 'e'}, {'Ё', 'E'},
            {'ж', 'g'}, {'Ж', 'G'},
            {'з', 'z'}, {'З', 'Z'},
            {'и', 'i'}, {'И', 'I'},
            {'й', 'y'}, {'Й', 'Y'},
            {'к', 'k'}, {'К', 'K'},
            {'л', 'l'}, {'Л', 'L'},
            {'м', 'm'}, {'М', 'M'},
            {'н', 'n'}, {'Н', 'N'},
            {'о', 'o'}, {'О', 'O'},
            {'п', 'p'}, {'П', 'P'},
            {'р', 'r'}, {'Р', 'R'},
            {'с', 's'}, {'С', 'S'},
            {'т', 't'}, {'Т', 'T'},
            {'у', 'u'}, {'У', 'U'},
            {'ф', 'f'}, {'Ф', 'F'},
            {'х', 'h'}, {'Х', 'H'},
            {'ц', 'c'}, {'Ц', 'C'},
            {'ч', 'h'}, {'Ч', 'H'},
            {'ш', 'h'}, {'Ш', 'H'},
            {'щ', 'h'}, {'Щ', 'H'},
            {'ъ', 'b'}, {'Ъ', 'b'},
            {'ы', 'i'}, {'Ы', 'I'},
            {'ь', 'b'}, {'Ь', 'b'},
            {'э', 'a'}, {'Э', 'A'},
            {'ю', 'u'}, {'Ю', 'U'},
            {'я', 'a'}, {'Я', 'A'},

            {' ', '_'}
        };

        foreach (char ch in cyrillicLine)
        {
            cyrillicLine = cyrillicLine.Replace(ch, DicVer.ContainsKey(ch)?DicVer[ch]:ch);
        }

        return cyrillicLine;
    }
}
