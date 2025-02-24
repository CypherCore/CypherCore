// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Collections;
using Framework.Constants;
using Framework.Database;
using Game.Chat;
using Game.DataStorage;
using Game.Entities;
using Game.Groups;
using Game.Guilds;
using Game.Mails;
using Game.Maps;
using Game.Networking;
using Game.Networking.Packets;
using System;
using System.Collections.Generic;
using System.Text;

namespace Game.Achievements
{
    public class AchievementManager : CriteriaHandler
    {
        protected Dictionary<uint, CompletedAchievementData> _completedAchievements = new();
        protected uint _achievementPoints;

        /// <summary>
        /// called at player login. The player might have fulfilled some achievements when the achievement system wasn't working yet
        /// </summary>
        /// <param name="referencePlayer"></param>
        public void CheckAllAchievementCriteria(Player referencePlayer)
        {
            // suppress sending packets
            foreach (CriteriaType criteriaType in CriteriaManager.GetRetroactivelyUpdateableCriteriaTypes())
                UpdateCriteria(criteriaType, 0, 0, 0, null, referencePlayer);
        }

        public bool HasAchieved(uint achievementId)
        {
            return _completedAchievements.ContainsKey(achievementId);
        }

        public uint GetAchievementPoints()
        {
            return _achievementPoints;
        }

        public ICollection<uint> GetCompletedAchievementIds()
        {
            return _completedAchievements.Keys;
        }

        public override bool CanUpdateCriteriaTree(Criteria criteria, CriteriaTree tree, Player referencePlayer)
        {
            AchievementRecord achievement = tree.Achievement;
            if (achievement == null)
                return false;

            if (HasAchieved(achievement.Id))
            {
                Log.outTrace(LogFilter.Achievement, "CanUpdateCriteriaTree: (Id: {0} Type {1} Achievement {2}) Achievement already earned",
                    criteria.Id, criteria.Entry.Type, achievement.Id);
                return false;
            }

            if ((achievement.Faction == AchievementFaction.Horde && referencePlayer.GetTeam() != Team.Horde) ||
                (achievement.Faction == AchievementFaction.Alliance && referencePlayer.GetTeam() != Team.Alliance))
            {
                Log.outTrace(LogFilter.Achievement, "CanUpdateCriteriaTree: (Id: {0} Type {1} Achievement {2}) Wrong faction",
                    criteria.Id, criteria.Entry.Type, achievement.Id);
                return false;
            }

            // Don't update realm first achievements if the player's account isn't allowed to do so
            if (achievement.Flags.HasAnyFlag(AchievementFlags.RealmFirstReach | AchievementFlags.RealmFirstKill))
                if (referencePlayer.GetSession().HasPermission(RBACPermissions.CannotEarnRealmFirstAchievements))
                    return false;

            if (achievement.CovenantID != 0 && referencePlayer.m_playerData.CovenantID != achievement.CovenantID)
            {
                Log.outTrace(LogFilter.Achievement, $"CanUpdateCriteriaTree: (Id: {criteria.Id} Type {criteria.Entry.Type} Achievement {achievement.Id}) Wrong covenant");
                return false;
            }

            return base.CanUpdateCriteriaTree(criteria, tree, referencePlayer);
        }

        public override bool CanCompleteCriteriaTree(CriteriaTree tree)
        {
            AchievementRecord achievement = tree.Achievement;
            if (achievement == null)
                return false;

            // counter can never complete
            if (achievement.Flags.HasAnyFlag(AchievementFlags.Counter))
                return false;

            if (achievement.Flags.HasAnyFlag(AchievementFlags.RealmFirstReach | AchievementFlags.RealmFirstKill))
            {
                // someone on this realm has already completed that achievement
                if (Global.AchievementMgr.IsRealmCompleted(achievement))
                    return false;
            }

            return true;
        }

        public override void CompletedCriteriaTree(CriteriaTree tree, Player referencePlayer)
        {
            AchievementRecord achievement = tree.Achievement;
            if (achievement == null)
                return;

            // counter can never complete
            if (achievement.Flags.HasAnyFlag(AchievementFlags.Counter))
                return;

            // already completed and stored
            if (HasAchieved(achievement.Id))
                return;

            if (IsCompletedAchievement(achievement))
                CompletedAchievement(achievement, referencePlayer);
        }

        public override void AfterCriteriaTreeUpdate(CriteriaTree tree, Player referencePlayer)
        {
            AchievementRecord achievement = tree.Achievement;
            if (achievement == null)
                return;

            // check again the completeness for SUMM and REQ COUNT achievements,
            // as they don't depend on the completed criteria but on the sum of the progress of each individual criteria
            if (achievement.Flags.HasAnyFlag(AchievementFlags.Summ))
                if (IsCompletedAchievement(achievement))
                    CompletedAchievement(achievement, referencePlayer);

            var achRefList = Global.AchievementMgr.GetAchievementByReferencedId(achievement.Id);
            foreach (AchievementRecord refAchievement in achRefList)
                if (IsCompletedAchievement(refAchievement))
                    CompletedAchievement(refAchievement, referencePlayer);
        }

        bool IsCompletedAchievement(AchievementRecord entry)
        {
            // counter can never complete
            if (entry.Flags.HasAnyFlag(AchievementFlags.Counter))
                return false;

            CriteriaTree tree = Global.CriteriaMgr.GetCriteriaTree(entry.CriteriaTree);
            if (tree == null)
                return false;

            // For SUMM achievements, we have to count the progress of each criteria of the achievement.
            // Oddly, the target count is NOT contained in the achievement, but in each individual criteria
            if (entry.Flags.HasAnyFlag(AchievementFlags.Summ))
            {
                long progress = 0;
                CriteriaManager.WalkCriteriaTree(tree, criteriaTree =>
                {
                    if (criteriaTree.Criteria != null)
                    {
                        CriteriaProgress criteriaProgress = GetCriteriaProgress(criteriaTree.Criteria);
                        if (criteriaProgress != null)
                            progress += (long)criteriaProgress.Counter;
                    }
                });
                return progress >= tree.Entry.Amount;
            }

            return IsCompletedCriteriaTree(tree);
        }

        public override bool RequiredAchievementSatisfied(uint achievementId)
        {
            return HasAchieved(achievementId);
        }

        public virtual void CompletedAchievement(AchievementRecord entry, Player referencePlayer) { }

        public Func<uint, AchievementRecord> VisibleAchievementCheck = id =>
        {
            AchievementRecord achievement = CliDB.AchievementStorage.LookupByKey(id);
            if (achievement != null && !achievement.Flags.HasAnyFlag(AchievementFlags.Hidden))
                return achievement;
            return null;
        };
    }

