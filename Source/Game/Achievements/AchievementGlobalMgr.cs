using System;
using System.Collections.Generic;
using Framework.Constants;
using Framework.Database;
using Game.DataStorage;

namespace Game.Achievements;

public class AchievementGlobalMgr : Singleton<AchievementGlobalMgr>
{
    // store achievements by referenced Achievement Id to speed up lookup
    private readonly MultiMap<uint, AchievementRecord> _achievementListByReferencedId = new();
    private readonly Dictionary<uint, AchievementRewardLocale> _achievementRewardLocales = new();

    private readonly Dictionary<uint, AchievementReward> _achievementRewards = new();
    private readonly Dictionary<uint, uint> _achievementScripts = new();

    // store realm first achievements
    private readonly Dictionary<uint /*achievementId*/, DateTime /*completionTime*/> _allCompletedAchievements = new();

    private AchievementGlobalMgr()
    {
    }

    public List<AchievementRecord> GetAchievementByReferencedId(uint id)
    {
        return _achievementListByReferencedId.LookupByKey(id);
    }

    public AchievementReward GetAchievementReward(AchievementRecord achievement)
    {
        return _achievementRewards.LookupByKey(achievement.Id);
    }

    public AchievementRewardLocale GetAchievementRewardLocale(AchievementRecord achievement)
    {
        return _achievementRewardLocales.LookupByKey(achievement.Id);
    }

    public bool IsRealmCompleted(AchievementRecord achievement)
    {
        var time = _allCompletedAchievements.LookupByKey(achievement.Id);

        if (time == default)
            return false;

        if (time == DateTime.MinValue)
            return false;

        if (time == DateTime.MaxValue)
            return true;

        // Allow completing the realm first kill for entire minute after first person did it
        // it may allow more than one group to achieve it (highly unlikely)
        // but apparently this is how blizz handles it as well
        if (achievement.Flags.HasAnyFlag(AchievementFlags.RealmFirstKill))
            return (DateTime.Now - time) > TimeSpan.FromMinutes(1);

        return true;
    }

    public void SetRealmCompleted(AchievementRecord achievement)
    {
        if (IsRealmCompleted(achievement))
            return;

        _allCompletedAchievements[achievement.Id] = DateTime.Now;
    }

    //==========================================================
    public void LoadAchievementReferenceList()
    {
        uint oldMSTime = Time.GetMSTime();

        if (CliDB.AchievementStorage.Empty())
        {
            Log.outInfo(LogFilter.ServerLoading, "Loaded 0 Achievement references.");

            return;
        }

        uint count = 0;

        foreach (var achievement in CliDB.AchievementStorage.Values)
        {
            if (achievement.SharesCriteria == 0)
                continue;

            _achievementListByReferencedId.Add(achievement.SharesCriteria, achievement);
            ++count;
        }

        // Once Bitten, Twice Shy (10 player) - Icecrown Citadel
        AchievementRecord achievement1 = CliDB.AchievementStorage.LookupByKey(4539);

        if (achievement1 != null)
            achievement1.InstanceID = 631; // Correct map requirement (currently has Ulduar); 6.0.3 note - it STILL has ulduar requirement

        Log.outInfo(LogFilter.ServerLoading, "Loaded {0} Achievement references in {1} ms.", count, Time.GetMSTimeDiffToNow(oldMSTime));
    }

    public void LoadAchievementScripts()
    {
        uint oldMSTime = Time.GetMSTime();

        _achievementScripts.Clear(); // need for reload case

        SQLResult result = DB.World.Query("SELECT AchievementId, ScriptName FROM achievement_scripts");

        if (result.IsEmpty())
        {
            Log.outInfo(LogFilter.ServerLoading, "Loaded 0 Achievement scripts. DB table `achievement_scripts` is empty.");

            return;
        }

        do
        {
            uint   achievementId = result.Read<uint>(0);
            string scriptName    = result.Read<string>(1);

            AchievementRecord achievement = CliDB.AchievementStorage.LookupByKey(achievementId);

            if (achievement == null)
            {
                Log.outError(LogFilter.Sql, $"Table `achievement_scripts` contains non-existing Achievement (ID: {achievementId}), skipped.");

                continue;
            }

            _achievementScripts[achievementId] = Global.ObjectMgr.GetScriptId(scriptName);
        } while (result.NextRow());

        Log.outInfo(LogFilter.ServerLoading, $"Loaded {_achievementScripts.Count} Achievement scripts in {Time.GetMSTimeDiffToNow(oldMSTime)} ms");
    }

