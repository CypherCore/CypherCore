/*
 * Copyright (C) 2012-2019 CypherCore <http://github.com/CypherCore>
 * 
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */

using Framework.Collections;
using Framework.Constants;
using Framework.Database;
using Game.Chat;
using Game.DataStorage;
using Game.Entities;
using Game.Groups;
using Game.Maps;
using Game.Network;
using Game.Network.Packets;
using System;
using System.Collections.Generic;

namespace Game
{
    public sealed class CreatureTextManager : Singleton<CreatureTextManager>
    {
        CreatureTextManager() { }

        public void LoadCreatureTexts()
        {
            uint oldMSTime = Time.GetMSTime();

            mTextMap.Clear(); // for reload case
            //all currently used temp texts are NOT reset

            PreparedStatement stmt = DB.World.GetPreparedStatement(WorldStatements.SEL_CREATURE_TEXT);
            SQLResult result = DB.World.Query(stmt);

            if (result.IsEmpty())
            {
                Log.outInfo(LogFilter.ServerLoading, "Loaded 0 ceature texts. DB table `creature_texts` is empty.");
                return;
            }

            uint textCount = 0;
            uint creatureCount = 0;

            do
            {
                CreatureTextEntry temp = new CreatureTextEntry();

                temp.creatureId = result.Read<uint>(0);
                temp.groupId = result.Read<byte>(1);
                temp.id = result.Read<byte>(2);
                temp.text = result.Read<string>(3);
                temp.type = (ChatMsg)result.Read<byte>(4);
                temp.lang = (Language)result.Read<byte>(5);
                temp.probability = result.Read<float>(6);
                temp.emote = (Emote)result.Read<uint>(7);
                temp.duration = result.Read<uint>(8);
                temp.sound = result.Read<uint>(9);
                temp.BroadcastTextId = result.Read<uint>(10);
                temp.TextRange = (CreatureTextRange)result.Read<byte>(11);

                if (temp.sound != 0)
                {
                    if (!CliDB.SoundKitStorage.ContainsKey(temp.sound))
                    {
                        Log.outError(LogFilter.Sql, "GossipManager:  Entry {0}, Group {1} in table `creature_texts` has Sound {2} but sound does not exist.", temp.creatureId, temp.groupId, temp.sound);
                        temp.sound = 0;
                    }
                }
                if (ObjectManager.GetLanguageDescByID(temp.lang) == null)
                {
                    Log.outError(LogFilter.Sql, "GossipManager:  Entry {0}, Group {1} in table `creature_texts` using Language {2} but Language does not exist.", temp.creatureId, temp.groupId, temp.lang);
                    temp.lang = Language.Universal;
                }
                if (temp.type >= ChatMsg.Max)
                {
                    Log.outError(LogFilter.Sql, "GossipManager:  Entry {0}, Group {1} in table `creature_texts` has Type {2} but this Chat Type does not exist.", temp.creatureId, temp.groupId, temp.type);
                    temp.type = ChatMsg.Say;
                }
                if (temp.emote != 0)
                {
                    if (!CliDB.EmotesStorage.ContainsKey((uint)temp.emote))
                    {
                        Log.outError(LogFilter.Sql, "GossipManager:  Entry {0}, Group {1} in table `creature_texts` has Emote {2} but emote does not exist.", temp.creatureId, temp.groupId, temp.emote);
                        temp.emote = Emote.OneshotNone;
                    }
                }

                if (temp.BroadcastTextId != 0)
                {
                    if (!CliDB.BroadcastTextStorage.ContainsKey(temp.BroadcastTextId))
                    {
                        Log.outError(LogFilter.Sql, "CreatureTextMgr: Entry {0}, Group {1}, Id {2} in table `creature_texts` has non-existing or incompatible BroadcastTextId {3}.", temp.creatureId, temp.groupId, temp.id, temp.BroadcastTextId);
                        temp.BroadcastTextId = 0;
                    }
                }

                if (temp.TextRange > CreatureTextRange.World)
                {
                    Log.outError(LogFilter.Sql, "CreatureTextMgr: Entry {0}, Group {1}, Id {2} in table `creature_text` has incorrect TextRange {3}.", temp.creatureId, temp.groupId, temp.id, temp.TextRange);
                    temp.TextRange = CreatureTextRange.Normal;
                }

                if (!mTextMap.ContainsKey(temp.creatureId))
                {
                    mTextMap[temp.creatureId] = new MultiMap<byte,CreatureTextEntry>();
                    ++creatureCount;
                }

                mTextMap[temp.creatureId].Add(temp.groupId, temp);
                ++textCount;
            } while (result.NextRow());

            Log.outInfo(LogFilter.ServerLoading, "Loaded {0} creature texts for {1} creatures in {2} ms", textCount, creatureCount, Time.GetMSTimeDiffToNow(oldMSTime));
        }