    public class PlayerAchievementMgr : AchievementManager
    {
        Player _owner;

        public PlayerAchievementMgr(Player owner)
        {
            _owner = owner;
        }

        public override void Reset()
        {
            base.Reset();

            foreach (var iter in _completedAchievements)
            {
                AchievementDeleted achievementDeleted = new();
                achievementDeleted.AchievementID = iter.Key;
                SendPacket(achievementDeleted);
            }

            _completedAchievements.Clear();
            _achievementPoints = 0;
            DeleteFromDB(_owner.GetGUID());

            // re-fill data
            CheckAllAchievementCriteria(_owner);
        }

        public static void DeleteFromDB(ObjectGuid guid)
        {
            SQLTransaction trans = new();

            PreparedStatement stmt = CharacterDatabase.GetPreparedStatement(CharStatements.DEL_CHAR_ACHIEVEMENT);
            stmt.AddValue(0, guid.GetCounter());
            DB.Characters.Execute(stmt);

            stmt = CharacterDatabase.GetPreparedStatement(CharStatements.DEL_CHAR_ACHIEVEMENT_PROGRESS);
            stmt.AddValue(0, guid.GetCounter());
            DB.Characters.Execute(stmt);

            DB.Characters.CommitTransaction(trans);
        }

        public void LoadFromDB(SQLResult achievementResult, SQLResult criteriaResult)
        {
            if (!achievementResult.IsEmpty())
            {
                do
                {
                    uint achievementid = achievementResult.Read<uint>(0);

                    // must not happen: cleanup at server startup in sAchievementMgr.LoadCompletedAchievements()
                    AchievementRecord achievement = CliDB.AchievementStorage.LookupByKey(achievementid);
                    if (achievement == null)
                        continue;

                    CompletedAchievementData ca = new();
                    ca.Date = achievementResult.Read<long>(1);
                    ca.Changed = false;

                    _achievementPoints += achievement.Points;

                    // title achievement rewards are retroactive
                    var reward = Global.AchievementMgr.GetAchievementReward(achievement);
                    if (reward != null)
                    {
                        uint titleId = reward.TitleId[Player.TeamForRace(_owner.GetRace()) == Team.Alliance ? 0 : 1];
                        if (titleId != 0)
                        {
                            CharTitlesRecord titleEntry = CliDB.CharTitlesStorage.LookupByKey(titleId);
                            if (titleEntry != null)
                                _owner.SetTitle(titleEntry);
                        }
                    }
                    _completedAchievements[achievementid] = ca;

                } while (achievementResult.NextRow());
            }

            if (!criteriaResult.IsEmpty())
            {
                var now = GameTime.GetGameTime();
                do
                {
                    uint id = criteriaResult.Read<uint>(0);
                    ulong counter = criteriaResult.Read<ulong>(1);
                    long date = criteriaResult.Read<long>(2);

                    Criteria criteria = Global.CriteriaMgr.GetCriteria(id);
                    if (criteria == null)
                    {
                        // Removing non-existing criteria data for all characters
                        Log.outError(LogFilter.Achievement, "Non-existing achievement criteria {0} data removed from table `character_achievement_progress`.", id);

                        PreparedStatement stmt = CharacterDatabase.GetPreparedStatement(CharStatements.DEL_INVALID_ACHIEV_PROGRESS_CRITERIA);
                        stmt.AddValue(0, id);
                        DB.Characters.Execute(stmt);
                        continue;
                    }

                    if (criteria.Entry.StartTimer != 0 && (date + criteria.Entry.StartTimer) < now)
                        continue;

                    CriteriaProgress progress = new();
                    progress.Counter = counter;
                    progress.Date = date;
                    progress.PlayerGUID = _owner.GetGUID();
                    progress.Changed = false;

                    _criteriaProgress[id] = progress;

                } while (criteriaResult.NextRow());
            }
        }

        public void SaveToDB(SQLTransaction trans)
        {
            if (!_completedAchievements.Empty())
            {
                foreach (var pair in _completedAchievements)
                {
                    if (!pair.Value.Changed)
                        continue;

                    PreparedStatement stmt = CharacterDatabase.GetPreparedStatement(CharStatements.DEL_CHAR_ACHIEVEMENT_BY_ACHIEVEMENT);
                    stmt.AddValue(0, pair.Key);
                    stmt.AddValue(1, _owner.GetGUID().GetCounter());
                    trans.Append(stmt);

                    stmt = CharacterDatabase.GetPreparedStatement(CharStatements.INS_CHAR_ACHIEVEMENT);
                    stmt.AddValue(0, _owner.GetGUID().GetCounter());
                    stmt.AddValue(1, pair.Key);
                    stmt.AddValue(2, pair.Value.Date);
                    trans.Append(stmt);

                    pair.Value.Changed = false;
                }
            }

            if (!_criteriaProgress.Empty())
            {
                foreach (var pair in _criteriaProgress)
                {
                    if (!pair.Value.Changed)
                        continue;

                    PreparedStatement stmt = CharacterDatabase.GetPreparedStatement(CharStatements.DEL_CHAR_ACHIEVEMENT_PROGRESS_BY_CRITERIA);
                    stmt.AddValue(0, _owner.GetGUID().GetCounter());
                    stmt.AddValue(1, pair.Key);
                    trans.Append(stmt);

                    if (pair.Value.Counter != 0)
                    {
                        stmt = CharacterDatabase.GetPreparedStatement(CharStatements.INS_CHAR_ACHIEVEMENT_PROGRESS);
                        stmt.AddValue(0, _owner.GetGUID().GetCounter());
                        stmt.AddValue(1, pair.Key);
                        stmt.AddValue(2, pair.Value.Counter);
                        stmt.AddValue(3, pair.Value.Date);
                        trans.Append(stmt);
                    }

                    pair.Value.Changed = false;
                }
            }
        }

