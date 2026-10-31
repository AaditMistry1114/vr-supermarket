using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// ═══════════════════════════════════════════════════════════════════════════
///   VR SUPERMARKET — Scene Builder  (Editor Only)
///   Menu: VR Supermarket → Step 2 - Build Supermarket Scene
/// ═══════════════════════════════════════════════════════════════════════════
///
/// This script programmatically builds the entire SupermarketScene:
///   • 24×32m store room (floor, walls, ceiling)
///   • 5 category shelf rows, each with 3 planks × 5 product slots
///   • 22 products loaded from ScriptableObjects (run Step 1 first)
///   • FPS player (CharacterController + Camera)
///   • Checkout counter with CheckoutTrigger
///   • All UI: Welcome screen, HUD, Cart panel, Checkout panel
///   • Directional + 6 point ceiling lights
///   • All Manager scripts wired up automatically
///
/// REQUIREMENTS:
///   1. Run "Step 1 - Create Product Assets" first so ScriptableObjects exist.
///   2. Go to Edit → Project Settings → Player → Active Input Handling → Both
/// ═══════════════════════════════════════════════════════════════════════════
/// </summary>
public static class SupermarketSceneBuilder
{
    // ─── Constants ────────────────────────────────────────────────────────────
    private const string SCENE_PATH   = "Assets/Scenes/SupermarketScene.unity";
    private const string PRODUCT_ROOT = "Assets/ScriptableObjects/Products";

    // Room dimensions (metres)
    private const float W  = 24f;   // Width  (X)
    private const float D  = 32f;   // Depth  (Z)
    private const float H  = 4.5f;  // Height (Y)
    private const float WT = 0.25f; // Wall thickness

    // Shelf row definitions: (label, category, Z position, sign colour)
    private static readonly (string label, ProductCategory cat, float z, Color signColor)[] ShelfDefs =
    {
        ("Fruits",     ProductCategory.Fruits,     -11f, new Color(1.0f, 0.45f, 0.20f)),
        ("Vegetables", ProductCategory.Vegetables,  -5f, new Color(0.20f, 0.72f, 0.20f)),
        ("Dairy",      ProductCategory.Dairy,        1f, new Color(0.60f, 0.80f, 1.00f)),
        ("Snacks",     ProductCategory.Snacks,       7f, new Color(1.0f, 0.82f, 0.10f)),
        ("Beverages",  ProductCategory.Beverages,   13f, new Color(0.25f, 0.55f, 1.00f)),
    };

    // ─── Entry Point ──────────────────────────────────────────────────────────

    [MenuItem("VR Supermarket/Step 2 - Build Supermarket Scene")]
    public static void Build()
    {
        // Confirm
        if (!EditorUtility.DisplayDialog("Build Scene?",
            "This will CREATE a new SupermarketScene.unity.\n\n" +
            "Make sure you ran Step 1 (Create Product Assets) first!\n\n" +
            "Proceed?", "Yes, Build It", "Cancel"))
            return;

        // Load all products
        var allProducts = LoadAllProducts();
        if (allProducts.Count == 0)
        {
            EditorUtility.DisplayDialog("No Products Found",
                "Run  VR Supermarket → Step 1  first to create product assets!", "OK");
            return;
        }

        // New empty scene
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // ── Build everything ──────────────────────────────────────────────────
        BuildRoom();
        BuildLighting();
        BuildShelves(allProducts);
        BuildCheckoutCounter();
        var player = BuildPlayer();
        var managers = BuildManagers();
        BuildUI(player, managers);

        // ── Save ──────────────────────────────────────────────────────────────
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, SCENE_PATH);
        AssetDatabase.Refresh();

        Debug.Log($"[SceneBuilder] Scene saved → {SCENE_PATH}");
        EditorUtility.DisplayDialog("Scene Built!",
            $"SupermarketScene saved to:\n{SCENE_PATH}\n\n" +
            "IMPORTANT: Go to Edit → Project Settings → Player\n" +
            "→ Active Input Handling → set to  Both\n\n" +
            "Then press Play to start!", "Open Scene");

