// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Framework.Database;
using Framework.IO;
using Game.DataStorage;
using Game.Entities;
using Game.Maps;
using Game.Movement;
using System;
using System.Collections.Generic;
using System.Linq;
using Framework.Collections;
using Game.Loots;

namespace Game.Chat
{
    [CommandGroup("npc")]
    class NPCCommands
    {
        [Command("despawngroup", RBACPermissions.CommandNpcDespawngroup)]
        static bool HandleNpcDespawnGroup(CommandHandler handler, string[] opts)
        {
            if (opts.Empty())
                return false;

            bool deleteRespawnTimes = false;
            uint groupId = 0;

            // Decode arguments
            foreach (var variant in opts)
            {
                if (!uint.TryParse(variant, out groupId))
                    deleteRespawnTimes = true;
            }

            Player player = handler.GetSession().GetPlayer();
            if (!player.GetMap().SpawnGroupDespawn(groupId, deleteRespawnTimes, out int despawnedCount))
            {
                handler.SendSysMessage(CypherStrings.SpawngroupBadgroup, groupId);
                return false;
            }
            handler.SendSysMessage($"Despawned a total of {despawnedCount} objects.");

            return true;
        }

        [Command("evade", RBACPermissions.CommandNpcEvade)]
        static bool HandleNpcEvadeCommand(CommandHandler handler, OptionalArg<EvadeReason> why, string force)
        {
            Creature creatureTarget = handler.GetSelectedCreature();
            if (creatureTarget == null || creatureTarget.IsPet())
            {
                handler.SendSysMessage(CypherStrings.SelectCreature);
                return false;
            }

            if (creatureTarget.IsAIEnabled())
            {
                handler.SendSysMessage(CypherStrings.CreatureNotAiEnabled);
                return false;
            }

            if (force.Equals("force", StringComparison.OrdinalIgnoreCase))
                creatureTarget.ClearUnitState(UnitState.Evade);
            creatureTarget.GetAI().EnterEvadeMode(why.GetValueOrDefault(EvadeReason.Other));

            return true;
        }

        [Command("info", RBACPermissions.CommandNpcInfo)]
        static bool HandleNpcInfoCommand(CommandHandler handler)
        {
            Creature target = handler.GetSelectedCreature();
            if (target == null)
            {
                handler.SendSysMessage(CypherStrings.SelectCreature);
                return false;
            }

            CreatureTemplate cInfo = target.GetCreatureTemplate();

            uint faction = target.GetFaction();
            ulong npcflags = (ulong)target.m_unitData.NpcFlags2 << 32 | target.m_unitData.NpcFlags;
            ulong mechanicImmuneMask = 0;
            CreatureImmunities immunities = Global.SpellMgr.GetCreatureImmunities(cInfo.CreatureImmunitiesId);
            if (immunities != null)
                mechanicImmuneMask = immunities.Mechanic.ToUInt();
            uint displayid = target.GetDisplayId();
            uint nativeid = target.GetNativeDisplayId();
            uint entry = target.GetEntry();

            long curRespawnDelay = target.GetRespawnCompatibilityMode() ? target.GetRespawnTimeEx() - GameTime.GetGameTime() : target.GetMap().GetCreatureRespawnTime(target.GetSpawnId()) - GameTime.GetGameTime();
            if (curRespawnDelay < 0)
                curRespawnDelay = 0;

            string curRespawnDelayStr = Time.secsToTimeString((ulong)curRespawnDelay, TimeFormat.ShortText);
            string defRespawnDelayStr = Time.secsToTimeString(target.GetRespawnDelay(), TimeFormat.ShortText);

            handler.SendSysMessage(CypherStrings.NpcinfoChar, target.GetName(), target.GetSpawnId(), target.GetGUID().ToString(), entry, faction, npcflags, displayid, nativeid);
            if (target.GetCreatureData() != null && target.GetCreatureData().spawnGroupData.groupId != 0)
            {
                SpawnGroupTemplateData groupData = target.GetCreatureData().spawnGroupData;
                handler.SendSysMessage(CypherStrings.SpawninfoGroupId, groupData.name, groupData.groupId, groupData.flags, target.GetMap().IsSpawnGroupActive(groupData.groupId));
            }
            handler.SendSysMessage(CypherStrings.SpawninfoCompatibilityMode, target.GetRespawnCompatibilityMode());
            handler.SendSysMessage(CypherStrings.NpcinfoLevel, target.GetLevel());
            handler.SendSysMessage(CypherStrings.NpcinfoEquipment, target.GetCurrentEquipmentId(), target.GetOriginalEquipmentId());
            handler.SendSysMessage(CypherStrings.NpcinfoHealth, target.GetCreateHealth(), target.GetMaxHealth(), target.GetHealth());
            handler.SendSysMessage(CypherStrings.NpcinfoMovementData, target.GetMovementTemplate().ToString());

            handler.SendSysMessage(CypherStrings.NpcinfoUnitFieldFlags, (uint)target.m_unitData.Flags);
            foreach (UnitFlags value in Enum.GetValues(typeof(UnitFlags)))
                if (target.HasUnitFlag(value))
                    handler.SendSysMessage("{0} (0x{1:X})", value, value);

            handler.SendSysMessage(CypherStrings.NpcinfoUnitFieldFlags2, (uint)target.m_unitData.Flags2);
            foreach (UnitFlags2 value in Enum.GetValues(typeof(UnitFlags2)))
                if (target.HasUnitFlag2(value))
                    handler.SendSysMessage("{0} (0x{1:X})", value, value);

            handler.SendSysMessage(CypherStrings.NpcinfoUnitFieldFlags3, (uint)target.m_unitData.Flags3);
            foreach (UnitFlags3 value in Enum.GetValues(typeof(UnitFlags3)))
                if (target.HasUnitFlag3(value))
                    handler.SendSysMessage("{0} (0x{1:X})", value, value);

            handler.SendSysMessage(CypherStrings.NpcinfoDynamicFlags, target.GetDynamicFlags());
            handler.SendSysMessage(CypherStrings.CommandRawpawntimes, defRespawnDelayStr, curRespawnDelayStr);

            CreatureDifficulty creatureDifficulty = target.GetCreatureDifficulty();
            handler.SendSysMessage(CypherStrings.NpcinfoLoot, creatureDifficulty.LootID, creatureDifficulty.PickPocketLootID, creatureDifficulty.SkinLootID);
            handler.SendSysMessage(CypherStrings.NpcinfoDungeonId, target.GetInstanceId());

            CreatureData data = Global.ObjectMgr.GetCreatureData(target.GetSpawnId());
            if (data != null)
                handler.SendSysMessage(CypherStrings.NpcinfoPhases, data.PhaseId, data.PhaseGroup);

            PhasingHandler.PrintToChat(handler, target);

            handler.SendSysMessage(CypherStrings.NpcinfoArmor, target.GetArmor());
            handler.SendSysMessage(CypherStrings.NpcinfoPosition, target.GetPositionX(), target.GetPositionY(), target.GetPositionZ());
            handler.SendSysMessage(CypherStrings.ObjectinfoAiInfo, target.GetAIName(), target.GetScriptName());
            handler.SendSysMessage(CypherStrings.ObjectinfoStringIds, target.GetStringId(StringIdType.Template), target.GetStringId(StringIdType.Spawn), target.GetStringId(StringIdType.Script));
            handler.SendSysMessage(CypherStrings.NpcinfoReactstate, target.GetReactState());
            var ai = target.GetAI();
            if (ai != null)
                handler.SendSysMessage(CypherStrings.ObjectinfoAiType, nameof(ai));
            handler.SendSysMessage(CypherStrings.NpcinfoFlagsExtra, cInfo.FlagsExtra);
            foreach (uint value in Enum.GetValues(typeof(CreatureFlagsExtra)))
                if (cInfo.FlagsExtra.HasAnyFlag((CreatureFlagsExtra)value))
                    handler.SendSysMessage("{0} (0x{1:X})", (CreatureFlagsExtra)value, value);

            handler.SendSysMessage(CypherStrings.NpcinfoNpcFlags, target.m_unitData.NpcFlags);
            foreach (uint value in Enum.GetValues(typeof(NPCFlags)))
                if (npcflags.HasAnyFlag(value))
                    handler.SendSysMessage("{0} (0x{1:X})", (NPCFlags)value, value);

            handler.SendSysMessage(CypherStrings.NpcinfoMechanicImmune, $"0x{mechanicImmuneMask:X}");
            foreach (int value in Enum.GetValues(typeof(Mechanics)))
                if (Convert.ToBoolean(mechanicImmuneMask & (1ul << (value - 1))))
                    handler.SendSysMessage("{0} (0x{1:X})", (Mechanics)value, value);

            return true;
        }