        public override void SendAllData(Player receiver)
        {
            AllAccountCriteria allAccountCriteria = new();
            AllAchievementData achievementData = new();

            foreach (var (id, completedAchievement) in _completedAchievements)
            {
                AchievementRecord achievement = VisibleAchievementCheck(id);
                if (achievement == null)
                    continue;

                EarnedAchievement earned = new();
                earned.Id = id;
                earned.Date.SetUtcTimeFromUnixTime(completedAchievement.Date);
                if (!achievement.Flags.HasAnyFlag(AchievementFlags.Account))
                {
                    earned.Owner = _owner.GetGUID();
                    earned.VirtualRealmAddress = earned.NativeRealmAddress = _owner.m_playerData.VirtualPlayerRealm;
                }

                achievementData.Data.Earned.Add(earned);
            }

            foreach (var (id, criteriaProgres) in _criteriaProgress)
            {
                Criteria criteria = Global.CriteriaMgr.GetCriteria(id);

                CriteriaProgressPkt progress = new();
                progress.Id = id;
                progress.Quantity = criteriaProgres.Counter;
                progress.Player = criteriaProgres.PlayerGUID;
                progress.Flags = 0;
                progress.Date.SetUtcTimeFromUnixTime(criteriaProgres.Date);
                progress.Date += _owner.GetSession().GetTimezoneOffset();
                progress.TimeFromStart = 0;
                progress.TimeFromCreate = 0;
                achievementData.Data.Progress.Add(progress);

                if (criteria.FlagsCu.HasAnyFlag(CriteriaFlagsCu.Account))
                {
                    CriteriaProgressPkt accountProgress = new();
                    accountProgress.Id = id;
                    accountProgress.Quantity = criteriaProgres.Counter;
                    accountProgress.Player = _owner.GetSession().GetBattlenetAccountGUID();
                    accountProgress.Flags = 0;
                    accountProgress.Date.SetUtcTimeFromUnixTime(criteriaProgres.Date);
                    accountProgress.Date += _owner.GetSession().GetTimezoneOffset();
                    accountProgress.TimeFromStart = 0;
                    accountProgress.TimeFromCreate = 0;
                    allAccountCriteria.Progress.Add(accountProgress);
                }
            }

            if (!allAccountCriteria.Progress.Empty())
                SendPacket(allAccountCriteria);

            SendPacket(achievementData);
        }

        public void SendAchievementInfo(Player receiver)
        {
            RespondInspectAchievements inspectedAchievements = new();
            inspectedAchievements.Player = _owner.GetGUID();

            foreach (var (id, completedAchievement) in _completedAchievements)
            {
                AchievementRecord achievement = VisibleAchievementCheck(id);
                if (achievement == null)
                    continue;

                EarnedAchievement earned = new();
                earned.Id = id;
                earned.Date.SetUtcTimeFromUnixTime(completedAchievement.Date);
                earned.Date += receiver.GetSession().GetTimezoneOffset();
                if (!achievement.Flags.HasAnyFlag(AchievementFlags.Account))
                {
                    earned.Owner = _owner.GetGUID();
                    earned.VirtualRealmAddress = earned.NativeRealmAddress = _owner.m_playerData.VirtualPlayerRealm;
                }

                inspectedAchievements.Data.Earned.Add(earned);
            }

            foreach (var (id, criteriaProgres) in _criteriaProgress)
            {
                CriteriaProgressPkt progress = new();
                progress.Id = id;
                progress.Quantity = criteriaProgres.Counter;
                progress.Player = criteriaProgres.PlayerGUID;
                progress.Flags = 0;
                progress.Date.SetUtcTimeFromUnixTime(criteriaProgres.Date);
                progress.Date += receiver.GetSession().GetTimezoneOffset();
                progress.TimeFromStart = 0;
                progress.TimeFromCreate = 0;
                inspectedAchievements.Data.Progress.Add(progress);

            }
            receiver.SendPacket(inspectedAchievements);
        }

        public override void CompletedAchievement(AchievementRecord achievement, Player referencePlayer)
        {
            // Disable for GameMasters with GM-mode enabled or for players that don't have the related RBAC permission
            if (_owner.IsGameMaster() || _owner.GetSession().HasPermission(RBACPermissions.CannotEarnAchievements))
                return;

            if ((achievement.Faction == AchievementFaction.Horde && referencePlayer.GetTeam() != Team.Horde) ||
                (achievement.Faction == AchievementFaction.Alliance && referencePlayer.GetTeam() != Team.Alliance))
                return;

            if (achievement.Flags.HasAnyFlag(AchievementFlags.Counter) || HasAchieved(achievement.Id))
                return;

            if (achievement.Flags.HasAnyFlag(AchievementFlags.ShowInGuildNews))
            {
                Guild guild = referencePlayer.GetGuild();
                if (guild != null)
                    guild.AddGuildNews(GuildNews.PlayerAchievement, referencePlayer.GetGUID(), (uint)(achievement.Flags & AchievementFlags.ShowInGuildHeader), achievement.Id);
            }

            if (!_owner.GetSession().PlayerLoading())
                SendAchievementEarned(achievement);

            Log.outDebug(LogFilter.Achievement, "PlayerAchievementMgr.CompletedAchievement({0}). {1}", achievement.Id, GetOwnerInfo());

            CompletedAchievementData ca = new();
            ca.Date = GameTime.GetGameTime();
            ca.Changed = true;
            _completedAchievements[achievement.Id] = ca;

            if (achievement.Flags.HasAnyFlag(AchievementFlags.RealmFirstReach | AchievementFlags.RealmFirstKill))
                Global.AchievementMgr.SetRealmCompleted(achievement);

            if (!achievement.Flags.HasAnyFlag(AchievementFlags.TrackingFlag))
                _achievementPoints += achievement.Points;

            referencePlayer.UpdateCriteria(CriteriaType.EarnAchievement, achievement.Id, 0, 0, null);
            referencePlayer.UpdateCriteria(CriteriaType.EarnAchievementPoints, achievement.Points, 0, 0, null);

            Global.ScriptMgr.OnAchievementCompleted(referencePlayer, achievement);

            // reward items and titles if any
            AchievementReward reward = Global.AchievementMgr.GetAchievementReward(achievement);

            // no rewards
            if (reward == null)
                return;

            // titles
            //! Currently there's only one achievement that deals with gender-specific titles.
            //! Since no common attributes were found, (not even in titleRewardFlags field)
            //! we explicitly check by ID. Maybe in the future we could move the achievement_reward
            //! condition fields to the condition system.
            uint titleId = 0;
            if (achievement.Id == 1793)
                titleId = reward.TitleId[(int)_owner.GetNativeGender()];
            else
            {
                switch (_owner.GetTeam())
                {
                    case Team.Alliance:
                        titleId = reward.TitleId[0];
                        break;
                    case Team.Horde:
                        titleId = reward.TitleId[1];
                        break;
                }
            }

            var titleEntry = CliDB.CharTitlesStorage.LookupByKey(titleId);
            if (titleEntry != null)
                _owner.SetTitle(titleEntry);

            // mail
            if (reward.SenderCreatureId != 0)
            {
                MailDraft draft = new(reward.MailTemplateId);

                if (reward.MailTemplateId == 0)
                {
                    // subject and text
                    string subject = reward.Subject;
                    string text = reward.Body;

                    Locale localeConstant = _owner.GetSession().GetSessionDbLocaleIndex();
                    if (localeConstant != Locale.enUS)
                    {
                        AchievementRewardLocale loc = Global.AchievementMgr.GetAchievementRewardLocale(achievement);
                        if (loc != null)
                        {
                            ObjectManager.GetLocaleString(loc.Subject, localeConstant, ref subject);
                            ObjectManager.GetLocaleString(loc.Body, localeConstant, ref text);
                        }
                    }

                    draft = new MailDraft(subject, text);
                }

                SQLTransaction trans = new();

                Item item = reward.ItemId != 0 ? Item.CreateItem(reward.ItemId, 1, ItemContext.None, _owner) : null;
                if (item != null)
                {
                    // save new item before send
                    item.SaveToDB(trans);                               // save for prevent lost at next mail load, if send fail then item will deleted

                    // item
                    draft.AddItem(item);
                }

                draft.SendMailTo(trans, _owner, new MailSender(MailMessageType.Creature, reward.SenderCreatureId));
                DB.Characters.CommitTransaction(trans);
            }
        }

