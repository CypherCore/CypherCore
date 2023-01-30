// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Framework.Constants;
using Framework.Database;
using Game.DataStorage;

namespace Game.Entities
{
    internal class ItemAdditionalLoadInfo
    {
        public ArtifactData Artifact { get; set; }
        public AzeriteEmpoweredData AzeriteEmpoweredItem { get; set; }
        public AzeriteData AzeriteItem { get; set; }

        public static void Init(Dictionary<ulong, ItemAdditionalLoadInfo> loadInfo, SQLResult artifactResult, SQLResult azeriteItemResult, SQLResult azeriteItemMilestonePowersResult,
                                SQLResult azeriteItemUnlockedEssencesResult, SQLResult azeriteEmpoweredItemResult)
        {
            ItemAdditionalLoadInfo GetOrCreateLoadInfo(ulong guid)
            {
                if (!loadInfo.ContainsKey(guid))
                    loadInfo[guid] = new ItemAdditionalLoadInfo();

                return loadInfo[guid];
            }

            if (!artifactResult.IsEmpty())
                do
                {
                    ItemAdditionalLoadInfo info = GetOrCreateLoadInfo(artifactResult.Read<ulong>(0));

                    if (info.Artifact == null)
                        info.Artifact = new ArtifactData();

                    info.Artifact.Xp = artifactResult.Read<ulong>(1);
                    info.Artifact.ArtifactAppearanceId = artifactResult.Read<uint>(2);
                    info.Artifact.ArtifactTierId = artifactResult.Read<uint>(3);

                    ArtifactPowerData artifactPowerData = new();
                    artifactPowerData.ArtifactPowerId = artifactResult.Read<uint>(4);
                    artifactPowerData.PurchasedRank = artifactResult.Read<byte>(5);

                    ArtifactPowerRecord artifactPower = CliDB.ArtifactPowerStorage.LookupByKey(artifactPowerData.ArtifactPowerId);

                    if (artifactPower != null)
                    {
                        uint maxRank = artifactPower.MaxPurchasableRank;

                        // allow ARTIFACT_POWER_FLAG_FINAL to overflow maxrank here - needs to be handled in Item::CheckArtifactUnlock (will refund artifact power)
                        if (artifactPower.Flags.HasAnyFlag(ArtifactPowerFlag.MaxRankWithTier) &&
                            artifactPower.Tier < info.Artifact.ArtifactTierId)
                            maxRank += info.Artifact.ArtifactTierId - artifactPower.Tier;

                        if (artifactPowerData.PurchasedRank > maxRank)
                            artifactPowerData.PurchasedRank = (byte)maxRank;

                        artifactPowerData.CurrentRankWithBonus = (byte)((artifactPower.Flags & ArtifactPowerFlag.First) == ArtifactPowerFlag.First ? 1 : 0);

                        info.Artifact.ArtifactPowers.Add(artifactPowerData);
                    }
                } while (artifactResult.NextRow());

            if (!azeriteItemResult.IsEmpty())
                do
                {
                    ItemAdditionalLoadInfo info = GetOrCreateLoadInfo(azeriteItemResult.Read<ulong>(0));

                    if (info.AzeriteItem == null)
                        info.AzeriteItem = new AzeriteData();

                    info.AzeriteItem.Xp = azeriteItemResult.Read<ulong>(1);
                    info.AzeriteItem.Level = azeriteItemResult.Read<uint>(2);
                    info.AzeriteItem.KnowledgeLevel = azeriteItemResult.Read<uint>(3);

                    for (int i = 0; i < info.AzeriteItem.SelectedAzeriteEssences.Length; ++i)
                    {
                        info.AzeriteItem.SelectedAzeriteEssences[i] = new AzeriteItemSelectedEssencesData();

                        uint specializationId = azeriteItemResult.Read<uint>(4 + i * 4);

                        if (!CliDB.ChrSpecializationStorage.ContainsKey(specializationId))
                            continue;

                        info.AzeriteItem.SelectedAzeriteEssences[i].SpecializationId = specializationId;

                        for (int j = 0; j < SharedConst.MaxAzeriteEssenceSlot; ++j)
                        {
                            AzeriteEssenceRecord azeriteEssence = CliDB.AzeriteEssenceStorage.LookupByKey(azeriteItemResult.Read<uint>(5 + i * 5 + j));

                            if (azeriteEssence == null ||
                                !Global.DB2Mgr.IsSpecSetMember(azeriteEssence.SpecSetID, specializationId))
                                continue;

                            info.AzeriteItem.SelectedAzeriteEssences[i].AzeriteEssenceId[j] = azeriteEssence.Id;
                        }
                    }
                } while (azeriteItemResult.NextRow());

            if (!azeriteItemMilestonePowersResult.IsEmpty())
                do
                {
                    ItemAdditionalLoadInfo info = GetOrCreateLoadInfo(azeriteItemMilestonePowersResult.Read<ulong>(0));

                    if (info.AzeriteItem == null)
                        info.AzeriteItem = new AzeriteData();

                    info.AzeriteItem.AzeriteItemMilestonePowers.Add(azeriteItemMilestonePowersResult.Read<uint>(1));
                } while (azeriteItemMilestonePowersResult.NextRow());

            if (!azeriteItemUnlockedEssencesResult.IsEmpty())
                do
                {
                    AzeriteEssencePowerRecord azeriteEssencePower = Global.DB2Mgr.GetAzeriteEssencePower(azeriteItemUnlockedEssencesResult.Read<uint>(1), azeriteItemUnlockedEssencesResult.Read<uint>(2));

                    if (azeriteEssencePower != null)
                    {
                        ItemAdditionalLoadInfo info = GetOrCreateLoadInfo(azeriteItemUnlockedEssencesResult.Read<ulong>(0));

                        if (info.AzeriteItem == null)
                            info.AzeriteItem = new AzeriteData();

                        info.AzeriteItem.UnlockedAzeriteEssences.Add(azeriteEssencePower);
                    }
                } while (azeriteItemUnlockedEssencesResult.NextRow());

            if (!azeriteEmpoweredItemResult.IsEmpty())
                do
                {
                    ItemAdditionalLoadInfo info = GetOrCreateLoadInfo(azeriteEmpoweredItemResult.Read<ulong>(0));

                    if (info.AzeriteEmpoweredItem == null)
                        info.AzeriteEmpoweredItem = new AzeriteEmpoweredData();

                    for (int i = 0; i < SharedConst.MaxAzeriteEmpoweredTier; ++i)
                        if (CliDB.AzeritePowerStorage.ContainsKey(azeriteEmpoweredItemResult.Read<int>(1 + i)))
                            info.AzeriteEmpoweredItem.SelectedAzeritePowers[i] = azeriteEmpoweredItemResult.Read<int>(1 + i);
                } while (azeriteEmpoweredItemResult.NextRow());
        }
    }
}