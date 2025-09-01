// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Framework.IO;
using Game.DataStorage;
using Game.Entities;
using Game.Spells;
using System;
using System.Collections.Generic;

namespace Game.Chat
{
    class HyperlinkDataTokenizer
    {
        public HyperlinkDataTokenizer(string arg, bool allowEmptyTokens = false)
        {
            _arg = new(arg);
            _allowEmptyTokens = allowEmptyTokens;
        }

        public bool TryConsumeTo<T>(out T val)
        {
            val = default;

            if (IsEmpty())
                return _allowEmptyTokens;

            switch (Type.GetTypeCode(typeof(T)))
            {
                case TypeCode.SByte:
                    val = (dynamic)_arg.NextByte(":");
                    return true;
                case TypeCode.Int16:
                    val = (dynamic)_arg.NextUInt16(":");
                    return true;
                case TypeCode.Int32:
                    val = (dynamic)_arg.NextUInt32(":");
                    return true;
                case TypeCode.Int64:
                    val = (dynamic)_arg.NextUInt64(":");
                    return true;
                case TypeCode.Byte:
                    val = (dynamic)_arg.NextByte(":");
                    return true;
                case TypeCode.UInt16:
                    val = (dynamic)_arg.NextUInt16(":");
                    return true;
                case TypeCode.UInt32:
                    val = (dynamic)_arg.NextUInt32(":");
                    return true;
                case TypeCode.UInt64:
                    val = (dynamic)_arg.NextUInt64(":");
                    return true;
                case TypeCode.Single:
                    val = (dynamic)_arg.NextSingle(":");
                    return true;
                case TypeCode.Double:
                    val = (dynamic)_arg.NextDouble(":");
                    return true;
                case TypeCode.String:
                    val = (dynamic)_arg.NextString(":");
                    return true;
                case TypeCode.Object:
                {
                    switch (typeof(T).Name)
                    {
                        case nameof(AchievementRecord):
                            val = (dynamic)CliDB.AchievementStorage.LookupByKey(_arg.NextUInt32(":"));
                            if (val != null)
                                return true;
                            break;
                        case nameof(CurrencyTypesRecord):
                            val = (dynamic)CliDB.CurrencyTypesStorage.LookupByKey(_arg.NextUInt32(":"));
                            if (val != null)
                                return true;
                            break;
                        case nameof(GameTele):
                            val = (dynamic)Global.ObjectMgr.GetGameTele(_arg.NextUInt32(":"));
                            if (val != null)
                                return true;
                            break;
                        case nameof(ItemTemplate):
                            val = (dynamic)Global.ObjectMgr.GetItemTemplate(_arg.NextUInt32(":"));
                            if (val != null)
                                return true;
                            break;
                        case nameof(Quest):
                            val = (dynamic)Global.ObjectMgr.GetQuestTemplate(_arg.NextUInt32(":"));
                            if (val != null)
                                return true;
                            break;
                        case nameof(SpellInfo):
                            val = (dynamic)Global.SpellMgr.GetSpellInfo(_arg.NextUInt32(":"), Framework.Constants.Difficulty.None);
                            if (val != null)
                                return true;
                            break;
                        case nameof(ObjectGuid):
                            val = (dynamic)ObjectGuid.FromString(_arg.NextString(":"));
                            if (val != (dynamic)ObjectGuid.FromStringFailed)
                                return true;
                            break;
                        default:
                            return false;
                    }
                    break;
                }
            }

            return false;
        }

        public bool IsEmpty() { return _arg.IsAtEnd(); }

        StringArguments _arg;
        bool _allowEmptyTokens;
    }

    public interface IHyperlink
    {
        public string tag();
        public bool Parse(string data);
    }

    abstract class GenericHyperlink<T> : IHyperlink
    {
        public T value;

        public abstract string tag();
        public abstract bool Parse(string data);

        public static implicit operator T(GenericHyperlink<T> genericHyperlink) => genericHyperlink.value;
    }

    public class AchievementLinkData : IHyperlink
    {
        public AchievementRecord Achievement;
        public ObjectGuid CharacterId;
        public bool IsFinished = false;
        public int Year;
        public int Month;
        public int Day;
        public uint[] Criteria = new uint[4];

        public string tag() => "achievement";