        public bool ModifierTreeSatisfied(uint modifierTreeId)
        {
            ModifierTreeNode modifierTree = Global.CriteriaMgr.GetModifierTree(modifierTreeId);
            if (modifierTree != null)
                return ModifierTreeSatisfied(modifierTree, 0, 0, null, _owner);

            return false;
        }

        public override void SendCriteriaUpdate(Criteria criteria, CriteriaProgress progress, TimeSpan timeElapsed, bool timedCompleted)
        {
            if (criteria.FlagsCu.HasAnyFlag(CriteriaFlagsCu.Account))
            {
                AccountCriteriaUpdate criteriaUpdate = new();
                criteriaUpdate.Progress.Id = criteria.Id;
                criteriaUpdate.Progress.Quantity = progress.Counter;
                criteriaUpdate.Progress.Player = _owner.GetSession().GetBattlenetAccountGUID();
                criteriaUpdate.Progress.Flags = 0;
                if (criteria.Entry.StartTimer != 0)
                    criteriaUpdate.Progress.Flags = timedCompleted ? 1 : 0u; // 1 is for keeping the counter at 0 in client

                criteriaUpdate.Progress.Date.SetUtcTimeFromUnixTime(progress.Date);
                criteriaUpdate.Progress.Date += _owner.GetSession().GetTimezoneOffset();
                criteriaUpdate.Progress.TimeFromStart = (uint)timeElapsed.TotalSeconds;
                criteriaUpdate.Progress.TimeFromCreate = 0;
                SendPacket(criteriaUpdate);
            }

            if (criteria.FlagsCu.HasAnyFlag(CriteriaFlagsCu.Player))
            {
                CriteriaUpdate criteriaUpdate = new();

                criteriaUpdate.CriteriaID = criteria.Id;
                criteriaUpdate.Quantity = progress.Counter;
                criteriaUpdate.PlayerGUID = _owner.GetGUID();
                criteriaUpdate.Flags = 0;
                if (criteria.Entry.StartTimer != 0)
                    criteriaUpdate.Flags = timedCompleted ? 1 : 0u; // 1 is for keeping the counter at 0 in client

                criteriaUpdate.CurrentTime.SetUtcTimeFromUnixTime(progress.Date);
                criteriaUpdate.CurrentTime += _owner.GetSession().GetTimezoneOffset();
                criteriaUpdate.ElapsedTime = (uint)timeElapsed.TotalSeconds;
                criteriaUpdate.CreationTime = 0;

                SendPacket(criteriaUpdate);
            }
        }

        public override void SendCriteriaProgressRemoved(uint criteriaId)
        {
            CriteriaDeleted criteriaDeleted = new();
            criteriaDeleted.CriteriaID = criteriaId;
            SendPacket(criteriaDeleted);
        }

        void SendAchievementEarned(AchievementRecord achievement)
        {
            // Don't send for achievements with ACHIEVEMENT_FLAG_HIDDEN
            if (achievement.Flags.HasAnyFlag(AchievementFlags.Hidden))
                return;

            Log.outDebug(LogFilter.Achievement, "PlayerAchievementMgr.SendAchievementEarned({0})", achievement.Id);

            if (!achievement.Flags.HasAnyFlag(AchievementFlags.TrackingFlag))
            {
                Guild guild = Global.GuildMgr.GetGuildById(_owner.GetGuildId());
                if (guild != null)
                {
                    BroadcastTextBuilder say_builder = new(_owner, ChatMsg.GuildAchievement, (uint)BroadcastTextIds.AchivementEarned, _owner.GetNativeGender(), _owner, achievement.Id);
                    var say_do = new LocalizedDo(say_builder);
                    guild.BroadcastWorker(say_do, _owner);
                }

                if (achievement.Flags.HasAnyFlag(AchievementFlags.RealmFirstReach | AchievementFlags.RealmFirstKill))
                {
                    // broadcast realm first reached
                    BroadcastAchievement serverFirstAchievement = new();
                    serverFirstAchievement.Name = _owner.GetName();
                    serverFirstAchievement.PlayerGUID = _owner.GetGUID();
                    serverFirstAchievement.AchievementID = achievement.Id;
                    Global.WorldMgr.SendGlobalMessage(serverFirstAchievement);
                }
                // if player is in world he can tell his friends about new achievement
                else if (_owner.IsInWorld)
                {
                    BroadcastTextBuilder _builder = new(_owner, ChatMsg.Achievement, (uint)BroadcastTextIds.AchivementEarned, _owner.GetNativeGender(), _owner, achievement.Id);
                    var _localizer = new LocalizedDo(_builder);
                    var _worker = new PlayerDistWorker(_owner, WorldConfig.GetFloatValue(WorldCfg.ListenRangeSay), _localizer);
                    Cell.VisitWorldObjects(_owner, _worker, WorldConfig.GetFloatValue(WorldCfg.ListenRangeSay));
                }
            }

            var achievementEarnedBuilder = (Player receiver) =>
            {
                AchievementEarned achievementEarned = new();
                achievementEarned.Sender = _owner.GetGUID();
                achievementEarned.Earner = _owner.GetGUID();
                achievementEarned.EarnerNativeRealm = achievementEarned.EarnerVirtualRealm = _owner.m_playerData.VirtualPlayerRealm;
                achievementEarned.AchievementID = achievement.Id;
                achievementEarned.Time = GameTime.GetUtcWowTime();
                achievementEarned.Time += receiver.GetSession().GetTimezoneOffset();
                receiver.SendPacket(achievementEarned);
            };
            
            achievementEarnedBuilder(_owner);

            if (!achievement.Flags.HasAnyFlag(AchievementFlags.TrackingFlag))
            {
                float dist = WorldConfig.GetFloatValue(WorldCfg.ListenRangeSay);
                MessageDistDeliverer notifier = new(_owner, achievementEarnedBuilder, dist);
                Cell.VisitWorldObjects(_owner, notifier, dist);
            }
        }