        [Command("move", RBACPermissions.CommandNpcMove)]
        static bool HandleNpcMoveCommand(CommandHandler handler, OptionalArg<ulong> spawnId)
        {
            Creature creature = handler.GetSelectedCreature();
            Player player = handler.GetSession().GetPlayer();
            if (player == null)
                return false;

            if (!spawnId.HasValue && creature == null)
                return false;

            ulong lowguid = spawnId.HasValue ? spawnId.Value : creature.GetSpawnId();

            // Attempting creature load from DB data
            CreatureData data = Global.ObjectMgr.GetCreatureData(lowguid);
            if (data == null)
            {
                handler.SendSysMessage(CypherStrings.CommandCreatguidnotfound, lowguid);
                return false;
            }

            if (player.GetMapId() != data.MapId)
            {
                handler.SendSysMessage(CypherStrings.CommandCreatureatsamemap, lowguid);
                return false;
            }

            Global.ObjectMgr.RemoveCreatureFromGrid(data);
            data.SpawnPoint.Relocate(player);
            Global.ObjectMgr.AddCreatureToGrid(data);

            // update position in DB
            PreparedStatement stmt = WorldDatabase.GetPreparedStatement(WorldStatements.UPD_CREATURE_POSITION);
            stmt.AddValue(0, player.GetPositionX());
            stmt.AddValue(1, player.GetPositionY());
            stmt.AddValue(2, player.GetPositionZ());
            stmt.AddValue(3, player.GetOrientation());
            stmt.AddValue(4, lowguid);

            DB.World.Execute(stmt);

            // respawn selected creature at the new location
            if (creature != null)
                creature.DespawnOrUnsummon(TimeSpan.FromSeconds(0), TimeSpan.FromSeconds(1));

            handler.SendSysMessage(CypherStrings.CommandCreaturemoved);
            return true;
        }

        [Command("near", RBACPermissions.CommandNpcNear)]
        static bool HandleNpcNearCommand(CommandHandler handler, OptionalArg<float> dist)
        {
            float distance = dist.GetValueOrDefault(10.0f);
            uint count = 0;

            Player player = handler.GetPlayer();

            PreparedStatement stmt = WorldDatabase.GetPreparedStatement(WorldStatements.SEL_CREATURE_NEAREST);
            stmt.AddValue(0, player.GetPositionX());
            stmt.AddValue(1, player.GetPositionY());
            stmt.AddValue(2, player.GetPositionZ());
            stmt.AddValue(3, player.GetMapId());
            stmt.AddValue(4, player.GetPositionX());
            stmt.AddValue(5, player.GetPositionY());
            stmt.AddValue(6, player.GetPositionZ());
            stmt.AddValue(7, distance * distance);
            SQLResult result = DB.World.Query(stmt);

            if (!result.IsEmpty())
            {
                do
                {
                    ulong guid = result.Read<ulong>(0);
                    uint entry = result.Read<uint>(1);
                    float x = result.Read<float>(2);
                    float y = result.Read<float>(3);
                    float z = result.Read<float>(4);
                    ushort mapId = result.Read<ushort>(5);

                    CreatureTemplate creatureTemplate = Global.ObjectMgr.GetCreatureTemplate(entry);
                    if (creatureTemplate == null)
                        continue;

                    handler.SendSysMessage(CypherStrings.CreatureListChat, guid, guid, creatureTemplate.Name, x, y, z, mapId, "", "");

                    ++count;
                }
                while (result.NextRow());
            }

            handler.SendSysMessage(CypherStrings.CommandNearNpcMessage, distance, count);

            return true;
        }

        [Command("playemote", RBACPermissions.CommandNpcPlayemote)]
        static bool HandleNpcPlayEmoteCommand(CommandHandler handler, uint emote)
        {
            Creature target = handler.GetSelectedCreature();
            if (target == null)
            {
                handler.SendSysMessage(CypherStrings.SelectCreature);
                return false;
            }

            target.SetEmoteState((Emote)emote);

            return true;
        }