        public bool Parse(string data)
        {
            HyperlinkDataTokenizer t = new(data);

            uint achievementId;
            if (!t.TryConsumeTo(out achievementId))
                return false;
            Achievement = CliDB.AchievementStorage.LookupByKey(achievementId);

            if (!(Achievement != null && t.TryConsumeTo(out CharacterId) && t.TryConsumeTo(out IsFinished) && t.TryConsumeTo(out Month) && t.TryConsumeTo(out Day)))
                return false;
            if ((12 < Month) || (31 < Day))
                return false;

            int year;
            if (!t.TryConsumeTo(out year))
                return false;
            if (IsFinished) // if finished, year must be >= 0
            {
                if (year < 0)
                    return false;
                Year = year;
            }
            else
                Year = 0;

            return (t.TryConsumeTo(out Criteria[0]) && t.TryConsumeTo(out Criteria[1]) && t.TryConsumeTo(out Criteria[2]) && t.TryConsumeTo(out Criteria[3]) && t.IsEmpty());
        }
    }

    class ApiLinkData : IHyperlink
    {
        public string Type;
        public string Name;
        public string Parent;

        public string tag() => "api";

        public bool Parse(string text)
        {
            HyperlinkDataTokenizer t = new(text, true);
            if (!(t.TryConsumeTo(out Type) && t.TryConsumeTo(out Name) && t.TryConsumeTo(out Parent) && t.IsEmpty()))
                return false;
            return true;
        }
    }

    class AreaLinkData : GenericHyperlink<uint>
    {
        public override string tag() => "area";

        public override bool Parse(string data)
        {
            if (uint.TryParse(data, out uint tempValue))
                return true;

            return false;
        }
    }

    class AreatriggerLinkData : GenericHyperlink<uint>
    {
        public override string tag() => "areatrigger";

        public override bool Parse(string data)
        {
            if (uint.TryParse(data, out value))
                return true;

            return false;
        }
    }

    class ArtifactPowerLinkData : IHyperlink
    {
        public ArtifactPowerRankRecord ArtifactPower;
        public byte PurchasedRank;
        public byte CurrentRankWithBonus;

        public string tag() => "apower";

        public bool Parse(string text)
        {
            HyperlinkDataTokenizer t = new(text);
            uint artifactPowerId;
            if (!(t.TryConsumeTo(out artifactPowerId) && t.TryConsumeTo(out PurchasedRank) && t.TryConsumeTo(out CurrentRankWithBonus) && t.IsEmpty()))
                return false;
            if (!CliDB.ArtifactPowerStorage.ContainsKey(artifactPowerId))
                return false;
            ArtifactPower = Global.DB2Mgr.GetArtifactPowerRank(artifactPowerId, Math.Max(CurrentRankWithBonus, (byte)1));
            if (ArtifactPower != null)
                return false;
            return true;
        }
    }

    class AzeriteEssenceLinkData : IHyperlink
    {
        public AzeriteEssenceRecord Essence;
        public byte Rank;

        public string tag() => "azessence";

        public bool Parse(string text)
        {
            HyperlinkDataTokenizer t = new(text);
            uint azeriteEssenceId;
            if (!t.TryConsumeTo(out azeriteEssenceId))
                return false;
            return (Essence = CliDB.AzeriteEssenceStorage.LookupByKey(azeriteEssenceId)) != null && t.TryConsumeTo(out Rank)
                && Global.DB2Mgr.GetAzeriteEssencePower(azeriteEssenceId, Rank) != null && t.IsEmpty();
        }
    }

    class BattlePetLinkData : IHyperlink
    {
        public BattlePetSpeciesRecord Species;
        public byte Level;
        public byte Quality;
        public uint MaxHealth;
        public uint Power;
        public uint Speed;
        public ObjectGuid PetGuid;
        public uint DisplayId;

        public string tag() => "battlepet";

        public bool Parse(string text)
        {
            HyperlinkDataTokenizer t = new(text);
            uint battlePetSpeciesId;
            if (!t.TryConsumeTo(out battlePetSpeciesId))
                return false;
            return (Species = CliDB.BattlePetSpeciesStorage.LookupByKey(battlePetSpeciesId)) != null && t.TryConsumeTo(out Level)
                && t.TryConsumeTo(out Quality) && Quality < (int)ItemQuality.Max
                && t.TryConsumeTo(out MaxHealth) && t.TryConsumeTo(out Power) && t.TryConsumeTo(out Speed)
                && t.TryConsumeTo(out PetGuid) && PetGuid.GetHigh() == HighGuid.BattlePet && t.TryConsumeTo(out DisplayId)
                && t.IsEmpty();
        }
    }

