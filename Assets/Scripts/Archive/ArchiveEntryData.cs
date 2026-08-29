using System;

public enum ArchiveCategory
{
    Civilization,
    RepairOfficer
}

[Serializable]
public class ArchiveEntryData
{
    public ArchiveCategory category;
    public string id;
    public string title;
    public string subtitle;
    public string classification;
    public string body;
    public int unlockStage;

    public ArchiveEntryData(
        ArchiveCategory entryCategory,
        string entryId,
        string entryTitle,
        string entrySubtitle,
        string entryClassification,
        string entryBody,
        int requiredStage
    )
    {
        category = entryCategory;
        id = entryId;
        title = entryTitle;
        subtitle = entrySubtitle;
        classification = entryClassification;
        body = entryBody;
        unlockStage = requiredStage;
    }
}
