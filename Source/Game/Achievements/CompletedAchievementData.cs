using System.Collections.Generic;
using Game.Entities;

namespace Game.Achievements;

public class CompletedAchievementData
{
    public bool Changed { get; set; }
    public List<ObjectGuid> CompletingPlayers { get; set; } = new();
    public long Date { get; set; }
}