    class BattlePetAbilLinkData : IHyperlink
    {
        public BattlePetAbilityRecord Ability;
        public uint MaxHealth;
        public uint Power;
        public uint Speed;

        public string tag() => "battlePetAbil";

        public bool Parse(string text)
        {
            HyperlinkDataTokenizer t = new(text);
            uint battlePetAbilityId;
            if (!t.TryConsumeTo(out battlePetAbilityId))
                return false;
            return (Ability = CliDB.BattlePetAbilityStorage.LookupByKey(battlePetAbilityId)) != null
                && t.TryConsumeTo(out MaxHealth) && t.TryConsumeTo(out Power) && t.TryConsumeTo(out Speed)
                && t.IsEmpty();
        }
    }

    class ClubFinderLinkData : GenericHyperlink<ObjectGuid>
    {
        public override string tag() => "clubFinder";

        public override bool Parse(string data)
        {
            ObjectGuid parsed = ObjectGuid.FromString(data);
            if (parsed != ObjectGuid.FromStringFailed)
            {
                value = parsed;
                return true;
            }

            return false;
        }
    }

    class ClubTicketLinkData : GenericHyperlink<string>
    {
        public override string tag() => "clubTicket";

        public override bool Parse(string data)
        {
            value = data;
            return true;
        }
    }

    class ConduitLinkData : GenericHyperlink<SoulbindConduitRankRecord>
    {
        public override string tag() => "conduit";

        public override bool Parse(string text)
        {
            HyperlinkDataTokenizer t = new(text);
            int soulbindConduitId, rank;
            if (!(t.TryConsumeTo(out soulbindConduitId) && t.TryConsumeTo(out rank) && t.IsEmpty()))
                return false;

            return (value = Global.DB2Mgr.GetSoulbindConduitRank(soulbindConduitId, rank)) != null;
        }
    }

    class CreatureLinkData : GenericHyperlink<ulong>
    {
        public override string tag() => "creature";

        public override bool Parse(string data)
        {
            if (ulong.TryParse(data, out value))
                return true;

            return false;
        }
    }

    class CreatureEntryLinkData : GenericHyperlink<uint>
    {
        public override string tag() => "creature_entry";

        public override bool Parse(string data)
        {
            if (uint.TryParse(data, out uint tempValue))
            {
                value = tempValue;
                return true;
            }

            value = default;
            return false;
        }
    }

    class CurioLinkData : GenericHyperlink<SpellInfo>
    {
        public override string tag() => "curio";

        public override bool Parse(string text)
        {
            HyperlinkDataTokenizer t = new(text);
            uint spellId;
            if (!(t.TryConsumeTo(out spellId) && t.IsEmpty()))
                return false;
            return (value = Global.SpellMgr.GetSpellInfo(spellId, Difficulty.None)) != null;
        }
    }

    class CurrencyLinkData : IHyperlink
    {
        public CurrencyTypesRecord Currency;
        public int Quantity;

        public CurrencyContainerRecord Container;

        public string tag() => "currency";

        public bool Parse(string text)
        {
            HyperlinkDataTokenizer t = new(text);
            uint currencyId;
            if (!t.TryConsumeTo(out currencyId))
                return false;
            Currency = CliDB.CurrencyTypesStorage.LookupByKey(currencyId);
            if (Currency == null || !t.TryConsumeTo(out Quantity) || !t.IsEmpty())
                return false;
            Container = Global.DB2Mgr.GetCurrencyContainerForCurrencyQuantity(currencyId, Quantity);
            return true;
        }
    }

    class DungeonScoreLinkData : IHyperlink
    {
        public uint Score;
        public ObjectGuid Player;
        public string PlayerName;
        public byte PlayerClass;
        public uint AvgItemLevel;
        public byte PlayerLevel;
        public uint RunsThisSeason;
        public uint BestSeasonScore;
        public uint BestSeasonNumber;

        public struct Dungeon
        {
            public uint MapChallengeModeID;
            public bool CompletedInTime;
            public uint KeystoneLevel;
        }

        public List<Dungeon> Dungeons = new();

        public string tag() => "dungeonScore";

