using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// A compact 2D Noah holiday chapter. It deliberately avoids a second 3D
/// environment: the player reconnects with family, completes one biodiversity
/// puzzle and receives a permanent archive/progress reward.
/// </summary>
public class NoahHolidayUI : MonoBehaviour
{
    private const int PuzzleReward = 10;

    private GameObject holidayRoot;
    private GameObject puzzleRoot;
    private GameObject resultRoot;
    private Text progressText;
    private Text puzzleStatusText;
    private Text departButtonText;
    private Button departButton;
    private Texture2D puzzleTexture;
    private Action departAction;
    private int placedPieceCount;

    public void BuildUI()
    {
        puzzleTexture = Resources.Load<Texture2D>(
            "Story/BiodiversityPuzzle"
        );

        BuildHolidayHub();
        BuildPuzzle();
        BuildResult();
        gameObject.SetActive(false);
    }

    public void Show(Action onDepart)
    {
        departAction = onDepart;
        gameObject.SetActive(true);
        ShowHolidayHub();
    }

    private void BuildHolidayHub()
    {
        holidayRoot = CreateFullPanel("NoahHolidayHub", transform);

        RawImage hero = CreateRawImage(
            "NoahGarden",
            holidayRoot.transform,
            puzzleTexture,
            Color.white
        );
        UIFactory.Stretch(hero.rectTransform);

        Image veil = UIFactory.CreatePanel(
            "WarmVeil",
            holidayRoot.transform,
            new Color(0.018f, 0.035f, 0.065f, 0.64f)
        );
        UIFactory.Stretch(veil.rectTransform);

        Image card = UIFactory.CreatePanel(
            "HolidayCard",
            holidayRoot.transform,
            new Color(0.035f, 0.09f, 0.12f, 0.96f)
        );
        SetAnchored(
            card.rectTransform,
            new Vector2(0f, 0.5f),
            new Vector2(0f, 0.5f),
            new Vector2(78f, 0f),
            new Vector2(780f, 830f)
        );

        Image accent = UIFactory.CreatePanel(
            "WarmAccent",
            card.transform,
            new Color(1f, 0.65f, 0.28f, 1f)
        );
        SetAnchored(
            accent.rectTransform,
            new Vector2(0f, 1f),
            new Vector2(1f, 1f),
            Vector2.zero,
            new Vector2(0f, 6f)
        );

        Text eyebrow = UIFactory.CreateText(
            "Eyebrow",
            card.transform,
            "NOAH // 休假生活 · 第七码头学区",
            20,
            new Color(1f, 0.75f, 0.42f, 1f),
            TextAnchor.MiddleLeft
        );
        SetAnchored(
            eyebrow.rectTransform,
            new Vector2(0f, 1f),
            new Vector2(1f, 1f),
            new Vector2(54f, -42f),
            new Vector2(-108f, 38f)
        );

        Text title = UIFactory.CreateText(
            "Title",
            card.transform,
            "欢迎回家，修复官",
            52,
            Color.white,
            TextAnchor.MiddleLeft
        );
        SetAnchored(
            title.rectTransform,
            new Vector2(0f, 1f),
            new Vector2(1f, 1f),
            new Vector2(52f, -105f),
            new Vector2(-104f, 78f)
        );

        Image sisterBadge = UIFactory.CreatePanel(
            "SisterBadge",
            card.transform,
            new Color(0.20f, 0.63f, 0.66f, 1f)
        );
        SetAnchored(
            sisterBadge.rectTransform,
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            new Vector2(54f, -220f),
            new Vector2(108f, 108f)
        );
        Text sisterGlyph = UIFactory.CreateText(
            "SisterGlyph",
            sisterBadge.transform,
            "妹",
            52,
            Color.white
        );
        UIFactory.Stretch(sisterGlyph.rectTransform);

        Text sisterLine = UIFactory.CreateText(
            "SisterLine",
            card.transform,
            "“你终于回来啦！学校把地球生物做成了拼图。\n" +
            "老师说，记住它们的样子，也是让它们回家的第一步。”",
            27,
            new Color(0.87f, 0.96f, 0.94f, 1f),
            TextAnchor.UpperLeft
        );
        sisterLine.horizontalOverflow = HorizontalWrapMode.Wrap;
        SetAnchored(
            sisterLine.rectTransform,
            new Vector2(0f, 1f),
            new Vector2(1f, 1f),
            new Vector2(190f, -220f),
            new Vector2(-240f, 150f)
        );

        progressText = UIFactory.CreateText(
            "Progress",
            card.transform,
            string.Empty,
            23,
            new Color(0.68f, 0.90f, 0.88f, 1f),
            TextAnchor.MiddleLeft
        );
        SetAnchored(
            progressText.rectTransform,
            new Vector2(0f, 1f),
            new Vector2(1f, 1f),
            new Vector2(54f, -410f),
            new Vector2(-108f, 45f)
        );

        puzzleStatusText = UIFactory.CreateText(
            "PuzzleStatus",
            card.transform,
            string.Empty,
            20,
            new Color(1f, 0.72f, 0.40f, 1f),
            TextAnchor.MiddleLeft
        );
        SetAnchored(
            puzzleStatusText.rectTransform,
            new Vector2(0f, 1f),
            new Vector2(1f, 1f),
            new Vector2(54f, -462f),
            new Vector2(-108f, 40f)
        );

        Button puzzleButton = UIFactory.CreateButton(
            "OpenPuzzle",
            card.transform,
            "完成妹妹的生物拼图  ›",
            new Vector2(520f, 78f),
            new Color(0.12f, 0.55f, 0.52f, 1f),
            28
        );
        SetAnchored(
            puzzleButton.GetComponent<RectTransform>(),
            new Vector2(0f, 0f),
            new Vector2(0f, 0f),
            new Vector2(54f, 164f),
            new Vector2(520f, 78f)
        );
        puzzleButton.onClick.AddListener(OpenPuzzle);

        departButton = UIFactory.CreateButton(
            "Depart",
            card.transform,
            "完成拼图后启程",
            new Vector2(520f, 78f),
            new Color(0.16f, 0.25f, 0.28f, 1f),
            27
        );
        departButtonText = departButton.GetComponentInChildren<Text>();
        SetAnchored(
            departButton.GetComponent<RectTransform>(),
            new Vector2(0f, 0f),
            new Vector2(0f, 0f),
            new Vector2(54f, 62f),
            new Vector2(520f, 78f)
        );
        departButton.onClick.AddListener(DepartForStation);
    }

