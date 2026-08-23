// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.DataStorage;
using Game.Entities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Game
{
    public class TransmogManager : Singleton<TransmogManager>
    {
        Dictionary<(uint itemId, uint appearanceMod), ItemModifiedAppearanceRecord> ItemModifiedAppearancesByItem = [];
        Dictionary<uint, TransmogIllusionRecord> TransmogIllusionBySpellItemEnchantment = [];
        MultiMap<uint, TransmogSetRecord> TransmogSetsByItemModifiedAppearance = [];
        List<TransmogSetItemRecord> TransmogSetItemsByTransmogSet = [];
        List<TransmogOutfitEntryRecord>[] TransmogOutfitsBySource = new List<TransmogOutfitEntryRecord>[(int)TransmogOutfitEntrySource.Max];
        List<TransmogOutfitEntryRecord> TransmogOutfitsAutomaticallyCreated = [];
        List<TransmogOutfitSlotAndOptionInfo> AllSlots = [];
        TransmogOutfitSlotInfo[] SlotInfoByOutfitSlot = new TransmogOutfitSlotInfo[(int)TransmogOutfitSlot.Max];
        TransmogOutfitSlotInfo[] SlotInfoByInvSlot = new TransmogOutfitSlotInfo[EquipmentSlot.End];
        List<TransmogSituationRecord> DefaultSituations = [];

        public static string[] DefaultOutfitName =
        [
            "Outfit",
            @"\354\235\230\354\203\201",
            "Tenue",
            "Outfit",
            @"\345\244\226\350\247\202\346\226\271\346\241\210",
            @"\346\234\215\350\243\235",
            "Atuendo",
            "Indumentaria",
            @"\320\241\320\275\320\260\321\200\321\217\320\266\320\265\320\275\320\270\320\265",
            "",
            "Roupa",
            "Completo"
        ];

        public static uint DefaultOutfitIcon = 134400;

        ItemSheatheType[][] TransmogSheatheMappingByCategoryAndSheatheType =
        [
            [ ItemSheatheType.None,                   ItemSheatheType.None,              ItemSheatheType.None, ItemSheatheType.None    ],
            [ ItemSheatheType.Shoulder,               ItemSheatheType.Shoulder,          ItemSheatheType.None, ItemSheatheType.ShoulderInvis],
            [ ItemSheatheType.Inverted,               ItemSheatheType.Inverted,          ItemSheatheType.None, ItemSheatheType.InvertedInvis          ],
            [ ItemSheatheType.Hip,                    ItemSheatheType.Shoulder,          ItemSheatheType.Hip,  ItemSheatheType.HipInvis               ],
            [ ItemSheatheType.Shield,                 ItemSheatheType.Shield,            ItemSheatheType.None, ItemSheatheType.ShieldInvis            ],
            [ ItemSheatheType.Crossbow,               ItemSheatheType.Crossbow,          ItemSheatheType.None, ItemSheatheType.CrossbowInvis          ],
            [ ItemSheatheType.ShoulderInvis,          ItemSheatheType.Shoulder,          ItemSheatheType.None, ItemSheatheType.ShoulderInvis          ],
            [ ItemSheatheType.InvertedInvis,          ItemSheatheType.Inverted,          ItemSheatheType.None, ItemSheatheType.InvertedInvis          ],
            [ ItemSheatheType.HipInvis,               ItemSheatheType.Shoulder,          ItemSheatheType.Hip,  ItemSheatheType.HipInvis               ],
            [ ItemSheatheType.ShieldInvis,            ItemSheatheType.Shield,            ItemSheatheType.None, ItemSheatheType.ShieldInvis            ],
            [ ItemSheatheType.CrossbowInvis,          ItemSheatheType.Crossbow,          ItemSheatheType.None, ItemSheatheType.CrossbowInvis          ],
            [ ItemSheatheType.InvertedDualWield,      ItemSheatheType.InvertedDualWield, ItemSheatheType.None, ItemSheatheType.InvertedDualWieldInvis ],
            [ ItemSheatheType.InvertedDualWieldInvis, ItemSheatheType.InvertedDualWield, ItemSheatheType.None, ItemSheatheType.InvertedDualWieldInvis ]
        ];

        TransmogManager() { }

        public bool IsArtifactTransmogOutfitSlotOption(TransmogOutfitSlotOption option)
        {
            return option == TransmogOutfitSlotOption.ArtifactSpecOne
                || option == TransmogOutfitSlotOption.ArtifactSpecTwo
                || option == TransmogOutfitSlotOption.ArtifactSpecThree
                || option == TransmogOutfitSlotOption.ArtifactSpecFour;
        }

        public bool IsValidTransmogOutfitSlotForItem(ItemTemplate item, TransmogOutfitSlot slot, TransmogOutfitSlotOption option)
        {
            if (IsArtifactTransmogOutfitSlotOption(option))
            {
                ArtifactRecord artifact = CliDB.ArtifactStorage.LookupByKey(item.GetArtifactID());
                if (artifact != null)
                {
                    ChrSpecializationRecord specialization = CliDB.ChrSpecializationStorage.LookupByKey(artifact.ChrSpecializationID);
                    if (specialization != null)
                        if ((option - TransmogOutfitSlotOption.ArtifactSpecOne) != specialization.OrderIndex)
                            return false;
                }
            }

            switch (item.GetInventoryType())
            {
                case InventoryType.Head:
                    return slot == TransmogOutfitSlot.Head;
                case InventoryType.Shoulders:
                    return slot == TransmogOutfitSlot.ShoulderLeft || slot == TransmogOutfitSlot.ShoulderRight;
                case InventoryType.Body:
                    return slot == TransmogOutfitSlot.Body;
                case InventoryType.Chest:
                case InventoryType.Robe:
                    return slot == TransmogOutfitSlot.Chest;
                case InventoryType.Waist:
                    return slot == TransmogOutfitSlot.Waist;
                case InventoryType.Legs:
                    return slot == TransmogOutfitSlot.Legs;
                case InventoryType.Feet:
                    return slot == TransmogOutfitSlot.Feet;
                case InventoryType.Wrists:
                    return slot == TransmogOutfitSlot.Wrist;
                case InventoryType.Hands:
                    return slot == TransmogOutfitSlot.Hand;
                case InventoryType.Weapon:
                case InventoryType.WeaponMainhand:
                case InventoryType.WeaponOffhand:
                    return slot == TransmogOutfitSlot.WeaponMainHand || slot == TransmogOutfitSlot.WeaponOffHand || IsArtifactTransmogOutfitSlotOption(option);
                case InventoryType.Shield:
                case InventoryType.Holdable:
                    return slot == TransmogOutfitSlot.WeaponOffHand || IsArtifactTransmogOutfitSlotOption(option);
                case InventoryType.Ranged:
                    return (slot == TransmogOutfitSlot.WeaponMainHand && option == TransmogOutfitSlotOption.RangedWeapon) || IsArtifactTransmogOutfitSlotOption(option);
                case InventoryType.Cloak:
                    return slot == TransmogOutfitSlot.Back;
                case InventoryType.Weapon2Hand:
                    return slot == TransmogOutfitSlot.WeaponMainHand || (slot == TransmogOutfitSlot.WeaponOffHand && option == TransmogOutfitSlotOption.FuryTwoHandedWeapon) || IsArtifactTransmogOutfitSlotOption(option);
                case InventoryType.Tabard:
                    return slot == TransmogOutfitSlot.Tabard;
                case InventoryType.RangedRight:
                    return slot == (item.GetSubClass() == (uint)ItemSubClassWeapon.Wand ? TransmogOutfitSlot.WeaponMainHand : TransmogOutfitSlot.WeaponRanged) || IsArtifactTransmogOutfitSlotOption(option);
                default:
                    break;
            }
            return false;
        }

        public void Load()
        {
            for (var i = 0; i < TransmogOutfitsBySource.Length; ++i)
                TransmogOutfitsBySource[i] = new List<TransmogOutfitEntryRecord>();

            foreach (ItemModifiedAppearanceRecord appearanceMod in CliDB.ItemModifiedAppearanceStorage.Values)
                ItemModifiedAppearancesByItem.Add((appearanceMod.ItemID, (uint)appearanceMod.ItemAppearanceModifierID), appearanceMod);

            foreach (TransmogIllusionRecord transmogIllusion in CliDB.TransmogIllusionStorage.Values)
                TransmogIllusionBySpellItemEnchantment[(uint)transmogIllusion.SpellItemEnchantmentID] = transmogIllusion;

            foreach (TransmogSetItemRecord transmogSetItem in CliDB.TransmogSetItemStorage.Values)
            {
                TransmogSetRecord set = CliDB.TransmogSetStorage.LookupByKey(transmogSetItem.TransmogSetID);
                if (set == null)
                    continue;

                TransmogSetsByItemModifiedAppearance.Add(transmogSetItem.ItemModifiedAppearanceID, set);
                TransmogSetItemsByTransmogSet.Add(transmogSetItem);
            }

            TransmogSetItemsByTransmogSet.Sort((x, y) => x.TransmogSetID.CompareTo(y.TransmogSetID));

            foreach (TransmogOutfitEntryRecord transmogOutfitEntry in CliDB.TransmogOutfitEntryStorage.Values)
            {
                if (transmogOutfitEntry.HasFlag(TransmogOutfitEntryFlags.AutomaticallyAwardedOnLogin))
                    TransmogOutfitsAutomaticallyCreated.Add(transmogOutfitEntry);

                if (transmogOutfitEntry.GetSetType() == TransmogOutfitSetType.Outfit)
                    TransmogOutfitsBySource[(int)transmogOutfitEntry.GetSource()].Add(transmogOutfitEntry);
            }

            foreach (var transmogOutfitEntries in TransmogOutfitsBySource)
                transmogOutfitEntries.Sort((x, y) => x.OrderIndex.CompareTo(y.OrderIndex));

            foreach (TransmogOutfitSlotInfoRecord transmogOutfitSlot in CliDB.TransmogOutfitSlotInfoStorage.Values)
            {
                Cypher.Assert(transmogOutfitSlot.GetSlot() < TransmogOutfitSlot.Max);

                TransmogOutfitSlotInfo slot = new();
                slot.Data = transmogOutfitSlot;

                if (!transmogOutfitSlot.HasFlag(TransmogOutfitSlotFlags.IsSecondarySlot))
                {
                    Cypher.Assert(transmogOutfitSlot.InventorySlotEnum < EquipmentSlot.End);
                    SlotInfoByInvSlot[transmogOutfitSlot.InventorySlotEnum] = slot;
                }

                SlotInfoByOutfitSlot[(int)transmogOutfitSlot.GetSlot()] = slot;
            }

            foreach (TransmogOutfitSlotOptionRecord transmogOutfitSlotOption in CliDB.TransmogOutfitSlotOptionInfoStorage.Values)
            {
                Cypher.Assert(transmogOutfitSlotOption.GetOption() < TransmogOutfitSlotOption.Max);

                TransmogOutfitSlotInfoRecord transmogOutfitSlot = CliDB.TransmogOutfitSlotInfoStorage.LookupByKey(transmogOutfitSlotOption.TransmogOutfitSlotInfoID);

                TransmogOutfitSlotInfo slotInfo = SlotInfoByOutfitSlot[(int)transmogOutfitSlot.GetSlot()];
                if (slotInfo.Options == null)
                    slotInfo.Options = new TransmogOutfitSlotOptionInfo[(int)TransmogOutfitSlotOption.Max];

                slotInfo.Options[(int)transmogOutfitSlotOption.GetOption()].Data = transmogOutfitSlotOption;
            }

            for (var i = 0; i < SlotInfoByOutfitSlot.Length; ++i)
            {
                TransmogOutfitSlotInfo slotInfo = SlotInfoByOutfitSlot[i];
                if (slotInfo == null)
                    continue;

                AllSlots.Add(new TransmogOutfitSlotAndOptionInfo());

                var slot = AllSlots.Last();
                slot.Slot = slotInfo.Data;
                slot.SlotIndex = (uint)AllSlots.Count - 1;

                if (slotInfo.Options != null)
                {
                    // if slot has options, keep adding transmog slots for every option
                    var options = slotInfo.Options;

                    var index = Array.FindIndex(options, p => p.Data != null);

                    slot.SlotOption = options[index].Data;
                    options[index].SlotIndex = AllSlots.Count - 1;

                    while (++index != options.Length)
                    {
                        if (options[index].Data == null)
                            continue;

                        AllSlots.Add(new TransmogOutfitSlotAndOptionInfo());

                        var newSlot = AllSlots.Last();
                        newSlot.Slot = slotInfo.Data;
                        newSlot.SlotOption = options[index].Data;
                        newSlot.SlotIndex = (uint)AllSlots.Count - 1;
                        options[index].SlotIndex = AllSlots.Count - 1;
                    }
                }
                if (slotInfo.SlotIndex == 0)
                    slotInfo.SlotIndex = AllSlots.Count - 1;

            }

            foreach (TransmogSituationRecord transmogSituation in CliDB.TransmogSituationStorage.Values)
                if (transmogSituation.HasFlag(TransmogSituationFlags.DefaultsToOn))
                    DefaultSituations.Add(transmogSituation);
        }

        public ItemModifiedAppearanceRecord GetItemModifiedAppearance(uint itemId, uint appearanceModId)
        {
            var itr = ItemModifiedAppearancesByItem.LookupByKey((itemId, appearanceModId));
            if (itr != null)
                return itr;

            // Fall back to unmodified appearance
            if (appearanceModId != 0)
                return GetDefaultItemModifiedAppearance(itemId);

            return null;
        }

        public ItemModifiedAppearanceRecord GetDefaultItemModifiedAppearance(uint itemId)
        {
            return ItemModifiedAppearancesByItem.LookupByKey((itemId, 0u));
        }

        public TransmogIllusionRecord GetTransmogIllusionForSpellItemEnchantment(uint spellItemEnchantmentId)
        {
            return TransmogIllusionBySpellItemEnchantment.LookupByKey(spellItemEnchantmentId);
        }

        public List<TransmogSetRecord> GetTransmogSetsForItemModifiedAppearance(uint itemModifiedAppearanceId)
        {
            List<TransmogSetRecord> result = [];
            var itr = TransmogSetsByItemModifiedAppearance.LookupByKey(itemModifiedAppearanceId);
            if (itr != null)
                result = itr;

            return result;
        }

        public Span<TransmogSetItemRecord> GetTransmogSetItems(uint transmogSetId)
        {
            return TransmogSetItemsByTransmogSet.Where(p => p.TransmogSetID == transmogSetId).ToArray();
        }

        public List<TransmogOutfitEntryRecord> GetAutomaticallyUnlockedOutfits()
        {
            return TransmogOutfitsAutomaticallyCreated;
        }

        public List<TransmogOutfitSlotAndOptionInfo> GetAllSlots()
        {
            return AllSlots;
        }

        public TransmogOutfitSlotAndOptionInfo GetSlotAndOption(TransmogOutfitSlot slot, TransmogOutfitSlotOption slotOption)
        {
            int slotIndex = SlotInfoByOutfitSlot[(int)slot].GetSlotIndex(slotOption);
            if (slotIndex >= 0)
                return AllSlots[slotIndex];

            return null;
        }

        public TransmogOutfitSlotAndOptionInfo GetSlotAndOption(uint inventorySlot, TransmogOutfitSlotOption slotOption)
        {
            TransmogOutfitSlotInfo slotInfo = SlotInfoByInvSlot[inventorySlot];
            if (slotInfo != null)
            {
                int slotIndex = slotInfo.GetSlotIndex(slotOption);
                if (slotIndex >= 0)
                    return AllSlots[slotIndex];
            }

            return null;
        }

        public List<TransmogSituationRecord> GetDefaultSituations()
        {
            return DefaultSituations;
        }

        public TransmogOutfitEntryRecord GetNextOutfitToUnlock(TransmogOutfitEntrySource source, Player player)
        {
            if (source >= TransmogOutfitEntrySource.Max)
                return null;

            TransmogOutfitEntryRecord lastOwnedOutfit = null;
            foreach (var (id, transmogOutfit) in player.m_activePlayerData.TransmogOutfits)
            {
                TransmogOutfitEntryRecord transmogOutfitEntry = CliDB.TransmogOutfitEntryStorage.LookupByKey(transmogOutfit.value.Id);
                if (transmogOutfitEntry == null || transmogOutfitEntry.GetSource() != source)
                    continue;

                if (lastOwnedOutfit == null || transmogOutfitEntry.OrderIndex > lastOwnedOutfit.OrderIndex)
                    lastOwnedOutfit = transmogOutfitEntry;
            }

            if (lastOwnedOutfit == null)
                return TransmogOutfitsBySource[(int)source].First();

            var index = TransmogOutfitsBySource[(int)source].IndexOf(lastOwnedOutfit) + 1;
            if (index != -1)
                return TransmogOutfitsBySource[(int)source][index];

            return null;
        }

        public bool ValidateSituations(ICollection<Networking.Packets.TransmogOutfitSituationInfo> situations)
        {
            SituationTriggerStatus[] statusByTrigger = new SituationTriggerStatus[(int)TransmogSituationTrigger.Max];

            foreach (var situation in situations)
            {
                TransmogSituationRecord transmogSituation = CliDB.TransmogSituationStorage.LookupByKey(situation.SituationID);
                if (transmogSituation == null)
                    return false;

                TransmogSituationGroupRecord transmogSituationGroup = CliDB.TransmogSituationGroupStorage.LookupByKey(transmogSituation.TransmogSituationGroupID);
                if (transmogSituationGroup == null)
                    return false;

                TransmogSituationTriggerRecord transmogSituationTrigger = CliDB.TransmogSituationTriggerStorage.LookupByKey(transmogSituationGroup.TransmogSituationTriggerID);
                if (transmogSituationTrigger == null)
                    return false;

                SituationTriggerStatus triggers = statusByTrigger[(int)transmogSituationTrigger.GetTrigger()];
                byte count = 0;
                if (transmogSituation.HasFlag(TransmogSituationFlags.AllSituation))
                    count = triggers.AllSituationCount;
                else if (transmogSituation.HasFlag(TransmogSituationFlags.NoneSituation))
                    count = triggers.NoneSituationCount;
                else
                    count = triggers.RegularSituationCount;

                count += 1;
                if (transmogSituationTrigger.HasFlag(TransmogSituationTriggerFlags.SituationsAreExclusive) && count > 1)
                    return false;
            }

            foreach (SituationTriggerStatus triggers in statusByTrigger)
                if (((triggers.AllSituationCount > 0) ? 1 : 0) + ((triggers.NoneSituationCount > 0) ? 1 : 0) + ((triggers.RegularSituationCount > 0) ? 1 : 0) > 1) // only 1 group can be active
                    return false;

            return true;
        }

        public bool ValidateSlots(ICollection<Networking.Packets.TransmogOutfitSlotData> slots)
        {
            foreach (var slot in slots)
            {
                if (slot.Slot >= TransmogOutfitSlot.Max)
                    return false;

                if (slot.SlotOption >= TransmogOutfitSlotOption.Max)
                    return false;

                if (slot.SheatheCategory >= TransmogOutfitSlotOptionSheatheCategory.Max)
                    return false;

                if (slot.AppearanceDisplayType >= TransmogOutfitDisplayType.Max)
                    return false;

                if (slot.IllusionDisplayType >= TransmogOutfitDisplayType.Max)
                    return false;

                if (GetSlotAndOption((TransmogOutfitSlot)(sbyte)slot.Slot, (TransmogOutfitSlotOption)(byte)slot.SlotOption) == null)
                    return false;

                if (slot.ItemModifiedAppearanceID != 0)
                {
                    ItemModifiedAppearanceRecord itemModifiedAppearance = CliDB.ItemModifiedAppearanceStorage.LookupByKey(slot.ItemModifiedAppearanceID);
                    if (itemModifiedAppearance == null)
                        return false;

                    ItemTemplate itemTemplate = Global.ObjectMgr.GetItemTemplate(itemModifiedAppearance.ItemID);
                    if (itemTemplate == null)
                        return false;

                    if (!IsValidTransmogOutfitSlotForItem(itemTemplate, (TransmogOutfitSlot)(sbyte)slot.Slot, (TransmogOutfitSlotOption)(byte)slot.SlotOption))
                        return false;

                    TransmogOutfitSlotOption appearanceSlotOption = itemTemplate.GetWeaponTransmogOutfitSlotOption();
                    if (appearanceSlotOption != slot.SlotOption && (slot.SlotOption != TransmogOutfitSlotOption.FuryTwoHandedWeapon || appearanceSlotOption != TransmogOutfitSlotOption.TwoHandedWeapon))
                        return false;

                    if (slot.SheatheCategory != TransmogOutfitSlotOptionSheatheCategory.Default && (itemTemplate.GetSheatheType() >= ItemSheatheType.Max ||
                        TransmogSheatheMappingByCategoryAndSheatheType[(int)itemTemplate.GetSheatheType()][(int)slot.SheatheCategory] == ItemSheatheType.None))
                        return false;
                }

                if (slot.SpellItemEnchantmentID != 0 && !TransmogIllusionBySpellItemEnchantment.ContainsKey(slot.SpellItemEnchantmentID))
                    return false;
            }

            return true;
        }
    }

    public class TransmogOutfitSlotAndOptionInfo
    {
        public TransmogOutfitSlotInfoRecord Slot;
        public TransmogOutfitSlotOptionRecord SlotOption;
        public uint SlotIndex;
    }

    public struct TransmogOutfitSlotOptionInfo()
    {
        public TransmogOutfitSlotOptionRecord Data;
        public int SlotIndex = -1;
    }

    public class TransmogOutfitSlotInfo
    {
        public TransmogOutfitSlotInfoRecord Data;
        public int SlotIndex;
        public TransmogOutfitSlotOptionInfo[] Options;

        public int GetSlotIndex(TransmogOutfitSlotOption slotOption)
        {
            if (Options != null)
                return Options[(int)slotOption].SlotIndex;

            return SlotIndex;
        }
    }

    public struct SituationTriggerStatus
    {
        public byte AllSituationCount;
        public byte NoneSituationCount;
        public byte RegularSituationCount;
    }
}
