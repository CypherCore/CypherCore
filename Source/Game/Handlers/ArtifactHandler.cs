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
using Game.Networking;
using Game.Networking.Packets;
using System;
using System.Collections.Generic;

namespace Game
{
    public partial class WorldSession
    {
        [WorldPacketHandler(ClientOpcodes.ArtifactAddPower)]
        void HandleArtifactAddPower(ArtifactAddPower artifactAddPower)
        {
            if (!_player.GetGameObjectIfCanInteractWith(artifactAddPower.ForgeGUID, GameObjectTypes.ItemForge))
                return;

            Item artifact = _player.GetItemByGuid(artifactAddPower.ArtifactGUID);
            if (!artifact || artifact.IsArtifactDisabled())
                return;

            uint currentArtifactTier = artifact.GetModifier(ItemModifier.ArtifactTier);

            ulong xpCost = 0;
            GtArtifactLevelXPRecord cost = CliDB.ArtifactLevelXPGameTable.GetRow(artifact.GetTotalPurchasedArtifactPowers() + 1);
            if (cost != null)
                xpCost = (ulong)(currentArtifactTier == PlayerConst.MaxArtifactTier ? cost.XP2 : cost.XP);

            if (xpCost > artifact.m_itemData.ArtifactXP)
                return;

            if (artifactAddPower.PowerChoices.Empty())
                return;

            ArtifactPower artifactPower = artifact.GetArtifactPower(artifactAddPower.PowerChoices[0].ArtifactPowerID);
            if (artifactPower == null)
                return;

            ArtifactPowerRecord artifactPowerEntry = CliDB.ArtifactPowerStorage.LookupByKey(artifactPower.ArtifactPowerId);
            if (artifactPowerEntry == null)
                return;

            if (artifactPowerEntry.Tier > currentArtifactTier)
                return;

            uint maxRank = artifactPowerEntry.MaxPurchasableRank;
            if (artifactPowerEntry.Tier < currentArtifactTier)
            {
                if (artifactPowerEntry.Flags.HasAnyFlag(ArtifactPowerFlag.Final))
                    maxRank = 1;
                else if (artifactPowerEntry.Flags.HasAnyFlag(ArtifactPowerFlag.MaxRankWithTier))
                    maxRank += currentArtifactTier - artifactPowerEntry.Tier;
            }

            if (artifactAddPower.PowerChoices[0].Rank != artifactPower.PurchasedRank + 1 ||
                artifactAddPower.PowerChoices[0].Rank > maxRank)
                return;
            if (!artifactPowerEntry.Flags.HasAnyFlag(ArtifactPowerFlag.NoLinkRequired))
            {
                var artifactPowerLinks = Global.DB2Mgr.GetArtifactPowerLinks(artifactPower.ArtifactPowerId);
                if (artifactPowerLinks != null)
                {
                    bool hasAnyLink = false;
                    foreach (uint artifactPowerLinkId in artifactPowerLinks)
                    {
                        ArtifactPowerRecord artifactPowerLink = CliDB.ArtifactPowerStorage.LookupByKey(artifactPowerLinkId);
                        if (artifactPowerLink == null)
                            continue;

                        ArtifactPower artifactPowerLinkLearned = artifact.GetArtifactPower(artifactPowerLinkId);
                        if (artifactPowerLinkLearned == null)
                            continue;

                        if (artifactPowerLinkLearned.PurchasedRank >= artifactPowerLink.MaxPurchasableRank)
                        {
                            hasAnyLink = true;
                            break;
                        }
                    }

                    if (!hasAnyLink)
                        return;
                }
            }

            ArtifactPowerRankRecord artifactPowerRank = Global.DB2Mgr.GetArtifactPowerRank(artifactPower.ArtifactPowerId, (byte)(artifactPower.CurrentRankWithBonus + 1 - 1)); // need data for next rank, but -1 because of how db2 data is structured
            if (artifactPowerRank == null)
                return;

            artifact.SetArtifactPower(artifactPower.ArtifactPowerId, (byte)(artifactPower.PurchasedRank + 1), (byte)(artifactPower.CurrentRankWithBonus + 1));

            if (artifact.IsEquipped())
            {
                _player.ApplyArtifactPowerRank(artifact, artifactPowerRank, true);

                foreach (ArtifactPower power in artifact.m_itemData.ArtifactPowers)
                {
                    ArtifactPowerRecord scaledArtifactPowerEntry = CliDB.ArtifactPowerStorage.LookupByKey(power.ArtifactPowerId);
                    if (!scaledArtifactPowerEntry.Flags.HasAnyFlag(ArtifactPowerFlag.ScalesWithNumPowers))
                        continue;

                    ArtifactPowerRankRecord scaledArtifactPowerRank = Global.DB2Mgr.GetArtifactPowerRank(scaledArtifactPowerEntry.Id, 0);
                    if (scaledArtifactPowerRank == null)
                        continue;

                    artifact.SetArtifactPower(power.ArtifactPowerId, power.PurchasedRank, (byte)(power.CurrentRankWithBonus + 1));

                    _player.ApplyArtifactPowerRank(artifact, scaledArtifactPowerRank, false);
                    _player.ApplyArtifactPowerRank(artifact, scaledArtifactPowerRank, true);
                }
            }

            artifact.SetArtifactXP(artifact.m_itemData.ArtifactXP - xpCost);
            artifact.SetState(ItemUpdateState.Changed, _player);

            uint totalPurchasedArtifactPower = artifact.GetTotalPurchasedArtifactPowers();
            uint artifactTier = 0;

            foreach (ArtifactTierRecord tier in CliDB.ArtifactTierStorage.Values)
            {
                if (artifactPowerEntry.Flags.HasAnyFlag(ArtifactPowerFlag.Final) && artifactPowerEntry.Tier < PlayerConst.MaxArtifactTier)
                {
                    artifactTier = artifactPowerEntry.Tier + 1u;
                    break;
                }

                if (totalPurchasedArtifactPower < tier.MaxNumTraits)
                {
                    artifactTier = tier.ArtifactTier;
                    break;
                }
            }

            artifactTier = Math.Max(artifactTier, currentArtifactTier);

            for (uint i = currentArtifactTier; i <= artifactTier; ++i)
                artifact.InitArtifactPowers(artifact.GetTemplate().GetArtifactID(), (byte)i);

            artifact.SetModifier(ItemModifier.ArtifactTier, artifactTier);
        }