        public override void SendPacket(ServerPacket data)
        {
            _owner.SendPacket(data);
        }

        public override List<Criteria> GetCriteriaByType(CriteriaType type, uint asset)
        {
            return Global.CriteriaMgr.GetPlayerCriteriaByType(type, asset);
        }

        public override string GetOwnerInfo()
        {
            return $"{_owner.GetGUID()} {_owner.GetName()}";
        }
    }

    public class GuildAchievementMgr : AchievementManager
    {
        Guild _owner;

        public GuildAchievementMgr(Guild owner)
        {
            _owner = owner;
        }

        public override void Reset()
        {
            base.Reset();

            ObjectGuid guid = _owner.GetGUID();
            foreach (var (id, _) in _completedAchievements)
            {
                _owner.BroadcastWorker(receiver =>
                {
                    GuildAchievementDeleted guildAchievementDeleted = new();
                    guildAchievementDeleted.AchievementID = id;
                    guildAchievementDeleted.GuildGUID = guid;
                    guildAchievementDeleted.TimeDeleted = GameTime.GetUtcWowTime();
                    guildAchievementDeleted.TimeDeleted += receiver.GetSession().GetTimezoneOffset();
                    receiver.SendPacket(guildAchievementDeleted);
                });
            }

            _achievementPoints = 0;
            _completedAchievements.Clear();
            DeleteFromDB(guid);
        }

        public static void DeleteFromDB(ObjectGuid guid)
        {
            SQLTransaction trans = new();

            PreparedStatement stmt = CharacterDatabase.GetPreparedStatement(CharStatements.DEL_ALL_GUILD_ACHIEVEMENTS);
            stmt.AddValue(0, guid.GetCounter());
            trans.Append(stmt);

            stmt = CharacterDatabase.GetPreparedStatement(CharStatements.DEL_ALL_GUILD_ACHIEVEMENT_CRITERIA);
            stmt.AddValue(0, guid.GetCounter());
            trans.Append(stmt);

            DB.Characters.CommitTransaction(trans);
        }

        public void LoadFromDB(SQLResult achievementResult, SQLResult criteriaResult)
        {
            if (!achievementResult.IsEmpty())
            {
                do
                {
                    uint achievementid = achievementResult.Read<uint>(0);

                    // must not happen: cleanup at server startup in sAchievementMgr.LoadCompletedAchievements()
                    AchievementRecord achievement = CliDB.AchievementStorage.LookupByKey(achievementid);
                    if (achievement == null)
                        continue;

                    CompletedAchievementData ca = _completedAchievements[achievementid];
                    ca.Date = achievementResult.Read<long>(1);
                    var guids = new StringArray(achievementResult.Read<string>(2), ',');
                    if (!guids.IsEmpty())
                    {
                        for (int i = 0; i < guids.Length; ++i)
                        {
                            if (ulong.TryParse(guids[i], out ulong guid))
                                ca.CompletingPlayers.Add(ObjectGuid.Create(HighGuid.Player, guid));
                        }
                    }

                    ca.Changed = false;

                    _achievementPoints += achievement.Points;
                } while (achievementResult.NextRow());
            }

            if (!criteriaResult.IsEmpty())
            {
                long now = GameTime.GetGameTime();
                do
                {
                    uint id = criteriaResult.Read<uint>(0);
                    ulong counter = criteriaResult.Read<ulong>(1);
                    long date = criteriaResult.Read<long>(2);
                    ulong guidLow = criteriaResult.Read<ulong>(3);

                    Criteria criteria = Global.CriteriaMgr.GetCriteria(id);
                    if (criteria == null)
                    {
                        // we will remove not existed criteria for all guilds
                        Log.outError(LogFilter.Achievement, "Non-existing achievement criteria {0} data removed from table `guild_achievement_progress`.", id);

                        PreparedStatement stmt = CharacterDatabase.GetPreparedStatement(CharStatements.DEL_INVALID_ACHIEV_PROGRESS_CRITERIA_GUILD);
                        stmt.AddValue(0, id);
                        DB.Characters.Execute(stmt);
                        continue;
                    }

                    if (criteria.Entry.StartTimer != 0 && date + criteria.Entry.StartTimer < now)
                        continue;

                    CriteriaProgress progress = new();
                    progress.Counter = counter;
                    progress.Date = date;
                    progress.PlayerGUID = ObjectGuid.Create(HighGuid.Player, guidLow);
                    progress.Changed = false;

                    _criteriaProgress[id] = progress;

                } while (criteriaResult.NextRow());
            }
        }

        public void SaveToDB(SQLTransaction trans)
        {
            PreparedStatement stmt;
            StringBuilder guidstr = new();
            foreach (var pair in _completedAchievements)
            {
                if (!pair.Value.Changed)
                    continue;

                stmt = CharacterDatabase.GetPreparedStatement(CharStatements.DEL_GUILD_ACHIEVEMENT);
                stmt.AddValue(0, _owner.GetId());
                stmt.AddValue(1, pair.Key);
                trans.Append(stmt);

                stmt = CharacterDatabase.GetPreparedStatement(CharStatements.INS_GUILD_ACHIEVEMENT);
                stmt.AddValue(0, _owner.GetId());
                stmt.AddValue(1, pair.Key);
                stmt.AddValue(2, pair.Value.Date);
                foreach (var guid in pair.Value.CompletingPlayers)
                    guidstr.AppendFormat("{0},", guid.GetCounter());

                stmt.AddValue(3, guidstr.ToString());
                trans.Append(stmt);

                guidstr.Clear();
            }

            foreach (var pair in _criteriaProgress)
            {
                if (!pair.Value.Changed)
                    continue;

                stmt = CharacterDatabase.GetPreparedStatement(CharStatements.DEL_GUILD_ACHIEVEMENT_CRITERIA);
                stmt.AddValue(0, _owner.GetId());
                stmt.AddValue(1, pair.Key);
                trans.Append(stmt);

                stmt = CharacterDatabase.GetPreparedStatement(CharStatements.INS_GUILD_ACHIEVEMENT_CRITERIA);
                stmt.AddValue(0, _owner.GetId());
                stmt.AddValue(1, pair.Key);
                stmt.AddValue(2, pair.Value.Counter);
                stmt.AddValue(3, pair.Value.Date);
                stmt.AddValue(4, pair.Value.PlayerGUID.GetCounter());
                trans.Append(stmt);
            }
        }

