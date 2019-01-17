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
using Framework.GameMath;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Game.Entities
{
    [StructLayout(LayoutKind.Explicit)]
    public class GameObjectTemplate
    {
        [FieldOffset(0)]
        public uint entry;

        [FieldOffset(4)]
        public GameObjectTypes type;

        [FieldOffset(8)]
        public uint displayId;

        [FieldOffset(16)]
        public string name;

        [FieldOffset(24)]
        public string IconName;

        [FieldOffset(32)]
        public string castBarCaption;

        [FieldOffset(40)]
        public string unk1;

        [FieldOffset(48)]
        public float size;

        [FieldOffset(52)]
        public int RequiredLevel;

        [FieldOffset(56)]
        public string AIName;

        [FieldOffset(64)]
        public uint ScriptId;

        [FieldOffset(68)]
        public door Door;

        [FieldOffset(68)]
        public button Button;

        [FieldOffset(68)]
        public questgiver QuestGiver;

        [FieldOffset(68)]
        public chest Chest;

        [FieldOffset(68)]
        public generic Generic;

        [FieldOffset(68)]
        public trap Trap;

        [FieldOffset(68)]
        public chair Chair;

        [FieldOffset(68)]
        public spellFocus SpellFocus;

        [FieldOffset(68)]
        public text Text;

        [FieldOffset(68)]
        public goober Goober;

        [FieldOffset(68)]
        public transport Transport;

        [FieldOffset(68)]
        public areadamage AreaDamage;

        [FieldOffset(68)]
        public camera Camera;

        [FieldOffset(68)]
        public moTransport MoTransport;

        [FieldOffset(68)]
        public ritual Ritual;

        [FieldOffset(68)]
        public mailbox MailBox;

        [FieldOffset(68)]
        public guardpost GuardPost;

        [FieldOffset(68)]
        public spellcaster SpellCaster;

        [FieldOffset(68)]
        public meetingstone MeetingStone;

        [FieldOffset(68)]
        public flagstand FlagStand;

        [FieldOffset(68)]
        public fishinghole FishingHole;

        [FieldOffset(68)]
        public flagdrop FlagDrop;

        [FieldOffset(68)]
        public controlzone ControlZone;

        [FieldOffset(68)]
        public auraGenerator AuraGenerator;

        [FieldOffset(68)]
        public dungeonDifficulty DungeonDifficulty;

        [FieldOffset(68)]
        public barberChair BarberChair;

        [FieldOffset(68)]
        public destructiblebuilding DestructibleBuilding;

        [FieldOffset(68)]
        public guildbank GuildBank;

        [FieldOffset(68)]
        public trapDoor TrapDoor;

        [FieldOffset(68)]
        public newflag NewFlag;

        [FieldOffset(68)]
        public newflagdrop NewFlagDrop;

        [FieldOffset(68)]
        public Garrisonbuilding garrisonBuilding;

        [FieldOffset(68)]
        public garrisonplot GarrisonPlot;

        [FieldOffset(68)]
        public clientcreature ClientCreature;

        [FieldOffset(68)]
        public clientitem ClientItem;

        [FieldOffset(68)]
        public capturepoint CapturePoint;

        [FieldOffset(68)]
        public phaseablemo PhaseableMO;

        [FieldOffset(68)]
        public garrisonmonument GarrisonMonument;

        [FieldOffset(68)]
        public garrisonshipment GarrisonShipment;

        [FieldOffset(68)]
        public garrisonmonumentplaque GarrisonMonumentPlaque;

        [FieldOffset(68)]
        public artifactforge ArtifactForge;

        [FieldOffset(68)]
        public uilink UILink;

        [FieldOffset(68)]
        public keystonereceptacle KeystoneReceptacle;

        [FieldOffset(68)]
        public gatheringnode GatheringNode;

        [FieldOffset(68)]
        public challengemodereward ChallengeModeReward;

        [FieldOffset(68)]
        public multi Multi;

        [FieldOffset(68)]
        public siegeableMulti SiegeableMulti;

        [FieldOffset(68)]
        public siegeableMO SiegeableMO;

        [FieldOffset(68)]
        public pvpReward PvpReward;

        [FieldOffset(68)]
        public raw Raw;

        // helpers
        public bool IsDespawnAtAction()
        {
            switch (type)
            {
                case GameObjectTypes.Chest:
                    return Chest.consumable != 0;
                case GameObjectTypes.Goober:
                    return Goober.consumable != 0;
                default:
                    return false;
            }
        }

        public bool IsUsableMounted()
        {
            switch (type)
            {
                case GameObjectTypes.QuestGiver:
                    return QuestGiver.allowMounted != 0;
                case GameObjectTypes.Text:
                    return Text.allowMounted != 0;
                case GameObjectTypes.Goober:
                    return Goober.allowMounted != 0;
                case GameObjectTypes.SpellCaster:
                    return SpellCaster.allowMounted != 0;
                case GameObjectTypes.UILink:
                    return UILink.allowMounted != 0;
                default:
                    return false;
            }
        }

        public uint GetLockId()
        {
            switch (type)
            {
                case GameObjectTypes.Door:
                    return Door.open;
                case GameObjectTypes.Button:
                    return Button.open;
                case GameObjectTypes.QuestGiver:
                    return QuestGiver.open;
                case GameObjectTypes.Chest:
                    return Chest.open;
                case GameObjectTypes.Trap:
                    return Trap.open;
                case GameObjectTypes.Goober:
                    return Goober.open;
                case GameObjectTypes.AreaDamage:
                    return AreaDamage.open;
                case GameObjectTypes.Camera:
                    return Camera.open;
                case GameObjectTypes.FlagStand:
                    return FlagStand.open;
                case GameObjectTypes.FishingHole:
                    return FishingHole.open;
                case GameObjectTypes.FlagDrop:
                    return FlagDrop.open;
                case GameObjectTypes.NewFlag:
                    return NewFlag.open;
                case GameObjectTypes.NewFlagDrop:
                    return NewFlagDrop.open;
                case GameObjectTypes.CapturePoint:
                    return CapturePoint.open;
                case GameObjectTypes.GatheringNode:
                    return GatheringNode.open;
                case GameObjectTypes.ChallengeModeReward:
                    return ChallengeModeReward.open;
                case GameObjectTypes.PvpReward:
                    return PvpReward.open;
                default:
                    return 0;
            }
        }

        // despawn at targeting of cast?
        public bool GetDespawnPossibility()
        {
            switch (type)
            {
                case GameObjectTypes.Door:
                    return Door.noDamageImmune != 0;
                case GameObjectTypes.Button:
                    return Button.noDamageImmune != 0;
                case GameObjectTypes.QuestGiver:
                    return QuestGiver.noDamageImmune != 0;
                case GameObjectTypes.Goober:
                    return Goober.noDamageImmune != 0;
                case GameObjectTypes.FlagStand:
                    return FlagStand.noDamageImmune != 0;
                case GameObjectTypes.FlagDrop:
                    return FlagDrop.noDamageImmune != 0;
                default:
                    return true;
            }
        }

        // despawn at uses amount
        public uint GetCharges()
        {
            switch (type)
            {
                case GameObjectTypes.GuardPost:
                    return GuardPost.charges;
                case GameObjectTypes.SpellCaster:
                    return (uint)SpellCaster.charges;
                default:
                    return 0;
            }
        }

        public uint GetLinkedGameObjectEntry()
        {
            switch (type)
            {
                case GameObjectTypes.Button:
                    return Button.linkedTrap;
                case GameObjectTypes.Chest:
                    return Chest.linkedTrap;
                case GameObjectTypes.SpellFocus:
                    return SpellFocus.linkedTrap;
                case GameObjectTypes.Goober:
                    return Goober.linkedTrap;
                case GameObjectTypes.GatheringNode:
                    return GatheringNode.linkedTrap;
                default: return 0;
            }
        }

        public uint GetAutoCloseTime()
        {
            uint autoCloseTime = 0;
            switch (type)
            {
                case GameObjectTypes.Door:
                    autoCloseTime = Door.autoClose;
                    break;
                case GameObjectTypes.Button:
                    autoCloseTime = Button.autoClose;
                    break;
                case GameObjectTypes.Trap:
                    autoCloseTime = Trap.autoClose;
                    break;
                case GameObjectTypes.Goober:
                    autoCloseTime = Goober.autoClose;
                    break;
                case GameObjectTypes.Transport:
                    autoCloseTime = Transport.autoClose;
                    break;
                case GameObjectTypes.AreaDamage:
                    autoCloseTime = AreaDamage.autoClose;
                    break;
                case GameObjectTypes.TrapDoor:
                    autoCloseTime = TrapDoor.autoClose;
                    break;
                default:
                    break;
            }
            return autoCloseTime / Time.InMilliseconds;   // prior to 3.0.3, conversion was / 0x10000;
        }

        public uint GetLootId()
        {
            switch (type)
            {
                case GameObjectTypes.Chest:
                    return Chest.chestLoot;
                case GameObjectTypes.FishingHole:
                    return FishingHole.chestLoot;
                case GameObjectTypes.GatheringNode:
                    return GatheringNode.chestLoot;
                case GameObjectTypes.ChallengeModeReward:
                    return ChallengeModeReward.chestLoot;
                default: return 0;
            }
        }

        public uint GetGossipMenuId()
        {
            switch (type)
            {
                case GameObjectTypes.QuestGiver:
                    return QuestGiver.gossipID;
                case GameObjectTypes.Goober:
                    return Goober.gossipID;
                default:
                    return 0;
            }
        }

        public uint GetEventScriptId()
        {
            switch (type)
            {
                case GameObjectTypes.Goober:
                    return Goober.eventID;
                case GameObjectTypes.Chest:
                    return Chest.triggeredEvent;
                case GameObjectTypes.Camera:
                    return Camera.eventID;
                case GameObjectTypes.GatheringNode:
                    return GatheringNode.triggeredEvent;
                default:
                    return 0;
            }
        }

        // Cooldown preventing goober and traps to cast spell
        public uint GetCooldown()
        {
            switch (type)
            {
                case GameObjectTypes.Trap:
                    return Trap.cooldown;
                case GameObjectTypes.Goober:
                    return Goober.cooldown;
                default:
                    return 0;
            }
        }

        public bool IsInfiniteGameObject()
        {
            switch (type)
            {
                case GameObjectTypes.Door:
                    return Door.InfiniteAOI != 0;
                case GameObjectTypes.FlagStand:
                    return FlagStand.InfiniteAOI != 0;
                case GameObjectTypes.FlagDrop:
                    return FlagDrop.InfiniteAOI != 0;
                case GameObjectTypes.TrapDoor:
                    return TrapDoor.InfiniteAOI != 0;
                case GameObjectTypes.NewFlag:
                    return NewFlag.InfiniteAOI != 0;
                default: return false;
            }
        }

        public bool IsGiganticGameObject()
        {
            switch (type)
            {
                case GameObjectTypes.Door:
                    return Door.GiganticAOI != 0;
                case GameObjectTypes.Button:
                    return Button.GiganticAOI != 0;
                case GameObjectTypes.QuestGiver:
                    return QuestGiver.GiganticAOI != 0;
                case GameObjectTypes.Chest:
                    return Chest.GiganticAOI != 0;
                case GameObjectTypes.Generic:
                    return Generic.GiganticAOI != 0;
                case GameObjectTypes.Trap:
                    return Trap.GiganticAOI != 0;
                case GameObjectTypes.SpellFocus:
                    return SpellFocus.GiganticAOI != 0;
                case GameObjectTypes.Goober:
                    return Goober.GiganticAOI != 0;
                case GameObjectTypes.SpellCaster:
                    return SpellCaster.GiganticAOI != 0;
                case GameObjectTypes.FlagStand:
                    return FlagStand.GiganticAOI != 0;
                case GameObjectTypes.FlagDrop:
                    return FlagDrop.GiganticAOI != 0;
                case GameObjectTypes.ControlZone:
                    return ControlZone.GiganticAOI != 0;
                case GameObjectTypes.DungeonDifficulty:
                    return DungeonDifficulty.GiganticAOI != 0;
                case GameObjectTypes.TrapDoor:
                    return TrapDoor.GiganticAOI != 0;
                case GameObjectTypes.NewFlag:
                    return NewFlag.GiganticAOI != 0;
                case GameObjectTypes.CapturePoint:
                    return CapturePoint.GiganticAOI != 0;
                case GameObjectTypes.GarrisonShipment:
                    return GarrisonShipment.GiganticAOI != 0;
                case GameObjectTypes.UILink:
                    return UILink.GiganticAOI != 0;
                case GameObjectTypes.GatheringNode:
                    return GatheringNode.GiganticAOI != 0;
                default: return false;
            }
        }

        public bool IsLargeGameObject()
        {
            switch (type)
            {
                case GameObjectTypes.Chest:
                    return Chest.LargeAOI != 0;
                case GameObjectTypes.Generic:
                    return Generic.LargeAOI != 0;
                case GameObjectTypes.DungeonDifficulty:
                    return DungeonDifficulty.LargeAOI != 0;
                case GameObjectTypes.GarrisonShipment:
                    return GarrisonShipment.LargeAOI != 0;
                case GameObjectTypes.ArtifactForge:
                    return ArtifactForge.LargeAOI != 0;
                case GameObjectTypes.GatheringNode:
                    return GatheringNode.LargeAOI != 0;
                default: return false;
            }
        }

        #region TypeStructs
        public unsafe struct raw
        {
            public fixed int data[SharedConst.MaxGOData];
        }

        public struct door
        {
            public uint startOpen;                               // 0 startOpen, enum { false, true, }; Default: false
            public uint open;                                    // 1 open, References: Lock_, NoValue = 0
            public uint autoClose;                               // 2 autoClose (ms), int, Min value: 0, Max value: 2147483647, Default value: 3000
            public uint noDamageImmune;                          // 3 noDamageImmune, enum { false, true, }; Default: false
            public uint openTextID;                              // 4 openTextID, References: BroadcastText, NoValue = 0
            public uint closeTextID;                             // 5 closeTextID, References: BroadcastText, NoValue = 0
            public uint ignoredByPathing;                        // 6 Ignored By Pathing, enum { false, true, }; Default: false
            public uint conditionID1;                            // 7 conditionID1, References: PlayerCondition, NoValue = 0
            public uint DoorisOpaque;                            // 8 Door is Opaque (Disable portal on close), enum { false, true, }; Default: true
            public uint GiganticAOI;                             // 9 Gigantic AOI, enum { false, true, }; Default: false
            public uint InfiniteAOI;                             // 10 Infinite AOI, enum { false, true, }; Default: false
            public uint NotLOSBlocking;                          // 11 Not LOS Blocking, enum { false, true, }; Default: false
        }


        public struct button
        {
            public uint startOpen;                               // 0 startOpen, enum { false, true, }; Default: false
            public uint open;                                    // 1 open, References: Lock_, NoValue = 0
            public uint autoClose;                               // 2 autoClose (ms), int, Min value: 0, Max value: 2147483647, Default value: 3000
            public uint linkedTrap;                              // 3 linkedTrap, References: GameObjects, NoValue = 0
            public uint noDamageImmune;                          // 4 noDamageImmune, enum { false, true, }; Default: false
            public uint GiganticAOI;                             // 5 Gigantic AOI, enum { false, true, }; Default: false
            public uint openTextID;                              // 6 openTextID, References: BroadcastText, NoValue = 0
            public uint closeTextID;                             // 7 closeTextID, References: BroadcastText, NoValue = 0
            public uint requireLOS;                              // 8 require LOS, enum { false, true, }; Default: false
            public uint conditionID1;                            // 9 conditionID1, References: PlayerCondition, NoValue = 0
        }


        public struct questgiver
        {
            public uint open;                                    // 0 open, References: Lock_, NoValue = 0
            public uint questGiver;                              // 1 questGiver, References: QuestGiver, NoValue = 0
            public uint pageMaterial;                            // 2 pageMaterial, References: PageTextMaterial, NoValue = 0
            public uint gossipID;                                // 3 gossipID, References: Gossip, NoValue = 0
            public uint customAnim;                              // 4 customAnim, int, Min value: 0, Max value: 4, Default value: 0
            public uint noDamageImmune;                          // 5 noDamageImmune, enum { false, true, }; Default: false
            public uint openTextID;                              // 6 openTextID, References: BroadcastText, NoValue = 0
            public uint requireLOS;                              // 7 require LOS, enum { false, true, }; Default: false
            public uint allowMounted;                            // 8 allowMounted, enum { false, true, }; Default: false
            public uint GiganticAOI;                             // 9 Gigantic AOI, enum { false, true, }; Default: false
            public uint conditionID1;                            // 10 conditionID1, References: PlayerCondition, NoValue = 0
            public uint NeverUsableWhileMounted;                 // 11 Never Usable While Mounted, enum { false, true, }; Default: false
        }


        public struct chest
        {
            public uint open;                                    // 0 open, References: Lock_, NoValue = 0
            public uint chestLoot;                               // 1 chestLoot, References: Treasure, NoValue = 0
            public uint chestRestockTime;                        // 2 chestRestockTime, int, Min value: 0, Max value: 1800000, Default value: 0
            public uint consumable;                              // 3 consumable, enum { false, true, }; Default: false
            public uint minRestock;                              // 4 minRestock, int, Min value: 0, Max value: 65535, Default value: 0
            public uint maxRestock;                              // 5 maxRestock, int, Min value: 0, Max value: 65535, Default value: 0
            public uint triggeredEvent;                          // 6 triggeredEvent, References: GameEvents, NoValue = 0
            public uint linkedTrap;                              // 7 linkedTrap, References: GameObjects, NoValue = 0
            public uint questID;                                 // 8 questID, References: QuestV2, NoValue = 0
            public uint level;                                   // 9 level, int, Min value: 0, Max value: 65535, Default value: 0
            public uint requireLOS;                              // 10 require LOS, enum { false, true, }; Default: false
            public uint leaveLoot;                               // 11 leaveLoot, enum { false, true, }; Default: false
            public uint notInCombat;                             // 12 notInCombat, enum { false, true, }; Default: false
            public uint logloot;                                 // 13 log loot, enum { false, true, }; Default: false
            public uint openTextID;                              // 14 openTextID, References: BroadcastText, NoValue = 0
            public uint usegrouplootrules;                       // 15 use group loot rules, enum { false, true, }; Default: false
            public uint floatingTooltip;                         // 16 floatingTooltip, enum { false, true, }; Default: false
            public uint conditionID1;                            // 17 conditionID1, References: PlayerCondition, NoValue = 0
            public uint xpLevel;                                 // 18 XP Level Range, References: ContentTuning, NoValue = 0
            public uint xpDifficulty;                            // 19 xpDifficulty, enum { No Exp, Trivial, Very Small, Small, Substandard, Standard, High, Epic, Dungeon, 5, }; Default: No Exp
            public uint lootLevel;                               // 20 lootLevel, int, Min value: 0, Max value: 123, Default value: 0
            public uint GroupXP;                                 // 21 Group XP, enum { false, true, }; Default: false
            public uint DamageImmuneOK;                          // 22 Damage Immune OK, enum { false, true, }; Default: false
            public uint trivialSkillLow;                         // 23 trivialSkillLow, int, Min value: 0, Max value: 65535, Default value: 0
            public uint trivialSkillHigh;                        // 24 trivialSkillHigh, int, Min value: 0, Max value: 65535, Default value: 0
            public uint DungeonEncounter;                        // 25 Dungeon Encounter, References: DungeonEncounter, NoValue = 0
            public uint spell;                                   // 26 spell, References: Spell, NoValue = 0
            public uint GiganticAOI;                             // 27 Gigantic AOI, enum { false, true, }; Default: false
            public uint LargeAOI;                                // 28 Large AOI, enum { false, true, }; Default: false
            public uint SpawnVignette;                           // 29 Spawn Vignette, References: vignette, NoValue = 0
            public uint chestPersonalLoot;                       // 30 chest Personal Loot, References: Treasure, NoValue = 0
            public uint turnpersonallootsecurityoff;             // 31 turn personal loot security off, enum { false, true, }; Default: false
            public uint ChestProperties;                         // 32 Chest Properties, References: ChestProperties, NoValue = 0
            public uint chestPushLoot;                           // 33 chest Push Loot, References: Treasure, NoValue = 0
        }


        public struct generic
        {
            public uint floatingTooltip;                         // 0 floatingTooltip, enum { false, true, }; Default: false
            public uint highlight;                               // 1 highlight, enum { false, true, }; Default: true
            public uint serverOnly;                              // 2 serverOnly, enum { false, true, }; Default: false
            public uint GiganticAOI;                             // 3 Gigantic AOI, enum { false, true, }; Default: false
            public uint floatOnWater;                            // 4 floatOnWater, enum { false, true, }; Default: false
            public uint questID;                                 // 5 questID, References: QuestV2, NoValue = 0
            public uint conditionID1;                            // 6 conditionID1, References: PlayerCondition, NoValue = 0
            public uint LargeAOI;                                // 7 Large AOI, enum { false, true, }; Default: false
            public uint UseGarrisonOwnerGuildColors;             // 8 Use Garrison Owner Guild Colors, enum { false, true, }; Default: false
        }


        public struct trap
        {
            public uint open;                                    // 0 open, References: Lock_, NoValue = 0
            public uint level;                                   // 1 level, int, Min value: 0, Max value: 65535, Default value: 0
            public uint radius;                                  // 2 radius, int, Min value: 0, Max value: 100, Default value: 0
            public uint spell;                                   // 3 spell, References: Spell, NoValue = 0
            public uint charges;                                 // 4 charges, int, Min value: 0, Max value: 65535, Default value: 1
            public uint cooldown;                                // 5 cooldown, int, Min value: 0, Max value: 65535, Default value: 0
            public uint autoClose;                               // 6 autoClose (ms), int, Min value: 0, Max value: 2147483647, Default value: 0
            public uint startDelay;                              // 7 startDelay, int, Min value: 0, Max value: 65535, Default value: 0
            public uint serverOnly;                              // 8 serverOnly, enum { false, true, }; Default: false
            public uint stealthed;                               // 9 stealthed, enum { false, true, }; Default: false
            public uint GiganticAOI;                             // 10 Gigantic AOI, enum { false, true, }; Default: false
            public uint stealthAffected;                         // 11 stealthAffected, enum { false, true, }; Default: false
            public uint openTextID;                              // 12 openTextID, References: BroadcastText, NoValue = 0
            public uint closeTextID;                             // 13 closeTextID, References: BroadcastText, NoValue = 0
            public uint IgnoreTotems;                            // 14 Ignore Totems, enum { false, true, }; Default: false
            public uint conditionID1;                            // 15 conditionID1, References: PlayerCondition, NoValue = 0
            public uint playerCast;                              // 16 playerCast, enum { false, true, }; Default: false
            public uint SummonerTriggered;                       // 17 Summoner Triggered, enum { false, true, }; Default: false
            public uint requireLOS;                              // 18 require LOS, enum { false, true, }; Default: false
        }


        public struct chair
        {
            public uint chairslots;                              // 0 chairslots, int, Min value: 1, Max value: 5, Default value: 1
            public uint chairheight;                             // 1 chairheight, int, Min value: 0, Max value: 2, Default value: 1
            public uint onlyCreatorUse;                          // 2 onlyCreatorUse, enum { false, true, }; Default: false
            public uint triggeredEvent;                          // 3 triggeredEvent, References: GameEvents, NoValue = 0
            public uint conditionID1;                            // 4 conditionID1, References: PlayerCondition, NoValue = 0
        }


        public struct spellFocus
        {
            public uint spellFocusType;                          // 0 spellFocusType, References: SpellFocusObject, NoValue = 0
            public uint radius;                                  // 1 radius, int, Min value: 0, Max value: 50, Default value: 10
            public uint linkedTrap;                              // 2 linkedTrap, References: GameObjects, NoValue = 0
            public uint serverOnly;                              // 3 serverOnly, enum { false, true, }; Default: false
            public uint questID;                                 // 4 questID, References: QuestV2, NoValue = 0
            public uint GiganticAOI;                             // 5 Gigantic AOI, enum { false, true, }; Default: false
            public uint floatingTooltip;                         // 6 floatingTooltip, enum { false, true, }; Default: false
            public uint floatOnWater;                            // 7 floatOnWater, enum { false, true, }; Default: false
            public uint conditionID1;                            // 8 conditionID1, References: PlayerCondition, NoValue = 0
        }


        public struct text
        {
            public uint pageID;                                  // 0 pageID, References: PageText, NoValue = 0
            public uint language;                                // 1 language, References: Languages, NoValue = 0
            public uint pageMaterial;                            // 2 pageMaterial, References: PageTextMaterial, NoValue = 0
            public uint allowMounted;                            // 3 allowMounted, enum { false, true, }; Default: false
            public uint conditionID1;                            // 4 conditionID1, References: PlayerCondition, NoValue = 0
            public uint NeverUsableWhileMounted;                 // 5 Never Usable While Mounted, enum { false, true, }; Default: false
        }


        public struct goober
        {
            public uint open;                                    // 0 open, References: Lock_, NoValue = 0
            public uint questID;                                 // 1 questID, References: QuestV2, NoValue = 0
            public uint eventID;                                 // 2 eventID, References: GameEvents, NoValue = 0
            public uint autoClose;                               // 3 autoClose (ms), int, Min value: 0, Max value: 2147483647, Default value: 3000
            public uint customAnim;                              // 4 customAnim, int, Min value: 0, Max value: 4, Default value: 0
            public uint consumable;                              // 5 consumable, enum { false, true, }; Default: false
            public uint cooldown;                                // 6 cooldown, int, Min value: 0, Max value: 65535, Default value: 0
            public uint pageID;                                  // 7 pageID, References: PageText, NoValue = 0
            public uint language;                                // 8 language, References: Languages, NoValue = 0
            public uint pageMaterial;                            // 9 pageMaterial, References: PageTextMaterial, NoValue = 0
            public uint spell;                                   // 10 spell, References: Spell, NoValue = 0
            public uint noDamageImmune;                          // 11 noDamageImmune, enum { false, true, }; Default: false
            public uint linkedTrap;                              // 12 linkedTrap, References: GameObjects, NoValue = 0
            public uint GiganticAOI;                             // 13 Gigantic AOI, enum { false, true, }; Default: false
            public uint openTextID;                              // 14 openTextID, References: BroadcastText, NoValue = 0
            public uint closeTextID;                             // 15 closeTextID, References: BroadcastText, NoValue = 0
            public uint requireLOS;                              // 16 require LOS, enum { false, true, }; Default: false
            public uint allowMounted;                            // 17 allowMounted, enum { false, true, }; Default: false
            public uint floatingTooltip;                         // 18 floatingTooltip, enum { false, true, }; Default: false
            public uint gossipID;                                // 19 gossipID, References: Gossip, NoValue = 0
            public uint AllowMultiInteract;                      // 20 Allow Multi-Interact, enum { false, true, }; Default: false
            public uint floatOnWater;                            // 21 floatOnWater, enum { false, true, }; Default: false
            public uint conditionID1;                            // 22 conditionID1, References: PlayerCondition, NoValue = 0
            public uint playerCast;                              // 23 playerCast, enum { false, true, }; Default: false
            public uint SpawnVignette;                           // 24 Spawn Vignette, References: vignette, NoValue = 0
            public uint startOpen;                               // 25 startOpen, enum { false, true, }; Default: false
            public uint DontPlayOpenAnim;                        // 26 Dont Play Open Anim, enum { false, true, }; Default: false
            public uint IgnoreBoundingBox;                       // 27 Ignore Bounding Box, enum { false, true, }; Default: false
            public uint NeverUsableWhileMounted;                 // 28 Never Usable While Mounted, enum { false, true, }; Default: false
            public uint SortFarZ;                                // 29 Sort Far Z, enum { false, true, }; Default: false
            public uint SyncAnimationtoObjectLifetime;           // 30 Sync Animation to Object Lifetime (global track only), enum { false, true, }; Default: false
            public uint NoFuzzyHit;                              // 31 No Fuzzy Hit, enum { false, true, }; Default: false
        }


        public struct transport
        {
            public uint Timeto2ndfloor;                          // 0 Time to 2nd floor (ms), int, Min value: 0, Max value: 2147483647, Default value: 0
            public uint startOpen;                               // 1 startOpen, enum { false, true, }; Default: false
            public uint autoClose;                               // 2 autoClose (ms), int, Min value: 0, Max value: 2147483647, Default value: 0
            public uint Reached1stfloor;                         // 3 Reached 1st floor, References: GameEvents, NoValue = 0
            public uint Reached2ndfloor;                         // 4 Reached 2nd floor, References: GameEvents, NoValue = 0
            public int SpawnMap;                                 // 5 Spawn Map, References: Map, NoValue = -1
            public uint Timeto3rdfloor;                          // 6 Time to 3rd floor (ms), int, Min value: 0, Max value: 2147483647, Default value: 0
            public uint Reached3rdfloor;                         // 7 Reached 3rd floor, References: GameEvents, NoValue = 0
            public uint Timeto4thfloor;                          // 8 Time to 4th floor (ms), int, Min value: 0, Max value: 2147483647, Default value: 0
            public uint Reached4thfloor;                         // 9 Reached 4th floor, References: GameEvents, NoValue = 0
            public uint Timeto5thfloor;                          // 10 Time to 5th floor (ms), int, Min value: 0, Max value: 2147483647, Default value: 0
            public uint Reached5thfloor;                         // 11 Reached 5th floor, References: GameEvents, NoValue = 0
            public uint Timeto6thfloor;                          // 12 Time to 6th floor (ms), int, Min value: 0, Max value: 2147483647, Default value: 0
            public uint Reached6thfloor;                         // 13 Reached 6th floor, References: GameEvents, NoValue = 0
            public uint Timeto7thfloor;                          // 14 Time to 7th floor (ms), int, Min value: 0, Max value: 2147483647, Default value: 0
            public uint Reached7thfloor;                         // 15 Reached 7th floor, References: GameEvents, NoValue = 0
            public uint Timeto8thfloor;                          // 16 Time to 8th floor (ms), int, Min value: 0, Max value: 2147483647, Default value: 0
            public uint Reached8thfloor;                         // 17 Reached 8th floor, References: GameEvents, NoValue = 0
            public uint Timeto9thfloor;                          // 18 Time to 9th floor (ms), int, Min value: 0, Max value: 2147483647, Default value: 0
            public uint Reached9thfloor;                         // 19 Reached 9th floor, References: GameEvents, NoValue = 0
            public uint Timeto10thfloor;                         // 20 Time to 10th floor (ms), int, Min value: 0, Max value: 2147483647, Default value: 0
            public uint Reached10thfloor;                        // 21 Reached 10th floor, References: GameEvents, NoValue = 0
            public uint onlychargeheightcheck;                   // 22 only charge height check. (yards), int, Min value: 0, Max value: 65535, Default value: 0
            public uint onlychargetimecheck;                     // 23 only charge time check, int, Min value: 0, Max value: 65535, Default value: 0
        }


        public struct areadamage
        {
            public uint open;                                    // 0 open, References: Lock_, NoValue = 0
            public uint radius;                                  // 1 radius, int, Min value: 0, Max value: 50, Default value: 3
            public uint damageMin;                               // 2 damageMin, int, Min value: 0, Max value: 65535, Default value: 0
            public uint damageMax;                               // 3 damageMax, int, Min value: 0, Max value: 65535, Default value: 0
            public uint damageSchool;                            // 4 damageSchool, int, Min value: 0, Max value: 65535, Default value: 0
            public uint autoClose;                               // 5 autoClose (ms), int, Min value: 0, Max value: 2147483647, Default value: 0
            public uint openTextID;                              // 6 openTextID, References: BroadcastText, NoValue = 0
            public uint closeTextID;                             // 7 closeTextID, References: BroadcastText, NoValue = 0
        }


        public struct camera
        {
            public uint open;                                    // 0 open, References: Lock_, NoValue = 0
            public uint _camera;                                  // 1 camera, References: CinematicSequences, NoValue = 0
            public uint eventID;                                 // 2 eventID, References: GameEvents, NoValue = 0
            public uint openTextID;                              // 3 openTextID, References: BroadcastText, NoValue = 0
            public uint conditionID1;                            // 4 conditionID1, References: PlayerCondition, NoValue = 0
        }


        public struct moTransport
        {
            public uint taxiPathID;                              // 0 taxiPathID, References: TaxiPath, NoValue = 0
            public uint moveSpeed;                               // 1 moveSpeed, int, Min value: 1, Max value: 60, Default value: 1
            public uint accelRate;                               // 2 accelRate, int, Min value: 1, Max value: 20, Default value: 1
            public uint startEventID;                            // 3 startEventID, References: GameEvents, NoValue = 0
            public uint stopEventID;                             // 4 stopEventID, References: GameEvents, NoValue = 0
            public uint transportPhysics;                        // 5 transportPhysics, References: TransportPhysics, NoValue = 0
            public int SpawnMap;                                 // 6 Spawn Map, References: Map, NoValue = -1
            public uint worldState1;                             // 7 worldState1, References: WorldState, NoValue = 0
            public uint allowstopping;                           // 8 allow stopping, enum { false, true, }; Default: false
            public uint InitStopped;                             // 9 Init Stopped, enum { false, true, }; Default: false
            public uint TrueInfiniteAOI;                         // 10 True Infinite AOI (programmer only!), enum { false, true, }; Default: false
        }


        public struct ritual
        {
            public uint casters;                                 // 0 casters, int, Min value: 1, Max value: 10, Default value: 1
            public uint spell;                                   // 1 spell, References: Spell, NoValue = 0
            public uint animSpell;                               // 2 animSpell, References: Spell, NoValue = 0
            public uint ritualPersistent;                        // 3 ritualPersistent, enum { false, true, }; Default: false
            public uint casterTargetSpell;                       // 4 casterTargetSpell, References: Spell, NoValue = 0
            public uint casterTargetSpellTargets;                // 5 casterTargetSpellTargets, int, Min value: 1, Max value: 10, Default value: 1
            public uint castersGrouped;                          // 6 castersGrouped, enum { false, true, }; Default: true
            public uint ritualNoTargetCheck;                     // 7 ritualNoTargetCheck, enum { false, true, }; Default: true
            public uint conditionID1;                            // 8 conditionID1, References: PlayerCondition, NoValue = 0
        }


        public struct mailbox
        {
            public uint conditionID1;                            // 0 conditionID1, References: PlayerCondition, NoValue = 0
        }


        public struct guardpost
        {
            public uint creatureID;                              // 0 creatureID, References: Creature, NoValue = 0
            public uint charges;                                 // 1 charges, int, Min value: 0, Max value: 65535, Default value: 1
            public uint Preferonlyifinlineofsight;               // 2 Prefer only if in line of sight (expensive), enum { false, true, }; Default: false
        }


        public struct spellcaster
        {
            public uint spell;                                   // 0 spell, References: Spell, NoValue = 0
            public int charges;                                  // 1 charges, int, Min value: -1, Max value: 65535, Default value: 1
            public uint partyOnly;                               // 2 partyOnly, enum { false, true, }; Default: false
            public uint allowMounted;                            // 3 allowMounted, enum { false, true, }; Default: false
            public uint GiganticAOI;                             // 4 Gigantic AOI, enum { false, true, }; Default: false
            public uint conditionID1;                            // 5 conditionID1, References: PlayerCondition, NoValue = 0
            public uint playerCast;                              // 6 playerCast, enum { false, true, }; Default: false
            public uint NeverUsableWhileMounted;                 // 7 Never Usable While Mounted, enum { false, true, }; Default: false
        }


        public struct meetingstone
        {
            public uint minLevel;                                // 0 minLevel, int, Min value: 0, Max value: 65535, Default value: 1
            public uint maxLevel;                                // 1 maxLevel, int, Min value: 1, Max value: 65535, Default value: 60
            public uint areaID;                                  // 2 areaID, References: AreaTable, NoValue = 0
        }


        public struct flagstand
        {
            public uint open;                                    // 0 open, References: Lock_, NoValue = 0
            public uint pickupSpell;                             // 1 pickupSpell, References: Spell, NoValue = 0
            public uint radius;                                  // 2 radius, int, Min value: 0, Max value: 50, Default value: 0
            public uint returnAura;                              // 3 returnAura, References: Spell, NoValue = 0
            public uint returnSpell;                             // 4 returnSpell, References: Spell, NoValue = 0
            public uint noDamageImmune;                          // 5 noDamageImmune, enum { false, true, }; Default: false
            public uint openTextID;                              // 6 openTextID, References: BroadcastText, NoValue = 0
            public uint requireLOS;                              // 7 require LOS, enum { false, true, }; Default: true
            public uint conditionID1;                            // 8 conditionID1, References: PlayerCondition, NoValue = 0
            public uint playerCast;                              // 9 playerCast, enum { false, true, }; Default: false
            public uint GiganticAOI;                             // 10 Gigantic AOI, enum { false, true, }; Default: false
            public uint InfiniteAOI;                             // 11 Infinite AOI, enum { false, true, }; Default: false
            public uint cooldown;                                // 12 cooldown, int, Min value: 0, Max value: 2147483647, Default value: 3000
        }


        public struct fishinghole
        {
            public uint radius;                                  // 0 radius, int, Min value: 0, Max value: 50, Default value: 0
            public uint chestLoot;                               // 1 chestLoot, References: Treasure, NoValue = 0
            public uint minRestock;                              // 2 minRestock, int, Min value: 0, Max value: 65535, Default value: 0
            public uint maxRestock;                              // 3 maxRestock, int, Min value: 0, Max value: 65535, Default value: 0
            public uint open;                                    // 4 open, References: Lock_, NoValue = 0
        }


        public struct flagdrop
        {
            public uint open;                                    // 0 open, References: Lock_, NoValue = 0
            public uint eventID;                                 // 1 eventID, References: GameEvents, NoValue = 0
            public uint pickupSpell;                             // 2 pickupSpell, References: Spell, NoValue = 0
            public uint noDamageImmune;                          // 3 noDamageImmune, enum { false, true, }; Default: false
            public uint openTextID;                              // 4 openTextID, References: BroadcastText, NoValue = 0
            public uint playerCast;                              // 5 playerCast, enum { false, true, }; Default: false
            public uint ExpireDuration;                          // 6 Expire Duration, int, Min value: 0, Max value: 60000, Default value: 10000
            public uint GiganticAOI;                             // 7 Gigantic AOI, enum { false, true, }; Default: false
            public uint InfiniteAOI;                             // 8 Infinite AOI, enum { false, true, }; Default: false
            public uint cooldown;                                // 9 cooldown, int, Min value: 0, Max value: 2147483647, Default value: 3000
        }


        public struct controlzone
        {
            public uint radius;                                  // 0 radius, int, Min value: 0, Max value: 100, Default value: 10
            public uint spell;                                   // 1 spell, References: Spell, NoValue = 0
            public uint worldState1;                             // 2 worldState1, References: WorldState, NoValue = 0
            public uint worldstate2;                             // 3 worldstate2, References: WorldState, NoValue = 0
            public uint CaptureEventHorde;                       // 4 Capture Event (Horde), References: GameEvents, NoValue = 0
            public uint CaptureEventAlliance;                    // 5 Capture Event (Alliance), References: GameEvents, NoValue = 0
            public uint ContestedEventHorde;                     // 6 Contested Event (Horde), References: GameEvents, NoValue = 0
            public uint ContestedEventAlliance;                  // 7 Contested Event (Alliance), References: GameEvents, NoValue = 0
            public uint ProgressEventHorde;                      // 8 Progress Event (Horde), References: GameEvents, NoValue = 0
            public uint ProgressEventAlliance;                   // 9 Progress Event (Alliance), References: GameEvents, NoValue = 0
            public uint NeutralEventHorde;                       // 10 Neutral Event (Horde), References: GameEvents, NoValue = 0
            public uint NeutralEventAlliance;                    // 11 Neutral Event (Alliance), References: GameEvents, NoValue = 0
            public uint neutralPercent;                          // 12 neutralPercent, int, Min value: 0, Max value: 100, Default value: 0
            public uint worldstate3;                             // 13 worldstate3, References: WorldState, NoValue = 0
            public uint minSuperiority;                          // 14 minSuperiority, int, Min value: 1, Max value: 65535, Default value: 1
            public uint maxSuperiority;                          // 15 maxSuperiority, int, Min value: 1, Max value: 65535, Default value: 1
            public uint minTime;                                 // 16 minTime, int, Min value: 1, Max value: 65535, Default value: 1
            public uint maxTime;                                 // 17 maxTime, int, Min value: 1, Max value: 65535, Default value: 1
            public uint GiganticAOI;                             // 18 Gigantic AOI, enum { false, true, }; Default: false
            public uint highlight;                               // 19 highlight, enum { false, true, }; Default: true
            public uint startingValue;                           // 20 startingValue, int, Min value: 0, Max value: 100, Default value: 50
            public uint unidirectional;                          // 21 unidirectional, enum { false, true, }; Default: false
            public uint killbonustime;                           // 22 kill bonus time %, int, Min value: 0, Max value: 100, Default value: 0
            public uint speedWorldState1;                        // 23 speedWorldState1, References: WorldState, NoValue = 0
            public uint speedWorldState2;                        // 24 speedWorldState2, References: WorldState, NoValue = 0
            public uint UncontestedTime;                         // 25 Uncontested Time, int, Min value: 0, Max value: 65535, Default value: 0
            public uint FrequentHeartbeat;                       // 26 Frequent Heartbeat, enum { false, true, }; Default: false
        }


        public struct auraGenerator
        {
            public uint startOpen;                               // 0 startOpen, enum { false, true, }; Default: true
            public uint radius;                                  // 1 radius, int, Min value: 0, Max value: 100, Default value: 10
            public uint auraID1;                                 // 2 auraID1, References: Spell, NoValue = 0
            public uint conditionID1;                            // 3 conditionID1, References: PlayerCondition, NoValue = 0
            public uint auraID2;                                 // 4 auraID2, References: Spell, NoValue = 0
            public uint conditionID2;                            // 5 conditionID2, References: PlayerCondition, NoValue = 0
            public uint serverOnly;                              // 6 serverOnly, enum { false, true, }; Default: false
        }


        public struct dungeonDifficulty
        {
            public uint InstanceType;                            // 0 Instance Type, enum { Not Instanced, Party Dungeon, Raid Dungeon, PVP Battlefield, Arena Battlefield, Scenario, }; Default: Party Dungeon
            public uint DifficultyNormal;                        // 1 Difficulty Normal, References: animationdata, NoValue = 0
            public uint DifficultyHeroic;                        // 2 Difficulty Heroic, References: animationdata, NoValue = 0
            public uint DifficultyEpic;                          // 3 Difficulty Epic, References: animationdata, NoValue = 0
            public uint DifficultyLegendary;                     // 4 Difficulty Legendary, References: animationdata, NoValue = 0
            public uint HeroicAttachment;                        // 5 Heroic Attachment, References: gameobjectdisplayinfo, NoValue = 0
            public uint ChallengeAttachment;                     // 6 Challenge Attachment, References: gameobjectdisplayinfo, NoValue = 0
            public uint DifficultyAnimations;                    // 7 Difficulty Animations, References: GameObjectDiffAnim, NoValue = 0
            public uint LargeAOI;                                // 8 Large AOI, enum { false, true, }; Default: false
            public uint GiganticAOI;                             // 9 Gigantic AOI, enum { false, true, }; Default: false
            public uint Legacy;                                  // 10 Legacy, enum { false, true, }; Default: false
        }


        public struct barberChair
        {
            public uint chairheight;                             // 0 chairheight, int, Min value: 0, Max value: 2, Default value: 1
            public int HeightOffset;                             // 1 Height Offset (inches), int, Min value: -100, Max value: 100, Default value: 0
            public uint SitAnimKit;                              // 2 Sit Anim Kit, References: AnimKit, NoValue = 0
        }


        public struct destructiblebuilding
        {
            public int Unused;                                   // 0 Unused, int, Min value: -2147483648, Max value: 2147483647, Default value: 0
            public uint CreditProxyCreature;                     // 1 Credit Proxy Creature, References: Creature, NoValue = 0
            public uint HealthRec;                               // 2 Health Rec, References: DestructibleHitpoint, NoValue = 0
            public uint IntactEvent;                             // 3 Intact Event, References: GameEvents, NoValue = 0
            public uint PVPEnabling;                             // 4 PVP Enabling, enum { false, true, }; Default: false
            public uint InteriorVisible;                         // 5 Interior Visible, enum { false, true, }; Default: false
            public uint InteriorLight;                           // 6 Interior Light, enum { false, true, }; Default: false
            public int Unused1;                                  // 7 Unused, int, Min value: -2147483648, Max value: 2147483647, Default value: 0
            public int Unused2;                                  // 8 Unused, int, Min value: -2147483648, Max value: 2147483647, Default value: 0
            public uint DamagedEvent;                            // 9 Damaged Event, References: GameEvents, NoValue = 0
            public int Unused3;                                  // 10 Unused, int, Min value: -2147483648, Max value: 2147483647, Default value: 0
            public int Unused4;                                  // 11 Unused, int, Min value: -2147483648, Max value: 2147483647, Default value: 0
            public int Unused5;                                  // 12 Unused, int, Min value: -2147483648, Max value: 2147483647, Default value: 0
            public int Unused6;                                  // 13 Unused, int, Min value: -2147483648, Max value: 2147483647, Default value: 0
            public uint DestroyedEvent;                          // 14 Destroyed Event, References: GameEvents, NoValue = 0
            public int Unused7;                                  // 15 Unused, int, Min value: -2147483648, Max value: 2147483647, Default value: 0
            public uint RebuildingTime;                          // 16 Rebuilding: Time (secs), int, Min value: 0, Max value: 65535, Default value: 0
            public int Unused8;                                  // 17 Unused, int, Min value: -2147483648, Max value: 2147483647, Default value: 0
            public uint DestructibleModelRec;                    // 18 Destructible Model Rec, References: DestructibleModelData, NoValue = 0
            public uint RebuildingEvent;                         // 19 Rebuilding: Event, References: GameEvents, NoValue = 0
            public int Unused9;                                  // 20 Unused, int, Min value: -2147483648, Max value: 2147483647, Default value: 0
            public int Unused10;                                 // 21 Unused, int, Min value: -2147483648, Max value: 2147483647, Default value: 0
            public uint DamageEvent;                             // 22 Damage Event, References: GameEvents, NoValue = 0
        }


        public struct guildbank
        {
            public uint conditionID1;                            // 0 conditionID1, References: PlayerCondition, NoValue = 0
        }


        public struct trapDoor
        {
            public uint AutoLink;                                // 0 Auto Link, enum { false, true, }; Default: false
            public uint startOpen;                               // 1 startOpen, enum { false, true, }; Default: false
            public uint autoClose;                               // 2 autoClose (ms), int, Min value: 0, Max value: 2147483647, Default value: 0
            public uint BlocksPathsDown;                         // 3 Blocks Paths Down, enum { false, true, }; Default: false
            public int PathBlockerBump;                          // 4 Path Blocker Bump (ft), int, Min value: -2147483648, Max value: 2147483647, Default value: 0
            public uint GiganticAOI;                             // 5 Gigantic AOI, enum { false, true, }; Default: false
            public uint InfiniteAOI;                             // 6 Infinite AOI, enum { false, true, }; Default: false
            public uint DoorisOpaque;                            // 7 Door is Opaque (Disable portal on close), enum { false, true, }; Default: false
        }


        public struct newflag
        {
            public uint open;                                    // 0 open, References: Lock_, NoValue = 0
            public uint pickupSpell;                             // 1 pickupSpell, References: Spell, NoValue = 0
            public uint openTextID;                              // 2 openTextID, References: BroadcastText, NoValue = 0
            public uint requireLOS;                              // 3 require LOS, enum { false, true, }; Default: true
            public uint conditionID1;                            // 4 conditionID1, References: PlayerCondition, NoValue = 0
            public uint GiganticAOI;                             // 5 Gigantic AOI, enum { false, true, }; Default: false
            public uint InfiniteAOI;                             // 6 Infinite AOI, enum { false, true, }; Default: false
            public uint ExpireDuration;                          // 7 Expire Duration, int, Min value: 0, Max value: 3600000, Default value: 10000
            public uint RespawnTime;                             // 8 Respawn Time, int, Min value: 0, Max value: 3600000, Default value: 20000
            public uint FlagDrop;                                // 9 Flag Drop, References: GameObjects, NoValue = 0
            public int ExclusiveCategory;                        // 10 Exclusive Category (BGs Only), int, Min value: -2147483648, Max value: 2147483647, Default value: 0
            public uint worldState1;                             // 11 worldState1, References: WorldState, NoValue = 0
            public uint ReturnonDefenderInteract;                // 12 Return on Defender Interact, enum { false, true, }; Default: false
        }


        public struct newflagdrop
        {
            public uint open;                                    // 0 open, References: Lock_, NoValue = 0
        }


        public struct Garrisonbuilding
        {
            public int SpawnMap;                                 // 0 Spawn Map, References: Map, NoValue = -1
        }


        public struct garrisonplot
        {
            public uint PlotInstance;                            // 0 Plot Instance, References: GarrPlotInstance, NoValue = 0
            public int SpawnMap;                                 // 1 Spawn Map, References: Map, NoValue = -1
        }


        public struct clientcreature
        {
            public uint CreatureDisplayInfo;                     // 0 Creature Display Info, References: CreatureDisplayInfo, NoValue = 0
            public uint AnimKit;                                 // 1 Anim Kit, References: AnimKit, NoValue = 0
            public uint creatureID;                              // 2 creatureID, References: Creature, NoValue = 0
        }


        public struct clientitem
        {
            public uint Item;                                    // 0 Item, References: Item, NoValue = 0
        }


        public struct capturepoint
        {
            public uint CaptureTime;                             // 0 Capture Time (ms), int, Min value: 0, Max value: 2147483647, Default value: 60000
            public uint GiganticAOI;                             // 1 Gigantic AOI, enum { false, true, }; Default: false
            public uint highlight;                               // 2 highlight, enum { false, true, }; Default: true
            public uint open;                                    // 3 open, References: Lock_, NoValue = 0
            public uint AssaultBroadcastHorde;                   // 4 Assault Broadcast (Horde), References: BroadcastText, NoValue = 0
            public uint CaptureBroadcastHorde;                   // 5 Capture Broadcast (Horde), References: BroadcastText, NoValue = 0
            public uint DefendedBroadcastHorde;                  // 6 Defended Broadcast (Horde), References: BroadcastText, NoValue = 0
            public uint AssaultBroadcastAlliance;                // 7 Assault Broadcast (Alliance), References: BroadcastText, NoValue = 0
            public uint CaptureBroadcastAlliance;                // 8 Capture Broadcast (Alliance), References: BroadcastText, NoValue = 0
            public uint DefendedBroadcastAlliance;               // 9 Defended Broadcast (Alliance), References: BroadcastText, NoValue = 0
            public uint worldState1;                             // 10 worldState1, References: WorldState, NoValue = 0
            public uint ContestedEventHorde;                     // 11 Contested Event (Horde), References: GameEvents, NoValue = 0
            public uint CaptureEventHorde;                       // 12 Capture Event (Horde), References: GameEvents, NoValue = 0
            public uint DefendedEventHorde;                      // 13 Defended Event (Horde), References: GameEvents, NoValue = 0
            public uint ContestedEventAlliance;                  // 14 Contested Event (Alliance), References: GameEvents, NoValue = 0
            public uint CaptureEventAlliance;                    // 15 Capture Event (Alliance), References: GameEvents, NoValue = 0
            public uint DefendedEventAlliance;                   // 16 Defended Event (Alliance), References: GameEvents, NoValue = 0
            public uint SpellVisual1;                            // 17 Spell Visual 1, References: SpellVisual, NoValue = 0
            public uint SpellVisual2;                            // 18 Spell Visual 2, References: SpellVisual, NoValue = 0
            public uint SpellVisual3;                            // 19 Spell Visual 3, References: SpellVisual, NoValue = 0
            public uint SpellVisual4;                            // 20 Spell Visual 4, References: SpellVisual, NoValue = 0
            public uint SpellVisual5;                            // 21 Spell Visual 5, References: SpellVisual, NoValue = 0
            public uint SpawnVignette;                           // 22 Spawn Vignette, References: vignette, NoValue = 0
        }


        public struct phaseablemo
        {
            public int SpawnMap;                                 // 0 Spawn Map, References: Map, NoValue = -1
            public int AreaNameSet;                              // 1 Area Name Set (Index), int, Min value: -2147483648, Max value: 2147483647, Default value: 0
            public uint DoodadSetA;                              // 2 Doodad Set A, int, Min value: 0, Max value: 2147483647, Default value: 0
            public uint DoodadSetB;                              // 3 Doodad Set B, int, Min value: 0, Max value: 2147483647, Default value: 0
        }


        public struct garrisonmonument
        {
            public uint TrophyTypeID;                            // 0 Trophy Type ID, References: TrophyType, NoValue = 0
            public uint TrophyInstanceID;                        // 1 Trophy Instance ID, References: TrophyInstance, NoValue = 0
        }


        public struct garrisonshipment
        {
            public uint ShipmentContainer;                       // 0 Shipment Container, References: CharShipmentContainer, NoValue = 0
            public uint GiganticAOI;                             // 1 Gigantic AOI, enum { false, true, }; Default: false
            public uint LargeAOI;                                // 2 Large AOI, enum { false, true, }; Default: false
        }


        public struct garrisonmonumentplaque
        {
            public uint TrophyInstanceID;                        // 0 Trophy Instance ID, References: TrophyInstance, NoValue = 0
        }

        public struct artifactforge
        {
            public uint conditionID1;                            // 0 conditionID1, References: PlayerCondition, NoValue = 0
            public uint LargeAOI;                                // 1 Large AOI, enum { false, true, }; Default: false
            public uint IgnoreBoundingBox;                       // 2 Ignore Bounding Box, enum { false, true, }; Default: false
            public uint CameraMode;                              // 3 Camera Mode, References: CameraMode, NoValue = 0
            public uint FadeRegionRadius;                        // 4 Fade Region Radius, int, Min value: 0, Max value: 2147483647, Default value: 0
            public uint ForgeType;                               // 5 Forge Type, enum { Artifact Forge, Relic Forge, }; Default: Relic Forge
        }

        public struct uilink
        {
            public uint UILinkType;                              // 0 UI Link Type, enum { Adventure Journal, Obliterum Forge, Scrapping Machine}; Default: Adventure Journal
            public uint allowMounted;                            // 1 allowMounted, enum { false, true, }; Default: false
            public uint GiganticAOI;                             // 2 Gigantic AOI, enum { false, true, }; Default: false
            public uint spellFocusType;                          // 3 spellFocusType, References: SpellFocusObject, NoValue = 0
            public uint radius;                                  // 4 radius, int, Min value: 0, Max value: 50, Default value: 10
        }

        public struct keystonereceptacle { }

        public struct gatheringnode
        {
            public uint open;                                    // 0 open, References: Lock_, NoValue = 0
            public uint chestLoot;                               // 1 chestLoot, References: Treasure, NoValue = 0
            public uint level;                                   // 2 level, int, Min value: 0, Max value: 65535, Default value: 0
            public uint notInCombat;                             // 3 notInCombat, enum { false, true, }; Default: false
            public uint trivialSkillLow;                         // 4 trivialSkillLow, int, Min value: 0, Max value: 65535, Default value: 0
            public uint trivialSkillHigh;                        // 5 trivialSkillHigh, int, Min value: 0, Max value: 65535, Default value: 0
            public uint ObjectDespawnDelay;                      // 6 Object Despawn Delay, int, Min value: 0, Max value: 600, Default value: 15
            public uint triggeredEvent;                          // 7 triggeredEvent, References: GameEvents, NoValue = 0
            public uint requireLOS;                              // 8 require LOS, enum { false, true, }; Default: false
            public uint openTextID;                              // 9 openTextID, References: BroadcastText, NoValue = 0
            public uint floatingTooltip;                         // 10 floatingTooltip, enum { false, true, }; Default: false
            public uint conditionID1;                            // 11 conditionID1, References: PlayerCondition, NoValue = 0
            public uint XPLevelRange;                            // 12 XP Level Range, References: ContentTuning, NoValue = 0
            public uint xpDifficulty;                            // 13 xpDifficulty, enum { No Exp, Trivial, Very Small, Small, Substandard, Standard, High, Epic, Dungeon, 5, }; Default: No Exp
            public uint spell;                                   // 14 spell, References: Spell, NoValue = 0
            public uint GiganticAOI;                             // 15 Gigantic AOI, enum { false, true, }; Default: false
            public uint LargeAOI;                                // 16 Large AOI, enum { false, true, }; Default: false
            public uint SpawnVignette;                           // 17 Spawn Vignette, References: vignette, NoValue = 0
            public uint MaxNumberofLoots;                        // 18 Max Number of Loots, int, Min value: 1, Max value: 40, Default value: 10
            public uint logloot;                                 // 19 log loot, enum { false, true, }; Default: false
            public uint linkedTrap;                              // 20 linkedTrap, References: GameObjects, NoValue = 0
            public uint PlayOpenAnimationonOpening;              // 21 Play Open Animation on Opening, enum { false, true, }; Default: false
        }

        public struct challengemodereward
        {
            public uint chestLoot;                               // 0 chestLoot, References: Treasure, NoValue = 0
            public uint WhenAvailable;                           // 1 When Available, References: GameObjectDisplayInfo, NoValue = 0
            public uint open;                                    // 2 open, References: Lock_, NoValue = 0
            public uint openTextID;                              // 3 openTextID, References: BroadcastText, NoValue = 0
        }

        public struct multi
        {
            public uint MultiProperties;                         // 0 Multi Properties, References: MultiProperties, NoValue = 0
        }

        public struct siegeableMulti
        {
            public uint MultiProperties;                         // 0 Multi Properties, References: MultiProperties, NoValue = 0
            public uint InitialDamage;                           // 1 Initial Damage, enum { None, Raw, Ratio, }; Default: None
        }

        public struct siegeableMO
        {
            public uint SiegeableProperties;                     // 0 Siegeable Properties, References: SiegeableProperties, NoValue = 0
            public uint DoodadSetA;                              // 1 Doodad Set A, int, Min value: 0, Max value: 2147483647, Default value: 0
            public uint DoodadSetB;                              // 2 Doodad Set B, int, Min value: 0, Max value: 2147483647, Default value: 0
            public uint DoodadSetC;                              // 3 Doodad Set C, int, Min value: 0, Max value: 2147483647, Default value: 0
            public int SpawnMap;                                 // 4 Spawn Map, References: Map, NoValue = -1
            public int AreaNameSet;                              // 5 Area Name Set (Index), int, Min value: -2147483648, Max value: 2147483647, Default value: 0
        }

        public struct pvpReward
        {
            public uint chestLoot;                               // 0 chestLoot, References: Treasure, NoValue = 0
            public uint WhenAvailable;                           // 1 When Available, References: GameObjectDisplayInfo, NoValue = 0
            public uint open;                                    // 2 open, References: Lock_, NoValue = 0
            public uint openTextID;                              // 3 openTextID, References: BroadcastText, NoValue = 0
        }
        #endregion
    }

    public class GameObjectTemplateAddon
    {
        public uint entry;
        public uint faction;
        public uint flags;
        public uint mingold;
        public uint maxgold;
        public uint WorldEffectID;
    }

    public class GameObjectLocale
    {
        public StringArray Name = new StringArray((int)LocaleConstant.Total);
        public StringArray CastBarCaption = new StringArray((int)LocaleConstant.Total);
        public StringArray Unk1 = new StringArray((int)LocaleConstant.Total);
    }

    public class GameObjectAddon
    {
        public Quaternion ParentRotation;
        public InvisibilityType invisibilityType;
        public uint invisibilityValue;
        public uint WorldEffectID;
    }

    public class GameObjectData
    {
        public uint id;                                              // entry in gamobject_template
        public ushort mapid;
        public float posX;
        public float posY;
        public float posZ;
        public float orientation;
        public Quaternion rotation;
        public int spawntimesecs;
        public uint animprogress;
        public GameObjectState go_state;
        public List<Difficulty> spawnDifficulties = new List<Difficulty>();
        public byte artKit;
        public PhaseUseFlagsValues phaseUseFlags;
        public uint phaseId;
        public uint phaseGroup;
        public int terrainSwapMap;
        public uint ScriptId;
        public bool dbData = true;
    }
}