        [WorldPacketHandler(ClientOpcodes.ArtifactSetAppearance)]
        void HandleArtifactSetAppearance(ArtifactSetAppearance artifactSetAppearance)
        {
            if (!_player.GetGameObjectIfCanInteractWith(artifactSetAppearance.ForgeGUID, GameObjectTypes.ItemForge))
                return;

            ArtifactAppearanceRecord artifactAppearance = CliDB.ArtifactAppearanceStorage.LookupByKey(artifactSetAppearance.ArtifactAppearanceID);
            if (artifactAppearance == null)
                return;

            Item artifact = _player.GetItemByGuid(artifactSetAppearance.ArtifactGUID);
            if (!artifact)
                return;

            ArtifactAppearanceSetRecord artifactAppearanceSet = CliDB.ArtifactAppearanceSetStorage.LookupByKey(artifactAppearance.ArtifactAppearanceSetID);
            if (artifactAppearanceSet == null || artifactAppearanceSet.ArtifactID != artifact.GetTemplate().GetArtifactID())
                return;

            PlayerConditionRecord playerCondition = CliDB.PlayerConditionStorage.LookupByKey(artifactAppearance.UnlockPlayerConditionID);
            if (playerCondition != null)
                if (!ConditionManager.IsPlayerMeetingCondition(_player, playerCondition))
                    return;

            artifact.SetAppearanceModId(artifactAppearance.ItemAppearanceModifierID);
            artifact.SetModifier(ItemModifier.ArtifactAppearanceId, artifactAppearance.Id);
            artifact.SetState(ItemUpdateState.Changed, _player);
            Item childItem = _player.GetChildItemByGuid(artifact.GetChildItem());
            if (childItem)
            {
                childItem.SetAppearanceModId(artifactAppearance.ItemAppearanceModifierID);
                childItem.SetState(ItemUpdateState.Changed, _player);
            }

            if (artifact.IsEquipped())
            {
                // change weapon appearance
                _player.SetVisibleItemSlot(artifact.GetSlot(), artifact);
                if (childItem)
                    _player.SetVisibleItemSlot(childItem.GetSlot(), childItem);

                // change druid form appearance
                if (artifactAppearance.OverrideShapeshiftDisplayID != 0 && artifactAppearance.OverrideShapeshiftFormID != 0 && _player.GetShapeshiftForm() == (ShapeShiftForm)artifactAppearance.OverrideShapeshiftFormID)
                    _player.RestoreDisplayId(_player.IsMounted());
            }
        }