        public void LoadCreatureTextLocales()
        {
            uint oldMSTime = Time.GetMSTime();

            mLocaleTextMap.Clear(); // for reload case

            SQLResult result = DB.World.Query("SELECT CreatureId, GroupId, ID, Locale, Text FROM creature_text_locale");

            if (result.IsEmpty())
                return;

            do
            {
                uint creatureId = result.Read<uint>(0);
                uint groupId = result.Read<byte>(1);
                uint id = result.Read<byte>(2);
                string localeName = result.Read<string>(3);
                LocaleConstant locale = localeName.ToEnum<LocaleConstant>();
                if (locale == LocaleConstant.enUS)
                    continue;

                var key = new CreatureTextId(creatureId, groupId, id);
                if (!mLocaleTextMap.ContainsKey(key))
                    mLocaleTextMap[key] = new CreatureTextLocale();

                CreatureTextLocale data = mLocaleTextMap[key];
                ObjectManager.AddLocaleString(result.Read<string>(4), locale, data.Text);

            } while (result.NextRow());

            Log.outInfo(LogFilter.ServerLoading, "Loaded {0} creature localized texts in {1} ms", mLocaleTextMap.Count, Time.GetMSTimeDiffToNow(oldMSTime));
        }

        public uint SendChat(Creature source, byte textGroup, WorldObject whisperTarget = null, ChatMsg msgType = ChatMsg.Addon, Language language = Language.Addon,
            CreatureTextRange range = CreatureTextRange.Normal, uint sound = 0, Team team = Team.Other, bool gmOnly = false, Player srcPlr = null)
        {
            if (source == null)
                return 0;

            var sList = mTextMap.LookupByKey(source.GetEntry());
            if (sList == null)
            {
                Log.outError(LogFilter.Sql, "GossipManager: Could not find Text for Creature({0}) Entry {1} in 'creature_text' table. Ignoring.", source.GetName(), source.GetEntry());
                return 0;
            }

            var textGroupContainer = sList.LookupByKey(textGroup);
            if (textGroupContainer.Empty())
            {
                Log.outError(LogFilter.ChatSystem, "GossipManager: Could not find TextGroup {0} for Creature({1}) GuidLow {2} Entry {3}. Ignoring.", textGroup, source.GetName(), source.GetGUID().ToString(), source.GetEntry());
                return 0;
            }

            List<CreatureTextEntry> tempGroup = new List<CreatureTextEntry>();
            var repeatGroup = source.GetTextRepeatGroup(textGroup);

            foreach (var entry in textGroupContainer)
                if (!repeatGroup.Contains(entry.id))
                    tempGroup.Add(entry);

            if (tempGroup.Empty())
            {
                source.ClearTextRepeatGroup(textGroup);
                tempGroup = textGroupContainer;
            }

            var textEntry = tempGroup.SelectRandomElementByWeight(t => t.probability);

            ChatMsg finalType = (msgType == ChatMsg.Addon) ? textEntry.type : msgType;
            Language finalLang = (language == Language.Addon) ? textEntry.lang : language;
            uint finalSound = textEntry.sound;
            if (sound != 0)
                finalSound = sound;
            else
            {
                BroadcastTextRecord bct = CliDB.BroadcastTextStorage.LookupByKey(textEntry.BroadcastTextId);
                if (bct != null)
                {
                    uint broadcastTextSoundId = bct.SoundEntriesID[source.GetGender() == Gender.Female ? 1 : 0];
                    if (broadcastTextSoundId != 0)
                        finalSound = broadcastTextSoundId;
                }
            }

            if (range == CreatureTextRange.Normal)
                range = textEntry.TextRange;

            if (finalSound != 0)
                SendSound(source, finalSound, finalType, whisperTarget, range, team, gmOnly);

            Unit finalSource = source;
            if (srcPlr)
                finalSource = srcPlr;

            if (textEntry.emote != 0)
                SendEmote(finalSource, textEntry.emote);

            if (srcPlr)
            {
                PlayerTextBuilder builder = new PlayerTextBuilder(source, finalSource, finalSource.GetGender(), finalType, textEntry.groupId, textEntry.id, finalLang, whisperTarget);
                SendChatPacket(finalSource, builder, finalType, whisperTarget, range, team, gmOnly);
            }
            else
            {
                CreatureTextBuilder builder = new CreatureTextBuilder(finalSource, finalSource.GetGender(), finalType, textEntry.groupId, textEntry.id, finalLang, whisperTarget);
                SendChatPacket(finalSource, builder, finalType, whisperTarget, range, team, gmOnly);
            }

            source.SetTextRepeatId(textGroup, textEntry.id);
            return textEntry.duration;
        }

