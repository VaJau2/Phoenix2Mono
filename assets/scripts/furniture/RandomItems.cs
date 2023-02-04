using Godot;
using Godot.Collections;

public class RandomItems : Node
{
    [Export]
    public int maxItemsCount = 5;
    [Export]
    public string moneyNameCode = "bits";
    [Export]
    public float moneyChance = 0.5f;
    [Export]
    public int maxMoneyCount = 5;
    
    //в списке лежат и патроны тоже
    [Export]
    public Array<string> itemCodes = new Array<string>();
    
    //здесь определяется их количество
    [Export]
    public Dictionary<string, int> ammoCount = new Dictionary<string, int>();

    public void LoadRandomItems(Array<string> chestItems, Dictionary<string, int> chestAmmo, int maxItems = 0) 
    {
        RandomNumberGenerator rand = new RandomNumberGenerator();
        rand.Randomize();

        chestItems.Clear();
        chestAmmo.Clear();

        int tempMax = maxItems != 0 ? maxItems : maxItemsCount;
        int itemsCount = rand.RandiRange(1, tempMax); //специально для плечачника не 0, а 1

        for (int i = 0; i < itemsCount; i++)
        {
            //берем рандомную вещь из списка
            int randItemNum = rand.RandiRange(0, itemCodes.Count - 1);
            string newItemCode = itemCodes[randItemNum];
            Dictionary itemData = ItemJSON.GetItemData(newItemCode);

            //если это патроны, ложим в список патронов
            if (itemData["type"].ToString() == "ammo")
            {
                if (chestAmmo.ContainsKey(newItemCode)) return;

                int count = ammoCount[newItemCode];
                chestAmmo.Add(newItemCode, count);
            }
            else
            {
                chestItems.Add(newItemCode);
            }
        }
    }
}