        [Command("say", RBACPermissions.CommandNpcSay)]
        static bool HandleNpcSayCommand(CommandHandler handler, Tail text)
        {
            if (text.IsEmpty())
                return false;

            Creature creature = handler.GetSelectedCreature();
            if (creature == null)
            {
                handler.SendSysMessage(CypherStrings.SelectCreature);
                return false;
            }

            creature.Say(text, Language.Universal);

            // make some emotes
            switch (((string)text).LastOrDefault())
            {
                case '?':
                    creature.HandleEmoteCommand(Emote.OneshotQuestion);
                    break;
                case '!':
                    creature.HandleEmoteCommand(Emote.OneshotExclamation);
                    break;
                default:
                    creature.HandleEmoteCommand(Emote.OneshotTalk);
                    break;
            }

            return true;
        }

        [Command("showloot", RBACPermissions.CommandNpcShowloot)]
        static bool HandleNpcShowLootCommand(CommandHandler handler, string all)
        {
            Creature creatureTarget = handler.GetSelectedCreature();
            if (creatureTarget == null || creatureTarget.IsPet())
            {
                handler.SendSysMessage(CypherStrings.SelectCreature);
                return false;
            }

            if (!creatureTarget.IsDead())
            {
                handler.SendSysMessage(CypherStrings.CommandNotDeadOrNoLoot, creatureTarget.GetName());
                return false;
            }

            Loot loot = creatureTarget._loot;
            if ((loot == null || loot.IsLooted()) && creatureTarget.m_personalLoot.Count(p => p.Value.IsLooted()) == 0)
            {
                handler.SendSysMessage(CypherStrings.CommandNotDeadOrNoLoot, creatureTarget.GetName());
                return false;
            }

            handler.SendSysMessage(CypherStrings.CommandNpcShowlootHeader, creatureTarget.GetName(), creatureTarget.GetEntry());

            if (loot != null)
                _ShowLootContents(handler, all == "all", loot);
            else
            {
                handler.SendSysMessage(CypherStrings.CommandNpcShowlootLabel, "Standard items", loot.items.Count);
                foreach (LootItem item in loot.items)
                    if (!item.is_looted && !item.freeforall && item.conditions.IsEmpty())
                        _ShowLootEntry(handler, item.itemid, item.count);

                foreach (var (lootOwner, personalLoot) in creatureTarget.m_personalLoot)
                {
                    var character = Global.CharacterCacheStorage.GetCharacterCacheByGuid(lootOwner);
                    handler.SendSysMessage(CypherStrings.CommandNpcShowlootLabel2, $"Personal loot for {(character != null ? character.Name : "")}");
                    _ShowLootContents(handler, all == "all", personalLoot);
                }
            }

            return true;
        }

        [Command("spawngroup", RBACPermissions.CommandNpcSpawngroup)]
        static bool HandleNpcSpawnGroup(CommandHandler handler, string[] opts)
        {
            if (opts.Empty())
                return false;

            bool ignoreRespawn = false;
            bool force = false;
            uint groupId = 0;

            // Decode arguments
            foreach (var variant in opts)
            {
                switch (variant)
                {
                    case "force":
                        force = true;
                        break;
                    case "ignorerespawn":
                        ignoreRespawn = true;
                        break;
                    default:
                        uint.TryParse(variant, out groupId);
                        break;
                }
            }

            Player player = handler.GetSession().GetPlayer();

            List<WorldObject> creatureList = new();
            if (!player.GetMap().SpawnGroupSpawn(groupId, ignoreRespawn, force, creatureList))
            {
                handler.SendSysMessage(CypherStrings.SpawngroupBadgroup, groupId);
                return false;
            }

            handler.SendSysMessage(CypherStrings.SpawngroupSpawncount, creatureList.Count);

            return true;
        }

        [Command("tame", RBACPermissions.CommandNpcTame)]
        static bool HandleNpcTameCommand(CommandHandler handler)
        {
            Creature creatureTarget = handler.GetSelectedCreature();
            if (creatureTarget == null || creatureTarget.IsPet())
            {
                handler.SendSysMessage(CypherStrings.SelectCreature);
                return false;
            }

            Player player = handler.GetSession().GetPlayer();

            if (!player.GetPetGUID().IsEmpty())
            {
                handler.SendSysMessage(CypherStrings.YouAlreadyHavePet);
                return false;
            }

            CreatureTemplate cInfo = creatureTarget.GetCreatureTemplate();

            if (!cInfo.IsTameable(player.CanTameExoticPets(), creatureTarget.GetCreatureDifficulty()))
            {
                handler.SendSysMessage(CypherStrings.CreatureNonTameable, cInfo.Entry);
                return false;
            }

            // Everything looks OK, create new pet
            Pet pet = player.CreateTamedPetFrom(creatureTarget);
            if (pet == null)
            {
                handler.SendSysMessage(CypherStrings.CreatureNonTameable, cInfo.Entry);
                return false;
            }

            // place pet before player
            float x, y, z;
            player.GetClosePoint(out x, out y, out z, creatureTarget.GetCombatReach(), SharedConst.ContactDistance);
            pet.Relocate(x, y, z, MathFunctions.PI - player.GetOrientation());

            // set pet to defensive mode by default (some classes can't control controlled pets in fact).
            pet.SetReactState(ReactStates.Defensive);

            // calculate proper level
            uint level = Math.Max(player.GetLevel() - 5, creatureTarget.GetLevel());

            // prepare visual effect for levelup
            pet.SetLevel(level - 1);

            // add to world
            pet.GetMap().AddToMap(pet.ToCreature());

            // visual effect for levelup
            pet.SetLevel(level);

            // caster have pet now
            player.SetMinion(pet, true);

            pet.SavePetToDB(PetSaveMode.AsCurrent);
            player.PetSpellInitialize();

            return true;
        }

        [Command("textemote", RBACPermissions.CommandNpcTextemote)]
        static bool HandleNpcTextEmoteCommand(CommandHandler handler, Tail text)
        {
            Creature creature = handler.GetSelectedCreature();
            if (creature == null)
            {
                handler.SendSysMessage(CypherStrings.SelectCreature);
                return false;
            }

            creature.TextEmote(text);

            return true;
        }

