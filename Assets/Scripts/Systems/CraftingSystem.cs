using System.Collections.Generic;
using UnityEngine;

public class CraftingRecipe
{
    public string Id { get; set; }
    public string Name { get; set; }
    public List<IngredientRequirement> Ingredients { get; set; }
    public CraftingResult Result { get; set; }
    public AffinityRequirement RequiredLevel { get; set; }

    public CraftingRecipe(string id, string name)
    {
        Id = id;
        Name = name;
        Ingredients = new List<IngredientRequirement>();
    }
}

public class IngredientRequirement
{
    public string ItemId { get; set; }
    public int Quantity { get; set; }

    public IngredientRequirement(string itemId, int quantity)
    {
        ItemId = itemId;
        Quantity = quantity;
    }
}

public class CraftingResult
{
    public string ItemId { get; set; }
    public int Quantity { get; set; }

    public CraftingResult(string itemId, int quantity)
    {
        ItemId = itemId;
        Quantity = quantity;
    }
}

public class AffinityRequirement
{
    public string Type { get; set; }
    public int Level { get; set; }

    public AffinityRequirement(string type, int level)
    {
        Type = type;
        Level = level;
    }
}

public class CraftingSystem : MonoBehaviour
{
    private List<CraftingRecipe> recipes = new List<CraftingRecipe>();

    void Awake()
    {
        InitializeRecipes();
    }

    void InitializeRecipes()
    {
        // Wood Sword recipe
        CraftingRecipe woodSword = new CraftingRecipe("wood_sword", "Wood Sword");
        woodSword.Ingredients.Add(new IngredientRequirement("wood", 3));
        woodSword.Result = new CraftingResult("wood_sword", 1);
        woodSword.RequiredLevel = new AffinityRequirement("craft", 10);
        recipes.Add(woodSword);

        // Iron Sword recipe
        CraftingRecipe ironSword = new CraftingRecipe("iron_sword", "Iron Sword");
        ironSword.Ingredients.Add(new IngredientRequirement("iron_ore", 2));
        ironSword.Ingredients.Add(new IngredientRequirement("wood", 1));
        ironSword.Result = new CraftingResult("iron_sword", 1);
        ironSword.RequiredLevel = new AffinityRequirement("craft", 30);
        recipes.Add(ironSword);

        // Fire Staff recipe
        CraftingRecipe fireStaff = new CraftingRecipe("fire_staff", "Fire Staff");
        fireStaff.Ingredients.Add(new IngredientRequirement("wood", 4));
        fireStaff.Ingredients.Add(new IngredientRequirement("iron_ore", 1));
        fireStaff.Result = new CraftingResult("fire_staff", 1);
        fireStaff.RequiredLevel = new AffinityRequirement("fire", 20);
        recipes.Add(fireStaff);
    }

    public bool CanCraft(string recipeId, InventorySystem inventory, Dictionary<string, int> affinityLevels)
    {
        CraftingRecipe recipe = recipes.Find(r => r.Id == recipeId);
        if (recipe == null) return false;

        // Check required level
        if (recipe.RequiredLevel != null)
        {
            int currentLevel = affinityLevels.ContainsKey(recipe.RequiredLevel.Type)
                ? affinityLevels[recipe.RequiredLevel.Type]
                : 0;
            if (currentLevel < recipe.RequiredLevel.Level) return false;
        }

        // Check ingredients
        return recipe.Ingredients.TrueForAll(ing => inventory.HasItem(ing.ItemId, ing.Quantity));
    }

    public bool Craft(string recipeId, InventorySystem inventory)
    {
        CraftingRecipe recipe = recipes.Find(r => r.Id == recipeId);
        if (recipe == null) return false;

        // Remove ingredients
        bool allRemoved = recipe.Ingredients.TrueForAll(ing => inventory.RemoveItem(ing.ItemId, ing.Quantity));
        if (!allRemoved) return false;

        // Add result
        var itemInfo = GetItemInfo(recipe.Result.ItemId);
        if (itemInfo != null)
        {
            inventory.AddItem(
                recipe.Result.ItemId,
                itemInfo.Value.name,
                itemInfo.Value.spriteKey,
                recipe.Result.Quantity
            );
        }

        return true;
    }

    public List<CraftingRecipe> GetAvailableRecipes(Dictionary<string, int> affinityLevels)
    {
        return recipes.FindAll(recipe => {
            if (recipe.RequiredLevel == null) return true;
            int currentLevel = affinityLevels.ContainsKey(recipe.RequiredLevel.Type)
                ? affinityLevels[recipe.RequiredLevel.Type]
                : 0;
            return currentLevel >= recipe.RequiredLevel.Level;
        });
    }

    private (string name, string spriteKey)? GetItemInfo(string itemId)
    {
        var itemMap = new Dictionary<string, (string name, string spriteKey)>
        {
            { "wood", ("Wood", "item_wood") },
            { "iron_ore", ("Iron Ore", "item_iron_ore") },
            { "wood_sword", ("Wood Sword", "item_wood_sword") },
            { "iron_sword", ("Iron Sword", "item_iron_sword") },
            { "fire_staff", ("Fire Staff", "item_fire_staff") }
        };
        return itemMap.ContainsKey(itemId) ? itemMap[itemId] : null;
    }
}