        [WorldPacketHandler(ClientOpcodes.ConfirmArtifactRespec)]
        void HandleConfirmArtifactRespec(ConfirmArtifactRespec confirmArtifactRespec)
        {
            if (!_player.GetNPCIfCanInteractWith(confirmArtifactRespec.NpcGUID, NPCFlags.ArtifactPowerRespec, NPCFlags2.None))
                return;

            Item artifact = _player.GetItemByGuid(confirmArtifactRespec.ArtifactGUID);
            if (!artifact || artifact.IsArtifactDisabled())
                return;

            ulong xpCost = 0;
            GtArtifactLevelXPRecord cost = CliDB.ArtifactLevelXPGameTable.GetRow(artifact.GetTotalPurchasedArtifactPowers() + 1);
            if (cost != null)
                xpCost = (ulong)(artifact.GetModifier(ItemModifier.ArtifactTier) == 1 ? cost.XP2 : cost.XP);

            if (xpCost > artifact.m_itemData.ArtifactXP)
                return;

            ulong newAmount = artifact.m_itemData.ArtifactXP - xpCost;
            for (uint i = 0; i <= artifact.GetTotalPurchasedArtifactPowers(); ++i)
            {
                GtArtifactLevelXPRecord cost1 = CliDB.ArtifactLevelXPGameTable.GetRow(i);
                if (cost1 != null)
                    newAmount += (ulong)(artifact.GetModifier(ItemModifier.ArtifactTier) == 1 ? cost1.XP2 : cost1.XP);
            }

            foreach (ArtifactPower artifactPower in artifact.m_itemData.ArtifactPowers)
            {
                byte oldPurchasedRank = artifactPower.PurchasedRank;
                if (oldPurchasedRank == 0)
                    continue;

                artifact.SetArtifactPower(artifactPower.ArtifactPowerId, (byte)(artifactPower.PurchasedRank - oldPurchasedRank), (byte)(artifactPower.CurrentRankWithBonus - oldPurchasedRank));

                if (artifact.IsEquipped())
                {
                    ArtifactPowerRankRecord artifactPowerRank = Global.DB2Mgr.GetArtifactPowerRank(artifactPower.ArtifactPowerId, 0);
                    if (artifactPowerRank != null)
                        _player.ApplyArtifactPowerRank(artifact, artifactPowerRank, false);
                }
            }

            foreach (ArtifactPower power in artifact.m_itemData.ArtifactPowers)
            {
                ArtifactPowerRecord scaledArtifactPowerEntry = CliDB.ArtifactPowerStorage.LookupByKey(power.ArtifactPowerId);
                if (!scaledArtifactPowerEntry.Flags.HasAnyFlag(ArtifactPowerFlag.ScalesWithNumPowers))
                    continue;

                ArtifactPowerRankRecord scaledArtifactPowerRank = Global.DB2Mgr.GetArtifactPowerRank(scaledArtifactPowerEntry.Id, 0);
                if (scaledArtifactPowerRank == null)
                    continue;

                artifact.SetArtifactPower(power.ArtifactPowerId, power.PurchasedRank, 0);

                _player.ApplyArtifactPowerRank(artifact, scaledArtifactPowerRank, false);
            }

            artifact.SetArtifactXP(newAmount);
            artifact.SetState(ItemUpdateState.Changed, _player);
        }
    }
}