        public bool Parse(string text)
        {
            HyperlinkDataTokenizer t = new(text);
            if (!t.TryConsumeTo(out Score) || !t.TryConsumeTo(out Player) || !Player.IsPlayer()
                || !t.TryConsumeTo(out PlayerName) || !t.TryConsumeTo(out PlayerClass) || !t.TryConsumeTo(out AvgItemLevel)
                || !t.TryConsumeTo(out PlayerLevel) || !t.TryConsumeTo(out RunsThisSeason)
                || !t.TryConsumeTo(out BestSeasonScore) || !t.TryConsumeTo(out BestSeasonNumber))
                return false;

            if (t.IsEmpty())
                return true;

            for (uint i = 0; i < 10; ++i)
            {
                DungeonScoreLinkData.Dungeon dungeon = new();
                if (!t.TryConsumeTo(out dungeon.MapChallengeModeID) || !CliDB.MapChallengeModeStorage.ContainsKey(dungeon.MapChallengeModeID))
                    return false;
                if (!t.TryConsumeTo(out dungeon.CompletedInTime) || !t.TryConsumeTo(out dungeon.KeystoneLevel))
                    return false;
                if (t.IsEmpty())
                    return true;

                Dungeons.Add(dungeon);
            }

            return false;
        }
    }

    class EnchantLinkData : GenericHyperlink<SpellInfo>
    {
        public override string tag() => "enchant";

        public override bool Parse(string text)
        {
            HyperlinkDataTokenizer t = new(text);
            uint spellId;
            if (!(t.TryConsumeTo(out spellId) && t.IsEmpty()))
                return false;
            return (value = Global.SpellMgr.GetSpellInfo(spellId, Difficulty.None)) != null && value.HasAttribute(SpellAttr0.IsTradeskill);
        }
    }

    class GameeventLinkData : GenericHyperlink<ushort>
    {
        public override string tag() => "gameevent";

        public override bool Parse(string data)
        {
            if (ushort.TryParse(data, out value))
                return true;

            return false;
        }
    }

    class GameobjectLinkData : GenericHyperlink<ulong>
    {
        public override string tag() => "gameobject";

        public override bool Parse(string data)
        {
            if (ulong.TryParse(data, out ulong value))
                return true;

            return false;
        }
    }

    class GameobjectEntryLinkData : GenericHyperlink<uint>
    {
        public override string tag() => "gameobject_entry";

        public override bool Parse(string data)
        {
            if (uint.TryParse(data, out uint value))
                return true;

            return false;
        }
    }

    class GarrisonFollowerLinkData : IHyperlink
    {
        public GarrFollowerRecord Follower;
        public uint Quality;
        public uint Level;
        public uint ItemLevel;
        public uint[] Abilities = new uint[4];
        public uint[] Traits = new uint[4];
        public uint Specialization;

        public string tag() => "garrfollower";

        public bool Parse(string text)
        {
            HyperlinkDataTokenizer t = new(text);
            uint garrFollowerId;
            if (!t.TryConsumeTo(out garrFollowerId))
                return false;

            Follower = CliDB.GarrFollowerStorage.LookupByKey(garrFollowerId);
            if (Follower == null || !t.TryConsumeTo(out Quality) || Quality >= (int)ItemQuality.Max || !t.TryConsumeTo(out Level) || !t.TryConsumeTo(out ItemLevel)
                || !t.TryConsumeTo(out Abilities[0]) || !t.TryConsumeTo(out Abilities[1]) || !t.TryConsumeTo(out Abilities[2]) || !t.TryConsumeTo(out Abilities[3])
                || !t.TryConsumeTo(out Traits[0]) || !t.TryConsumeTo(out Traits[1]) || !t.TryConsumeTo(out Traits[2]) || !t.TryConsumeTo(out Traits[3])
                || !t.TryConsumeTo(out Specialization) || !t.IsEmpty())
                return false;

            foreach (uint ability in Abilities)
                if (ability != 0 && !CliDB.GarrAbilityStorage.ContainsKey(ability))
                    return false;

            foreach (uint trait in Traits)
                if (trait != 0 && !CliDB.GarrAbilityStorage.ContainsKey(trait))
                    return false;

            if (Specialization != 0 && !CliDB.GarrAbilityStorage.ContainsKey(Specialization))
                return false;

            return true;
        }
    }