        float GetRangeForChatType(ChatMsg msgType)
        {
            float dist = WorldConfig.GetFloatValue(WorldCfg.ListenRangeSay);
            switch (msgType)
            {
                case ChatMsg.MonsterYell:
                    dist = WorldConfig.GetFloatValue(WorldCfg.ListenRangeYell);
                    break;
                case ChatMsg.MonsterEmote:
                case ChatMsg.RaidBossEmote:
                    dist = WorldConfig.GetFloatValue(WorldCfg.ListenRangeTextemote);
                    break;
                default:
                    break;
            }

            return dist;
        }

        void SendSound(Creature source, uint sound, ChatMsg msgType, WorldObject whisperTarget = null, CreatureTextRange range = CreatureTextRange.Normal, Team team = Team.Other, bool gmOnly = false)
        {
            if (sound == 0 || !source)
                return;

            SendNonChatPacket(source, new PlaySound(source.GetGUID(), sound), msgType, whisperTarget, range, team, gmOnly);
        }

        void SendNonChatPacket(WorldObject source, ServerPacket data, ChatMsg msgType, WorldObject whisperTarget, CreatureTextRange range, Team team, bool gmOnly)
        {
            float dist = GetRangeForChatType(msgType);

            switch (msgType)
            {
                case ChatMsg.MonsterParty:
                    if (!whisperTarget)
                        return;

                    Player whisperPlayer = whisperTarget.ToPlayer();
                    if (whisperPlayer)
                    {
                        Group group = whisperPlayer.GetGroup();
                        if (group)
                            group.BroadcastWorker(player => player.SendPacket(data));
                    }
                    return;
                case ChatMsg.MonsterWhisper:
                case ChatMsg.RaidBossWhisper:
                    {
                        if (range == CreatureTextRange.Normal)//ignores team and gmOnly
                        {
                            if (!whisperTarget || !whisperTarget.IsTypeId(TypeId.Player))
                                return;

                            whisperTarget.ToPlayer().SendPacket(data);
                            return;
                        }
                        break;
                    }
                default:
                    break;
            }

            switch (range)
            {
                case CreatureTextRange.Area:
                    {
                        uint areaId = source.GetAreaId();
                        var players = source.GetMap().GetPlayers();
                        foreach (var pl in players)
                            if (pl.GetAreaId() == areaId && (team == 0 || pl.GetTeam() == team) && (!gmOnly || pl.IsGameMaster()))
                                pl.SendPacket(data);
                        return;
                    }
                case CreatureTextRange.Zone:
                    {
                        uint zoneId = source.GetZoneId();
                        var players = source.GetMap().GetPlayers();
                        foreach (var pl in players)
                            if (pl.GetZoneId() == zoneId && (team == 0 || pl.GetTeam() == team) && (!gmOnly || pl.IsGameMaster()))
                                pl.SendPacket(data);
                        return;
                    }
                case CreatureTextRange.Map:
                    {
                        var players = source.GetMap().GetPlayers();
                        foreach (var pl in players)
                            if ((team == 0 || pl.GetTeam() == team) && (!gmOnly || pl.IsGameMaster()))
                                pl.SendPacket(data);
                        return;
                    }
                case CreatureTextRange.World:
                    {
                        var smap = Global.WorldMgr.GetAllSessions();
                        foreach (var session in smap)
                        {
                            Player player = session.GetPlayer();
                            if (player != null)
                                if ((team == 0 || player.GetTeam() == team) && (!gmOnly || player.IsGameMaster()))
                                    player.SendPacket(data);
                        }
                        return;
                    }
                case CreatureTextRange.Normal:
                default:
                    break;
            }

            source.SendMessageToSetInRange(data, dist, true);
        }