        [Command("whisper", RBACPermissions.CommandNpcWhisper)]
        static bool HandleNpcWhisperCommand(CommandHandler handler, string recv, Tail text)
        {
            if (text.IsEmpty())
            {
                handler.SendSysMessage(CypherStrings.CmdSyntax);
                return false;
            }

            Creature creature = handler.GetSelectedCreature();
            if (creature == null)
            {
                handler.SendSysMessage(CypherStrings.SelectCreature);
                return false;
            }

            // check online security
            Player receiver = Global.ObjAccessor.FindPlayerByName(recv);
            if (handler.HasLowerSecurity(receiver, ObjectGuid.Empty))
                return false;

            creature.Whisper(text, Language.Universal, receiver);
            return true;
        }

        [Command("yell", RBACPermissions.CommandNpcYell)]
        static bool HandleNpcYellCommand(CommandHandler handler, Tail text)
        {
            if (text.IsEmpty())
                return false;

            Creature creature = handler.GetSelectedCreature();
            if (creature == null)
            {
                handler.SendSysMessage(CypherStrings.SelectCreature);
                return false;
            }

            creature.Yell(text, Language.Universal);

            // make an emote
            creature.HandleEmoteCommand(Emote.OneshotShout);

            return true;
        }

        static void _ShowLootEntry(CommandHandler handler, uint itemId, uint itemCount, bool alternateString = false)
        {
            string name = "Unknown item";

            ItemTemplate itemTemplate = Global.ObjectMgr.GetItemTemplate(itemId);
            if (itemTemplate != null)
                name = itemTemplate.GetName(handler.GetSessionDbcLocale());

            handler.SendSysMessage(CypherStrings.CommandNpcShowlootEntry, alternateString ? 6 : 3 /*number of bytes from following string*/, "\xE2\x94\x80\xE2\x94\x80",
                itemCount, ItemConst.ItemQualityColors[(int)(itemTemplate != null ? itemTemplate.GetQuality() : ItemQuality.Poor)], itemId, name, itemId);
        }

        static void _ShowLootCurrencyEntry(CommandHandler handler, uint currencyId, uint count, bool alternateString = false)
        {
            CurrencyTypesRecord currency = CliDB.CurrencyTypesStorage.LookupByKey(currencyId);
            string name = "Unknown currency";
            if (currency != null)
                name = currency.Name;

            handler.SendSysMessage(CypherStrings.CommandNpcShowLootCurrency, alternateString ? 6 : 3 /*number of bytes from following string*/, "\u2500\u2500",
                count, ItemConst.ItemQualityColors[currency != null ? currency.Quality : (int)ItemQuality.Poor], currencyId, count, name, currencyId);
        }

        static void _ShowLootTrackingQuestCurrencyEntry(CommandHandler handler, uint questId, bool alternateString = false)
        {
            Quest quest = Global.ObjectMgr.GetQuestTemplate(questId);
            string name = "Unknown quest";
            if (quest != null)
            {
                name = quest.LogTitle;
                if (handler.GetSessionDbcLocale() != Locale.enUS)
                {
                    QuestTemplateLocale localeData = Global.ObjectMgr.GetQuestLocale(questId);
                    if (localeData != null)
                        ObjectManager.GetLocaleString(localeData.LogTitle, handler.GetSessionDbcLocale(), ref name);
                }
            }
            handler.SendSysMessage(CypherStrings.CommandNpcShowlootTrackingQuest, alternateString ? 6 : 3 /*number of bytes from following string*/, "\xE2\x94\x80\xE2\x94\x80", questId, name, questId);
        }

        static void _IterateNotNormalLootMap(CommandHandler handler, MultiMap<ObjectGuid, NotNormalLootItem> map, List<LootItem> items)
        {
            foreach (var key in map.Keys)
            {
                if (map[key].Empty())
                    continue;

                var list = map[key];

                Player player = Global.ObjAccessor.FindConnectedPlayer(key);
                handler.SendSysMessage(CypherStrings.CommandNpcShowlootSublabel, player != null ? player.GetName() : $"Offline player (GUID {key})", list.Count);

                foreach (var it in list)
                {
                    LootItem item = items[it.LootListId];
                    if (!it.is_looted && !item.is_looted)
                    {
                        switch (item.type)
                        {
                            case LootItemType.Item:
                                _ShowLootEntry(handler, item.itemid, item.count, true);
                                break;
                            case LootItemType.Currency:
                                _ShowLootCurrencyEntry(handler, item.itemid, item.count, true);
                                break;
                            case LootItemType.TrackingQuest:
                                _ShowLootTrackingQuestCurrencyEntry(handler, item.itemid, true);
                                break;
                        }
                    }
                }
            }
        }

        static void _ShowLootContents(CommandHandler handler, bool all, Loot loot)
        {
            handler.SendSysMessage(CypherStrings.CommandNpcShowlootMoney, loot.gold / MoneyConstants.Gold, (loot.gold % MoneyConstants.Gold) / MoneyConstants.Silver, loot.gold % MoneyConstants.Silver);

            if (!all)
            {
                handler.SendSysMessage(CypherStrings.CommandNpcShowlootLabel, "Standard items", loot.items.Count);
                foreach (LootItem item in loot.items)
                {
                    if (!item.is_looted)
                    {
                        switch (item.type)
                        {
                            case LootItemType.Item:
                                _ShowLootEntry(handler, item.itemid, item.count);
                                break;
                            case LootItemType.Currency:
                                _ShowLootCurrencyEntry(handler, item.itemid, item.count);
                                break;
                            case LootItemType.TrackingQuest:
                                _ShowLootTrackingQuestCurrencyEntry(handler, item.itemid);
                                break;
                        }
                    }
                }
            }
            else
            {
                handler.SendSysMessage(CypherStrings.CommandNpcShowlootLabel, "Standard items", loot.items.Count);
                foreach (LootItem item in loot.items)
                {
                    if (!item.is_looted && !item.freeforall && item.conditions.IsEmpty())
                    {
                        switch (item.type)
                        {
                            case LootItemType.Item:
                                _ShowLootEntry(handler, item.itemid, item.count);
                                break;
                            case LootItemType.Currency:
                                _ShowLootCurrencyEntry(handler, item.itemid, item.count);
                                break;
                            case LootItemType.TrackingQuest:
                                _ShowLootTrackingQuestCurrencyEntry(handler, item.itemid);
                                break;
                        }
                    }
                }

                if (!loot.GetPlayerFFAItems().Empty())
                {
                    handler.SendSysMessage(CypherStrings.CommandNpcShowlootLabel2, "FFA items per allowed player");
                    _IterateNotNormalLootMap(handler, loot.GetPlayerFFAItems(), loot.items);
                }
            }
        }

