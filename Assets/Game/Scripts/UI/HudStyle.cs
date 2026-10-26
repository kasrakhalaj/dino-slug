using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DinoSlug
{
    /// <summary>
    /// Runtime HUD readability pass: one pixel font everywhere, dark backing panels behind every
    /// cluster that sits on the busy Metal Slug backgrounds, and layout fixes for overlapping text.
    /// Applied once by GameHud.Awake, so the scene layout stays editable in the Inspector.
    /// </summary>
    public static class HudStyle
    {
        public static readonly Color Panel = new Color(.07f, .06f, .09f, .68f);
        public const float FontScale = .72f;          // Press Start 2P is much wider than Liberation Sans

        public static void Apply(Transform canvas, TMP_FontAsset pixelFont)
        {
            if (canvas == null) return;
            var hud = canvas.Find("In Game HUD");
            if (hud != null) StyleInGame(hud);
            Text(canvas, "Main Menu/Eyebrow", "A LITTLE DINOSAUR VS. THE REBEL ARMY", new Vector2(430, -160));
            Text(canvas, "Pause Menu/Pause Title", "PAUSED", null);
            Text(canvas, "Game Over/Over Title", "MISSION FAILED", null);
            Text(canvas, "Game Over/Over Eyebrow", "THE REBELS GOT YOU THIS TIME", null);
            var description = canvas.Find("Main Menu/Description");
            if (description != null) Backing(description as RectTransform, new Vector2(28, 18));
            var controls = canvas.Find("Main Menu/Controls");
            if (controls != null) Backing(controls as RectTransform, new Vector2(24, 12));
            if (pixelFont != null)
                foreach (var text in canvas.GetComponentsInChildren<TMP_Text>(true))
                {
                    if (text.GetComponentInParent<CharacterSelection>(true) != null) continue;   // already pixel art
                    Pixel(text, pixelFont);
                }
        }

        private static void StyleInGame(Transform hud)
        {
            Hide(hud, "Brand");                                           // the game's name is not live information
            var lives = hud.Find("Lives") as RectTransform;
            if (lives != null)
            {
                lives.sizeDelta = new Vector2(80, lives.sizeDelta.y);
                var label = lives.GetComponent<TMP_Text>();
                if (label != null) label.textWrappingMode = TextWrappingModes.NoWrap;
            }
            // Coins: "+00" becomes a labelled counter laid out like SCORE / PERSONAL BEST.
            var icon = hud.Find("Coin Icon") as RectTransform;
            if (icon != null)
            {
                icon.anchoredPosition = new Vector2(1180, -70); icon.sizeDelta = new Vector2(150, 25);
                var t = icon.GetComponent<TMP_Text>(); if (t != null) { t.text = "COINS"; t.fontSize = 13; t.alignment = TextAlignmentOptions.Center; }
            }
            var count = hud.Find("Coin Count") as RectTransform;
            if (count != null)
            {
                count.anchoredPosition = new Vector2(1180, -111); count.sizeDelta = new Vector2(150, 55);
                var t = count.GetComponent<TMP_Text>(); if (t != null) { t.fontSize = 30; t.alignment = TextAlignmentOptions.Center; }
            }
            Move(hud, "Jump Hint", 60, 0);
            Move(hud, "Dash Hint", 60, 0);
            Move(hud, "Dash Cooldown", 60, 0);
            // Announcements sit under the sector badge instead of on top of it.
            Move(hud, "Weapon Announcement", 0, -58);

            Block(hud, "Vitality Panel", new Vector2(215, -151), new Vector2(270, 78));
            Block(hud, "Score Panel", new Vector2(765, -100), new Vector2(480, 100));
            Block(hud, "Coin Panel", new Vector2(1180, -100), new Vector2(170, 100));
            foreach (var name in new[] { "Weapon Status", "Jump Hint", "Dash Hint", "Distance", "Weapon Announcement" })
            {
                var r = hud.Find(name) as RectTransform;
                if (r != null) Backing(r, name == "Weapon Announcement" ? new Vector2(40, 20) : new Vector2(22, 12));
            }
        }

        private static void Pixel(TMP_Text text, TMP_FontAsset font)
        {
            float size = text.fontSize;
            text.font = font;
            text.fontSharedMaterial = font.material;
            text.fontStyle = FontStyles.Normal;
            text.characterSpacing = 0;
            text.enableAutoSizing = true;                 // never overflow a rect sized for the old font
            text.fontSizeMax = Mathf.Max(8, Mathf.Round(size * FontScale));
            text.fontSizeMin = 8;
            text.fontSize = text.fontSizeMax;
            text.textWrappingMode = text.name == "Lives" ? TextWrappingModes.NoWrap : TextWrappingModes.Normal;
        }

        private static void Text(Transform root, string path, string value, Vector2? position)
        {
            var t = root.Find(path);
            if (t == null) return;
            var label = t.GetComponent<TMP_Text>();
            if (label != null) label.text = value;
            if (position.HasValue) ((RectTransform)t).anchoredPosition = position.Value;
        }

        private static void Hide(Transform root, string path) { var t = root.Find(path); if (t != null) t.gameObject.SetActive(false); }

        private static void Move(Transform root, string path, float x, float y)
        {
            var r = root.Find(path) as RectTransform;
            if (r != null) r.anchoredPosition += new Vector2(x, y);
        }

        private static Image Block(Transform parent, string name, Vector2 center, Vector2 size)
        {
            if (parent.Find(name) != null) return null;
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            var r = (RectTransform)go.transform;
            r.SetParent(parent, false);
            r.anchorMin = r.anchorMax = new Vector2(0, 1); r.pivot = new Vector2(.5f, .5f);
            r.anchoredPosition = center; r.sizeDelta = size;
            var image = go.GetComponent<Image>(); image.color = Panel; image.raycastTarget = false;
            var contrast = parent.Find("HUD Contrast");
            r.SetSiblingIndex(contrast != null ? contrast.GetSiblingIndex() + 1 : 0);
            return image;
        }

        /// <summary>A backing chip that follows the rendered text bounds and hides with the text.</summary>
        private static void Backing(RectTransform text, Vector2 padding)
        {
            var label = text.GetComponent<TMP_Text>();
            if (label == null || text.parent.Find(text.name + " Backing") != null) return;
            var image = Block(text.parent, text.name + " Backing", text.anchoredPosition, text.sizeDelta);
            if (image == null) return;
            image.transform.SetSiblingIndex(text.GetSiblingIndex());
            var chip = image.gameObject.AddComponent<HudChip>();
            chip.Bind(label, padding);
        }
    }

    public sealed class HudChip : MonoBehaviour
    {
        private TMP_Text label;
        private RectTransform target, self;
        private Image image;
        private Vector2 padding;

        public void Bind(TMP_Text text, Vector2 pad)
        {
            label = text; target = (RectTransform)text.transform; padding = pad;
            self = (RectTransform)transform; image = GetComponent<Image>();
        }

        private void LateUpdate()
        {
            if (label == null) return;
            bool show = label.isActiveAndEnabled && !string.IsNullOrWhiteSpace(label.text) && label.alpha > .05f;
            image.enabled = show;
            if (!show) return;
            var bounds = label.textBounds;
            if (bounds.size.x <= 0) { image.enabled = false; return; }
            self.anchorMin = target.anchorMin; self.anchorMax = target.anchorMax; self.pivot = new Vector2(.5f, .5f);
            // textBounds is local to the label's pivot, which sits at its anchoredPosition.
            self.anchoredPosition = target.anchoredPosition + (Vector2)bounds.center;
            self.sizeDelta = (Vector2)bounds.size + padding;
            var c = HudStyle.Panel; c.a *= label.alpha; image.color = c;
        }
    }
}