        void SendEmote(Unit source, Emote emote)
        {
            if (!source)
                return;

            source.HandleEmoteCommand(emote);
        }

        public bool TextExist(uint sourceEntry, byte textGroup)
        {
            if (sourceEntry == 0)
                return false;

            var textHolder = mTextMap.LookupByKey(sourceEntry);
            if (textHolder == null)
            {
                Log.outDebug(LogFilter.Unit, "CreatureTextMgr.TextExist: Could not find Text for Creature (entry {0}) in 'creature_text' table.", sourceEntry);
                return false;
            }

            var textEntryList = textHolder.LookupByKey(textGroup);
            if (textEntryList.Empty())
            {
                Log.outDebug(LogFilter.Unit, "CreatureTextMgr.TextExist: Could not find TextGroup {0} for Creature (entry {1}).", textGroup, sourceEntry);
                return false;
            }

            return true;
        }

        public string GetLocalizedChatString(uint entry, Gender gender, byte textGroup, uint id, LocaleConstant locale = LocaleConstant.enUS)
        {
            var multiMap = mTextMap.LookupByKey(entry);
            if (multiMap == null)
                return "";

            var creatureTextEntryList = multiMap.LookupByKey(textGroup);
            if (creatureTextEntryList.Empty())
                return "";

            CreatureTextEntry creatureTextEntry = null;
            for (var i = 0; i != creatureTextEntryList.Count; ++i)
            {
                creatureTextEntry = creatureTextEntryList[i];
                if (creatureTextEntry.id == id)
                    break;
            }

            if (creatureTextEntry == null)
                return "";

            if (locale >= LocaleConstant.Total)
                locale = LocaleConstant.enUS;

            string baseText = "";
            BroadcastTextRecord bct = CliDB.BroadcastTextStorage.LookupByKey(creatureTextEntry.BroadcastTextId);

            if (bct != null)
                baseText = Global.DB2Mgr.GetBroadcastTextValue(bct, locale, gender);
            else
                baseText = creatureTextEntry.text;

            if (locale != LocaleConstant.enUS && bct == null)
            {
                var creatureTextLocale = mLocaleTextMap.LookupByKey(new CreatureTextId(entry, textGroup, id));
                if (creatureTextLocale != null)
                    ObjectManager.GetLocaleString(creatureTextLocale.Text, locale, ref baseText);
            }

            return baseText;
        }