    private void BuildPuzzle()
    {
        puzzleRoot = CreateFullPanel("BiodiversityPuzzle", transform);
        Image background = puzzleRoot.AddComponent<Image>();
        background.color = new Color(0.008f, 0.028f, 0.045f, 1f);

        Text title = UIFactory.CreateText(
            "Title",
            puzzleRoot.transform,
            "妹妹的生物拼图  //  银杏与复绿群落",
            40,
            Color.white,
            TextAnchor.MiddleLeft
        );
        SetAnchored(
            title.rectTransform,
            new Vector2(0f, 1f),
            new Vector2(1f, 1f),
            new Vector2(72f, -35f),
            new Vector2(-260f, 62f)
        );

        Text hint = UIFactory.CreateText(
            "Hint",
            puzzleRoot.transform,
            "把右侧碎片拖到左侧对应位置。正确后会自动吸附。",
            22,
            new Color(0.58f, 0.82f, 0.82f, 1f),
            TextAnchor.MiddleLeft
        );
        SetAnchored(
            hint.rectTransform,
            new Vector2(0f, 1f),
            new Vector2(1f, 1f),
            new Vector2(74f, -96f),
            new Vector2(-148f, 40f)
        );

        Button back = UIFactory.CreateButton(
            "Back",
            puzzleRoot.transform,
            "返回休假页",
            new Vector2(210f, 52f),
            new Color(0.06f, 0.15f, 0.18f, 1f),
            21
        );
        SetAnchored(
            back.GetComponent<RectTransform>(),
            new Vector2(1f, 1f),
            new Vector2(1f, 1f),
            new Vector2(-58f, -28f),
            new Vector2(210f, 52f)
        );
        back.onClick.AddListener(ShowHolidayHub);

        RectTransform board = UIFactory.CreateRect(
            "PuzzleBoard",
            puzzleRoot.transform
        );
        SetAnchored(
            board,
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(-420f, -25f),
            new Vector2(720f, 540f)
        );

        RawImage preview = CreateRawImage(
            "BoardPreview",
            board,
            puzzleTexture,
            new Color(1f, 1f, 1f, 0.12f)
        );
        UIFactory.Stretch(preview.rectTransform);

        RectTransform tray = UIFactory.CreateRect(
            "PieceTray",
            puzzleRoot.transform
        );
        SetAnchored(
            tray,
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(490f, -25f),
            new Vector2(600f, 540f)
        );
        Image trayBg = tray.gameObject.AddComponent<Image>();
        trayBg.color = new Color(0.03f, 0.10f, 0.13f, 0.96f);

        RectTransform[] slots = new RectTransform[9];

        for (int row = 0; row < 3; row++)
        {
            for (int column = 0; column < 3; column++)
            {
                int index = row * 3 + column;
                Image slot = UIFactory.CreatePanel(
                    "Slot_" + index,
                    board,
                    new Color(0.10f, 0.28f, 0.30f, 0.36f)
                );
                RectTransform rect = slot.rectTransform;
                rect.anchorMin = new Vector2(column / 3f, 1f - (row + 1) / 3f);
                rect.anchorMax = new Vector2((column + 1) / 3f, 1f - row / 3f);
                rect.offsetMin = new Vector2(3f, 3f);
                rect.offsetMax = new Vector2(-3f, -3f);
                slots[index] = rect;
            }
        }

        int[] shuffle = { 4, 0, 7, 2, 8, 1, 5, 3, 6 };
        Canvas canvas = GetComponentInParent<Canvas>();

        for (int position = 0; position < shuffle.Length; position++)
        {
            int pieceIndex = shuffle[position];
            int sourceRow = pieceIndex / 3;
            int sourceColumn = pieceIndex % 3;
            int trayRow = position / 3;
            int trayColumn = position % 3;

            RawImage piece = CreateRawImage(
                "Piece_" + pieceIndex,
                tray,
                puzzleTexture,
                Color.white
            );
            piece.uvRect = new Rect(
                sourceColumn / 3f,
                1f - (sourceRow + 1) / 3f,
                1f / 3f,
                1f / 3f
            );
            SetAnchored(
                piece.rectTransform,
                new Vector2(0f, 1f),
                new Vector2(0f, 1f),
                new Vector2(24f + trayColumn * 190f, -26f - trayRow * 168f),
                new Vector2(172f, 142f)
            );

            NoahPuzzlePiece drag = piece.gameObject.AddComponent<NoahPuzzlePiece>();
            drag.Configure(slots[pieceIndex], canvas, HandlePiecePlaced);
        }

        puzzleRoot.SetActive(false);
    }

