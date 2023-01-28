using System.Collections.Generic;

namespace Game.Misc;

public class QuestMenu
{
    private readonly List<QuestMenuItem> _questMenuItems = new();

    public void AddMenuItem(uint QuestId, byte Icon)
    {
        if (Global.ObjectMgr.GetQuestTemplate(QuestId) == null)
            return;

        QuestMenuItem questMenuItem = new();

        questMenuItem.QuestId   = QuestId;
        questMenuItem.QuestIcon = Icon;

        _questMenuItems.Add(questMenuItem);
    }

    private bool HasItem(uint questId)
    {
        foreach (var item in _questMenuItems)
            if (item.QuestId == questId)
                return true;

        return false;
    }

    public void ClearMenu()
    {
        _questMenuItems.Clear();
    }

    public int GetMenuItemCount()
    {
        return _questMenuItems.Count;
    }

    public bool IsEmpty()
    {
        return _questMenuItems.Empty();
    }

    public QuestMenuItem GetItem(int index)
    {
        return _questMenuItems.LookupByIndex(index);
    }
}