        public void SendChatPacket(WorldObject source, MessageBuilder builder, ChatMsg msgType, WorldObject whisperTarget = null, CreatureTextRange range = CreatureTextRange.Normal, Team team = Team.Other, bool gmOnly = false)
        {
            if (source == null)
                return;

            var localizer = new CreatureTextLocalizer(builder, msgType);

            switch (msgType)
            {
                case ChatMsg.MonsterWhisper:
                case ChatMsg.RaidBossWhisper:
                    {
                        if (range == CreatureTextRange.Normal) //ignores team and gmOnly
                        {
                            if (!whisperTarget || !whisperTarget.IsTypeId(TypeId.Player))
                                return;

                            localizer.Invoke(whisperTarget.ToPlayer());
                            return;
                        }
                        break;
                    }
                default:
                    break;
            }

            switch (range)
            {
                case CreatureTextRange.Area:
                    {
                        uint areaId = source.GetAreaId();
                        var players = source.GetMap().GetPlayers();
                        foreach (var pl in players)
                            if (pl.GetAreaId() == areaId && (team == 0 || pl.GetTeam() == team) && (!gmOnly || pl.IsGameMaster()))
                                localizer.Invoke(pl);
                        return;
                    }
                case CreatureTextRange.Zone:
                    {
                        uint zoneId = source.GetZoneId();
                        var players = source.GetMap().GetPlayers();
                        foreach (var pl in players)
                            if (pl.GetZoneId() == zoneId && (team == 0 || pl.GetTeam() == team) && (!gmOnly || pl.IsGameMaster()))
                                localizer.Invoke(pl);
                        return;
                    }
                case CreatureTextRange.Map:
                    {
                        var players = source.GetMap().GetPlayers();
                        foreach (var pl in players)
                            if ((team == 0 || pl.GetTeam() == team) && (!gmOnly || pl.IsGameMaster()))
                                localizer.Invoke(pl);
                        return;
                    }
                case CreatureTextRange.World:
                    {
                        var smap = Global.WorldMgr.GetAllSessions();
                        foreach (var session in smap)
                        {
                            Player player = session.GetPlayer();
                            if (player != null)
                                if ((team == 0 || player.GetTeam() == team) && (!gmOnly || player.IsGameMaster()))
                                    localizer.Invoke(player);
                        }
                        return;
                    }
                case CreatureTextRange.Normal:
                default:
                    break;
            }

            float dist = GetRangeForChatType(msgType);
            var worker = new PlayerDistWorker(source, dist, localizer);
            Cell.VisitWorldObjects(source, worker, dist);
        }

        Dictionary<uint, MultiMap<byte, CreatureTextEntry>> mTextMap = new Dictionary<uint, MultiMap<byte, CreatureTextEntry>>();
        Dictionary<CreatureTextId, CreatureTextLocale> mLocaleTextMap = new Dictionary<CreatureTextId,CreatureTextLocale>();
    }

    public class CreatureTextHolder
    {
        public CreatureTextHolder(uint entry)
        {
            Entry = entry;
            Groups = new MultiMap<uint, CreatureTextEntry>();
        }

        public void AddText(uint group, CreatureTextEntry entry)
        {
            Groups.Add(group, entry);
        }

        public List<CreatureTextEntry> GetGroupList(uint group)
        {
            return Groups.LookupByKey(group);
        }

        uint Entry;
        MultiMap<uint, CreatureTextEntry> Groups;
    }

    public class CreatureTextRepeatHolder
    {
        public CreatureTextRepeatHolder(ulong guid)
        {
            Guid = guid;
            Groups = new MultiMap<byte, byte>();
        }

        public void AddText(byte group, byte entry)
        {
            Groups.Add(group, entry);
        }

        public List<byte> GetGroupList(byte group)
        {
            return Groups.LookupByKey(group);
        }

        ulong Guid;
        MultiMap<byte, byte> Groups;
    }