    private void BuildResult()
    {
        resultRoot = CreateFullPanel("PuzzleResult", transform);
        Image backdrop = resultRoot.AddComponent<Image>();
        backdrop.color = new Color(0.005f, 0.02f, 0.03f, 0.98f);

        RawImage image = CreateRawImage(
            "CompletedImage",
            resultRoot.transform,
            puzzleTexture,
            Color.white
        );
        SetAnchored(
            image.rectTransform,
            new Vector2(0f, 0f),
            new Vector2(0.62f, 1f),
            new Vector2(42f, 42f),
            new Vector2(-68f, -84f)
        );

        Image card = UIFactory.CreatePanel(
            "ArchiveCard",
            resultRoot.transform,
            new Color(0.04f, 0.13f, 0.13f, 0.98f)
        );
        SetAnchored(
            card.rectTransform,
            new Vector2(0.64f, 0f),
            new Vector2(1f, 1f),
            new Vector2(0f, 42f),
            new Vector2(-42f, -84f)
        );

        Text eyebrow = UIFactory.CreateText(
            "Eyebrow",
            card.transform,
            "图鉴解锁  //  BIO-GINKGO",
            19,
            new Color(1f, 0.72f, 0.34f, 1f),
            TextAnchor.MiddleLeft
        );
        SetAnchored(
            eyebrow.rectTransform,
            new Vector2(0f, 1f),
            new Vector2(1f, 1f),
            new Vector2(42f, -44f),
            new Vector2(-84f, 36f)
        );

        Text title = UIFactory.CreateText(
            "Title",
            card.transform,
            "银杏与早期复绿群落",
            42,
            Color.white,
            TextAnchor.UpperLeft
        );
        SetAnchored(
            title.rectTransform,
            new Vector2(0f, 1f),
            new Vector2(1f, 1f),
            new Vector2(40f, -105f),
            new Vector2(-80f, 105f)
        );

        Text body = UIFactory.CreateText(
            "Body",
            card.transform,
            "银杏是地球现存最古老的种子植物之一。它的祖先曾与恐龙共享" +
            "同一片天空。\n\n复绿不是让一株植物孤独地回来：苔藓保存水分，" +
            "蕨类稳定土壤，昆虫传递花粉，幼苗才有机会长成森林。\n\n" +
            "图鉴已同步至空间站资料库，地球修复进度 +10。",
            26,
            new Color(0.82f, 0.93f, 0.88f, 1f),
            TextAnchor.UpperLeft
        );
        body.horizontalOverflow = HorizontalWrapMode.Wrap;
        SetAnchored(
            body.rectTransform,
            new Vector2(0f, 1f),
            new Vector2(1f, 1f),
            new Vector2(42f, -245f),
            new Vector2(-84f, 390f)
        );

        Button close = UIFactory.CreateButton(
            "Continue",
            card.transform,
            "回到休假大厅  ›",
            new Vector2(340f, 76f),
            new Color(0.12f, 0.55f, 0.47f, 1f),
            27
        );
        SetAnchored(
            close.GetComponent<RectTransform>(),
            new Vector2(1f, 0f),
            new Vector2(1f, 0f),
            new Vector2(-40f, 40f),
            new Vector2(340f, 76f)
        );
        close.onClick.AddListener(ShowHolidayHub);
        resultRoot.SetActive(false);
    }

