using UnityEngine;
using UnityEditor;
using System.IO;

/// <summary>
/// Editor tool that creates all ProductData ScriptableObject assets.
/// Run from menu: VR Supermarket → Step 1: Create Product Assets
///
/// After running, open Assets/ScriptableObjects/Products/ to see 22 products.
/// The SupermarketSceneBuilder (Step 2) will automatically assign them to shelves.
/// </summary>
public static class ProductAssetCreator
{
    private const string ROOT = "Assets/ScriptableObjects/Products";

    [MenuItem("VR Supermarket/Step 1 - Create Product Assets")]
    public static void CreateAll()
    {
        EnsureFolder("Assets/ScriptableObjects");
        EnsureFolder(ROOT);

        int n = 0;

        // ── Fruits (5) ────────────────────────────────────────────────────────
        n += Make("Apple",    "Fresh red apple",           ProductCategory.Fruits,     40f,  new Color(0.90f, 0.10f, 0.10f));
        n += Make("Banana",   "Sweet yellow banana",       ProductCategory.Fruits,     25f,  new Color(0.97f, 0.88f, 0.08f));
        n += Make("Orange",   "Juicy navel orange",        ProductCategory.Fruits,     35f,  new Color(0.95f, 0.55f, 0.05f));
        n += Make("Grapes",   "Fresh green grapes",        ProductCategory.Fruits,     80f,  new Color(0.40f, 0.82f, 0.25f));
        n += Make("Mango",    "Alphonso mango",            ProductCategory.Fruits,     60f,  new Color(1.00f, 0.73f, 0.00f));

        // ── Vegetables (5) ────────────────────────────────────────────────────
        n += Make("Carrot",   "Fresh orange carrot",       ProductCategory.Vegetables, 20f,  new Color(0.96f, 0.49f, 0.04f));
        n += Make("Broccoli", "Nutritious broccoli head",  ProductCategory.Vegetables, 45f,  new Color(0.14f, 0.63f, 0.14f));
        n += Make("Tomato",   "Ripe red tomato",           ProductCategory.Vegetables, 30f,  new Color(0.88f, 0.15f, 0.08f));
        n += Make("Spinach",  "Baby spinach leaves",       ProductCategory.Vegetables, 35f,  new Color(0.08f, 0.48f, 0.12f));
        n += Make("Potato",   "Fresh farm potatoes",       ProductCategory.Vegetables, 15f,  new Color(0.70f, 0.60f, 0.33f));

        // ── Dairy (4) ─────────────────────────────────────────────────────────
        n += Make("MilkBottle",  "Full cream milk 1L",    ProductCategory.Dairy,      65f,  new Color(0.92f, 0.93f, 1.00f));
        n += Make("YogurtCup",   "Natural curd 400g",     ProductCategory.Dairy,      40f,  new Color(1.00f, 0.96f, 0.88f));
        n += Make("CheeseBlock", "Processed cheese 200g", ProductCategory.Dairy,     120f,  new Color(1.00f, 0.86f, 0.25f));
        n += Make("Butter",      "Salted butter 100g",    ProductCategory.Dairy,      55f,  new Color(0.97f, 0.84f, 0.36f));

        // ── Snacks (4) ────────────────────────────────────────────────────────
        n += Make("ChipsBag",    "Crispy potato chips",   ProductCategory.Snacks,     20f,  new Color(0.96f, 0.79f, 0.08f));
        n += Make("CookieBox",   "Chocolate chip cookies",ProductCategory.Snacks,     50f,  new Color(0.53f, 0.33f, 0.12f));
        n += Make("NoodlePack",  "Instant noodles",       ProductCategory.Snacks,     15f,  new Color(0.97f, 0.62f, 0.04f));
        n += Make("BiscuitPack", "Cream biscuits",        ProductCategory.Snacks,     30f,  new Color(0.84f, 0.73f, 0.53f));

        // ── Beverages (4) ─────────────────────────────────────────────────────
        n += Make("JuiceBottle", "Orange juice 1L",       ProductCategory.Beverages,  90f,  new Color(0.97f, 0.57f, 0.04f));
        n += Make("SodaCan",     "Cola soda 330ml",       ProductCategory.Beverages,  40f,  new Color(0.18f, 0.10f, 0.04f));
        n += Make("WaterBottle", "Mineral water 1L",      ProductCategory.Beverages,  20f,  new Color(0.55f, 0.85f, 1.00f));
        n += Make("TeaPack",     "Masala chai tea 250g",  ProductCategory.Beverages,  55f,  new Color(0.38f, 0.18f, 0.04f));

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[ProductAssetCreator] Done — {n} product assets saved to {ROOT}");
        EditorUtility.DisplayDialog("Step 1 Complete!",
            $"Created {n} ProductData assets in:\n{ROOT}\n\n" +
            "Now run:  VR Supermarket → Step 2 - Build Supermarket Scene", "OK");
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private static int Make(string name, string desc, ProductCategory cat, float price, Color color)
    {
        string path = $"{ROOT}/SO_{name}.asset";

        // Don't overwrite existing assets
        if (AssetDatabase.LoadAssetAtPath<ProductData>(path) != null)
        {
            Debug.Log($"[ProductAssetCreator] Skipped (already exists): {path}");
            return 0;
        }

        var so = ScriptableObject.CreateInstance<ProductData>();
        so.productName  = name;
        so.description  = desc;
        so.category     = cat;
        so.price        = price;
        so.productColor = color;

        AssetDatabase.CreateAsset(so, path);
        return 1;
    }

    private static void EnsureFolder(string path)
    {
        if (!AssetDatabase.IsValidFolder(path))
        {
            var parts = path.Split('/');
            string parent = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string child  = parts[i];
                string full   = parent + "/" + child;
                if (!AssetDatabase.IsValidFolder(full))
                    AssetDatabase.CreateFolder(parent, child);
                parent = full;
            }
        }
    }
}
