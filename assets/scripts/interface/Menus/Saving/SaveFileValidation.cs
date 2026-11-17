using Godot;
using System.Linq;

// Валидация текста в поле ввода автосохранения в главном меню
// Логика дублирует валидацию из SaveMenu.cs
public partial class SaveFileValidation : Node
{
    private LineEdit lineEdit;
    private string tempText = "autosave";

    
    public override void _Ready()
    {
        lineEdit = GetParent<LineEdit>();
    }
    
    public void _on_input_text_changed(string newText)
    {
        var textNotEmpty = newText.Length > 0;

        if (textNotEmpty)
        {
            if (!IsValidFilename(newText))
            {
                var oldCaretPosition = lineEdit.CaretColumn;
                lineEdit.Text = tempText;
                lineEdit.CaretColumn = oldCaretPosition - 1;
                return;
            }
        }
        
        tempText = newText;
    }
    
    public static bool IsValidFilename(string line)
    {
        var forbiddenChars = new[] {':', '/', '\\', '?', '*', '\"', '|', '%', '<', '>'};
        var lineArray = line.ToCharArray();
        return !forbiddenChars.Any(tempLineChar => lineArray.Any(tempCheckChar => tempLineChar == tempCheckChar));
    }
}