        public override void SendAllData(Player receiver)
        {
            AllGuildAchievements allGuildAchievements = new();

            foreach (var (id, completedAchievement) in _completedAchievements)
            {
                AchievementRecord achievement = VisibleAchievementCheck(id);
                if (achievement == null)
                    continue;

                EarnedAchievement earned = new();
                earned.Id = id;
                earned.Date.SetUtcTimeFromUnixTime(completedAchievement.Date);
                earned.Date += receiver.GetSession().GetTimezoneOffset();
                allGuildAchievements.Earned.Add(earned);
            }

            receiver.SendPacket(allGuildAchievements);
        }

        public void SendAchievementInfo(Player receiver, uint achievementId = 0)
        {
            GuildCriteriaUpdate guildCriteriaUpdate = new();
            AchievementRecord achievement = CliDB.AchievementStorage.LookupByKey(achievementId);
            if (achievement != null)
            {
                CriteriaTree tree = Global.CriteriaMgr.GetCriteriaTree(achievement.CriteriaTree);
                if (tree != null)
                {
                    CriteriaManager.WalkCriteriaTree(tree, node =>
                    {
                        if (node.Criteria != null)
                        {
                            var progress = _criteriaProgress.LookupByKey(node.Criteria.Id);
                            if (progress != null)
                            {
                                GuildCriteriaProgress guildCriteriaProgress = new();
                                guildCriteriaProgress.CriteriaID = node.Criteria.Id;
                                guildCriteriaProgress.DateCreated = 0;
                                guildCriteriaProgress.DateStarted = 0;
                                guildCriteriaProgress.DateUpdated.SetUtcTimeFromUnixTime(progress.Date);
                                guildCriteriaProgress.DateUpdated += receiver.GetSession().GetTimezoneOffset();
                                guildCriteriaProgress.Quantity = progress.Counter;
                                guildCriteriaProgress.PlayerGUID = progress.PlayerGUID;
                                guildCriteriaProgress.Flags = 0;

                                guildCriteriaUpdate.Progress.Add(guildCriteriaProgress);
                            }
                        }
                    });
                }
            }

            receiver.SendPacket(guildCriteriaUpdate);
        }

        public void SendAllTrackedCriterias(Player receiver, List<uint> trackedCriterias)
        {
            GuildCriteriaUpdate guildCriteriaUpdate = new();

            foreach (uint criteriaId in trackedCriterias)
            {
                var progress = _criteriaProgress.LookupByKey(criteriaId);
                if (progress == null)
                    continue;

                GuildCriteriaProgress guildCriteriaProgress = new();
                guildCriteriaProgress.CriteriaID = criteriaId;
                guildCriteriaProgress.DateCreated = 0;
                guildCriteriaProgress.DateStarted = 0;
                guildCriteriaProgress.DateUpdated.SetUtcTimeFromUnixTime(progress.Date);
                guildCriteriaProgress.DateUpdated += receiver.GetSession().GetTimezoneOffset();
                guildCriteriaProgress.Quantity = progress.Counter;
                guildCriteriaProgress.PlayerGUID = progress.PlayerGUID;
                guildCriteriaProgress.Flags = 0;

                guildCriteriaUpdate.Progress.Add(guildCriteriaProgress);
            }

            receiver.SendPacket(guildCriteriaUpdate);
        }

        public void SendAchievementMembers(Player receiver, uint achievementId)
        {
            var achievementData = _completedAchievements.LookupByKey(achievementId);
            if (achievementData != null)
            {
                GuildAchievementMembers guildAchievementMembers = new();
                guildAchievementMembers.GuildGUID = _owner.GetGUID();
                guildAchievementMembers.AchievementID = achievementId;

                foreach (ObjectGuid guid in achievementData.CompletingPlayers)
                    guildAchievementMembers.Member.Add(guid);

                receiver.SendPacket(guildAchievementMembers);
            }
        }

        public override void CompletedAchievement(AchievementRecord achievement, Player referencePlayer)
        {
            Log.outDebug(LogFilter.Achievement, "CompletedAchievement({0})", achievement.Id);

            if (achievement.Flags.HasAnyFlag(AchievementFlags.Counter) || HasAchieved(achievement.Id))
                return;

            if (achievement.Flags.HasAnyFlag(AchievementFlags.ShowInGuildNews))
            {
                Guild guild = referencePlayer.GetGuild();
                if (guild != null)
                    guild.AddGuildNews(GuildNews.Achievement, ObjectGuid.Empty, (uint)(achievement.Flags & AchievementFlags.ShowInGuildHeader), achievement.Id);
            }

            SendAchievementEarned(achievement);
            CompletedAchievementData ca = new();
            ca.Date = GameTime.GetGameTime();
            ca.Changed = true;

            if (achievement.Flags.HasAnyFlag(AchievementFlags.ShowGuildMembers))
            {
                if (referencePlayer.GetGuildId() == _owner.GetId())
                    ca.CompletingPlayers.Add(referencePlayer.GetGUID());

                Group group = referencePlayer.GetGroup();
                if (group != null)
                {
                    for (GroupReference refe = group.GetFirstMember(); refe != null; refe = refe.Next())
                    {
                        Player groupMember = refe.GetSource();
                        if (groupMember != null)
                            if (groupMember.GetGuildId() == _owner.GetId())
                                ca.CompletingPlayers.Add(groupMember.GetGUID());
                    }
                }
            }

            _completedAchievements[achievement.Id] = ca;

            if (achievement.Flags.HasAnyFlag(AchievementFlags.RealmFirstReach | AchievementFlags.RealmFirstKill))
                Global.AchievementMgr.SetRealmCompleted(achievement);

            if (!achievement.Flags.HasAnyFlag(AchievementFlags.TrackingFlag))
                _achievementPoints += achievement.Points;

            UpdateCriteria(CriteriaType.EarnAchievement, achievement.Id, 0, 0, null, referencePlayer);
            UpdateCriteria(CriteriaType.EarnAchievementPoints, achievement.Points, 0, 0, null, referencePlayer);

            Global.ScriptMgr.OnAchievementCompleted(referencePlayer, achievement);
        }