        [CommandGroup("add")]
        class AddCommands
        {
            [Command("", RBACPermissions.CommandNpcAdd)]
            static bool HandleNpcAddCommand(CommandHandler handler, uint id)
            {
                if (Global.ObjectMgr.GetCreatureTemplate(id) == null)
                    return false;

                Player chr = handler.GetSession().GetPlayer();
                Map map = chr.GetMap();

                Transport trans = chr.GetTransport<Transport>();
                if (trans != null)
                {
                    ulong guid = Global.ObjectMgr.GenerateCreatureSpawnId();
                    CreatureData data = Global.ObjectMgr.NewOrExistCreatureData(guid);
                    data.SpawnId = guid;
                    data.spawnGroupData = Global.ObjectMgr.GetDefaultSpawnGroup();
                    data.Id = id;
                    data.SpawnPoint.Relocate(chr.GetTransOffsetX(), chr.GetTransOffsetY(), chr.GetTransOffsetZ(), chr.GetTransOffsetO());
                    data.spawnGroupData = new();

                    Creature creaturePassenger = trans.CreateNPCPassenger(guid, data);
                    if (creaturePassenger != null)
                    {
                        creaturePassenger.SaveToDB((uint)trans.GetGoInfo().MoTransport.SpawnMap, new List<Difficulty>() { map.GetDifficultyID() });
                        Global.ObjectMgr.AddCreatureToGrid(data);
                    }
                    return true;
                }

                Creature creature = Creature.CreateCreature(id, map, chr.GetPosition());
                if (creature == null)
                    return false;

                PhasingHandler.InheritPhaseShift(creature, chr);
                creature.SaveToDB(map.GetId(), new List<Difficulty>() { map.GetDifficultyID() });

                ulong db_guid = creature.GetSpawnId();

                // To call _LoadGoods(); _LoadQuests(); CreateTrainerSpells()
                // current "creature" variable is deleted and created fresh new, otherwise old values might trigger asserts or cause undefined behavior
                creature.CleanupsBeforeDelete();
                creature = Creature.CreateCreatureFromDB(db_guid, map, true, true);
                if (creature == null)
                    return false;

                Global.ObjectMgr.AddCreatureToGrid(Global.ObjectMgr.GetCreatureData(db_guid));
                return true;
            }

            [Command("item", RBACPermissions.CommandNpcAddItem)]
            static bool HandleNpcAddVendorItemCommand(CommandHandler handler, uint itemId, OptionalArg<uint> mc, OptionalArg<uint> it, OptionalArg<uint> ec, OptionalArg<string> bonusListIds)
            {
                if (itemId == 0)
                {
                    handler.SendSysMessage(CypherStrings.CommandNeeditemsend);
                    return false;
                }

                Creature vendor = handler.GetSelectedCreature();
                if (vendor == null)
                {
                    handler.SendSysMessage(CypherStrings.SelectCreature);
                    return false;
                }

                uint maxcount = mc.GetValueOrDefault(0);
                uint incrtime = it.GetValueOrDefault(0);
                uint extendedcost = ec.GetValueOrDefault(0);
                uint vendor_entry = vendor.GetEntry();

                VendorItem vItem = new();
                vItem.item = itemId;
                vItem.maxcount = maxcount;
                vItem.incrtime = incrtime;
                vItem.ExtendedCost = extendedcost;
                vItem.Type = ItemVendorType.Item;

                if (bonusListIds.HasValue)
                {
                    var bonusListIDsTok = new StringArray(bonusListIds, ';');
                    if (!bonusListIDsTok.IsEmpty())
                    {
                        foreach (string token in bonusListIDsTok)
                        {
                            if (uint.TryParse(token, out uint id))
                                vItem.BonusListIDs.Add(id);
                        }
                    }
                }

                if (!Global.ObjectMgr.IsVendorItemValid(vendor_entry, vItem, handler.GetSession().GetPlayer()))
                    return false;

                Global.ObjectMgr.AddVendorItem(vendor_entry, vItem);

                ItemTemplate itemTemplate = Global.ObjectMgr.GetItemTemplate(itemId);

                handler.SendSysMessage(CypherStrings.ItemAddedToList, itemId, itemTemplate.GetName(), maxcount, incrtime, extendedcost);
                return true;
            }

            [Command("move", RBACPermissions.CommandNpcAddMove)]
            static bool HandleNpcAddMoveCommand(CommandHandler handler, ulong lowGuid)
            {
                // attempt check creature existence by DB data
                CreatureData data = Global.ObjectMgr.GetCreatureData(lowGuid);
                if (data == null)
                {
                    handler.SendSysMessage(CypherStrings.CommandCreatguidnotfound, lowGuid);
                    return false;
                }

                // Update movement type
                PreparedStatement stmt = WorldDatabase.GetPreparedStatement(WorldStatements.UPD_CREATURE_MOVEMENT_TYPE);
                stmt.AddValue(0, (byte)MovementGeneratorType.Waypoint);
                stmt.AddValue(1, lowGuid);
                DB.World.Execute(stmt);

                handler.SendSysMessage(CypherStrings.WaypointAdded);

                return true;
            }