    class GarrFollowerAbilityLinkData : GenericHyperlink<GarrAbilityRecord>
    {
        public override string tag() => "garrfollowerability";

        public override bool Parse(string text)
        {
            HyperlinkDataTokenizer t = new(text);
            uint garrAbilityId;
            if (!t.TryConsumeTo(out garrAbilityId))
                return false;
            return (value = CliDB.GarrAbilityStorage.LookupByKey(garrAbilityId)) != null && t.IsEmpty();
        }
    }

    class GarrisonMissionLinkData : IHyperlink
    {
        public GarrMissionRecord Mission;
        public ulong DbID;

        public string tag() => "garrmission";

        public bool Parse(string text)
        {
            HyperlinkDataTokenizer t = new(text);
            uint garrMissionId;
            if (!t.TryConsumeTo(out garrMissionId))
                return false;
            return (Mission = CliDB.GarrMissionStorage.LookupByKey(garrMissionId)) != null && t.TryConsumeTo(out DbID) && t.IsEmpty();
        }
    }

    class InstanceLockLinkData : IHyperlink
    {
        public ObjectGuid Owner;
        public MapRecord Map;
        public uint Difficulty;
        public uint CompletedEncountersMask;

        public string tag() => "instancelock";

        public bool Parse(string text)
        {
            HyperlinkDataTokenizer t = new(text);
            if (!t.TryConsumeTo(out Owner))
                return false;
            uint mapId;
            if (!t.TryConsumeTo(out mapId))
                return false;
            return (Map = CliDB.MapStorage.LookupByKey(mapId)) != null
                && t.TryConsumeTo(out Difficulty) && Global.DB2Mgr.GetMapDifficultyData(mapId, (Difficulty)Difficulty) != null
                && t.TryConsumeTo(out CompletedEncountersMask) && t.IsEmpty();
        }
    }

    class ItemLinkData : IHyperlink
    {
        public ItemTemplate Item;
        public uint EnchantId;
        public uint[] GemItemId = new uint[3];
        public byte RenderLevel;
        public uint RenderSpecialization;
        public byte Context;
        public List<uint> ItemBonusListIDs = new();

        public struct Modifier
        {
            public uint Type;
            public int Value;
        }

        public List<Modifier> Modifiers = new();
        public List<int>[] GemItemBonusListIDs = new List<int>[3];
        public ObjectGuid Creator;
        public uint UseEnchantId;

        public uint Quality;
        public ItemNameDescriptionRecord Suffix;

        public string tag() => "item";

        public bool Parse(string text)
        {
            HyperlinkDataTokenizer t = new(text, true);
            uint itemId, dummy, numBonusListIDs;
            if (!t.TryConsumeTo(out itemId))
                return false;
            Item = Global.ObjectMgr.GetItemTemplate(itemId);
            if (!(Item != null && t.TryConsumeTo(out EnchantId) && t.TryConsumeTo(out GemItemId[0]) && t.TryConsumeTo(out GemItemId[1]) &&
                t.TryConsumeTo(out GemItemId[2]) && t.TryConsumeTo(out dummy) && t.TryConsumeTo(out dummy) && t.TryConsumeTo(out dummy) &&
                t.TryConsumeTo(out RenderLevel) && t.TryConsumeTo(out RenderSpecialization) && t.TryConsumeTo(out dummy) &&
                t.TryConsumeTo(out Context) && t.TryConsumeTo(out numBonusListIDs)))
                return false;

            uint maxBonusListIDs = 16;
            if (numBonusListIDs > maxBonusListIDs)
                return false;

            BonusData evaluatedBonus = new(Item);

            for (var i = 0; i < numBonusListIDs; ++i)
            {
                if (!t.TryConsumeTo(out uint itemBonusListID))
                    return false;

                ItemBonusListIDs.Add(itemBonusListID);
                evaluatedBonus.AddBonusList(itemBonusListID);
            }

            if (!ItemBonusListIDs.Empty() && ItemBonusListIDs[0] == 3524) // default uninitialized bonus
            {
                ItemBonusListIDs = ItemBonusMgr.GetBonusListsForItem(itemId, new((ItemContext)Context));

                // reset bonuses
                evaluatedBonus = new(Item);
                foreach (uint itemBonusListID in ItemBonusListIDs)
                    evaluatedBonus.AddBonusList(itemBonusListID);
            }

            Quality = (uint)evaluatedBonus.Quality;
            Suffix = CliDB.ItemNameDescriptionStorage.LookupByKey(evaluatedBonus.Suffix);
            if (evaluatedBonus.Suffix != 0 && Suffix == null)
                return false;

            uint numModifiers;
            if (!t.TryConsumeTo(out numModifiers))
                return false;

            if (numModifiers > (int)ItemModifier.Max)
                return false;

            for (var i = 0; i < numModifiers; ++i)
            {
                Modifier modifier = new();
                if (!(t.TryConsumeTo(out modifier.Type) && modifier.Type < (int)ItemModifier.Max && t.TryConsumeTo(out modifier.Value)))
                    return false;

                Modifiers.Add(modifier);
            }

            for (uint i = 0; i < ItemConst.MaxGemSockets; ++i)
            {
                if (!t.TryConsumeTo(out numBonusListIDs) || numBonusListIDs > maxBonusListIDs)
                    return false;

                for (var c = 0; c < numBonusListIDs; ++c)
                {
                    if (!t.TryConsumeTo(out int itemBonusListID))
                        return false;

                    GemItemBonusListIDs[i].Add(itemBonusListID);
                }
            }

            return t.TryConsumeTo(out Creator) && t.TryConsumeTo(out UseEnchantId) && t.IsEmpty();
        }
    }