        public override void SendCriteriaUpdate(Criteria entry, CriteriaProgress progress, TimeSpan timeElapsed, bool timedCompleted)
        {
            foreach (Player member in _owner.GetMembersTrackingCriteria(entry.Id))
            {
                GuildCriteriaUpdate guildCriteriaUpdate = new();
                GuildCriteriaProgress guildCriteriaProgress = new();
                guildCriteriaProgress.CriteriaID = entry.Id;
                guildCriteriaProgress.DateCreated = 0;
                guildCriteriaProgress.DateStarted = 0;
                guildCriteriaProgress.DateUpdated.SetUtcTimeFromUnixTime(progress.Date);
                guildCriteriaProgress.DateUpdated += member.GetSession().GetTimezoneOffset();
                guildCriteriaProgress.Quantity = progress.Counter;
                guildCriteriaProgress.PlayerGUID = progress.PlayerGUID;
                guildCriteriaProgress.Flags = 0;
                guildCriteriaUpdate.Progress.Add(guildCriteriaProgress);

                member.SendPacket(guildCriteriaUpdate);
            }
        }

        public override void SendCriteriaProgressRemoved(uint criteriaId)
        {
            GuildCriteriaDeleted guildCriteriaDeleted = new();
            guildCriteriaDeleted.GuildGUID = _owner.GetGUID();
            guildCriteriaDeleted.CriteriaID = criteriaId;
            SendPacket(guildCriteriaDeleted);
        }

        void SendAchievementEarned(AchievementRecord achievement)
        {
            if (achievement.Flags.HasAnyFlag(AchievementFlags.RealmFirstReach | AchievementFlags.RealmFirstKill))
            {
                // broadcast realm first reached
                BroadcastAchievement serverFirstAchievement = new();
                serverFirstAchievement.Name = _owner.GetName();
                serverFirstAchievement.PlayerGUID = _owner.GetGUID();
                serverFirstAchievement.AchievementID = achievement.Id;
                serverFirstAchievement.GuildAchievement = true;
                Global.WorldMgr.SendGlobalMessage(serverFirstAchievement);
            }

            _owner.BroadcastWorker(receiver =>
            {
                GuildAchievementEarned guildAchievementEarned = new();
                guildAchievementEarned.AchievementID = achievement.Id;
                guildAchievementEarned.GuildGUID = _owner.GetGUID();
                guildAchievementEarned.TimeEarned = GameTime.GetUtcWowTime();
                guildAchievementEarned.TimeEarned += receiver.GetSession().GetTimezoneOffset();
                receiver.SendPacket(guildAchievementEarned);
            });
        }

        public override void SendPacket(ServerPacket data)
        {
            _owner.BroadcastPacket(data);
        }

        public override List<Criteria> GetCriteriaByType(CriteriaType type, uint asset)
        {
            return Global.CriteriaMgr.GetGuildCriteriaByType(type);
        }

        public override string GetOwnerInfo()
        {
            return $"Guild ID {_owner.GetId()} {_owner.GetName()}";
        }
    }

    public class AchievementGlobalMgr : Singleton<AchievementGlobalMgr>
    {
        // store achievements by referenced achievement id to speed up lookup
        MultiMap<uint, AchievementRecord> _achievementListByReferencedId = new();

        // store realm first achievements
        Dictionary<uint /*achievementId*/, DateTime /*completionTime*/> _allCompletedAchievements = new();

        Dictionary<uint, AchievementReward> _achievementRewards = new();
        Dictionary<uint, AchievementRewardLocale> _achievementRewardLocales = new();
        Dictionary<uint, uint> _achievementScripts = new();

        AchievementGlobalMgr() { }

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
                Log.outInfo(LogFilter.ServerLoading, "Loaded 0 achievement references.");
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
                achievement1.InstanceID = 631;    // Correct map requirement (currently has Ulduar); 6.0.3 note - it STILL has ulduar requirement

            Log.outInfo(LogFilter.ServerLoading, "Loaded {0} achievement references in {1} ms.", count, Time.GetMSTimeDiffToNow(oldMSTime));
        }

        public void LoadAchievementScripts()
        {
            uint oldMSTime = Time.GetMSTime();

            _achievementScripts.Clear();                            // need for reload case

            SQLResult result = DB.World.Query("SELECT AchievementId, ScriptName FROM achievement_scripts");
            if (result.IsEmpty())
            {
                Log.outInfo(LogFilter.ServerLoading, "Loaded 0 achievement scripts. DB table `achievement_scripts` is empty.");
                return;
            }

            do
            {
                uint achievementId = result.Read<uint>(0);
                string scriptName = result.Read<string>(1);

                AchievementRecord achievement = CliDB.AchievementStorage.LookupByKey(achievementId);
                if (achievement == null)
                {
                    Log.outError(LogFilter.Sql, $"Table `achievement_scripts` contains non-existing Achievement (ID: {achievementId}), skipped.");
                    continue;
                }
                _achievementScripts[achievementId] = Global.ObjectMgr.GetScriptId(scriptName);
            }
            while (result.NextRow());

            Log.outInfo(LogFilter.ServerLoading, $"Loaded {_achievementScripts.Count} achievement scripts in {Time.GetMSTimeDiffToNow(oldMSTime)} ms");
        }

        public void LoadCompletedAchievements()
        {
            uint oldMSTime = Time.GetMSTime();

            // Populate _allCompletedAchievements with all realm first achievement ids to make multithreaded access safer
            // while it will not prevent races, it will prevent crashes that happen because std::unordered_map key was added
            // instead the only potential race will happen on value associated with the key
            foreach (AchievementRecord achievement in CliDB.AchievementStorage.Values)
                if (achievement.Flags.HasAnyFlag(AchievementFlags.RealmFirstReach | AchievementFlags.RealmFirstKill))
                    _allCompletedAchievements[achievement.Id] = DateTime.MinValue;

            SQLResult result = DB.Characters.Query("SELECT achievement FROM character_achievement GROUP BY achievement");
            if (result.IsEmpty())
            {
                Log.outInfo(LogFilter.ServerLoading, "Loaded 0 realm first completed achievements. DB table `character_achievement` is empty.");
                return;
            }

            do
            {
                uint achievementId = result.Read<uint>(0);
                AchievementRecord achievement = CliDB.AchievementStorage.LookupByKey(achievementId);
                if (achievement == null)
                {
                    // Remove non-existing achievements from all characters
                    Log.outError(LogFilter.Achievement, "Non-existing achievement {0} data has been removed from the table `character_achievement`.", achievementId);

                    PreparedStatement stmt = CharacterDatabase.GetPreparedStatement(CharStatements.DEL_INVALID_ACHIEVMENT);
                    stmt.AddValue(0, achievementId);
                    DB.Characters.Execute(stmt);

                    continue;
                }
                else if (achievement.Flags.HasAnyFlag(AchievementFlags.RealmFirstReach | AchievementFlags.RealmFirstKill))
                    _allCompletedAchievements[achievementId] = DateTime.MaxValue;
            }
            while (result.NextRow());

            Log.outInfo(LogFilter.ServerLoading, "Loaded {0} realm first completed achievements in {1} ms.", _allCompletedAchievements.Count, Time.GetMSTimeDiffToNow(oldMSTime));
        }

