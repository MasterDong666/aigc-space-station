using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// One reusable station archive for civilization records and repair-officer
/// files. The current MVP exposes it through TAB; a future 3D archive terminal
/// can call OpenArchive without changing this controller.
/// </summary>
public class StationArchiveController : MonoBehaviour
{
    [Header("入口与主面板")]
    [SerializeField] private GameObject shortcutRoot;
    [SerializeField] private Button shortcutButton;
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private Button closeButton;

    [Header("分类")]
    [SerializeField] private Button civilizationTab;
    [SerializeField] private Button officerTab;
    [SerializeField] private Text civilizationTabText;
    [SerializeField] private Text officerTabText;

    [Header("列表与详情")]
    [SerializeField] private RectTransform listRoot;
    [SerializeField] private Button rowTemplate;
    [SerializeField] private Text sectionTitleText;
    [SerializeField] private Text collectionStatusText;
    [SerializeField] private Text entryIdText;
    [SerializeField] private Text entryTitleText;
    [SerializeField] private Text entrySubtitleText;
    [SerializeField] private Text classificationText;
    [SerializeField] private Text bodyText;
    [SerializeField] private Text lockHintText;

    [Header("场景引用")]
    [SerializeField] private PlayerInteractor playerInteractor;
    [SerializeField] private MVPFlowController flowController;

    [Header("资料")]
    [SerializeField] private List<ArchiveEntryData> entries = new();

    private static Font runtimeChineseFont;
    private readonly List<GameObject> runtimeRows = new();
    private ArchiveCategory activeCategory = ArchiveCategory.Civilization;
    private ArchiveEntryData selectedEntry;
    private bool ownsPlayerLock;
    private bool legacyOfficerArchivesUnlocked;

    public bool IsOpen => panelRoot != null && panelRoot.activeSelf;
    public ArchiveCategory ActiveCategory => activeCategory;
    public string SelectedEntryId =>
        selectedEntry == null ? string.Empty : selectedEntry.id;
    public int CurrentUnlockStage =>
        flowController == null ? 3 : flowController.RepairStage;
    public bool LegacyOfficerArchivesUnlocked =>
        legacyOfficerArchivesUnlocked;

    private void Awake()
    {
        ResolveSceneReferences();
        ApplyRuntimeFont();
        RegisterButtons();

        if (rowTemplate != null)
        {
            rowTemplate.gameObject.SetActive(false);
        }

        if (panelRoot != null)
        {
            panelRoot.SetActive(false);
        }

        if (shortcutRoot != null)
        {
            shortcutRoot.SetActive(true);
        }
    }

    private void OnEnable()
    {
        ResolveSceneReferences();

        if (flowController != null)
        {
            flowController.RepairStageChanged += HandleRepairStageChanged;
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            if (IsOpen)
            {
                CloseArchive();
            }
            else
            {
                OpenArchive();
            }
        }

        if (IsOpen && Input.GetKeyDown(KeyCode.Escape))
        {
            CloseArchive();
        }
    }

    private void OnDisable()
    {
        if (flowController != null)
        {
            flowController.RepairStageChanged -= HandleRepairStageChanged;
        }

        ReleaseOwnedPlayerLock();
    }

    private void OnDestroy()
    {
        UnregisterButtons();
    }

    public void OpenArchive()
    {
        ResolveSceneReferences();

        if (IsOpen)
        {
            return;
        }

        if (
            playerInteractor != null &&
            playerInteractor.IsPlayerControlLocked
        )
        {
            return;
        }

        if (panelRoot != null)
        {
            panelRoot.SetActive(true);
        }

        if (shortcutRoot != null)
        {
            shortcutRoot.SetActive(false);
        }

        if (playerInteractor != null)
        {
            playerInteractor.SetPlayerControlLocked(true);
            ownsPlayerLock = true;
        }

        ShowCategory(activeCategory);
    }

    public void CloseArchive()
    {
        if (panelRoot != null)
        {
            panelRoot.SetActive(false);
        }

        if (shortcutRoot != null)
        {
            shortcutRoot.SetActive(true);
        }

        ReleaseOwnedPlayerLock();
    }

    public void ShowCivilizationCategory()
    {
        ShowCategory(ArchiveCategory.Civilization);
    }

    public void ShowOfficerCategory()
    {
        ShowCategory(ArchiveCategory.RepairOfficer);
    }

    public void UnlockLegacyOfficerArchives()
    {
        if (legacyOfficerArchivesUnlocked)
        {
            return;
        }

        legacyOfficerArchivesUnlocked = true;

        if (IsOpen)
        {
            ShowCategory(activeCategory);
        }
    }

