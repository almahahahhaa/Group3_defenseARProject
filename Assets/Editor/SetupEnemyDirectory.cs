using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;

// Run once via Tools > Setup Enemy Directory.
// Wraps the existing Pollution Cloud elements in a page, then adds
// Oil Slick and Mini Oil Slick pages with navigation arrows.
public static class SetupEnemyDirectory
{
    // Image GUIDs (Assets/Resources/)
    const string OilSlickGuid     = "2f1f191621a3b834693f5bdfd1b483ca";
    const string MiniOilSlickGuid = "380e42ffb6cfdbe46899c0f95e167156";

    // TMP font used by every label in the directory
    const string FontGuid = "8f586378b4e144a9851e7b34d9b748ee";

    [MenuItem("Tools/Setup Enemy Directory")]
    static void Run()
    {
        // ── 1. Find the content Image container ──────────────────────────────
        // Hierarchy: EnemyDirectory > Popup > Image  (m_Name: "Image", parent "Popup")
        GameObject popup = FindInactive("Popup");
        if (popup == null) { Debug.LogError("[EnemyDir] Could not find 'Popup'."); return; }

        Transform contentTr = null;
        foreach (Transform c in popup.transform)
            if (c.name == "Image" && c.GetComponent<Image>() != null)
                { contentTr = c; break; }

        if (contentTr == null) { Debug.LogError("[EnemyDir] Could not find content Image child of Popup."); return; }

        // Guard: already set up
        if (contentTr.Find("Page1_PollutionCloud") != null)
        { Debug.Log("[EnemyDir] Already set up — nothing to do."); return; }

        GameObject content = contentTr.gameObject;
        RectTransform contentRT = content.GetComponent<RectTransform>();

        // ── 2. Wrap existing children in Page1 ───────────────────────────────
        GameObject page1 = new GameObject("Page1_PollutionCloud");
        RectTransform p1rt = page1.AddComponent<RectTransform>();
        SetStretch(p1rt, contentRT);
        page1.transform.SetParent(contentTr, false);

        // Move all existing children (collected first to avoid iterator invalidation)
        var children = new System.Collections.Generic.List<Transform>();
        foreach (Transform ch in contentTr) if (ch != page1.transform) children.Add(ch);
        foreach (var ch in children) ch.SetParent(page1.transform, true);

        // ── 3. Create Oil Slick page ─────────────────────────────────────────
        GameObject page2 = DuplicatePage(page1, "Page2_OilSlick");
        page2.transform.SetParent(contentTr, false);
        SetStretch(page2.GetComponent<RectTransform>(), contentRT);
        ApplyEntryData(page2,
            "Oil Slick",
            "A hazardous oil spill that creeps steadily toward landmarks. It moves slowly but deals heavy damage on contact. Tap to destroy it before it reaches the tower.",
            OilSlickGuid,
            waveText: "2",
            damageText: "3 dmg / hit");

        // ── 4. Create Mini Oil Slick page ────────────────────────────────────
        GameObject page3 = DuplicatePage(page1, "Page3_MiniOilSlick");
        page3.transform.SetParent(contentTr, false);
        SetStretch(page3.GetComponent<RectTransform>(), contentRT);
        ApplyEntryData(page3,
            "Mini Oil Slick",
            "A smaller, faster variant of the Oil Slick. It darts quickly toward landmarks and deals light damage. Tap fast to neutralise it before it gets through.",
            MiniOilSlickGuid,
            waveText: "1",
            damageText: "1 dmg / hit");

        // ── 5. Add navigation to TopPanel ────────────────────────────────────
        Transform topPanel = null;
        foreach (Transform c in popup.transform)
            if (c.name == "TopPanel") { topPanel = c; break; }

        TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
            AssetDatabase.GUIDToAssetPath(FontGuid));

        Button prevBtn = null, nextBtn = null;
        TextMeshProUGUI pageLabel = null;

        if (topPanel != null)
        {
            prevBtn  = MakeNavButton(topPanel, "PrevBtn",  "◀", new Vector2(-340, 0), font);
            nextBtn  = MakeNavButton(topPanel, "NextBtn",  "▶", new Vector2( 340, 0), font);
            pageLabel = MakePageLabel(topPanel, font);
        }