    private void OpenPuzzle()
    {
        if (MVPGameSession.BiodiversityPuzzleCompleted)
        {
            ShowOnly(resultRoot);
            return;
        }

        ShowOnly(puzzleRoot);
    }

    private void HandlePiecePlaced()
    {
        placedPieceCount++;

        if (placedPieceCount >= 9)
        {
            StartCoroutine(CompletePuzzleAfterDelay());
        }
    }

    private IEnumerator CompletePuzzleAfterDelay()
    {
        yield return new WaitForSecondsRealtime(0.45f);
        MVPGameSession.CompleteBiodiversityPuzzle(PuzzleReward);
        ShowOnly(resultRoot);
    }

    private void ShowHolidayHub()
    {
        ShowOnly(holidayRoot);
        bool completed = MVPGameSession.BiodiversityPuzzleCompleted;

        progressText.text =
            "地球修复进度  " + MVPGameSession.EarthProgress + " / " +
            MVPGameSession.EndingProgress;
        puzzleStatusText.text = completed
            ? "✓ 生物拼图完成 · 银杏图鉴已同步到空间站"
            : "待完成：生物拼图（奖励 +10）";
        departButton.interactable = completed;
        departButtonText.text = completed
            ? "返回空间站 · 开始下一工作日  ›"
            : "完成拼图后启程";
    }

    private void DepartForStation()
    {
        if (!MVPGameSession.BiodiversityPuzzleCompleted)
        {
            return;
        }

        departAction?.Invoke();
    }

    private void ShowOnly(GameObject target)
    {
        holidayRoot.SetActive(target == holidayRoot);
        puzzleRoot.SetActive(target == puzzleRoot);
        resultRoot.SetActive(target == resultRoot);
    }

    private static GameObject CreateFullPanel(string name, Transform parent)
    {
        RectTransform rect = UIFactory.CreateRect(name, parent);
        UIFactory.Stretch(rect);
        return rect.gameObject;
    }

    private static RawImage CreateRawImage(
        string name,
        Transform parent,
        Texture texture,
        Color color
    )
    {
        GameObject root = new(name, typeof(RectTransform), typeof(RawImage));
        root.transform.SetParent(parent, false);
        RawImage image = root.GetComponent<RawImage>();
        image.texture = texture;
        image.color = color;
        return image;
    }

    private static void SetAnchored(
        RectTransform rect,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 position,
        Vector2 size
    )
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = anchorMin;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }
}

public class NoahPuzzlePiece : MonoBehaviour,
    IBeginDragHandler,
    IDragHandler,
    IEndDragHandler
{
    private RectTransform rect;
    private RectTransform correctSlot;
    private Canvas canvas;
    private Action placedAction;
    private Transform homeParent;
    private Vector2 homePosition;
    private bool placed;

    public void Configure(
        RectTransform targetSlot,
        Canvas rootCanvas,
        Action onPlaced
    )
    {
        rect = GetComponent<RectTransform>();
        correctSlot = targetSlot;
        canvas = rootCanvas;
        placedAction = onPlaced;
        homeParent = transform.parent;
        homePosition = rect.anchoredPosition;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (placed)
        {
            return;
        }

        transform.SetAsLastSibling();
        GetComponent<RawImage>().raycastTarget = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (placed)
        {
            return;
        }

        float scale = canvas == null ? 1f : canvas.scaleFactor;
        rect.anchoredPosition += eventData.delta / Mathf.Max(0.01f, scale);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (placed)
        {
            return;
        }

        GetComponent<RawImage>().raycastTarget = true;
        bool correct = RectTransformUtility.RectangleContainsScreenPoint(
            correctSlot,
            eventData.position,
            eventData.pressEventCamera
        );

        if (!correct)
        {
            transform.SetParent(homeParent, false);
            rect.anchoredPosition = homePosition;
            return;
        }

        placed = true;
        transform.SetParent(correctSlot, false);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        placedAction?.Invoke();
    }
}