    class ItemsetLinkData : GenericHyperlink<uint>
    {
        public override string tag() => "itemset";

        public override bool Parse(string data)
        {
            if (uint.TryParse(data, out uint value))
                return true;

            return false;
        }
    }

    class JournalLinkData : IHyperlink
    {
        public enum Types
        {
            Instance = 0,
            Encounter = 1,
            EncounterSection = 2,
            Tier = 3
        }

        public byte Type;
        public LocalizedString ExpectedText;
        public uint Difficulty;

        public string tag() => "journal";

        public bool Parse(string text)
        {
            HyperlinkDataTokenizer t = new(text);
            uint id;
            if (!t.TryConsumeTo(out Type) || !t.TryConsumeTo(out id) || !t.TryConsumeTo(out Difficulty) || !t.IsEmpty())
                return false;
            switch ((JournalLinkData.Types)Type)
            {
                case JournalLinkData.Types.Instance:
                {
                    JournalInstanceRecord instance = CliDB.JournalInstanceStorage.LookupByKey(id);
                    if (instance == null)
                        return false;
                    ExpectedText = instance.Name;
                    break;
                }
                case JournalLinkData.Types.Encounter:
                {
                    JournalEncounterRecord encounter = CliDB.JournalEncounterStorage.LookupByKey(id);
                    if (encounter == null)
                        return false;
                    ExpectedText = encounter.Name;
                    break;
                }
                case JournalLinkData.Types.EncounterSection:
                {
                    JournalEncounterSectionRecord encounterSection = CliDB.JournalEncounterSectionStorage.LookupByKey(id);
                    if (encounterSection == null)
                        return false;
                    ExpectedText = encounterSection.Title;
                    break;
                }
                case JournalLinkData.Types.Tier:
                {
                    JournalTierRecord tier = Global.DB2Mgr.GetJournalTier(id);
                    if (tier == null)
                        return false;
                    ExpectedText = tier.Name;
                    break;
                }
                default:
                    return false;
            }
            return true;
        }
    }

    class KeystoneLinkData : IHyperlink
    {
        public uint ItemId;
        public MapChallengeModeRecord Map;
        public uint Level;
        public uint[] Affix = new uint[4];

        public string tag() => "keystone";

        public bool Parse(string text)
        {
            HyperlinkDataTokenizer t = new(text);
            uint mapChallengeModeId;
            if (!t.TryConsumeTo(out ItemId) || !t.TryConsumeTo(out mapChallengeModeId) || !t.TryConsumeTo(out Level)
                || !t.TryConsumeTo(out Affix[0]) || !t.TryConsumeTo(out Affix[1]) || !t.TryConsumeTo(out Affix[2]) || !t.TryConsumeTo(out Affix[3])
                || !t.IsEmpty())
                return false;
            Map = CliDB.MapChallengeModeStorage.LookupByKey(mapChallengeModeId);
            if (Map == null)
                return false;
            ItemTemplate item = Global.ObjectMgr.GetItemTemplate(ItemId);
            if (item == null || item.GetClass() != ItemClass.Reagent || item.GetSubClass() != (uint)ItemSubClassReagent.Keystone)
                return false;
            foreach (uint keystoneAffix in Affix)
                if (keystoneAffix != 0 && !CliDB.KeystoneAffixStorage.ContainsKey(keystoneAffix))
                    return false;
            return true;
        }
    }