    public void Configure(
        GameObject shortcut,
        Button shortcutOpenButton,
        GameObject panel,
        Button panelCloseButton,
        Button civilizationButton,
        Button officerButton,
        Text civilizationLabel,
        Text officerLabel,
        RectTransform rowsRoot,
        Button template,
        Text sectionTitle,
        Text collectionStatus,
        Text entryId,
        Text entryTitle,
        Text entrySubtitle,
        Text classification,
        Text entryBody,
        Text lockHint,
        PlayerInteractor interactor,
        MVPFlowController flow,
        List<ArchiveEntryData> archiveEntries
    )
    {
        shortcutRoot = shortcut;
        shortcutButton = shortcutOpenButton;
        panelRoot = panel;
        closeButton = panelCloseButton;
        civilizationTab = civilizationButton;
        officerTab = officerButton;
        civilizationTabText = civilizationLabel;
        officerTabText = officerLabel;
        listRoot = rowsRoot;
        rowTemplate = template;
        sectionTitleText = sectionTitle;
        collectionStatusText = collectionStatus;
        entryIdText = entryId;
        entryTitleText = entryTitle;
        entrySubtitleText = entrySubtitle;
        classificationText = classification;
        bodyText = entryBody;
        lockHintText = lockHint;
        playerInteractor = interactor;
        flowController = flow;
        entries = archiveEntries ?? new List<ArchiveEntryData>();
    }

    private void RegisterButtons()
    {
        if (shortcutButton != null)
        {
            shortcutButton.onClick.AddListener(OpenArchive);
        }

        if (closeButton != null)
        {
            closeButton.onClick.AddListener(CloseArchive);
        }

        if (civilizationTab != null)
        {
            civilizationTab.onClick.AddListener(
                ShowCivilizationCategory
            );
        }

        if (officerTab != null)
        {
            officerTab.onClick.AddListener(ShowOfficerCategory);
        }
    }

    private void UnregisterButtons()
    {
        if (shortcutButton != null)
        {
            shortcutButton.onClick.RemoveListener(OpenArchive);
        }

        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(CloseArchive);
        }

        if (civilizationTab != null)
        {
            civilizationTab.onClick.RemoveListener(
                ShowCivilizationCategory
            );
        }