    public class CreatureTextEntry
    {
        public uint creatureId;
        public byte groupId;
        public byte id;
        public string text;
        public ChatMsg type;
        public Language lang;
        public float probability;
        public Emote emote;
        public uint duration;
        public uint sound;
        public uint BroadcastTextId;
        public CreatureTextRange TextRange;
    }
    public class CreatureTextLocale
    {
        public StringArray Text = new StringArray((int)LocaleConstant.Total);
    }
    public class CreatureTextId
    {
        public CreatureTextId(uint e, uint g, uint i)
        {
            entry = e;
            textGroup = g;
            textId = i;
        }

        uint entry;
        uint textGroup;
        uint textId;
    }

    public enum CreatureTextRange
    {
        Normal = 0,
        Area = 1,
        Zone = 2,
        Map = 3,
        World = 4
    }

    public class CreatureTextLocalizer : IDoWork<Player>
    {
        public CreatureTextLocalizer(MessageBuilder builder, ChatMsg msgType)
        {
            _builder = builder;
            _msgType = msgType;
        }

        public void Invoke(Player player)
        {
            LocaleConstant loc_idx = player.GetSession().GetSessionDbLocaleIndex();
            ServerPacket messageTemplate;

            // create if not cached yet
            if (!_packetCache.ContainsKey(loc_idx))
            {
                messageTemplate = _builder.Invoke(loc_idx);
                _packetCache[loc_idx] = messageTemplate;
            }
            else
                messageTemplate = _packetCache[loc_idx];

            ChatPkt message = (ChatPkt)messageTemplate;
            switch (_msgType)
            {
                case ChatMsg.MonsterWhisper:
                case ChatMsg.RaidBossWhisper:
                    message.SetReceiver(player, loc_idx);
                    break;
                default:
                    break;
            }

            player.SendPacket(message);
        }

        Dictionary<LocaleConstant, ServerPacket> _packetCache = new Dictionary<LocaleConstant, ServerPacket>();
        MessageBuilder _builder;
        ChatMsg _msgType;
    }

    public class CreatureTextBuilder : MessageBuilder
    {
        public CreatureTextBuilder(WorldObject obj, Gender gender, ChatMsg msgtype, byte textGroup, uint id, Language language, WorldObject target)
        {
            _source = obj;
            _gender = gender;
            _msgType = msgtype;
            _textGroup = textGroup;
            _textId = id;
            _language = language;
            _target = target;
        }

        public override ServerPacket Invoke(LocaleConstant locale = LocaleConstant.enUS)
        {
            string text = Global.CreatureTextMgr.GetLocalizedChatString(_source.GetEntry(), _gender, _textGroup, _textId, locale);
            var packet = new ChatPkt();
            packet.Initialize(_msgType, _language, _source, _target, text, 0, "", locale);
            return packet;
        }

        WorldObject _source;
        Gender _gender;
        ChatMsg _msgType;
        byte _textGroup;
        uint _textId;
        Language _language;
        WorldObject _target;
    }

    public class PlayerTextBuilder : MessageBuilder
    {
        public PlayerTextBuilder(WorldObject obj, WorldObject speaker, Gender gender, ChatMsg msgtype, byte textGroup, uint id, Language language, WorldObject target)
        {
            _source = obj;
            _gender = gender;
            _talker = speaker;
            _msgType = msgtype;
            _textGroup = textGroup;
            _textId = id;
            _language = language;
            _target = target;
        }

        public override ServerPacket Invoke(LocaleConstant loc_idx = LocaleConstant.enUS)
        {
            string text = Global.CreatureTextMgr.GetLocalizedChatString(_source.GetEntry(), _gender, _textGroup, _textId, loc_idx);
            var packet = new ChatPkt();
            packet.Initialize(_msgType, _language, _talker, _target, text, 0, "", loc_idx);
            return packet;
        }

        WorldObject _source;
        WorldObject _talker;
        Gender _gender;
        ChatMsg _msgType;
        byte _textGroup;
        uint _textId;
        Language _language;
        WorldObject _target;
    }
}