    class MawPowerLinkData : GenericHyperlink<MawPowerRecord>
    {
        public override string tag() => "mawpower";

        public override bool Parse(string text)
        {
            HyperlinkDataTokenizer t = new(text);
            uint mawPowerId;
            if (!t.TryConsumeTo(out mawPowerId))
                return false;
            return (value = CliDB.MawPowerStorage.LookupByKey(mawPowerId)) != null && t.IsEmpty();
        }
    }

    class MountLinkData : IHyperlink
    {
        public SpellInfo Spell;
        public uint DisplayId;
        public string Customizations;

        public string tag() => "mount";

        public bool Parse(string text)
        {
            HyperlinkDataTokenizer t = new(text);
            uint spellId;
            if (!t.TryConsumeTo(out spellId) || (Spell = Global.SpellMgr.GetSpellInfo(spellId, Difficulty.None)) == null)
                return false;
            if (!t.TryConsumeTo(out DisplayId) || !CliDB.CreatureDisplayInfoStorage.ContainsKey(DisplayId))
                return false;
            return t.TryConsumeTo(out Customizations) && t.IsEmpty();
        }
    }

    class OutfitLinkData : GenericHyperlink<string>
    {
        public override string tag() => "outfit";

        public override bool Parse(string data)
        {
            value = data;
            return true;
        }
    }

    class PerksActivityLinkData : GenericHyperlink<PerksActivityRecord>
    {
        public override string tag() => "perksactivity";

        public override bool Parse(string text)
        {
            HyperlinkDataTokenizer t = new(text);
            uint perksActivityId;
            if (!t.TryConsumeTo(out perksActivityId))
                return false;
            return (value = CliDB.PerksActivityStorage.LookupByKey(perksActivityId)) != null && t.IsEmpty();
        }
    }

    class PlayerLinkData : GenericHyperlink<string>
    {
        public override string tag() => "player";

        public override bool Parse(string data)
        {
            value = data;
            return true;
        }
    }

    class PvpTalLinkData : GenericHyperlink<PvpTalentRecord>
    {
        public override string tag() => "pvptal";

        public override bool Parse(string text)
        {
            HyperlinkDataTokenizer t = new(text);
            uint pvpTalentId;
            if (!(t.TryConsumeTo(out pvpTalentId) && t.IsEmpty()))
                return false;
            if ((value = CliDB.PvpTalentStorage.LookupByKey(pvpTalentId)) == null)
                return false;
            return true;
        }
    }

    class QuestLinkData : IHyperlink
    {
        public Quest Quest;
        public uint ContentTuningId;

        public string tag() => "quest";

        public bool Parse(string text)
        {
            HyperlinkDataTokenizer t = new(text);
            uint questId;
            if (!t.TryConsumeTo(out questId))
                return false;
            return (Quest = Global.ObjectMgr.GetQuestTemplate(questId)) != null && t.TryConsumeTo(out ContentTuningId) && t.IsEmpty();
        }
    }

    class SkillLinkData : GenericHyperlink<uint>
    {
        public override string tag() => "skill";

        public override bool Parse(string data)
        {
            if (uint.TryParse(data, out value))
                return true;

            return false;
        }
    }

    class SpellLinkData : IHyperlink
    {
        public SpellInfo Spell;
        public GlyphPropertiesRecord Glyph;

        public string tag() => "spell";

        public bool Parse(string text)
        {
            HyperlinkDataTokenizer t = new(text);
            uint spellId, glyphPropertiesId;
            if (!(t.TryConsumeTo(out spellId) && t.TryConsumeTo(out glyphPropertiesId) && t.IsEmpty()))
                return false;
            return (Spell = Global.SpellMgr.GetSpellInfo(spellId, Difficulty.None)) != null
                && (glyphPropertiesId == 0 || (Glyph = CliDB.GlyphPropertiesStorage.LookupByKey(glyphPropertiesId)) != null);
        }
    }

    class TalentBuildLinkData : IHyperlink
    {
        public ChrSpecializationRecord Spec;
        public uint Level;
        public string ImportString;