        // ── 6. Attach navigator component ────────────────────────────────────
        EnemyDirectoryNavigator nav = popup.GetComponent<EnemyDirectoryNavigator>();
        if (nav == null) nav = popup.AddComponent<EnemyDirectoryNavigator>();

        nav.pages = new GameObject[] { page1, page2, page3 };
        nav.pageIndicator = pageLabel;
        nav.prevButton    = prevBtn;
        nav.nextButton    = nextBtn;

        if (prevBtn != null) prevBtn.onClick.AddListener(nav.Prev);
        if (nextBtn != null) nextBtn.onClick.AddListener(nav.Next);

        // Hide pages 2 and 3 by default
        page2.SetActive(false);
        page3.SetActive(false);

        // ── 7. Save ──────────────────────────────────────────────────────────
        EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();
        Debug.Log("[EnemyDir] Done. Three pages added with Prev/Next navigation.");
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    static GameObject FindInactive(string name)
    {
        foreach (var root in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
        {
            var found = FindInactiveRecursive(root.transform, name);
            if (found != null) return found.gameObject;
        }
        return null;
    }

    static Transform FindInactiveRecursive(Transform t, string name)
    {
        if (t.name == name) return t;
        foreach (Transform c in t)
        {
            var r = FindInactiveRecursive(c, name);
            if (r != null) return r;
        }
        return null;
    }

    static void SetStretch(RectTransform rt, RectTransform reference)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = Vector2.zero;
    }

    static GameObject DuplicatePage(GameObject source, string newName)
    {
        GameObject dup = Object.Instantiate(source);
        dup.name = newName;
        return dup;
    }

    // Walks the duplicated page tree and updates the fields that differ per entry.
    static void ApplyEntryData(GameObject page, string enemyName, string description,
                               string imageGuid, string waveText, string damageText)
    {
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(
            AssetDatabase.GUIDToAssetPath(imageGuid));

        // Walk every Image and TMP component in the page
        foreach (var img in page.GetComponentsInChildren<Image>(true))
        {
            // The enemy portrait image: it has no tint (white) and a non-null sprite
            // that comes from the original Pollution Cloud. Replace it.
            if (img.color == Color.white && img.sprite != null
                && img.sprite.name != null
                && !img.sprite.name.Contains("Background")
                && img.rectTransform.sizeDelta.x > 200f)
            {
                if (sprite != null) img.sprite = sprite;
            }
        }

        // Replace text fields by position / content heuristics
        foreach (var tmp in page.GetComponentsInChildren<TextMeshProUGUI>(true))
        {
            string t = tmp.text.Trim();

            // Name label (large font ~36, short text containing "Pollution Cloud")
            if (t == "Pollution Cloud")
            {
                tmp.text = enemyName;
                continue;
            }

            // Description (long text, font 30)
            if (t.StartsWith("A toxic smog"))
            {
                tmp.text = description;
                continue;
            }

            // Damage label
            if (t == "2 dmg / hit")
            {
                tmp.text = damageText;
                continue;
            }

            // Wave count ("2" in the wave badge, font size ~45, right-aligned)
            if (t == "2" && Mathf.Approximately(tmp.fontSize, 45f)
                          && tmp.rectTransform.sizeDelta.x < 210f)
            {
                tmp.text = waveText;
                continue;
            }
        }
    }

    static Button MakeNavButton(Transform parent, string goName,
                                string label, Vector2 anchoredPos,
                                TMP_FontAsset font)
    {
        var go = new GameObject(goName);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(80, 60);
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = anchoredPos;

        var img = go.AddComponent<Image>();
        img.color = new Color(0.16f, 0.09f, 0.38f, 1f);

        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;

        // Label child
        var lblGO = new GameObject("Label");
        lblGO.transform.SetParent(go.transform, false);
        var lrt = lblGO.AddComponent<RectTransform>();
        lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
        lrt.offsetMin = lrt.offsetMax = Vector2.zero;
        var tmp = lblGO.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = 36;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;
        if (font != null) tmp.font = font;

        return btn;
    }

    static TextMeshProUGUI MakePageLabel(Transform parent, TMP_FontAsset font)
    {
        var go = new GameObject("PageLabel");
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(120, 40);
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;

        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = "1 / 3";
        tmp.fontSize = 28;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;
        if (font != null) tmp.font = font;
        return tmp;
    }
}