            [Command("formation", RBACPermissions.CommandNpcAddFormation)]
            static bool HandleNpcAddFormationCommand(CommandHandler handler, ulong leaderGUID, OptionalArg<bool> linkedAggro, OptionalArg<bool> formationMovement)
            {
                Creature creature = handler.GetSelectedCreature();
                if (creature == null || creature.GetSpawnId() == 0)
                {
                    handler.SendSysMessage(CypherStrings.SelectCreature);
                    return false;
                }

                ulong lowguid = creature.GetSpawnId();
                if (creature.GetFormation() != null)
                {
                    handler.SendSysMessage("Selected creature is already member of group {0}", creature.GetFormation().GetLeaderSpawnId());
                    return false;
                }

                if (lowguid == 0)
                    return false;

                Player chr = handler.GetSession().GetPlayer();
                float followAngle = creature.GetRelativeAngle(chr) * 180.0f / MathF.PI;
                float followDist = chr.GetExactDist2d(creature);
                GroupAIFlags groupAI = 0;
                if (linkedAggro == true)
                    groupAI |= GroupAIFlags.MembersAssistMember;
                if (formationMovement == true)
                    groupAI |= GroupAIFlags.IdleInFormation;

                FormationMgr.AddFormationMember(lowguid, followAngle, followDist, leaderGUID, (uint)groupAI);
                creature.SearchFormation();

                PreparedStatement stmt = WorldDatabase.GetPreparedStatement(WorldStatements.INS_CREATURE_FORMATION);
                stmt.AddValue(0, leaderGUID);
                stmt.AddValue(1, lowguid);
                stmt.AddValue(2, followDist);
                stmt.AddValue(3, followAngle);
                stmt.AddValue(4, (uint)groupAI);

                DB.World.Execute(stmt);

                handler.SendSysMessage("Creature {0} added to formation with leader {1}", lowguid, leaderGUID);

                return true;
            }

            [Command("temp", RBACPermissions.CommandNpcAddTemp)]
            static bool HandleNpcAddTempSpawnCommand(CommandHandler handler, OptionalArg<string> lootStr, uint id)
            {
                bool loot = false;
                if (lootStr.HasValue)
                {
                    if (lootStr.Value.Equals("loot", StringComparison.OrdinalIgnoreCase))
                        loot = true;
                    else if (lootStr.Value.Equals("noloot", StringComparison.OrdinalIgnoreCase))
                        loot = false;
                    else
                        return false;
                }

                if (Global.ObjectMgr.GetCreatureTemplate(id) == null)
                    return false;

                Player chr = handler.GetSession().GetPlayer();
                chr.SummonCreature(id, chr.GetPosition(), loot ? TempSummonType.CorpseTimedDespawn : TempSummonType.CorpseDespawn, TimeSpan.FromSeconds(30));

                return true;
            }
        }

        [CommandGroup("delete")]
        class DeleteCommands
        {
            [Command("", RBACPermissions.CommandNpcDelete)]
            static bool HandleNpcDeleteCommand(CommandHandler handler, OptionalArg<ulong> spawnIdArg)
            {
                ulong spawnId;
                if (spawnIdArg.HasValue)
                    spawnId = spawnIdArg.Value;
                else
                {
                    Creature creature = handler.GetSelectedCreature();
                    if (creature == null || creature.IsPet() || creature.IsTotem())
                    {
                        handler.SendSysMessage(CypherStrings.SelectCreature);
                        return false;
                    }

                    TempSummon summon = creature.ToTempSummon();
                    if (summon != null)
                    {
                        summon.UnSummon();
                        handler.SendSysMessage(CypherStrings.CommandDelcreatmessage);
                        return true;
                    }
                    spawnId = creature.GetSpawnId();
                }

                if (Creature.DeleteFromDB(spawnId))
                {
                    handler.SendSysMessage(CypherStrings.CommandDelcreatmessage);
                    return true;
                }

                handler.SendSysMessage(CypherStrings.CommandCreatguidnotfound, spawnId);
                return false;
            }

            [Command("item", RBACPermissions.CommandNpcDeleteItem)]
            static bool HandleNpcDeleteVendorItemCommand(CommandHandler handler, uint itemId)
            {
                Creature vendor = handler.GetSelectedCreature();
                if (vendor == null || !vendor.IsVendor())
                {
                    handler.SendSysMessage(CypherStrings.CommandVendorselection);
                    return false;
                }

                if (itemId == 0)
                    return false;

                if (!Global.ObjectMgr.RemoveVendorItem(vendor.GetEntry(), itemId, ItemVendorType.Item))
                {
                    handler.SendSysMessage(CypherStrings.ItemNotInList, itemId);
                    return false;
                }

                ItemTemplate itemTemplate = Global.ObjectMgr.GetItemTemplate(itemId);
                handler.SendSysMessage(CypherStrings.ItemDeletedFromList, itemId, itemTemplate.GetName());
                return true;
            }
        }

        [CommandGroup("follow")]
        class FollowCommands
        {
            [Command("", RBACPermissions.CommandNpcFollow)]
            static bool HandleNpcFollowCommand(CommandHandler handler)
            {
                Player player = handler.GetSession().GetPlayer();
                Creature creature = handler.GetSelectedCreature();

                if (creature == null)
                {
                    handler.SendSysMessage(CypherStrings.SelectCreature);
                    return false;
                }

                // Follow player - Using pet's default dist and angle
                creature.GetMotionMaster().MoveFollow(player, SharedConst.PetFollowDist, creature.GetFollowAngle());

                handler.SendSysMessage(CypherStrings.CreatureFollowYouNow, creature.GetName());
                return true;
            }

            [Command("stop", RBACPermissions.CommandNpcFollowStop)]
            static bool HandleNpcUnFollowCommand(CommandHandler handler)
            {
                Player player = handler.GetPlayer();
                Creature creature = handler.GetSelectedCreature();

                if (creature == null)
                {
                    handler.SendSysMessage(CypherStrings.SelectCreature);
                    return false;
                }

                MovementGenerator movement = creature.GetMotionMaster().GetMovementGenerator(a =>
                {
                    if (a.GetMovementGeneratorType() == MovementGeneratorType.Follow)
                    {
                        return a is FollowMovementGenerator followMovement && followMovement.GetTarget() == player;
                    }
                    return false;
                });

                if (movement != null)
                {
                    handler.SendSysMessage(CypherStrings.CreatureNotFollowYou, creature.GetName());
                    return false;
                }

                creature.GetMotionMaster().Remove(movement);
                handler.SendSysMessage(CypherStrings.CreatureNotFollowYouNow, creature.GetName());
                return true;
            }
        }