        public string tag() => "talent";

        public bool Parse(string text)
        {
            HyperlinkDataTokenizer t = new(text);
            uint chrSpecializationId;
            if (!t.TryConsumeTo(out chrSpecializationId))
                return false;
            if ((Spec = CliDB.ChrSpecializationStorage.LookupByKey(chrSpecializationId)) == null)
                return false;
            if (!t.TryConsumeTo(out Level) || !t.TryConsumeTo(out ImportString))
                return false;
            return true;
        }
    }

    class TaxiNodeLinkData : GenericHyperlink<uint>
    {
        public override string tag() => "taxinode";

        public override bool Parse(string data)
        {
            if (uint.TryParse(data, out value))
                return true;

            return false;
        }
    }

    class TeleLinkData : GenericHyperlink<uint>
    {
        public override string tag() => "tele";

        public override bool Parse(string data)
        {
            if (uint.TryParse(data, out value))
                return true;

            return false;
        }
    }

    class TitleLinkData : GenericHyperlink<uint>
    {
        public override string tag() => "title";

        public override bool Parse(string data)
        {
            if (uint.TryParse(data, out value))
                return true;

            return false;
        }
    }

    class TradeskillLinkData : IHyperlink
    {
        public ObjectGuid Owner;
        public SpellInfo Spell;
        public SkillLineRecord Skill;

        public string tag() => "trade";

        public bool Parse(string text)
        {
            HyperlinkDataTokenizer t = new(text);
            uint spellId, skillId;
            if (!t.TryConsumeTo(out Owner) || !t.TryConsumeTo(out spellId) || !t.TryConsumeTo(out skillId) || !t.IsEmpty())
                return false;
            Spell = Global.SpellMgr.GetSpellInfo(spellId, Difficulty.None);
            Skill = CliDB.SkillLineStorage.LookupByKey(skillId);
            if (Spell == null || !Spell.HasEffect(SpellEffectName.TradeSkill) || Skill == null)
                return false;
            return true;
        }
    }

    class TransmogAppearanceLinkData : GenericHyperlink<ItemModifiedAppearanceRecord>
    {
        public override string tag() => "transmogappearance";

        public override bool Parse(string text)
        {
            HyperlinkDataTokenizer t = new(text);
            if (!t.TryConsumeTo(out uint itemModifiedAppearanceId))
                return false;
            return (value = CliDB.ItemModifiedAppearanceStorage.LookupByKey(itemModifiedAppearanceId)) != null && t.IsEmpty();
        }
    }

    class TransmogIllusionLinkData : GenericHyperlink<SpellItemEnchantmentRecord>
    {
        public override string tag() => "transmogillusion";

        public override bool Parse(string text)
        {
            HyperlinkDataTokenizer t = new(text);
            if (!t.TryConsumeTo(out uint spellItemEnchantmentId))
                return false;
            return (value = CliDB.SpellItemEnchantmentStorage.LookupByKey(spellItemEnchantmentId)) != null
                && Global.DB2Mgr.GetTransmogIllusionForEnchantment(spellItemEnchantmentId) != null && t.IsEmpty();
        }
    }

    class TransmogSetLinkData : GenericHyperlink<TransmogSetRecord>
    {
        public override string tag() => "transmogset";

        public override bool Parse(string text)
        {
            HyperlinkDataTokenizer t = new(text);
            if (!t.TryConsumeTo(out uint transmogSetId))
                return false;
            return (value = CliDB.TransmogSetStorage.LookupByKey(transmogSetId)) != null && t.IsEmpty();
        }
    }

    class WorldMapLinkData : IHyperlink
    {
        public UiMapRecord UiMap;
        public uint X;
        public uint Y;
        public uint? Z;

        public string tag() => "worldmap";

        public bool Parse(string text)
        {
            HyperlinkDataTokenizer t = new(text);
            uint uiMapId;
            if (!t.TryConsumeTo(out uiMapId))
                return false;
            UiMap = CliDB.UiMapStorage.LookupByKey(uiMapId);
            if (UiMap == null || !t.TryConsumeTo(out X) || !t.TryConsumeTo(out Y))
                return false;
            if (t.IsEmpty())
                return true;
            if (!t.TryConsumeTo(out Z))
                return false;
            return t.IsEmpty();
        }
    }
}