    public void LoadCompletedAchievements()
    {
        uint oldMSTime = Time.GetMSTime();

        // Populate _allCompletedAchievements with all realm first Achievement ids to make multithreaded access safer
        // while it will not prevent races, it will prevent crashes that happen because std::unordered_map key was added
        // instead the only potential race will happen on value associated with the key
        foreach (AchievementRecord achievement in CliDB.AchievementStorage.Values)
            if (achievement.Flags.HasAnyFlag(AchievementFlags.RealmFirstReach | AchievementFlags.RealmFirstKill))
                _allCompletedAchievements[achievement.Id] = DateTime.MinValue;

        SQLResult result = DB.Characters.Query("SELECT Achievement FROM character_achievement GROUP BY Achievement");

        if (result.IsEmpty())
        {
            Log.outInfo(LogFilter.ServerLoading, "Loaded 0 realm first completed achievements. DB table `character_achievement` is empty.");

            return;
        }

        do
        {
            uint              achievementId = result.Read<uint>(0);
            AchievementRecord achievement   = CliDB.AchievementStorage.LookupByKey(achievementId);

            if (achievement == null)
            {
                // Remove non-existing achievements from all characters
                Log.outError(LogFilter.Achievement, "Non-existing Achievement {0} _data has been removed from the table `character_achievement`.", achievementId);

                PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_INVALID_ACHIEVMENT);
                stmt.AddValue(0, achievementId);
                DB.Characters.Execute(stmt);

                continue;
            }
            else if (achievement.Flags.HasAnyFlag(AchievementFlags.RealmFirstReach | AchievementFlags.RealmFirstKill))
            {
                _allCompletedAchievements[achievementId] = DateTime.MaxValue;
            }
        } while (result.NextRow());

