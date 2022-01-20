/*
 * Copyright (C) 2012-2020 CypherCore <http://github.com/CypherCore>
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

using Framework.Constants;
using Game.DataStorage;
using Game.Entities;
using Game.Spells;
using System;
using System.Collections.Generic;

namespace Game.Chat
{
    class achievement : HyperLinks<AchievementLinkData>
    {
        public override string GetTag() { return nameof(achievement); }

        public override bool StoreTo(out AchievementLinkData val, string arg)
        {
            val = default;

            HyperlinkDataTokenizer t = new(arg);
            if (!t.TryConsumeTo(out uint achievementId))
                return false;

            val.Achievement = CliDB.AchievementStorage.LookupByKey(achievementId);

            return val.Achievement != null && t.TryConsumeTo(out val.CharacterId) && t.TryConsumeTo(out val.IsFinished) &&
                t.TryConsumeTo(out val.Month) && t.TryConsumeTo(out val.Day) && t.TryConsumeTo(out val.Year) && t.TryConsumeTo(out val.Criteria[0]) &&
                t.TryConsumeTo(out val.Criteria[1]) && t.TryConsumeTo(out val.Criteria[2]) && t.TryConsumeTo(out val.Criteria[3]) && t.IsEmpty();
        }
    }

    struct apower
    {
        public string GetTag() { return nameof(apower); }

        public bool StoreTo(out ArtifactPowerLinkData val, string arg)
        {
            val = default;

            HyperlinkDataTokenizer t = new(arg);
            if (!(t.TryConsumeTo(out uint artifactPowerId) && t.TryConsumeTo(out val.PurchasedRank) && t.TryConsumeTo(out val.CurrentRankWithBonus) && t.IsEmpty()))
                return false;

            if (!CliDB.ArtifactPowerStorage.ContainsKey(artifactPowerId))
                return false;

            val.ArtifactPower = Global.DB2Mgr.GetArtifactPowerRank(artifactPowerId, Math.Max(val.CurrentRankWithBonus, (byte)1));
            if (val.ArtifactPower == null)
                return false;

            return true;
        }
    }

    struct area
    {
        public string GetTag() { return nameof(area); }

        public bool StoreTo(out uint val, string arg)
        {
            if (!uint.TryParse(arg, out val))
                return false;

            return true;
        }
    }

    struct areatrigger
    {
        public string GetTag() { return nameof(areatrigger); }

        public bool StoreTo(out uint val, string arg)
        {
            if (!uint.TryParse(arg, out val))
                return false;

            return true;
        }
    }

    struct azessence
    {
        public string GetTag() { return nameof(azessence); }

        public bool StoreTo(out AzeriteEssenceLinkData val, string arg)
        {
            val = default;

            HyperlinkDataTokenizer t = new(arg);
            if (!t.TryConsumeTo(out uint azeriteEssenceId))
                return false;

            val.Essence = CliDB.AzeriteEssenceStorage.LookupByKey(azeriteEssenceId);
            return val.Essence != null && t.TryConsumeTo(out val.Rank)
                && Global.DB2Mgr.GetAzeriteEssencePower(azeriteEssenceId, val.Rank) != null && t.IsEmpty();
        }
    }

    struct battlepet
    {
        public string GetTag() { return nameof(battlepet); }

        public bool StoreTo(out BattlePetLinkData val, string arg)
        {
            val = default;

            HyperlinkDataTokenizer t = new(arg);
            if (!t.TryConsumeTo(out uint battlePetSpeciesId))
                return false;

            val.Species = CliDB.BattlePetSpeciesStorage.LookupByKey(battlePetSpeciesId);

            return val.Species != null && t.TryConsumeTo(out val.Level)
                && t.TryConsumeTo(out val.Quality) && val.Quality < (int)ItemQuality.Max
                && t.TryConsumeTo(out val.MaxHealth) && t.TryConsumeTo(out val.Power) && t.TryConsumeTo(out val.Speed)
                && t.TryConsumeTo(out val.PetGuid) && val.PetGuid.GetHigh() == HighGuid.BattlePet && t.TryConsumeTo(out val.DisplayId);
        }
    }

    struct conduit
    {
        public string GetTag() { return nameof(conduit); }

        public bool StoreTo(out SoulbindConduitRankRecord val, string arg)
        {
            val = default;

            HyperlinkDataTokenizer t = new(arg);
            uint soulbindConduitId, rank;
            if (!(t.TryConsumeTo(out soulbindConduitId) && t.TryConsumeTo(out rank) && t.IsEmpty()))
                return false;

            val = Global.DB2Mgr.GetSoulbindConduitRank((int)soulbindConduitId, (int)rank);
            return val != null;
        }
    }

    struct creature
    {
        public string GetTag() { return nameof(creature); }

        public bool StoreTo(out ulong val, string arg)
        {
            if (!ulong.TryParse(arg, out val))
                return false;

            return true;
        }
    }

    struct creature_entry
    {
        public string GetTag() { return nameof(creature_entry); }

        public bool StoreTo(out uint val, string arg)
        {
            if (!uint.TryParse(arg, out val))
                return false;

            return true;
        }
    }

    struct currency
    {
        public string GetTag() { return nameof(currency); }

        public bool StoreTo(out CurrencyLinkData val, string arg)
        {
            val = default;

            HyperlinkDataTokenizer t = new(arg);
            if (!t.TryConsumeTo(out uint currencyId))
                return false;

            val.Currency = CliDB.CurrencyTypesStorage.LookupByKey(currencyId);
            if (val.Currency == null || !t.TryConsumeTo(out val.Quantity) || !t.IsEmpty())
                return false;
            val.Container = Global.DB2Mgr.GetCurrencyContainerForCurrencyQuantity(currencyId, val.Quantity);
            return true;
        }
    }

    struct enchant
    {
        public string GetTag() { return nameof(enchant); }

        public bool StoreTo(out SpellInfo val, string arg)
        {
            val = default;

            HyperlinkDataTokenizer t = new(arg);
            if (!(t.TryConsumeTo(out uint spellId) && t.IsEmpty()))
                return false;

            val = Global.SpellMgr.GetSpellInfo(spellId, Difficulty.None);
            return val != null && val.HasAttribute(SpellAttr0.Tradespell);
        }
    }

    struct gameevent
    {
        public string GetTag() { return nameof(gameevent); }

        public bool StoreTo(out uint val, string arg)
        {
            if (!uint.TryParse(arg, out val))
                return false;

            return true;
        }
    }

    struct gameobject
    {
        public string GetTag() { return nameof(gameobject); }

        public bool StoreTo(out ulong val, string arg)
        {
            if (!ulong.TryParse(arg, out val))
                return false;

            return true;
        }
    }

    struct gameobject_entry
    {
        public string GetTag() { return nameof(gameobject_entry); }

        public bool StoreTo(out uint val, string arg)
        {
            if (!uint.TryParse(arg, out val))
                return false;

            return true;
        }
    }

    struct garrfollower
    {
        public string GetTag() { return nameof(garrfollower); }

        public bool StoreTo(out GarrisonFollowerLinkData val, string arg)
        {
            val = default;

            HyperlinkDataTokenizer t = new(arg);
            if (!t.TryConsumeTo(out uint garrFollowerId))
                return false;

            val.Follower = CliDB.GarrFollowerStorage.LookupByKey(garrFollowerId);
            if (val.Follower == null || !t.TryConsumeTo(out val.Quality) || val.Quality >= (int)ItemQuality.Max || !t.TryConsumeTo(out val.Level) || !t.TryConsumeTo(out val.ItemLevel)
                || !t.TryConsumeTo(out val.Abilities[0]) || !t.TryConsumeTo(out val.Abilities[1]) || !t.TryConsumeTo(out val.Abilities[2]) || !t.TryConsumeTo(out val.Abilities[3])
                || !t.TryConsumeTo(out val.Traits[0]) || !t.TryConsumeTo(out val.Traits[1]) || !t.TryConsumeTo(out val.Traits[2]) || !t.TryConsumeTo(out val.Traits[3])
                || !t.TryConsumeTo(out val.Specialization))
                return false;

            foreach (uint ability in val.Abilities)
                if (ability != 0 && !CliDB.GarrAbilityStorage.ContainsKey(ability))
                    return false;

            foreach (uint trait in val.Traits)
                if (trait != 0 && !CliDB.GarrAbilityStorage.ContainsKey(trait))
                    return false;

            if (val.Specialization != 0 && !CliDB.GarrAbilityStorage.ContainsKey(val.Specialization))
                return false;

            return true;
        }
    }

    struct garrfollowerability
    {
        public string GetTag() { return nameof(garrfollowerability); }

        public bool StoreTo(out GarrAbilityRecord val, string arg)
        {
            val = default;

            HyperlinkDataTokenizer t = new(arg);
            if (!t.TryConsumeTo(out uint garrAbilityId))
                return false;

            val = CliDB.GarrAbilityStorage.LookupByKey(garrAbilityId);
            return val != null && t.IsEmpty();
        }
    }

    struct garrmission
    {
        public string GetTag() { return nameof(garrmission); }

        public bool StoreTo(out GarrisonMissionLinkData val, string arg)
        {
            val = default;

            HyperlinkDataTokenizer t = new(arg);
            if (!t.TryConsumeTo(out uint garrMissionId))
                return false;

            val.Mission = CliDB.GarrMissionStorage.LookupByKey(garrMissionId);
            return val.Mission != null && t.TryConsumeTo(out val.DbID) && t.IsEmpty();
        }
    }

    struct instancelock
    {
        public string GetTag() { return nameof(instancelock); }

        public bool StoreTo(out InstanceLockLinkData val, string arg)
        {
            val = default;

            HyperlinkDataTokenizer t = new(arg);
            if (!t.TryConsumeTo(out val.Owner))
                return false;
            uint mapId;
            if (!t.TryConsumeTo(out mapId))
                return false;

            val.Map = CliDB.MapStorage.LookupByKey(mapId);
            return val.Map != null
                && t.TryConsumeTo(out val.Difficulty) && Global.DB2Mgr.GetMapDifficultyData(mapId, (Difficulty)val.Difficulty) != null
                && t.TryConsumeTo(out val.CompletedEncountersMask) && t.IsEmpty();
        }
    }

    struct item
    {
        public string GetTag() { return nameof(item); }

        public bool StoreTo(out ItemLinkData val, string arg)
        {
            val = default;

            HyperlinkDataTokenizer t = new(arg, true);
            if (!t.TryConsumeTo(out uint itemId))
                return false;

            val.Item = Global.ObjectMgr.GetItemTemplate(itemId);
            if (!(val.Item != null && t.TryConsumeTo(out val.EnchantId) && t.TryConsumeTo(out val.GemItemId[0]) && t.TryConsumeTo(out val.GemItemId[1]) &&
                t.TryConsumeTo(out val.GemItemId[2]) && t.TryConsumeTo(out uint dummy) && dummy == 0 && t.TryConsumeTo(out dummy) && dummy == 0 && t.TryConsumeTo(out dummy) && dummy == 0 &&
                t.TryConsumeTo(out val.RenderLevel) && t.TryConsumeTo(out val.RenderSpecialization) && t.TryConsumeTo(out dummy) && dummy == 0 &&
                t.TryConsumeTo(out val.Context) && t.TryConsumeTo(out uint numBonusListIDs)))
                return false;

            uint maxBonusListIDs = 16;
            if (numBonusListIDs > maxBonusListIDs)
                return false;

            BonusData evaluatedBonus = new(val.Item);

            for (var i = 0; i < numBonusListIDs; ++i)
            {
                if (!t.TryConsumeTo(out uint itemBonusListID) || Global.DB2Mgr.GetItemBonusList(itemBonusListID) == null)
                    return false;

                val.ItemBonusListIDs.Add((int)itemBonusListID);
                evaluatedBonus.AddBonusList(itemBonusListID);
            }

            val.Quality = (uint)evaluatedBonus.Quality;
            val.Suffix = CliDB.ItemNameDescriptionStorage.LookupByKey(evaluatedBonus.Suffix);
            if (evaluatedBonus.Suffix != 0 && val.Suffix == null)
                return false;

            uint numModifiers;
            if (!t.TryConsumeTo(out numModifiers))
                return false;

            if (numModifiers > (int)ItemModifier.Max)
                return false;

            val.Modifiers.Resize(numModifiers);
            for (var i = 0; i < numModifiers; ++i)
            {
                ItemLinkData.Modifier modifier;
                if (!(t.TryConsumeTo(out modifier.Type) && modifier.Type < (int)ItemModifier.Max && t.TryConsumeTo(out modifier.Value)))
                    return false;

                val.Modifiers.Add(modifier);
            }

            for (uint i = 0; i < ItemConst.MaxGemSockets; ++i)
            {
                if (!t.TryConsumeTo(out numBonusListIDs) || numBonusListIDs > maxBonusListIDs)
                    return false;

                for (var c = 0; c < numBonusListIDs; ++c)
                {
                    if (!t.TryConsumeTo(out int itemBonusListID) || Global.DB2Mgr.GetItemBonusList((uint)itemBonusListID) == null)
                        return false;

                    val.GemItemBonusListIDs[i].Add(itemBonusListID);
                }
            }

            return t.TryConsumeTo(out val.Creator) && t.TryConsumeTo(out val.UseEnchantId) && t.IsEmpty();
        }
    }

    struct itemset
    {
        public string GetTag() { return nameof(itemset); }

        public bool StoreTo(out uint val, string arg)
        {
            if (!uint.TryParse(arg, out val))
                return false;

            return true;
        }
    }

    struct journal
    {
        public string GetTag() { return nameof(journal); }

        public bool StoreTo(out JournalLinkData val, string arg)
        {
            val = default;

            HyperlinkDataTokenizer t = new(arg);
            if (!t.TryConsumeTo(out val.Type) || !t.TryConsumeTo(out uint id) || !t.TryConsumeTo(out val.Difficulty) || !t.IsEmpty())
                return false;

            switch ((JournalLinkData.Types)val.Type)
            {
                case JournalLinkData.Types.Instance:
                {
                    JournalInstanceRecord instance = CliDB.JournalInstanceStorage.LookupByKey(id);
                    if (instance == null)
                        return false;
                    val.ExpectedText = instance.Name;
                    break;
                }
                case JournalLinkData.Types.Encounter:
                {
                    JournalEncounterRecord encounter = CliDB.JournalEncounterStorage.LookupByKey(id);
                    if (encounter == null)
                        return false;
                    val.ExpectedText = encounter.Name;
                    break;
                }
                case JournalLinkData.Types.EncounterSection:
                {
                    JournalEncounterSectionRecord encounterSection = CliDB.JournalEncounterSectionStorage.LookupByKey(id);
                    if (encounterSection == null)
                        return false;
                    val.ExpectedText = encounterSection.Title;
                    break;
                }
                case JournalLinkData.Types.Tier:
                {
                    JournalTierRecord tier = Global.DB2Mgr.GetJournalTier(id);
                    if (tier == null)
                        return false;
                    val.ExpectedText = tier.Name;
                    break;
                }
                default:
                    return false;
            }
            return true;
        }
    }

    struct keystone
    {
        public string GetTag() { return nameof(keystone); }

        public bool StoreTo(out KeystoneLinkData val, string arg)
        {
            val = default;

            HyperlinkDataTokenizer t = new(arg);
            if (!t.TryConsumeTo(out val.ItemId) || !t.TryConsumeTo(out uint mapChallengeModeId) || !t.TryConsumeTo(out val.Level)
                || !t.TryConsumeTo(out val.Affix[0]) || !t.TryConsumeTo(out val.Affix[1]) || !t.TryConsumeTo(out val.Affix[2]) || !t.TryConsumeTo(out val.Affix[3])
                || !t.IsEmpty())
                return false;

            val.Map = CliDB.MapChallengeModeStorage.LookupByKey(mapChallengeModeId);
            if (val.Map == null)
                return false;

            ItemTemplate item = Global.ObjectMgr.GetItemTemplate(val.ItemId);
            if (item == null || item.GetClass() != ItemClass.Reagent || item.GetSubClass() != (uint)ItemSubClassReagent.Keystone)
                return false;

            foreach (uint keystoneAffix in val.Affix)
                if (keystoneAffix != 0 && !CliDB.KeystoneAffixStorage.ContainsKey(keystoneAffix))
                    return false;
            return true;
        }
    }

    struct mawpower
    {
        public string GetTag() { return nameof(mawpower); }

        public bool StoreTo(out MawPowerRecord val, string arg)
        {
            val = default;

            HyperlinkDataTokenizer t = new(arg);
            if (!t.TryConsumeTo(out uint mawPowerId))
                return false;

            val = CliDB.MawPowerStorage.LookupByKey(mawPowerId);
            return val != null && t.IsEmpty();
        }
    }

    struct player
    {
        public string GetTag() { return nameof(player); }

        public bool StoreTo(out string val, string arg)
        {
            val = arg;
            return true;
        }
    }

    struct pvptal
    {
        public string GetTag() { return nameof(pvptal); }

        public bool StoreTo(out PvpTalentRecord val, string arg)
        {
            val = default;

            HyperlinkDataTokenizer t = new(arg);
            if (!(t.TryConsumeTo(out uint pvpTalentId) && t.IsEmpty()))
                return false;

            val = CliDB.PvpTalentStorage.LookupByKey(pvpTalentId);
            return val != null;
        }
    }

    struct quest
    {
        public string GetTag() { return nameof(quest); }

        public bool StoreTo(out QuestLinkData val, string arg)
        {
            val = default;

            HyperlinkDataTokenizer t = new(arg);
            if (!t.TryConsumeTo(out uint questId))
                return false;

            val.Quest = Global.ObjectMgr.GetQuestTemplate(questId);
            return val.Quest != null && t.TryConsumeTo(out val.ContentTuningId) && t.IsEmpty();
        }
    }

    struct skill
    {
        public string GetTag() { return nameof(skill); }

        public bool StoreTo(out uint val, string arg)
        {
            if (!uint.TryParse(arg, out val))
                return false;

            return true;
        }
    }

    struct spell
    {
        public string GetTag() { return nameof(spell); }

        public bool StoreTo(out SpellLinkData val, string arg)
        {
            val = default;

            HyperlinkDataTokenizer t = new(arg);
            if (!(t.TryConsumeTo(out uint spellId) && t.TryConsumeTo(out uint glyphPropertiesId) && t.IsEmpty()))
                return false;

            val.Spell = Global.SpellMgr.GetSpellInfo(spellId, Difficulty.None);
            val.Glyph = CliDB.GlyphPropertiesStorage.LookupByKey(glyphPropertiesId);

            return val.Spell != null && (glyphPropertiesId == 0 || val.Glyph != null);
        }
    }

    struct talent
    {
        public string GetTag() { return nameof(talent); }

        public bool StoreTo(out TalentRecord val, string arg)
        {
            val = default;

            HyperlinkDataTokenizer t = new(arg);
            if (!(t.TryConsumeTo(out uint talentId) && t.IsEmpty()))
                return false;

            val = CliDB.TalentStorage.LookupByKey(talentId);
            return val != null;
        }
    }

    struct taxinode
    {
        public string GetTag() { return nameof(taxinode); }

        public bool StoreTo(out uint val, string arg)
        {
            if (!uint.TryParse(arg, out val))
                return false;

            return true;
        }
    }

    struct tele
    {
        public string GetTag() { return nameof(tele); }

        public bool StoreTo(out uint val, string arg)
        {
            if (!uint.TryParse(arg, out val))
                return false;

            return true;
        }
    }

    struct title
    {
        public string GetTag() { return nameof(title); }

        public bool StoreTo(out uint val, string arg)
        {
            if (!uint.TryParse(arg, out val))
                return false;

            return true;
        }
    }

    struct trade
    {
        public string GetTag() { return nameof(trade); }

        public bool StoreTo(out TradeskillLinkData val, string arg)
        {
            val = default;

            HyperlinkDataTokenizer t = new(arg);
            if (!t.TryConsumeTo(out val.Owner) || !t.TryConsumeTo(out uint spellId) || !t.TryConsumeTo(out uint skillId) || !t.IsEmpty())
                return false;

            val.Spell = Global.SpellMgr.GetSpellInfo(spellId, Difficulty.None);
            val.Skill = CliDB.SkillLineStorage.LookupByKey(skillId);
            if (val.Spell == null || !val.Spell.HasEffect(SpellEffectName.TradeSkill) || val.Skill == null)
                return false;

            return true;
        }
    }

    struct transmogappearance
    {
        public string GetTag() { return nameof(transmogappearance); }

        public bool StoreTo(out ItemModifiedAppearanceRecord val, string arg)
        {
            val = default;

            HyperlinkDataTokenizer t = new(arg);
            if (!t.TryConsumeTo(out uint itemModifiedAppearanceId))
                return false;

            val = CliDB.ItemModifiedAppearanceStorage.LookupByKey(itemModifiedAppearanceId);
            return val != null && t.IsEmpty();
        }
    }

    struct transmogillusion
    {
        public string GetTag() { return nameof(transmogillusion); }

        public bool StoreTo(out SpellItemEnchantmentRecord val, string arg)
        {
            val = default;

            HyperlinkDataTokenizer t = new(arg);
            if (!t.TryConsumeTo(out uint spellItemEnchantmentId))
                return false;

            val = CliDB.SpellItemEnchantmentStorage.LookupByKey(spellItemEnchantmentId);
            return val != null
                && Global.DB2Mgr.GetTransmogIllusionForEnchantment(spellItemEnchantmentId) != null && t.IsEmpty();
        }
    }

    struct transmogset
    {
        public string GetTag() { return nameof(transmogset); }

        public bool StoreTo(out TransmogSetRecord val, string arg)
        {
            val = default;

            HyperlinkDataTokenizer t = new(arg);
            if (!t.TryConsumeTo(out uint transmogSetId))
                return false;

            val = CliDB.TransmogSetStorage.LookupByKey(transmogSetId);
            return val != null && t.IsEmpty();
        }
    }

    struct worldmap
    {
        public string GetTag() { return nameof(worldmap); }

        public bool StoreTo(out WorldMapLinkData val, string arg)
        {
            val = default;

            HyperlinkDataTokenizer t = new(arg);
            if (!t.TryConsumeTo(out uint uiMapId))
                return false;

            val.UiMap = CliDB.UiMapStorage.LookupByKey(uiMapId);
            if (val.UiMap == null || !t.TryConsumeTo(out val.X) || !t.TryConsumeTo(out val.Y))
                return false;

            if (t.IsEmpty())
                return true;

            if (!t.TryConsumeTo(out uint z))
                return false;

            val.Z = z;

            return t.IsEmpty();
        }
    }
}