using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Alpha.World.EditorTools
{
    // Canvas-ul boss-ului: benzi cinematice, titlul "THE SLEEPER" si bara de viata.
    public static class Arena1BossUiBuilder
    {
        public const string BossTitle = "THE SLEEPER";
        public const string BossSubtitle = "Stone Golem of the Old Road";
        public const string BossBarName = "The Sleeper";
        public const string CarvingTitle = "THE FIRST LAYER FALLS";
        public const string CarvingSubtitle = "The Sleeper's heart beats within the stone";

        static readonly Color Parchment = new Color(0.94f, 0.87f, 0.7f);
        static readonly Color Gold = new Color(0.8f, 0.64f, 0.36f, 0.95f);
        static readonly Color Ink = new Color(0.05f, 0.035f, 0.02f, 0.9f);

        public static BossIntro Build(Transform parent, Arena1FxBuilder.Result fx, out BossHealthBar healthBar)
        {
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            GameObject canvasGo = new GameObject("Arena1_BossCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            canvasGo.transform.SetParent(parent, false);
            Canvas canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 20;
            CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            Transform root = canvasGo.transform;

            RectTransform top = Bar(root, "Letterbox_Top", 1f);
            RectTransform bottom = Bar(root, "Letterbox_Bottom", 0f);
            BossTitleCard card = BuildTitleCard(root, "TitleCard", BossTitle, BossSubtitle, font, fx);
            healthBar = BuildHealthBar(root, font, fx);

            BossIntro intro = new GameObject("Arena1_BossIntro").AddComponent<BossIntro>();
            intro.transform.SetParent(parent, false);
            SetRef(intro, "titleCard", card);
            SerializedObject so = new SerializedObject(intro);
            SerializedProperty bars = so.FindProperty("letterbox");
            bars.arraySize = 2;
            bars.GetArrayElementAtIndex(0).objectReferenceValue = top;
            bars.GetArrayElementAtIndex(1).objectReferenceValue = bottom;
            so.ApplyModifiedPropertiesWithoutUndo();
            return intro;
        }

        static RectTransform Bar(Transform parent, string name, float edge)
        {
            RectTransform rect = Rect(parent, name, new Vector2(0f, edge), new Vector2(1f, edge), new Vector2(0.5f, edge), Vector2.zero, new Vector2(0f, 120f));
            Image image = rect.gameObject.AddComponent<Image>();
            image.color = Color.black;
            image.raycastTarget = false;
            rect.localScale = new Vector3(1f, 0f, 1f);
            return rect;
        }

        // cinematica de cioplire refoloseste benzile intro-ului; are titlul ei si un flash pe tot ecranul
        public static Arena1Carving BuildCarving(Transform parent, Arena1FxBuilder.Result fx, ParticleSystem dust)
        {
            Transform root = parent.Find("Arena1_BossCanvas");
            if (root == null) return null;
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            RectTransform flashRect = Rect(root, "CarvingFlash", Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            flashRect.SetAsFirstSibling();
            Image flash = flashRect.gameObject.AddComponent<Image>();
            flash.color = new Color(0.86f, 1f, 0.9f, 0f);
            flash.raycastTarget = false;
            flash.enabled = false;
            BossTitleCard card = BuildTitleCard(root, "CarvingCard", CarvingTitle, CarvingSubtitle, font, fx);

            Arena1Carving carving = new GameObject("Arena1_Carving").AddComponent<Arena1Carving>();
            carving.transform.SetParent(parent, false);
            SetRef(carving, "titleCard", card);
            SetRef(carving, "flash", flash);
            SetRef(carving, "dustPrefab", dust);
            SerializedObject so = new SerializedObject(carving);
            SerializedProperty bars = so.FindProperty("letterbox");
            bars.arraySize = 2;
            bars.GetArrayElementAtIndex(0).objectReferenceValue = root.Find("Letterbox_Top");
            bars.GetArrayElementAtIndex(1).objectReferenceValue = root.Find("Letterbox_Bottom");
            so.ApplyModifiedPropertiesWithoutUndo();
            return carving;
        }

        static BossTitleCard BuildTitleCard(Transform parent, string name, string titleText, string subtitleText, Font font, Arena1FxBuilder.Result fx)
        {
            RectTransform card = Center(parent, name, new Vector2(0f, -170f), new Vector2(1600f, 300f));
            CanvasGroup group = card.gameObject.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            group.interactable = false;
            group.alpha = 0f;

            Text title = Label(card, "Title", font, titleText, 96, FontStyle.Bold, Parchment, new Vector2(0f, 46f), new Vector2(1600f, 130f));
            UILetterSpacing titleSpacing = title.gameObject.AddComponent<UILetterSpacing>();
            SetFloat(titleSpacing, "spacing", 14f);
            Outline outline = title.gameObject.AddComponent<Outline>();
            outline.effectColor = Ink;
            outline.effectDistance = new Vector2(2f, -2f);
            Shadow shadow = title.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.65f);
            shadow.effectDistance = new Vector2(4f, -6f);

            RectTransform ornament = Center(card, "Ornament", new Vector2(0f, -26f), new Vector2(980f, 60f));
            Image line = ornament.gameObject.AddComponent<Image>();
            line.sprite = fx.ornament;
            line.color = Gold;
            line.raycastTarget = false;

            RectTransform sub = Center(card, "Subtitle", new Vector2(0f, -84f), new Vector2(1400f, 60f));
            CanvasGroup subGroup = sub.gameObject.AddComponent<CanvasGroup>();
            subGroup.blocksRaycasts = false;
            Text subtitle = Label(sub, "Text", font, subtitleText, 36, FontStyle.Italic, new Color(0.86f, 0.81f, 0.7f), Vector2.zero, new Vector2(1400f, 60f));
            UILetterSpacing subSpacing = subtitle.gameObject.AddComponent<UILetterSpacing>();
            SetFloat(subSpacing, "spacing", 4f);
            Shadow subShadow = subtitle.gameObject.AddComponent<Shadow>();
            subShadow.effectColor = new Color(0f, 0f, 0f, 0.8f);
            subShadow.effectDistance = new Vector2(2f, -3f);

            BossTitleCard titleCard = card.gameObject.AddComponent<BossTitleCard>();
            SetRef(titleCard, "group", group);
            SetRef(titleCard, "subtitleGroup", subGroup);
            SetRef(titleCard, "titleSpacing", titleSpacing);
            SetRef(titleCard, "ornament", ornament);
            return titleCard;
        }

        static BossHealthBar BuildHealthBar(Transform parent, Font font, Arena1FxBuilder.Result fx)
        {
            RectTransform bar = Rect(parent, "BossHealthBar", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -60f), new Vector2(1000f, 110f));
            CanvasGroup group = bar.gameObject.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            group.interactable = false;
            group.alpha = 0f;

            Text name = Label(bar, "Name", font, BossBarName, 30, FontStyle.Bold, Parchment, new Vector2(0f, 34f), new Vector2(1000f, 44f));
            SetFloat(name.gameObject.AddComponent<UILetterSpacing>(), "spacing", 6f);
            Shadow nameShadow = name.gameObject.AddComponent<Shadow>();
            nameShadow.effectColor = new Color(0f, 0f, 0f, 0.85f);
            nameShadow.effectDistance = new Vector2(2f, -3f);

            Image frame = Fill(bar, "Frame", fx.bar, new Color(0.07f, 0.05f, 0.04f, 0.88f), new Vector2(0f, -4f), new Vector2(980f, 26f), false);
            Outline frameLine = frame.gameObject.AddComponent<Outline>();
            frameLine.effectColor = new Color(0.62f, 0.5f, 0.3f, 0.9f);
            frameLine.effectDistance = new Vector2(2f, -2f);
            Image trail = Fill(bar, "Trail", fx.bar, new Color(0.96f, 0.82f, 0.58f, 0.85f), new Vector2(0f, -4f), new Vector2(968f, 16f), true);
            Image fill = Fill(bar, "Fill", fx.bar, new Color(0.66f, 0.09f, 0.06f), new Vector2(0f, -4f), new Vector2(968f, 16f), true);

            RectTransform ornament = Center(bar, "Ornament", new Vector2(0f, -30f), new Vector2(1120f, 40f));
            Image line = ornament.gameObject.AddComponent<Image>();
            line.sprite = fx.ornament;
            line.color = new Color(Gold.r, Gold.g, Gold.b, 0.7f);
            line.raycastTarget = false;

            BossHealthBar health = bar.gameObject.AddComponent<BossHealthBar>();
            SetRef(health, "group", group);
            SetRef(health, "fill", fill);
            SetRef(health, "trail", trail);
            SetRef(health, "nameLabel", name);
            return health;
        }

        static Image Fill(Transform parent, string name, Sprite sprite, Color color, Vector2 position, Vector2 size, bool filled)
        {
            RectTransform rect = Center(parent, name, position, size);
            Image image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.raycastTarget = false;
            if (filled)
            {
                image.type = Image.Type.Filled;
                image.fillMethod = Image.FillMethod.Horizontal;
                image.fillOrigin = (int)Image.OriginHorizontal.Left;
                image.fillAmount = 1f;
            }
            return image;
        }

        static Text Label(Transform parent, string name, Font font, string value, int size, FontStyle style, Color color, Vector2 position, Vector2 box)
        {
            RectTransform rect = Center(parent, name, position, box);
            Text text = rect.gameObject.AddComponent<Text>();
            text.font = font;
            text.text = value;
            text.fontSize = size;
            text.fontStyle = style;
            text.color = color;
            text.alignment = TextAnchor.MiddleCenter;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            return text;
        }

        static RectTransform Center(Transform parent, string name, Vector2 position, Vector2 size) =>
            Rect(parent, name, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), position, size);

        static RectTransform Rect(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 position, Vector2 size)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            RectTransform rect = (RectTransform)go.transform;
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return rect;
        }

        static void SetRef(Object target, string name, Object value)
        {
            SerializedObject so = new SerializedObject(target);
            so.FindProperty(name).objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void SetFloat(Object target, string name, float value)
        {
            SerializedObject so = new SerializedObject(target);
            so.FindProperty(name).floatValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