        [CommandGroup("set")]
        class SetCommands
        {
            [Command("allowmove", RBACPermissions.CommandNpcSetAllowmove)]
            static bool HandleNpcSetAllowMovementCommand(CommandHandler handler)
            {
                /*
                if (Global.WorldMgr.getAllowMovement())
                {
                    Global.WorldMgr.SetAllowMovement(false);
                    handler.SendSysMessage(LANG_CREATURE_MOVE_DISABLED);
                }
                else
                {
                    Global.WorldMgr.SetAllowMovement(true);
                    handler.SendSysMessage(LANG_CREATURE_MOVE_ENABLED);
                }
                */
                return true;
            }

            [Command("data", RBACPermissions.CommandNpcSetData)]
            static bool HandleNpcSetDataCommand(CommandHandler handler, uint data_1, uint data_2)
            {
                Creature creature = handler.GetSelectedCreature();
                if (creature == null)
                {
                    handler.SendSysMessage(CypherStrings.SelectCreature);
                    return false;
                }

                creature.GetAI().SetData(data_1, data_2);
                string AIorScript = creature.GetAIName() != "" ? "AI type: " + creature.GetAIName() : (creature.GetScriptName() != "" ? "Script Name: " + creature.GetScriptName() : "No AI or Script Name Set");
                handler.SendSysMessage(CypherStrings.NpcSetdata, creature.GetGUID(), creature.GetEntry(), creature.GetName(), data_1, data_2, AIorScript);
                return true;
            }

            [Command("entry", RBACPermissions.CommandNpcSetEntry)]
            static bool HandleNpcSetEntryCommand(CommandHandler handler, uint newEntryNum)
            {
                if (newEntryNum == 0)
                    return false;

                Unit unit = handler.GetSelectedUnit();
                if (unit == null || !unit.IsTypeId(TypeId.Unit))
                {
                    handler.SendSysMessage(CypherStrings.SelectCreature);
                    return false;
                }
                Creature creature = unit.ToCreature();
                if (creature.UpdateEntry(newEntryNum))
                    handler.SendSysMessage(CypherStrings.Done);
                else
                    handler.SendSysMessage(CypherStrings.Error);
                return true;

            }

            [Command("factionid", RBACPermissions.CommandNpcSetFactionid)]
            static bool HandleNpcSetFactionIdCommand(CommandHandler handler, uint factionId)
            {
                if (!CliDB.FactionTemplateStorage.ContainsKey(factionId))
                {
                    handler.SendSysMessage(CypherStrings.WrongFaction, factionId);
                    return false;
                }

                Creature creature = handler.GetSelectedCreature();
                if (creature == null)
                {
                    handler.SendSysMessage(CypherStrings.SelectCreature);
                    return false;
                }

                creature.SetFaction(factionId);

                // Faction is set in creature_template - not inside creature

                // Update in memory..
                CreatureTemplate cinfo = creature.GetCreatureTemplate();
                if (cinfo != null)
                    cinfo.Faction = factionId;

                // ..and DB
                PreparedStatement stmt = WorldDatabase.GetPreparedStatement(WorldStatements.UPD_CREATURE_FACTION);

                stmt.AddValue(0, factionId);
                stmt.AddValue(1, factionId);
                stmt.AddValue(2, creature.GetEntry());

                DB.World.Execute(stmt);

                return true;
            }

            [Command("flag", RBACPermissions.CommandNpcSetFlag)]
            static bool HandleNpcSetFlagCommand(CommandHandler handler, NPCFlags npcFlags, NPCFlags2 npcFlags2)
            {
                Creature creature = handler.GetSelectedCreature();
                if (creature == null)
                {
                    handler.SendSysMessage(CypherStrings.SelectCreature);
                    return false;
                }

                creature.ReplaceAllNpcFlags(npcFlags);
                creature.ReplaceAllNpcFlags2(npcFlags2);

                PreparedStatement stmt = WorldDatabase.GetPreparedStatement(WorldStatements.UPD_CREATURE_NPCFLAG);
                stmt.AddValue(0, (ulong)npcFlags | ((ulong)npcFlags2 << 32));
                stmt.AddValue(1, creature.GetEntry());
                DB.World.Execute(stmt);

                handler.SendSysMessage(CypherStrings.ValueSavedRejoin);

                return true;
            }

            [Command("level", RBACPermissions.CommandNpcSetLevel)]
            static bool HandleNpcSetLevelCommand(CommandHandler handler, byte lvl)
            {
                if (lvl < 1 || lvl > WorldConfig.GetIntValue(WorldCfg.MaxPlayerLevel) + 3)
                {
                    handler.SendSysMessage(CypherStrings.BadValue);
                    return false;
                }

                Creature creature = handler.GetSelectedCreature();
                if (creature == null || creature.IsPet())
                {
                    handler.SendSysMessage(CypherStrings.SelectCreature);
                    return false;
                }

                creature.SetLevel(lvl);
                creature.UpdateLevelDependantStats();
                creature.SaveToDB();

                return true;
            }

            [Command("link", RBACPermissions.CommandNpcSetLink)]
            static bool HandleNpcSetLinkCommand(CommandHandler handler, ulong linkguid)
            {
                Creature creature = handler.GetSelectedCreature();
                if (creature == null)
                {
                    handler.SendSysMessage(CypherStrings.SelectCreature);
                    return false;
                }

                if (creature.GetSpawnId() == 0)
                {
                    handler.SendSysMessage("Selected creature {0} isn't in creature table", creature.GetGUID().ToString());
                    return false;
                }

                if (!Global.ObjectMgr.SetCreatureLinkedRespawn(creature.GetSpawnId(), linkguid))
                {
                    handler.SendSysMessage("Selected creature can't link with guid '{0}'", linkguid);
                    return false;
                }

                handler.SendSysMessage("LinkGUID '{0}' added to creature with DBTableGUID: '{1}'", linkguid, creature.GetSpawnId());
                return true;
            }

            [Command("model", RBACPermissions.CommandNpcSetModel)]
            static bool HandleNpcSetModelCommand(CommandHandler handler, uint displayId)
            {
                Creature creature = handler.GetSelectedCreature();
                if (creature == null || creature.IsPet())
                {
                    handler.SendSysMessage(CypherStrings.SelectCreature);
                    return false;
                }

                if (!CliDB.CreatureDisplayInfoStorage.ContainsKey(displayId))
                {
                    handler.SendSysMessage(CypherStrings.CommandInvalidParam, displayId);
                    return false;
                }

                creature.SetDisplayId(displayId, true);

                creature.SaveToDB();

                return true;
            }