        Log.outInfo(LogFilter.ServerLoading, "Loaded {0} realm first completed achievements in {1} ms.", _allCompletedAchievements.Count, Time.GetMSTimeDiffToNow(oldMSTime));
    }

    public void LoadRewards()
    {
        uint oldMSTime = Time.GetMSTime();

        _achievementRewards.Clear(); // need for reload case

        //                                         0   1       2       3       4       5        6     7
        SQLResult result = DB.World.Query("SELECT ID, TitleA, TitleH, ItemID, Sender, Subject, Body, MailTemplateID FROM achievement_reward");

        if (result.IsEmpty())
        {
            Log.outInfo(LogFilter.ServerLoading, ">> Loaded 0 Achievement rewards. DB table `achievement_reward` is empty.");

            return;
        }

        do
        {
            uint              id          = result.Read<uint>(0);
            AchievementRecord achievement = CliDB.AchievementStorage.LookupByKey(id);

            if (achievement == null)
            {
                Log.outError(LogFilter.Sql, $"Table `achievement_reward` contains a wrong Achievement ID ({id}), ignored.");

                continue;
            }

            AchievementReward reward = new();
            reward.TitleId[0]       = result.Read<uint>(1);
            reward.TitleId[1]       = result.Read<uint>(2);
            reward.ItemId           = result.Read<uint>(3);
            reward.SenderCreatureId = result.Read<uint>(4);
            reward.Subject          = result.Read<string>(5);
            reward.Body             = result.Read<string>(6);
            reward.MailTemplateId   = result.Read<uint>(7);

            // must be title or mail at least
            if (reward.TitleId[0] == 0 &&
                reward.TitleId[1] == 0 &&
                reward.SenderCreatureId == 0)
            {
                Log.outError(LogFilter.Sql, $"Table `achievement_reward` (ID: {id}) does not contain title or Item reward _data. Ignored.");

                continue;
            }

            if (achievement.Faction == AchievementFaction.Any &&
                ((reward.TitleId[0] == 0) ^ (reward.TitleId[1] == 0)))
                Log.outError(LogFilter.Sql, $"Table `achievement_reward` (ID: {id}) contains the title (A: {reward.TitleId[0]} H: {reward.TitleId[1]}) for only one team.");

            if (reward.TitleId[0] != 0)
            {
                CharTitlesRecord titleEntry = CliDB.CharTitlesStorage.LookupByKey(reward.TitleId[0]);

                if (titleEntry == null)
                {
                    Log.outError(LogFilter.Sql, $"Table `achievement_reward` (ID: {id}) contains an invalid title ID ({reward.TitleId[0]}) in `title_A`, set to 0");
                    reward.TitleId[0] = 0;
                }
            }

            if (reward.TitleId[1] != 0)
            {
                CharTitlesRecord titleEntry = CliDB.CharTitlesStorage.LookupByKey(reward.TitleId[1]);

                if (titleEntry == null)
                {
                    Log.outError(LogFilter.Sql, $"Table `achievement_reward` (ID: {id}) contains an invalid title ID ({reward.TitleId[1]}) in `title_H`, set to 0");
                    reward.TitleId[1] = 0;
                }
            }

            //check mail _data before Item for report including wrong Item case
            if (reward.SenderCreatureId != 0)
            {
                if (Global.ObjectMgr.GetCreatureTemplate(reward.SenderCreatureId) == null)
                {
                    Log.outError(LogFilter.Sql, $"Table `achievement_reward` (ID: {id}) contains an invalid creature ID {reward.SenderCreatureId} as sender, mail reward skipped.");
                    reward.SenderCreatureId = 0;
                }
            }
            else
            {
                if (reward.ItemId != 0)
                    Log.outError(LogFilter.Sql, $"Table `achievement_reward` (ID: {id}) does not have sender _data, but contains an Item reward. Item will not be rewarded.");

                if (!reward.Subject.IsEmpty())
                    Log.outError(LogFilter.Sql, $"Table `achievement_reward` (ID: {id}) does not have sender _data, but contains a mail subject.");

                if (!reward.Body.IsEmpty())
                    Log.outError(LogFilter.Sql, $"Table `achievement_reward` (ID: {id}) does not have sender _data, but contains mail text.");

                if (reward.MailTemplateId != 0)
                    Log.outError(LogFilter.Sql, $"Table `achievement_reward` (ID: {id}) does not have sender _data, but has a MailTemplateId.");
            }

            if (reward.MailTemplateId != 0)
            {
                if (!CliDB.MailTemplateStorage.ContainsKey(reward.MailTemplateId))
                {
                    Log.outError(LogFilter.Sql, $"Table `achievement_reward` (ID: {id}) is using an invalid MailTemplateId ({reward.MailTemplateId}).");
                    reward.MailTemplateId = 0;
                }
                else if (!reward.Subject.IsEmpty() ||
                         !reward.Body.IsEmpty())
                {
                    Log.outError(LogFilter.Sql, $"Table `achievement_reward` (ID: {id}) is using MailTemplateId ({reward.MailTemplateId}) and mail subject/text.");
                }
            }

            if (reward.ItemId != 0)
                if (Global.ObjectMgr.GetItemTemplate(reward.ItemId) == null)
                {
                    Log.outError(LogFilter.Sql, $"Table `achievement_reward` (ID: {id}) contains an invalid Item Id {reward.ItemId}, reward mail will not contain the rewarded Item.");
                    reward.ItemId = 0;
                }

            _achievementRewards[id] = reward;
        } while (result.NextRow());

        Log.outInfo(LogFilter.ServerLoading, "Loaded {0} Achievement rewards in {1} ms.", _achievementRewards.Count, Time.GetMSTimeDiffToNow(oldMSTime));
    }

    public void LoadRewardLocales()
    {
        uint oldMSTime = Time.GetMSTime();

        _achievementRewardLocales.Clear(); // need for reload case

        //                                         0   1       2        3
        SQLResult result = DB.World.Query("SELECT ID, Locale, Subject, Body FROM achievement_reward_locale");

        if (result.IsEmpty())
        {
            Log.outInfo(LogFilter.ServerLoading, "Loaded 0 Achievement reward locale strings.  DB table `achievement_reward_locale` is empty.");

            return;
        }

        do
        {
            uint   id         = result.Read<uint>(0);
            string localeName = result.Read<string>(1);

            if (!_achievementRewards.ContainsKey(id))
            {
                Log.outError(LogFilter.Sql, $"Table `achievement_reward_locale` (ID: {id}) contains locale strings for a non-existing Achievement reward.");

                continue;
            }

            AchievementRewardLocale data   = new();
            Locale                  locale = localeName.ToEnum<Locale>();

            if (!SharedConst.IsValidLocale(locale) ||
                locale == Locale.enUS)
                continue;

            ObjectManager.AddLocaleString(result.Read<string>(2), locale, data.Subject);
            ObjectManager.AddLocaleString(result.Read<string>(3), locale, data.Body);

            _achievementRewardLocales[id] = data;
        } while (result.NextRow());

        Log.outInfo(LogFilter.ServerLoading, "Loaded {0} Achievement reward locale strings in {1} ms.", _achievementRewardLocales.Count, Time.GetMSTimeDiffToNow(oldMSTime));
    }

    public uint GetAchievementScriptId(uint achievementId)
    {
        return _achievementScripts.LookupByKey(achievementId);
    }
}