        public void LoadRewards()
        {
            uint oldMSTime = Time.GetMSTime();

            _achievementRewards.Clear();                           // need for reload case

            //                                         0   1       2       3       4       5        6     7
            SQLResult result = DB.World.Query("SELECT ID, TitleA, TitleH, ItemID, Sender, Subject, Body, MailTemplateID FROM achievement_reward");
            if (result.IsEmpty())
            {
                Log.outInfo(LogFilter.ServerLoading, ">> Loaded 0 achievement rewards. DB table `achievement_reward` is empty.");
                return;
            }

            do
            {
                uint id = result.Read<uint>(0);
                AchievementRecord achievement = CliDB.AchievementStorage.LookupByKey(id);
                if (achievement == null)
                {
                    Log.outError(LogFilter.Sql, $"Table `achievement_reward` contains a wrong achievement ID ({id}), ignored.");
                    continue;
                }

                AchievementReward reward = new();
                reward.TitleId[0] = result.Read<uint>(1);
                reward.TitleId[1] = result.Read<uint>(2);
                reward.ItemId = result.Read<uint>(3);
                reward.SenderCreatureId = result.Read<uint>(4);
                reward.Subject = result.Read<string>(5);
                reward.Body = result.Read<string>(6);
                reward.MailTemplateId = result.Read<uint>(7);

                // must be title or mail at least
                if (reward.TitleId[0] == 0 && reward.TitleId[1] == 0 && reward.SenderCreatureId == 0)
                {
                    Log.outError(LogFilter.Sql, $"Table `achievement_reward` (ID: {id}) does not contain title or item reward data. Ignored.");
                    continue;
                }

                if (achievement.Faction == AchievementFaction.Any && (reward.TitleId[0] == 0 ^ reward.TitleId[1] == 0))
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

                //check mail data before item for report including wrong item case
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
                        Log.outError(LogFilter.Sql, $"Table `achievement_reward` (ID: {id}) does not have sender data, but contains an item reward. Item will not be rewarded.");

                    if (!reward.Subject.IsEmpty())
                        Log.outError(LogFilter.Sql, $"Table `achievement_reward` (ID: {id}) does not have sender data, but contains a mail subject.");

                    if (!reward.Body.IsEmpty())
                        Log.outError(LogFilter.Sql, $"Table `achievement_reward` (ID: {id}) does not have sender data, but contains mail text.");

                    if (reward.MailTemplateId != 0)
                        Log.outError(LogFilter.Sql, $"Table `achievement_reward` (ID: {id}) does not have sender data, but has a MailTemplateId.");
                }

                if (reward.MailTemplateId != 0)
                {
                    if (!CliDB.MailTemplateStorage.ContainsKey(reward.MailTemplateId))
                    {
                        Log.outError(LogFilter.Sql, $"Table `achievement_reward` (ID: {id}) is using an invalid MailTemplateId ({reward.MailTemplateId}).");
                        reward.MailTemplateId = 0;
                    }
                    else if (!reward.Subject.IsEmpty() || !reward.Body.IsEmpty())
                        Log.outError(LogFilter.Sql, $"Table `achievement_reward` (ID: {id}) is using MailTemplateId ({reward.MailTemplateId}) and mail subject/text.");
                }

                if (reward.ItemId != 0)
                {
                    if (Global.ObjectMgr.GetItemTemplate(reward.ItemId) == null)
                    {
                        Log.outError(LogFilter.Sql, $"Table `achievement_reward` (ID: {id}) contains an invalid item id {reward.ItemId}, reward mail will not contain the rewarded item.");
                        reward.ItemId = 0;
                    }
                }

                _achievementRewards[id] = reward;
            }
            while (result.NextRow());

            Log.outInfo(LogFilter.ServerLoading, "Loaded {0} achievement rewards in {1} ms.", _achievementRewards.Count, Time.GetMSTimeDiffToNow(oldMSTime));
        }

        public void LoadRewardLocales()
        {
            uint oldMSTime = Time.GetMSTime();

            _achievementRewardLocales.Clear();                       // need for reload case

            //                                         0   1       2        3
            SQLResult result = DB.World.Query("SELECT ID, Locale, Subject, Body FROM achievement_reward_locale");
            if (result.IsEmpty())
            {
                Log.outInfo(LogFilter.ServerLoading, "Loaded 0 achievement reward locale strings.  DB table `achievement_reward_locale` is empty.");
                return;
            }

            do
            {
                uint id = result.Read<uint>(0);
                string localeName = result.Read<string>(1);

                if (!_achievementRewards.ContainsKey(id))
                {
                    Log.outError(LogFilter.Sql, $"Table `achievement_reward_locale` (ID: {id}) contains locale strings for a non-existing achievement reward.");
                    continue;
                }

                AchievementRewardLocale data = new();
                Locale locale = localeName.ToEnum<Locale>();
                if (!SharedConst.IsValidLocale(locale) || locale == Locale.enUS)
                    continue;

                ObjectManager.AddLocaleString(result.Read<string>(2), locale, data.Subject);
                ObjectManager.AddLocaleString(result.Read<string>(3), locale, data.Body);

                _achievementRewardLocales[id] = data;
            }
            while (result.NextRow());

            Log.outInfo(LogFilter.ServerLoading, "Loaded {0} achievement reward locale strings in {1} ms.", _achievementRewardLocales.Count, Time.GetMSTimeDiffToNow(oldMSTime));
        }

        public uint GetAchievementScriptId(uint achievementId)
        {
            return _achievementScripts.LookupByKey(achievementId);
        }
    }

    public class AchievementReward
    {
        public uint[] TitleId = new uint[2];
        public uint ItemId;
        public uint SenderCreatureId;
        public string Subject;
        public string Body;
        public uint MailTemplateId;
    }

    public class AchievementRewardLocale
    {
        public StringArray Subject = new((int)Locale.Total);
        public StringArray Body = new((int)Locale.Total);
    }

    public class CompletedAchievementData
    {
        public long Date;
        public List<ObjectGuid> CompletingPlayers = new();
        public bool Changed;
    }
}