            [Command("movetype", RBACPermissions.CommandNpcSetMovetype)]
            static bool HandleNpcSetMoveTypeCommand(CommandHandler handler, OptionalArg<ulong> lowGuid, string type, string nodel)
            {
                // 3 arguments:
                // GUID (optional - you can also select the creature)
                // stay|random|way (determines the kind of movement)
                // NODEL (optional - tells the system NOT to delete any waypoints)
                //        this is very handy if you want to do waypoints, that are
                //        later switched on/off according to special events (like escort
                //        quests, etc)
                bool doNotDelete = !nodel.IsEmpty();

                ulong lowguid = 0;
                Creature creature = null;

                if (!lowGuid.HasValue)                                           // case .setmovetype $move_type (with selected creature)
                {
                    creature = handler.GetSelectedCreature();
                    if (creature == null || creature.IsPet())
                        return false;

                    lowguid = creature.GetSpawnId();
                }
                else
                {
                    lowguid = lowGuid.Value;

                    if (lowguid != 0)
                        creature = handler.GetCreatureFromPlayerMapByDbGuid(lowguid);

                    // attempt check creature existence by DB data
                    if (creature == null)
                    {
                        CreatureData data = Global.ObjectMgr.GetCreatureData(lowguid);
                        if (data == null)
                        {
                            handler.SendSysMessage(CypherStrings.CommandCreatguidnotfound, lowguid);
                            return false;
                        }
                    }
                    else
                    {
                        lowguid = creature.GetSpawnId();
                    }
                }

                // now lowguid is low guid really existed creature
                // and creature point (maybe) to this creature or NULL

                MovementGeneratorType move_type;

                switch (type)
                {
                    case "stay":
                        move_type = MovementGeneratorType.Idle;
                        break;
                    case "random":
                        move_type = MovementGeneratorType.Random;
                        break;
                    case "way":
                        move_type = MovementGeneratorType.Waypoint;
                        break;
                    default:
                        return false;
                }

                if (creature != null)
                {
                    // update movement type
                    if (!doNotDelete)
                        creature.LoadPath(0);

                    creature.SetDefaultMovementType(move_type);
                    creature.GetMotionMaster().Initialize();
                    if (creature.IsAlive())                            // dead creature will reset movement generator at respawn
                    {
                        creature.SetDeathState(DeathState.JustDied);
                        creature.Respawn();
                    }
                    creature.SaveToDB();
                }

                if (!doNotDelete)
                {
                    handler.SendSysMessage(CypherStrings.MoveTypeSet, type);
                }
                else
                {
                    handler.SendSysMessage(CypherStrings.MoveTypeSetNodel, type);
                }

                return true;
            }

            [Command("phase", RBACPermissions.CommandNpcSetPhase)]
            static bool HandleNpcSetPhaseCommand(CommandHandler handler, uint phaseId)
            {
                if (phaseId == 0)
                {
                    handler.SendSysMessage(CypherStrings.PhaseNotfound);
                    return false;
                }

                Creature creature = handler.GetSelectedCreature();
                if (creature == null || creature.IsPet())
                {
                    handler.SendSysMessage(CypherStrings.SelectCreature);
                    return false;
                }

                PhasingHandler.ResetPhaseShift(creature);
                PhasingHandler.AddPhase(creature, phaseId, true);
                creature.SetDBPhase((int)phaseId);

                creature.SaveToDB();
                return true;
            }

            [Command("phasegroup", RBACPermissions.CommandNpcSetPhase)]
            static bool HandleNpcSetPhaseGroup(CommandHandler handler, StringArguments args)
            {
                if (args.Empty())
                    return false;

                int phaseGroupId = args.NextInt32();

                Creature creature = handler.GetSelectedCreature();
                if (creature == null || creature.IsPet())
                {
                    handler.SendSysMessage(CypherStrings.SelectCreature);
                    return false;
                }

                PhasingHandler.ResetPhaseShift(creature);
                PhasingHandler.AddPhaseGroup(creature, (uint)phaseGroupId, true);
                creature.SetDBPhase(-phaseGroupId);

                creature.SaveToDB();

                return true;
            }

            [Command("wanderdistance", RBACPermissions.CommandNpcSetSpawndist)]
            static bool HandleNpcSetWanderDistanceCommand(CommandHandler handler, float option)
            {
                if (option < 0.0f)
                {
                    handler.SendSysMessage(CypherStrings.BadValue);
                    return false;
                }

                MovementGeneratorType mtype = MovementGeneratorType.Idle;
                if (option > 0.0f)
                    mtype = MovementGeneratorType.Random;

                Creature creature = handler.GetSelectedCreature();
                ulong guidLow;

                if (creature != null)
                    guidLow = creature.GetSpawnId();
                else
                    return false;

                creature.SetWanderDistance(option);
                creature.SetDefaultMovementType(mtype);
                creature.GetMotionMaster().Initialize();
                if (creature.IsAlive())                                // dead creature will reset movement generator at respawn
                {
                    creature.SetDeathState(DeathState.JustDied);
                    creature.Respawn();
                }

                PreparedStatement stmt = WorldDatabase.GetPreparedStatement(WorldStatements.UPD_CREATURE_WANDER_DISTANCE);
                stmt.AddValue(0, option);
                stmt.AddValue(1, (byte)mtype);
                stmt.AddValue(2, guidLow);

                DB.World.Execute(stmt);

                handler.SendSysMessage(CypherStrings.CommandWanderDistance, option);
                return true;
            }

            [Command("spawntime", RBACPermissions.CommandNpcSetSpawntime)]
            static bool HandleNpcSetSpawnTimeCommand(CommandHandler handler, uint spawnTime)
            {
                Creature creature = handler.GetSelectedCreature();
                if (creature == null)
                    return false;

                PreparedStatement stmt = WorldDatabase.GetPreparedStatement(WorldStatements.UPD_CREATURE_SPAWN_TIME_SECS);
                stmt.AddValue(0, spawnTime);
                stmt.AddValue(1, creature.GetSpawnId());
                DB.World.Execute(stmt);

                creature.SetRespawnDelay(spawnTime);
                handler.SendSysMessage(CypherStrings.CommandSpawntime, spawnTime);

                return true;
            }
        }
    }
}