        EditorSceneManager.OpenScene(SCENE_PATH);
    }

    // ═════════════════════════════════════════════════════════════════════════
    //  ROOM
    // ═════════════════════════════════════════════════════════════════════════

    private static void BuildRoom()
    {
       var root = Folder("[Environment]").transform;

        // Floor
        var floor = Box("Floor", root, new Vector3(0, -0.05f, 0), new Vector3(W, 0.1f, D));
        ApplyColor(floor, new Color(0.80f, 0.75f, 0.70f));  // Cream tile
        SetStatic(floor);

        // Ceiling
        var ceil = Box("Ceiling", root, new Vector3(0, H, 0), new Vector3(W, 0.2f, D));
        ApplyColor(ceil, new Color(0.93f, 0.93f, 0.95f));
        SetStatic(ceil);

        // Walls
        float hw = W / 2f, hd = D / 2f, hh = H / 2f;
        Color wallColor = new Color(0.92f, 0.90f, 0.88f);

        var walls = new (string n, Vector3 pos, Vector3 sz)[]
        {
            ("Wall_Front",  new Vector3(0,  hh, -hd),  new Vector3(W,  H, WT)),
            ("Wall_Back",   new Vector3(0,  hh,  hd),  new Vector3(W,  H, WT)),
            ("Wall_Left",   new Vector3(-hw, hh, 0),   new Vector3(WT, H, D)),
            ("Wall_Right",  new Vector3( hw, hh, 0),   new Vector3(WT, H, D)),
        };

        foreach (var w in walls)
        {
            var go = Box(w.n, root, w.pos, w.sz);
            ApplyColor(go, wallColor);
            SetStatic(go);
        }

        // Entrance sign strip on front wall
        var sign = Box("EntranceSign", root,
            new Vector3(0, H - 0.4f, -D / 2f + 0.16f),
            new Vector3(8f, 0.8f, 0.05f));
        ApplyColor(sign, new Color(0.10f, 0.45f, 0.30f));

        // Floor aisle lines (decorative strips)
        for (int i = 0; i < 5; i++)
        {
            float z = -12f + i * 6f;
            var line = Box($"AisleLine_{i}", root,
                new Vector3(0, 0.01f, z + 3f),
                new Vector3(W - 0.5f, 0.01f, 0.08f));
            ApplyColor(line, new Color(0.65f, 0.60f, 0.55f));
            SetStatic(line);
        }
    }

    // ═════════════════════════════════════════════════════════════════════════
    //  LIGHTING
    // ═════════════════════════════════════════════════════════════════════════

    private static void BuildLighting()
    {
        var root = Folder("[Lighting]");

        // Soft directional fill light
        var dir = new GameObject("DirectionalLight");
        dir.transform.SetParent(root.transform);
        dir.transform.rotation = Quaternion.Euler(55f, -30f, 0f);
        var dl = dir.AddComponent<Light>();
        dl.type      = LightType.Directional;
        dl.intensity = 0.45f;
        dl.color     = new Color(1f, 0.97f, 0.90f);
        dl.shadows   = LightShadows.Soft;

        // Ceiling fluorescent point lights (2 rows × 3 = 6 lights)
        int idx = 0;
        foreach (float z in new[] { -9f, 0f, 10f })
        {
            foreach (float x in new[] { -7f, 7f })
            {
                var lo = new GameObject($"CeilingLight_{++idx}");
                lo.transform.SetParent(root.transform);
                lo.transform.position = new Vector3(x, H - 0.3f, z);
                var l = lo.AddComponent<Light>();
                l.type      = LightType.Point;
                l.range     = 14f;
                l.intensity = 2.2f;
                l.color     = new Color(1f, 0.96f, 0.85f);
                l.shadows   = LightShadows.None;    // No shadows → better performance

                // Visual fixture (small flat box)
                var fixture = Box($"Fixture_{idx}", lo.transform.parent,
                    new Vector3(x, H - 0.05f, z), new Vector3(1.2f, 0.08f, 0.4f));
                ApplyColor(fixture, Color.white);
                SetStatic(fixture);
            }
        }

        // Ambient light settings
        RenderSettings.ambientMode      = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight     = new Color(0.38f, 0.37f, 0.40f);
        RenderSettings.ambientIntensity = 1f;
    }

    // ═════════════════════════════════════════════════════════════════════════
    //  SHELVES + PRODUCTS
    // ═════════════════════════════════════════════════════════════════════════

    private static void BuildShelves(List<ProductData> allProducts)
    {
        var root = Folder("[Shelves]");

        foreach (var def in ShelfDefs)
        {
            var products = new List<ProductData>();
            foreach (var p in allProducts)
                if (p.category == def.cat) products.Add(p);

            BuildShelfRow(def.label, def.cat, def.z, def.signColor, products, root.transform);
        }
    }

    private static void BuildShelfRow(string label, ProductCategory cat, float zPos,
        Color signColor, List<ProductData> products, Transform parent)
    {
        var row = new GameObject($"Shelf_{label}");
        row.transform.SetParent(parent);
        row.transform.position = new Vector3(0f, 0f, zPos);

        const float SW = 20f;     // Shelf total width
        const float SD = 0.85f;   // Shelf depth
        const float SH = 2.1f;    // Shelf total height
        const int   PLANKS = 3;   // Number of shelves (horizontal planks)
        const int   SLOTS  = 5;   // Products per plank

        Color woodColor = new Color(0.58f, 0.44f, 0.30f);
        Color darkWood  = new Color(0.45f, 0.33f, 0.21f);

        // Back panel
        var back = Box("Back", row.transform,
            new Vector3(0, SH / 2f, -SD / 2f), new Vector3(SW, SH, 0.08f));
        ApplyColor(back, darkWood);
        SetStatic(back);

        // Side supports
        foreach (float sx in new[] { -(SW / 2f), SW / 2f })
        {
            var side = Box("Side", row.transform,
                new Vector3(sx, SH / 2f, 0), new Vector3(0.07f, SH, SD));
            ApplyColor(side, darkWood);
            SetStatic(side);
        }

        // Horizontal planks + product slots
        var slots = new List<Transform>();
        float plankSpacing = (SH - 0.4f) / PLANKS;

        for (int p = 0; p < PLANKS; p++)
        {
            float py = 0.45f + p * plankSpacing;

            // Plank
            var plank = Box($"Plank_{p}", row.transform,
                new Vector3(0, py, 0), new Vector3(SW, 0.06f, SD));
            ApplyColor(plank, woodColor);
            SetStatic(plank);

            // Slot transforms on each plank
            float slotSpacing = (SW - 4f) / (SLOTS - 1);
            for (int s = 0; s < SLOTS; s++)
            {
                float sx = -(SW / 2f - 2f) + s * slotSpacing;
                var slot = new GameObject($"Slot_P{p}_S{s}");
                slot.transform.SetParent(row.transform);
                slot.transform.localPosition = new Vector3(sx, py + 0.20f, -SD / 4f);
                slots.Add(slot.transform);
            }
        }

        // Category sign above shelf
        var signBoard = Box("Sign", row.transform,
            new Vector3(0, SH + 0.30f, -SD / 2f - 0.05f),
            new Vector3(5f, 0.55f, 0.07f));
        ApplyColor(signBoard, signColor);

        var signLabel = new GameObject("SignText");
        signLabel.transform.SetParent(signBoard.transform);
        signLabel.transform.localPosition = new Vector3(0f, 0f, -0.55f);
        signLabel.transform.localScale    = Vector3.one * 0.025f;
        var tmp = signLabel.AddComponent<TextMeshPro>();   // 3D text (not UI)
        tmp.text      = label.ToUpper();
        tmp.fontSize  = 12;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color     = Color.white;

        // ShelfPopulator
        var pop = row.AddComponent<ShelfPopulator>();
        pop.productScale = new Vector3(0.24f, 0.24f, 0.24f);
        foreach (var s in slots) pop.slots.Add(s);

        // Cycle products across all slots
        for (int i = 0; i < slots.Count; i++)
        {
            if (products.Count > 0)
                pop.products.Add(products[i % products.Count]);
        }

        // Price tag strip in front of each plank (decorative)
        for (int p = 0; p < PLANKS; p++)
        {
            float py = 0.45f + p * plankSpacing;
            var tag = Box($"PriceTag_{p}", row.transform,
                new Vector3(0, py + 0.04f, SD / 2f + 0.01f),
                new Vector3(SW, 0.06f, 0.02f));
            ApplyColor(tag, new Color(1f, 1f, 0.85f));
            SetStatic(tag);
        }
    }

    // ═════════════════════════════════════════════════════════════════════════
    //  CHECKOUT COUNTER
    // ═════════════════════════════════════════════════════════════════════════

    private static void BuildCheckoutCounter()
    {
        var root = new GameObject("CheckoutCounter");
        root.transform.position = new Vector3(0f, 0f, 14.5f);

        Color teal  = new Color(0.14f, 0.48f, 0.48f);
        Color dteal = new Color(0.10f, 0.36f, 0.36f);

        // Counter body
        var body = Box("Body", root.transform,
            new Vector3(0, 0.55f, 0), new Vector3(5.5f, 1.1f, 1.6f));
        ApplyColor(body, teal);
        SetStatic(body);

        // Counter top
        var top = Box("Top", root.transform,
            new Vector3(0, 1.12f, 0), new Vector3(5.7f, 0.1f, 1.8f));
        ApplyColor(top, dteal);
        SetStatic(top);

        // Register box (decorative)
        var reg = Box("Register", root.transform,
            new Vector3(1.8f, 1.3f, -0.3f), new Vector3(0.8f, 0.4f, 0.5f));
        ApplyColor(reg, new Color(0.15f, 0.15f, 0.18f));
        SetStatic(reg);

        // Screen (decorative)
        var screen = Box("Screen", root.transform,
            new Vector3(1.8f, 1.6f, -0.3f), new Vector3(0.7f, 0.4f, 0.05f));
        ApplyColor(screen, new Color(0.05f, 0.65f, 0.45f));
        SetStatic(screen);

        // "CHECKOUT" sign above counter
        var signObj = Box("CheckoutSign", root.transform,
            new Vector3(0f, 3.2f, -0.6f), new Vector3(4f, 0.7f, 0.1f));
        ApplyColor(signObj, new Color(0.12f, 0.45f, 0.30f));

        // Sign text
        var signTxt = new GameObject("CheckoutSignText");
        signTxt.transform.SetParent(signObj.transform);
        signTxt.transform.localPosition = new Vector3(0, 0, -1.2f);
        signTxt.transform.localScale    = Vector3.one * 0.03f;
        var t = signTxt.AddComponent<TextMeshPro>();
        t.text      = "CHECKOUT";
        t.fontSize  = 12;
        t.alignment = TextAlignmentOptions.Center;
        t.fontStyle = FontStyles.Bold;
        t.color     = Color.white;

        // CheckoutTrigger collider (invisible, player looks at it)
        var triggerGO = new GameObject("CheckoutInteractable");
        triggerGO.transform.SetParent(root.transform);
        triggerGO.transform.localPosition = new Vector3(0, 1f, -0.6f);
        var bc = triggerGO.AddComponent<BoxCollider>();
        bc.size = new Vector3(5f, 1f, 1.5f);
        triggerGO.AddComponent<CheckoutTrigger>();

        // Barrier rope posts
        foreach (float px in new[] { -2.8f, 2.8f })
        {
            var post = Box($"Post_{px}", root.transform,
                new Vector3(px, 0.6f, -0.9f), new Vector3(0.1f, 1.2f, 0.1f));
            ApplyColor(post, new Color(0.75f, 0.65f, 0.20f));
            SetStatic(post);
        }
    }

    // ═════════════════════════════════════════════════════════════════════════
    //  PLAYER
    // ═════════════════════════════════════════════════════════════════════════

    private static GameObject BuildPlayer()
    {
        var player = new GameObject("FPSPlayer");
        player.tag = "Player";
        player.transform.position = new Vector3(0f, 0.95f, -14f);  // Near entrance

        // CharacterController
        var cc = player.AddComponent<CharacterController>();
        cc.height = 1.9f;
        cc.radius = 0.4f;
        cc.center = new Vector3(0, 0.95f, 0);
        cc.skinWidth = 0.04f;

        // Camera (eye level)
        var camGO = new GameObject("MainCamera");
        camGO.tag = "MainCamera";
        camGO.transform.SetParent(player.transform);
        camGO.transform.localPosition = new Vector3(0, 0.75f, 0);
        var cam = camGO.AddComponent<Camera>();
        cam.fieldOfView    = 75f;
        cam.nearClipPlane  = 0.08f;
        cam.farClipPlane   = 200f;
        cam.backgroundColor = new Color(0.52f, 0.80f, 0.92f);
        camGO.AddComponent<AudioListener>();

        // Player scripts
        var ctrl = player.AddComponent<PlayerController>();
        ctrl.cameraTransform  = camGO.transform;
        ctrl.walkSpeed        = 4f;
        ctrl.sprintSpeed      = 7f;
        ctrl.mouseSensitivity = 2f;

        var interact = player.AddComponent<PlayerInteraction>();
        interact.cameraTransform  = camGO.transform;
        interact.interactionRange = 3.5f;

        // VR stub (inactive by default)
        var vr = player.AddComponent<VRPlayerAdapter>();
        vr.enableVRMode = false;

        return player;
    }

    // ═════════════════════════════════════════════════════════════════════════
    //  MANAGERS
    // ═════════════════════════════════════════════════════════════════════════

    private static (StoreManager store, CheckoutManager checkout) BuildManagers()
    {
        var root = Folder("[Managers]");

        // StoreManager
        var sm = root.AddComponent<StoreManager>();

        // ShoppingCart
        var cartGO = Child(root, "ShoppingCart");
        var sc = cartGO.AddComponent<ShoppingCart>();
        sc.taxPercent = 18f;

        // UIAudioManager
        var audioGO = Child(root, "UIAudioManager");
        audioGO.AddComponent<UIAudioManager>();

        // CheckoutManager
        var cmGO = Child(root, "CheckoutManager");
        var cm = cmGO.AddComponent<CheckoutManager>();

        // EventSystem (required for UI button clicks)
        var evGO = Child(root, "EventSystem");
        evGO.AddComponent<EventSystem>();
        evGO.AddComponent<StandaloneInputModule>();

        return (sm, cm);
    }

    // ═════════════════════════════════════════════════════════════════════════
    //  UI
    // ═════════════════════════════════════════════════════════════════════════

    private static void BuildUI(GameObject player,
        (StoreManager store, CheckoutManager checkout) mgrs)
    {
        // ── Welcome Screen ─────────────────────────────────────────────────────
        var welcomeCanvas = MakeCanvas("Canvas_Welcome", 5);
        var welcomeRoot   = BuildWelcomeScreen(welcomeCanvas.transform);

        // ── HUD ────────────────────────────────────────────────────────────────
        var hudCanvas = MakeCanvas("Canvas_HUD", 10);
        var (crosshairImg, promptPanel, promptTxt, badgeRoot, badgeTxt)
            = BuildHUD(hudCanvas.transform);

        // ── Cart Panel ─────────────────────────────────────────────────────────
        var cartCanvas = MakeCanvas("Canvas_Cart", 20);
        cartCanvas.gameObject.SetActive(false);
        var (cartUI, checkoutBtn) = BuildCartPanel(cartCanvas.transform);

        // ── Checkout Panel ─────────────────────────────────────────────────────
        var checkoutCanvas = MakeCanvas("Canvas_Checkout", 30);
        checkoutCanvas.gameObject.SetActive(false);
        var checkoutUI = BuildCheckoutPanel(checkoutCanvas.transform);

        // ── Wire up Manager references ─────────────────────────────────────────
        mgrs.store.welcomeScreenRoot  = welcomeCanvas.gameObject;
        mgrs.store.cartPanelRoot      = cartCanvas.gameObject;
        mgrs.store.checkoutPanelRoot  = checkoutCanvas.gameObject;
        mgrs.store.playerController   = player.GetComponent<PlayerController>();

        mgrs.checkout.checkoutUI = checkoutUI;

        // ── HUDController ──────────────────────────────────────────────────────
        var hud = hudCanvas.gameObject.AddComponent<HUDController>();
        hud.crosshairImage = crosshairImg;
        hud.promptPanel    = promptPanel;
        hud.promptText     = promptTxt;
        hud.cartBadgeRoot  = badgeRoot;
        hud.cartBadgeText  = badgeTxt;
    }

    // ─── Welcome Screen ────────────────────────────────────────────────────────

    private static GameObject BuildWelcomeScreen(Transform parent)
    {
        // Full-screen dark gradient panel
        var bg = UIPanel("Welcome_BG", parent,
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero,
            new Color(0.04f, 0.10f, 0.08f, 0.97f));

        // Decorative top bar
        var topBar = UIPanel("TopBar", bg.transform,
            new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -80), Vector2.zero,
            new Color(0.08f, 0.42f, 0.28f, 1f));
        UIText(topBar.transform, "🛒  VR SUPERMARKET  —  DESKTOP EDITION", 18f,
            TextAlignmentOptions.Center, new Color(0.9f, 1f, 0.9f), true);

        // Title
        var title = UIText(bg.transform, "WELCOME TO\nVR SUPERMARKET", 48f,
            TextAlignmentOptions.Center, new Color(0.30f, 0.95f, 0.62f), true);
        PlaceRect(title.GetComponent<RectTransform>(),
            new Vector2(0.1f, 0.6f), new Vector2(0.9f, 0.9f));

        // Subtitle
        var sub = UIText(bg.transform, "A 3D Desktop Supermarket Simulation", 20f,
            TextAlignmentOptions.Center, new Color(0.7f, 0.9f, 0.8f), false);
        PlaceRect(sub.GetComponent<RectTransform>(),
            new Vector2(0.15f, 0.53f), new Vector2(0.85f, 0.62f));

        // Divider line
        var divLine = UIPanel("Divider", bg.transform,
            new Vector2(0.1f, 0.52f), new Vector2(0.9f, 0.52f),
            new Vector2(0, -1), new Vector2(0, 1),
            new Color(0.3f, 0.9f, 0.6f, 0.5f));

        // Controls guide panel
        var guidePanel = UIPanel("GuidePanel", bg.transform,
            new Vector2(0.1f, 0.24f), new Vector2(0.9f, 0.52f),
            Vector2.zero, Vector2.zero,
            new Color(0.06f, 0.14f, 0.12f, 0.85f));

        string guide =
            "HOW TO PLAY\n\n" +
            "W / A / S / D          Move\n" +
            "Mouse                      Look around\n" +
            "Left Shift                  Sprint\n" +
            "E                              Pick up product\n" +
            "Tab                          Open / close cart\n" +
            "Escape                    Close any panel\n" +
            "Walk to Checkout → Press E to pay";

        UIText(guidePanel.transform, guide, 16f,
            TextAlignmentOptions.Left, new Color(0.85f, 0.95f, 0.90f), false);

        // START button
        var startBtn = MakeButton(bg.transform, "▶  START SHOPPING",
            new Color(0.12f, 0.68f, 0.42f), 22f);
        PlaceRect(startBtn.GetComponent<RectTransform>(),
            new Vector2(0.25f, 0.10f), new Vector2(0.75f, 0.22f));
        startBtn.onClick.AddListener(() =>
            Object.FindFirstObjectByType<StoreManager>()?.StartGame());

        // QUIT button
        var quitBtn = MakeButton(bg.transform, "✕  Quit Game",
            new Color(0.45f, 0.12f, 0.12f), 16f);
        PlaceRect(quitBtn.GetComponent<RectTransform>(),
            new Vector2(0.38f, 0.03f), new Vector2(0.62f, 0.10f));
        quitBtn.onClick.AddListener(() =>
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        });

        // WelcomeScreenUI component
        var wsUI = bg.AddComponent<WelcomeScreenUI>();
        wsUI.startButton = startBtn;
        wsUI.quitButton  = quitBtn;

        return bg;
    }

    // ─── HUD ──────────────────────────────────────────────────────────────────

    private static (Image crosshair, GameObject promptPanel, TextMeshProUGUI promptTxt,
        GameObject badgeRoot, TextMeshProUGUI badgeTxt)
        BuildHUD(Transform parent)
    {
        // Crosshair (dot at screen centre)
        var chGO = new GameObject("Crosshair");
        chGO.transform.SetParent(parent, false);
        var chImg = chGO.AddComponent<Image>();
        chImg.color = new Color(1f, 1f, 1f, 0.85f);
        var chRect = chGO.GetComponent<RectTransform>();
        chRect.anchorMin = chRect.anchorMax = new Vector2(0.5f, 0.5f);
        chRect.sizeDelta = new Vector2(8f, 8f);
        chRect.anchoredPosition = Vector2.zero;

        // Interaction prompt (bottom centre)
        var promptPanel = UIPanel("PromptPanel", parent,
            new Vector2(0.15f, 0.04f), new Vector2(0.85f, 0.12f),
            Vector2.zero, Vector2.zero,
            new Color(0f, 0f, 0f, 0.60f));
        promptPanel.SetActive(false);

        var promptTxt = UIText(promptPanel.transform, "", 18f,
            TextAlignmentOptions.Center, Color.white, false);
        PlaceRect(promptTxt.GetComponent<RectTransform>(), Vector2.zero, Vector2.one);

        // Cart badge (top right)
        var badge = UIPanel("CartBadge", parent,
            new Vector2(0.78f, 0.90f), new Vector2(0.98f, 0.98f),
            Vector2.zero, Vector2.zero,
            new Color(0.10f, 0.65f, 0.38f, 0.92f));
        badge.SetActive(false);

        var badgeTxt = UIText(badge.transform, "0", 15f,
            TextAlignmentOptions.Center, Color.white, false);
        PlaceRect(badgeTxt.GetComponent<RectTransform>(), Vector2.zero, Vector2.one);

        // Fixed controls reminder (bottom right)
        var hint = UIText(parent, "[Tab] Cart   [E] Interact   [Shift] Sprint", 11f,
            TextAlignmentOptions.Right, new Color(1f, 1f, 1f, 0.45f), false);
        PlaceRect(hint.GetComponent<RectTransform>(),
            new Vector2(0.3f, 0f), new Vector2(0.99f, 0.06f));

        return (chImg, promptPanel, promptTxt, badge.gameObject, badgeTxt);
    }

    // ─── Cart Panel ───────────────────────────────────────────────────────────

    private static (CartUI cartUI, Button checkoutBtn) BuildCartPanel(Transform canvasParent)
    {
        // Semi-transparent overlay
        UIPanel("Overlay", canvasParent,
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero,
            new Color(0f, 0f, 0f, 0.55f));

        // Side panel (right 35%)
        var panel = UIPanel("CartPanel", canvasParent,
            new Vector2(0.65f, 0f), Vector2.one, Vector2.zero, Vector2.zero,
            new Color(0.06f, 0.09f, 0.14f, 0.97f));

        // Header
        var header = UIPanel("Header", panel.transform,
            new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -70), Vector2.zero,
            new Color(0.08f, 0.38f, 0.25f));
        UIText(header.transform, "🛒  SHOPPING CART", 20f,
            TextAlignmentOptions.Center, new Color(0.85f, 1f, 0.92f), true);

        // Close button (X)
        var closeBtn = MakeButton(header.transform, "✕", new Color(0.55f, 0.12f, 0.12f), 16f);
        PlaceRect(closeBtn.GetComponent<RectTransform>(),
            new Vector2(0.85f, 0.05f), new Vector2(0.98f, 0.95f));
        closeBtn.onClick.AddListener(() =>
            Object.FindFirstObjectByType<StoreManager>()?.ToggleCartPanel());

        // Scroll area for items
        var (scrollView, content) = BuildScrollView("ItemScroll", panel.transform,
            new Vector2(0, 0.22f), new Vector2(1, 0.85f), new Vector2(10, 0), new Vector2(-10, 0));

        // Empty cart message
        var emptyMsg = UIText(scrollView.transform, "Cart is empty.\nWalk to a product and\npress  [E]  to add it.", 15f,
            TextAlignmentOptions.Center, new Color(0.55f, 0.65f, 0.60f), false);
        FillParent(emptyMsg.GetComponent<RectTransform>());

        // Totals footer
        var totals = UIPanel("Totals", panel.transform,
            new Vector2(0, 0.08f), new Vector2(1, 0.22f), new Vector2(8, 0), new Vector2(-8, 0),
            new Color(0.04f, 0.07f, 0.11f));
        var tLayout = totals.AddComponent<VerticalLayoutGroup>();
        tLayout.padding = new RectOffset(10, 10, 6, 6);
        tLayout.spacing = 2;
        tLayout.childControlHeight = true;
        tLayout.childForceExpandHeight = true;

        var subtotalTxt  = UIText(totals.transform, "Subtotal   ₹0.00", 13f, TextAlignmentOptions.Left,  new Color(0.80f, 0.90f, 0.85f), false);
        var taxTxt       = UIText(totals.transform, "GST (18%)  ₹0.00", 12f, TextAlignmentOptions.Left,  new Color(0.65f, 0.78f, 0.72f), false);
        var grandTxt     = UIText(totals.transform, "TOTAL      ₹0.00", 16f, TextAlignmentOptions.Left,  new Color(0.30f, 0.95f, 0.62f), true);

        // Checkout button at bottom
        var checkoutBtn = MakeButton(panel.transform, "🏪  Proceed to Checkout",
            new Color(0.12f, 0.60f, 0.38f), 18f);
        PlaceRect(checkoutBtn.GetComponent<RectTransform>(),
            new Vector2(0.05f, 0.01f), new Vector2(0.95f, 0.08f));
        checkoutBtn.interactable = false;

        // CartUI component
        var cartUI = panel.AddComponent<CartUI>();
        cartUI.itemListContent  = content;
        cartUI.subtotalText     = subtotalTxt;
        cartUI.taxText          = taxTxt;
        cartUI.grandTotalText   = grandTxt;
        cartUI.emptyCartMessage = emptyMsg.gameObject;
        cartUI.checkoutButton   = checkoutBtn;

        return (cartUI, checkoutBtn);
    }

    // ─── Checkout Panel ───────────────────────────────────────────────────────

    private static CheckoutUI BuildCheckoutPanel(Transform canvasParent)
    {
        UIPanel("Overlay", canvasParent,
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero,
            new Color(0f, 0f, 0f, 0.72f));

        // Centre panel (60% wide)
        var panel = UIPanel("CheckoutPanel", canvasParent,
            new Vector2(0.20f, 0.04f), new Vector2(0.80f, 0.96f),
            Vector2.zero, Vector2.zero,
            new Color(0.06f, 0.09f, 0.14f, 0.98f));

        // Header bar
        var header = UIPanel("Header", panel.transform,
            new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -70), Vector2.zero,
            new Color(0.08f, 0.38f, 0.25f));
        UIText(header.transform, "🏪  VR SUPERMARKET  —  RECEIPT", 18f,
            TextAlignmentOptions.Center, new Color(0.85f, 1f, 0.92f), true);

        // Receipt title
        var receiptTitle = UIText(panel.transform, "━━━ ITEMIZED BILL ━━━", 14f,
            TextAlignmentOptions.Center, new Color(0.5f, 0.7f, 0.6f), false);
        PlaceRect(receiptTitle.GetComponent<RectTransform>(),
            new Vector2(0, 0.83f), new Vector2(1, 0.90f));

        // Receipt scroll
        var (scrollView, content) = BuildScrollView("ReceiptScroll", panel.transform,
            new Vector2(0, 0.36f), new Vector2(1, 0.83f), new Vector2(10, 0), new Vector2(-10, 0));

        // Totals section
        var totSection = UIPanel("Totals", panel.transform,
            new Vector2(0, 0.20f), new Vector2(1, 0.36f), new Vector2(10, 0), new Vector2(-10, 0),
            new Color(0.04f, 0.07f, 0.11f));
        var tl2 = totSection.AddComponent<VerticalLayoutGroup>();
        tl2.padding = new RectOffset(12, 12, 8, 8);
        tl2.spacing = 3;
        tl2.childControlHeight = true;
        tl2.childForceExpandHeight = true;

        UIText(totSection.transform, "──────────────────────────────────────", 11f, TextAlignmentOptions.Center, new Color(0.40f, 0.50f, 0.45f), false);
        var stTxt  = UIText(totSection.transform, "Subtotal:", 13f, TextAlignmentOptions.Left, new Color(0.80f, 0.90f, 0.85f), false);
        var txTxt  = UIText(totSection.transform, "GST:", 13f, TextAlignmentOptions.Left, new Color(0.65f, 0.78f, 0.72f), false);
        var gtTxt  = UIText(totSection.transform, "GRAND TOTAL:", 17f, TextAlignmentOptions.Left, new Color(0.30f, 0.95f, 0.62f), true);

        // Button row
        var btnRow = UIPanel("Buttons", panel.transform,
            new Vector2(0, 0.06f), new Vector2(1, 0.20f), new Vector2(16, 8), new Vector2(-16, -8),
            Color.clear);
        var btnLayout = btnRow.AddComponent<HorizontalLayoutGroup>();
        btnLayout.spacing = 14;
        btnLayout.childControlWidth = true;
        btnLayout.childForceExpandWidth = true;
        btnLayout.padding = new RectOffset(0, 0, 0, 0);

        var cancelBtn = MakeButton(btnRow.transform, "✕  Cancel",
            new Color(0.48f, 0.12f, 0.12f), 17f);
        var payBtn = MakeButton(btnRow.transform, "✔  Pay Now",
            new Color(0.12f, 0.62f, 0.38f), 17f);
        payBtn.GetComponent<LayoutElement>().preferredWidth = 999;

        // Thank-you overlay
        var tyPanel = UIPanel("ThankYou", panel.transform,
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero,
            new Color(0.04f, 0.20f, 0.12f, 0.97f));
        tyPanel.SetActive(false);

        UIText(tyPanel.transform,
            "✅\n\nPayment Successful!\n\nThank you for shopping at\nVR Supermarket.\n\nHave a great day!",
            26f, TextAlignmentOptions.Center, Color.white, false);

        // Restart button on thank-you screen
        var restartBtn = MakeButton(tyPanel.transform, "🔄  Shop Again",
            new Color(0.10f, 0.48f, 0.30f), 18f);
        PlaceRect(restartBtn.GetComponent<RectTransform>(),
            new Vector2(0.25f, 0.08f), new Vector2(0.75f, 0.18f));
        restartBtn.onClick.AddListener(() =>
            Object.FindFirstObjectByType<StoreManager>()?.RestartGame());

        // CheckoutUI component
        var cui = panel.AddComponent<CheckoutUI>();
        cui.receiptContent = content;
        cui.subtotalText   = stTxt;
        cui.taxText        = txTxt;
        cui.totalText      = gtTxt;
        cui.payButton      = payBtn;
        cui.cancelButton   = cancelBtn;
        cui.receiptView    = scrollView;
        cui.thankYouView   = tyPanel;

        return cui;
    }

    // ═════════════════════════════════════════════════════════════════════════
    //  PRODUCT LOADING
    // ═════════════════════════════════════════════════════════════════════════

    private static List<ProductData> LoadAllProducts()
    {
        var list  = new List<ProductData>();
        var guids = AssetDatabase.FindAssets("t:ProductData", new[] { PRODUCT_ROOT });
        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var p    = AssetDatabase.LoadAssetAtPath<ProductData>(path);
            if (p != null) list.Add(p);
        }
        Debug.Log($"[SceneBuilder] Loaded {list.Count} products.");
        return list;
    }

    // ═════════════════════════════════════════════════════════════════════════
    //  UI UTILITIES
    // ═════════════════════════════════════════════════════════════════════════

    private static Canvas MakeCanvas(string name, int order)
    {
        var go = new GameObject(name);
        var c  = go.AddComponent<Canvas>();
        c.renderMode    = RenderMode.ScreenSpaceOverlay;
        c.sortingOrder  = order;
        var cs = go.AddComponent<CanvasScaler>();
        cs.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        cs.referenceResolution = new Vector2(1920, 1080);
        cs.screenMatchMode     = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        cs.matchWidthOrHeight  = 0.5f;
        go.AddComponent<GraphicRaycaster>();
        return c;
    }

    private static GameObject UIPanel(string name, Transform parent,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax, Color color)
    {
        var go  = new GameObject(name);
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.color = color;
        var r = go.GetComponent<RectTransform>();
        r.anchorMin = anchorMin;
        r.anchorMax = anchorMax;
        r.offsetMin = offsetMin;
        r.offsetMax = offsetMax;
        return go;
    }

    private static TextMeshProUGUI UIText(Transform parent, string text, float size,
        TextAlignmentOptions align, Color color, bool bold)
    {
        var go = new GameObject("Text");
        go.transform.SetParent(parent, false);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text      = text;
        tmp.fontSize  = size;
        tmp.alignment = align;
        tmp.color     = color;
        if (bold) tmp.fontStyle = FontStyles.Bold;
        return tmp;
    }

    private static Button MakeButton(Transform parent, string label, Color bgColor, float fontSize)
    {
        var go = new GameObject(label + "_Btn");
        go.transform.SetParent(parent, false);
        go.AddComponent<Image>().color = bgColor;
        var btn = go.AddComponent<Button>();

        // Add hover colour transition
        var colors = btn.colors;
        colors.highlightedColor = Color.Lerp(bgColor, Color.white, 0.25f);
        colors.pressedColor     = Color.Lerp(bgColor, Color.black, 0.25f);
        btn.colors = colors;

        go.AddComponent<LayoutElement>();   // Allow layout groups to size it

        var lbl = new GameObject("Label");
        lbl.transform.SetParent(go.transform, false);
        var tmp = lbl.AddComponent<TextMeshProUGUI>();
        tmp.text      = label;
        tmp.fontSize  = fontSize;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color     = Color.white;
        FillParent(lbl.GetComponent<RectTransform>());

        return btn;
    }

    private static (GameObject scrollView, Transform content) BuildScrollView(
        string name, Transform parent,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
    {
        var sv = UIPanel(name, parent, anchorMin, anchorMax, offsetMin, offsetMax, new Color(0.03f, 0.05f, 0.09f));
        var sr = sv.AddComponent<ScrollRect>();
        sr.horizontal = false;

        var vp = UIPanel("Viewport", sv.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, Color.clear);
        vp.AddComponent<Mask>().showMaskGraphic = false;

        var ct = new GameObject("Content");
        ct.transform.SetParent(vp.transform, false);
        var vl = ct.AddComponent<VerticalLayoutGroup>();
        vl.spacing = 3;
        vl.padding = new RectOffset(6, 6, 6, 6);
        vl.childControlHeight = false;
        vl.childForceExpandHeight = false;
        var csf = ct.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        var crt = ct.GetComponent<RectTransform>();
        crt.anchorMin = new Vector2(0, 1);
        crt.anchorMax = new Vector2(1, 1);
        crt.pivot     = new Vector2(0.5f, 1f);
        crt.anchoredPosition = Vector2.zero;

        sr.content  = crt;
        sr.viewport = vp.GetComponent<RectTransform>();

        return (sv, ct.transform);
    }

    private static void PlaceRect(RectTransform r, Vector2 min, Vector2 max)
    {
        r.anchorMin = min; r.anchorMax = max;
        r.offsetMin = r.offsetMax = Vector2.zero;
    }

    private static void FillParent(RectTransform r)
    {
        r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one;
        r.offsetMin = r.offsetMax  = Vector2.zero;
    }

    // ═════════════════════════════════════════════════════════════════════════
    //  3D GEOMETRY UTILITIES
    // ═════════════════════════════════════════════════════════════════════════

    private static GameObject Box(string name, Transform parent, Vector3 localPos, Vector3 size)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localScale    = size;
        return go;
    }

    private static void ApplyColor(GameObject go, Color color)
    {
        var r = go.GetComponent<Renderer>();
        if (r == null) return;
        var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        mat.color = color;
        r.sharedMaterial = mat;
    }

    private static void SetStatic(GameObject go)
    {
        GameObjectUtility.SetStaticEditorFlags(go,
            StaticEditorFlags.ContributeGI | StaticEditorFlags.BatchingStatic);
    }

    private static GameObject Folder(string name)
    {
        var go = new GameObject(name);
        return go;
    }

    private static GameObject Child(GameObject parent, string name)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent.transform);
        return go;
    }
}