        if (officerTab != null)
        {
            officerTab.onClick.RemoveListener(ShowOfficerCategory);
        }
    }

    private void ShowCategory(ArchiveCategory category)
    {
        activeCategory = category;
        BuildRows();
        UpdateTabVisuals();

        ArchiveEntryData firstUnlocked = null;
        ArchiveEntryData firstEntry = null;

        foreach (ArchiveEntryData entry in entries)
        {
            if (entry.category != activeCategory)
            {
                continue;
            }

            firstEntry ??= entry;

            if (firstUnlocked == null && IsUnlocked(entry))
            {
                firstUnlocked = entry;
            }
        }

        SelectEntry(firstUnlocked ?? firstEntry);
    }

    private void BuildRows()
    {
        foreach (GameObject row in runtimeRows)
        {
            if (row != null)
            {
                Destroy(row);
            }
        }

        runtimeRows.Clear();

        if (rowTemplate == null || listRoot == null)
        {
            return;
        }

        foreach (ArchiveEntryData entry in entries)
        {
            if (entry.category != activeCategory)
            {
                continue;
            }

            Button row = Instantiate(rowTemplate, listRoot);
            row.gameObject.SetActive(true);
            row.name = "ArchiveRow_" + entry.id;
            runtimeRows.Add(row.gameObject);

            bool unlocked = IsUnlocked(entry);
            Text label = row.GetComponentInChildren<Text>(true);

            if (label != null)
            {
                label.text = unlocked
                    ? $"{entry.id}   {entry.title}"
                    : $"LOCKED   阶段 {entry.unlockStage} 解密";
                label.color = unlocked
                    ? new Color(0.76f, 0.96f, 1f, 1f)
                    : new Color(0.38f, 0.52f, 0.56f, 1f);
            }

            Image background = row.GetComponent<Image>();

            if (background != null)
            {
                background.color = unlocked
                    ? new Color(0.035f, 0.16f, 0.19f, 0.96f)
                    : new Color(0.035f, 0.07f, 0.08f, 0.9f);
            }

            ArchiveEntryData capturedEntry = entry;
            row.onClick.AddListener(
                () => SelectEntry(capturedEntry)
            );
        }

        UpdateCollectionStatus();
    }

    private void SelectEntry(ArchiveEntryData entry)
    {
        selectedEntry = entry;

        if (entry == null)
        {
            return;
        }

        bool unlocked = IsUnlocked(entry);

        if (entryIdText != null)
        {
            entryIdText.text = unlocked ? entry.id : "ACCESS DENIED";
        }

        if (entryTitleText != null)
        {
            entryTitleText.text = unlocked ? entry.title : "档案尚未解密";
        }

        if (entrySubtitleText != null)
        {
            entrySubtitleText.text = unlocked
                ? entry.subtitle
                : "继续完成地球修复任务以恢复数据";
        }

        if (classificationText != null)
        {
            classificationText.text = unlocked
                ? entry.classification
                : "ENCRYPTED // RESTORATION STAGE REQUIRED";
        }

        if (bodyText != null)
        {
            bodyText.text = unlocked
                ? entry.body
                : "这份资料在空间站数据库中仍处于加密状态。\n\n" +
                  $"完成第 {entry.unlockStage} 阶段修复后自动解锁。";
        }

        if (lockHintText != null)
        {
            lockHintText.text = unlocked
                ? "ARCHIVE VERIFIED  //  本地副本完整"
                : $"LOCKED  //  当前阶段 {CurrentUnlockStage}/3";
            lockHintText.color = unlocked
                ? new Color(0.35f, 1f, 0.78f, 1f)
                : new Color(1f, 0.58f, 0.34f, 1f);
        }
    }

    private void UpdateCollectionStatus()
    {
        int unlocked = 0;
        int total = 0;

        foreach (ArchiveEntryData entry in entries)
        {
            if (entry.category != activeCategory)
            {
                continue;
            }

            total++;

            if (IsUnlocked(entry))
            {
                unlocked++;
            }
        }

        if (sectionTitleText != null)
        {
            sectionTitleText.text =
                activeCategory == ArchiveCategory.Civilization
                    ? "地球文明图鉴"
                    : "历代修复官档案";
        }

        if (collectionStatusText != null)
        {
            collectionStatusText.text =
                activeCategory == ArchiveCategory.Civilization
                    ? $"已收录 {unlocked}/{total}  ·  修复阶段 {CurrentUnlockStage}/3"
                    : $"重点档案 {unlocked}/{total}  ·  全库索引 78 任";
        }
    }

    private void UpdateTabVisuals()
    {
        SetTabState(
            civilizationTab,
            civilizationTabText,
            activeCategory == ArchiveCategory.Civilization
        );
        SetTabState(
            officerTab,
            officerTabText,
            activeCategory == ArchiveCategory.RepairOfficer
        );
    }

    private static void SetTabState(
        Button button,
        Text label,
        bool selected
    )
    {
        if (button != null)
        {
            Image image = button.GetComponent<Image>();

            if (image != null)
            {
                image.color = selected
                    ? new Color(0.02f, 0.48f, 0.58f, 0.98f)
                    : new Color(0.025f, 0.1f, 0.13f, 0.96f);
            }
        }

        if (label != null)
        {
            label.color = selected
                ? Color.white
                : new Color(0.48f, 0.7f, 0.74f, 1f);
        }
    }

    private bool IsUnlocked(ArchiveEntryData entry)
    {
        if (entry == null)
        {
            return false;
        }

        bool isLegacyFamilyFile =
            entry.id == "OFF-077" || entry.id == "OFF-078";

        return
            CurrentUnlockStage >= entry.unlockStage ||
            (legacyOfficerArchivesUnlocked && isLegacyFamilyFile);
    }

    private void HandleRepairStageChanged(int stage)
    {
        if (IsOpen)
        {
            ShowCategory(activeCategory);
        }
    }

    private void ResolveSceneReferences()
    {
        if (playerInteractor == null)
        {
            playerInteractor = FindObjectOfType<PlayerInteractor>();
        }

        if (flowController == null)
        {
            flowController = FindObjectOfType<MVPFlowController>();
        }
    }

    private void ReleaseOwnedPlayerLock()
    {
        if (!ownsPlayerLock)
        {
            return;
        }

        if (playerInteractor != null)
        {
            playerInteractor.SetPlayerControlLocked(false);
        }

        ownsPlayerLock = false;
    }

    private void ApplyRuntimeFont()
    {
        if (runtimeChineseFont == null)
        {
            runtimeChineseFont = Font.CreateDynamicFontFromOSFont(
                new[]
                {
                    "PingFang SC",
                    "Hiragino Sans GB",
                    "Arial"
                },
                32
            );
        }

        if (runtimeChineseFont == null)
        {
            return;
        }

        foreach (Text text in GetComponentsInChildren<Text>(true))
        {
            text.font = runtimeChineseFont;
        }
    }
}
