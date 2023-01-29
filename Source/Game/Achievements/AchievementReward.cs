namespace Game.Achievements;

public class AchievementReward
{
    public string Body { get; set; }
    public uint ItemId { get; set; }
    public uint MailTemplateId { get; set; }
    public uint SenderCreatureId { get; set; }
    public string Subject { get; set; }
    public uint[] TitleId { get; set; } = new uint[2];
}