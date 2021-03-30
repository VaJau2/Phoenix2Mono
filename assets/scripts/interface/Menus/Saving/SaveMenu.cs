using System;
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

    public void _on_LineEdit_text_changed(string newText)
    {
        var textNotEmpty = newText.Length > 0;
        if (textNotEmpty)
        {
            var lineExists = table.LineExists(newText);
            UpdateControls(true, lineExists);
            return;
        }

        UpdateControls(false);
    }

    public void _on_Save_pressed()
    {
        SaveGame(lineEdit.Text);
        CreateTableLine(lineEdit.Text);
    }

    public void _on_Rewrite_pressed()
    {
        parentMenu.SoundClick();
        SaveGame(lineEdit.Text);
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

    private void SaveGame(string fileName)
    {
        var saveFile = new File();
        
        var filePath = $"res://saves/{GetLikeLatinString(fileName)}.sav";
        //saveFile.OpenCompressed(filePath, File.ModeFlags.Write);
        saveFile.Open(filePath, File.ModeFlags.Write);
        
        saveFile.StoreLine(fileName);                             //название сохранения
        saveFile.StoreLine(DateTime.Now.ToShortDateString());     //дата
        saveFile.StoreLine(LevelsLoader.tempLevelNum.ToString()); //номер текущего уровня
        //данные игровых объектов
        var objectsData = new Dictionary<string, Dictionary>();
        foreach (Node tempNode in GetTree().GetNodesInGroup("savable"))
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
            {'а', 'а'},
            {'б', 'b'},
            {'в', 'v'},
            {'г', 'g'},
            {'д', 'd'},
            {'е', 'e'},
            {'ё', 'e'},
            {'ж', 'g'},
            {'з', 'z'},
            {'и', 'i'},
            {'й', 'y'},
            {'к', 'k'},
            {'л', 'l'},
            {'м', 'm'},
            {'н', 'n'},
            {'о', 'o'},
            {'п', 'p'},
            {'р', 'r'},
            {'с', 's'},
            {'т', 't'},
            {'у', 'u'},
            {'ф', 'f'},
            {'х', 'h'},
            {'ц', 'c'},
            {'ч', 'h'},
            {'ш', 'h'},
            {'щ', 'h'},
            {'ъ', 'b'},
            {'ы', 'i'},
            {'ь', 'b'},
            {'э', 'a'},
            {'ю', 'u'},
            {'я', 'a'},
            {' ', '_'}
        };

        foreach (char ch in cyrillicLine)
        {
            cyrillicLine = cyrillicLine.Replace(ch, DicVer.ContainsKey(ch)?DicVer[ch]:ch);
        }

        return cyrillicLine;
    }
}
