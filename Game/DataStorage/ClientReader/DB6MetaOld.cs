using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game.DataStorage
{
    public struct DB6Meta
    {
        public DB6Meta(int indexField, byte[] arraySizes, object[] fieldDefaults)
        {
            IndexField = indexField;
            ArraySizes = arraySizes;
            FieldDefaults = fieldDefaults;
        }

        bool HasIndexFieldInData()
        {
            return IndexField != -1;
        }

        uint GetIndexField()
        {
            return (uint)(IndexField == -1 ? 0 : IndexField);
        }

        uint GetDbIndexField()
        {
            if (IndexField == -1)
                return 0;

            uint index = 0;
            for (uint i = 0; i < ArraySizes.Length && i < IndexField; ++i)
                index += ArraySizes[i];

            return index;
        }

        uint GetDbFieldCount()
        {
            uint fields = 0;
            for (uint i = 0; i < ArraySizes.Length; ++i)
                fields += ArraySizes[i];

            if (!HasIndexFieldInData())
                ++fields;

            return fields;
        }

        int IndexField;
        public byte[] ArraySizes;
        public object[] FieldDefaults;
    }

    struct AchievementMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 };
            object[] fieldDefaults = { "", "", 0, "", 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            return new DB6Meta(13, arraySizes, fieldDefaults);        }
    }

    struct Achievement_CategoryMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1, 1 };
            object[] fieldDefaults = { "", 0, 0, 0 };
            return new DB6Meta(3, arraySizes, fieldDefaults);
        }
    }

    struct AdventureJournalMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 2, 1, 1, 1, 1, 1, 1, 1, 2, 1, 1, 1 };
            object[] fieldDefaults = { "", "", "", 0, 0, "", "", 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct AdventureMapPOIMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 2, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 };
            object[] fieldDefaults = { 0.0f, 0, "", "", 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct AnimKitMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1 };
            object[] fieldDefaults = { 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct AnimKitBoneSetMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1, 1, 1 };
            object[] fieldDefaults = { "", 0, 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct AnimKitBoneSetAliasMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1 };
            object[] fieldDefaults = { 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct AnimKitConfigMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1 };
            object[] fieldDefaults = { 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct AnimKitConfigBoneSetMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1 };
            object[] fieldDefaults = { 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct AnimKitPriorityMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1 };
            object[] fieldDefaults = { 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct AnimKitReplacementMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1, 1, 1 };
            object[] fieldDefaults = { 0, 0, 0, 0, 0 };
            return new DB6Meta(4, arraySizes, fieldDefaults);
        }
    }

    struct AnimKitSegmentMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 };
            object[] fieldDefaults = { 0, 0, 0, 0.0f, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct AnimReplacementMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1, 1, 1 };
            object[] fieldDefaults = { 0, 0, 0, 0, 0 };
            return new DB6Meta(4, arraySizes, fieldDefaults);
        }
    }

    struct AnimReplacementSetMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1 };
            object[] fieldDefaults = { 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct AnimationDataMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1, 1, 1, 1 };
            object[] fieldDefaults = { 0, "", 0, 0, 0, 0 };
            return new DB6Meta(0, arraySizes, fieldDefaults);
        }
    }

    struct AreaFarClipOverrideMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1, 1, 1 };
            object[] fieldDefaults = { 0, 0.0f, 0.0f, 0, 0 };
            return new DB6Meta(4, arraySizes, fieldDefaults);
        }
    }

    struct AreaGroupMemberMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1 };
            object[] fieldDefaults = { 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct AreaPOIMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 2, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 };
            object[] fieldDefaults = { 0, 0.0f, "", "", 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct AreaPOIStateMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1, 1, 1 };
            object[] fieldDefaults = { "", 0, 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct AreaTableMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 2, 1, 1, 1, 1, 1, 1, 1, 1, 1, 4, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 };
            object[] fieldDefaults = { 0, "", 0.0f, "", 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct AreaTriggerMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 3, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 };
            object[] fieldDefaults = { 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            return new DB6Meta(14, arraySizes, fieldDefaults);
        }
    }

    struct AreaTriggerActionSetMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1 };
            object[] fieldDefaults = { 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct AreaTriggerBoxMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 3 };
            object[] fieldDefaults = { 0.0f };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct AreaTriggerCylinderMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1 };
            object[] fieldDefaults = { 0.0f, 0.0f, 0.0f };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct AreaTriggerSphereMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1 };
            object[] fieldDefaults = { 0.0f };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct ArmorLocationMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1, 1, 1 };
            object[] fieldDefaults = { 0.0f, 0.0f, 0.0f, 0.0f, 0.0f };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct ArtifactMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 };
            object[] fieldDefaults = { "", 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct ArtifactAppearanceMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 };
            object[] fieldDefaults = { "", 0, 0.0f, 0.0f, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            return new DB6Meta(11, arraySizes, fieldDefaults);
        }
    }

    struct ArtifactAppearanceSetMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1, 1, 1, 1, 1, 1, 1 };
            object[] fieldDefaults = { "", "", 0, 0, 0, 0, 0, 0, 0 };
            return new DB6Meta(8, arraySizes, fieldDefaults);
        }
    }

    struct ArtifactCategoryMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1 };
            object[] fieldDefaults = { 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct ArtifactPowerMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 2, 1, 1, 1, 1, 1, 1 };
            object[] fieldDefaults = { 0.0f, 0, 0, 0, 0, 0, 0 };
            return new DB6Meta(5, arraySizes, fieldDefaults);
        }
    }

    struct ArtifactPowerLinkMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1 };
            object[] fieldDefaults = { 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct ArtifactPowerPickerMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1 };
            object[] fieldDefaults = { 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct ArtifactPowerRankMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1, 1, 1 };
            object[] fieldDefaults = { 0, 0.0f, 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct ArtifactQuestXPMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 10 };
            object[] fieldDefaults = { 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct ArtifactTierMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1, 1 };
            object[] fieldDefaults = { 0, 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct ArtifactUnlockMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1, 1, 1 };
            object[] fieldDefaults = { 0, 0, 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct AuctionHouseMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1, 1 };
            object[] fieldDefaults = { "", 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct BankBagSlotPricesMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1 };
            object[] fieldDefaults = { 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct BannedAddOnsMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1 };
            object[] fieldDefaults = { "", "", 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct BarberShopStyleMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1, 1, 1, 1, 1, 1 };
            object[] fieldDefaults = { "", "", 0.0f, 0, 0, 0, 0, 0 };
            return new DB6Meta(7, arraySizes, fieldDefaults);
        }
    }

    struct BattlePetAbilityMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1, 1, 1, 1, 1 };
            object[] fieldDefaults = { 0, "", "", 0, 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct BattlePetAbilityEffectMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1, 1, 6, 1, 1 };
            object[] fieldDefaults = { 0, 0, 0, 0, 0, 0, 0 };
            return new DB6Meta(6, arraySizes, fieldDefaults);
        }
    }

    struct BattlePetAbilityStateMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1 };
            object[] fieldDefaults = { 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct BattlePetAbilityTurnMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1, 1, 1, 1 };
            object[] fieldDefaults = { 0, 0, 0, 0, 0, 0 };
            return new DB6Meta(5, arraySizes, fieldDefaults);
        }
    }

    struct BattlePetBreedQualityMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1 };
            object[] fieldDefaults = { 0.0f, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct BattlePetBreedStateMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1 };
            object[] fieldDefaults = { 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct BattlePetEffectPropertiesMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 6, 1, 6 };
            object[] fieldDefaults = { "", 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct BattlePetNPCTeamMemberMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1 };
            object[] fieldDefaults = { "" };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct BattlePetSpeciesMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 };
            object[] fieldDefaults = { 0, 0, 0, "", "", 0, 0, 0, 0, 0, 0 };
            return new DB6Meta(8, arraySizes, fieldDefaults);
        }
    }

    struct BattlePetSpeciesStateMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1 };
            object[] fieldDefaults = { 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct BattlePetSpeciesXAbilityMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1, 1 };
            object[] fieldDefaults = { 0, 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct BattlePetStateMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1 };
            object[] fieldDefaults = { "", 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct BattlePetVisualMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1, 1, 1, 1, 1 };
            object[] fieldDefaults = { 0, "", 0, 0, 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct BattlemasterListMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1, 1, 1, 16, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 };
            object[] fieldDefaults = { "", 0, "", "", "", 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct BeamEffectMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 };
            object[] fieldDefaults = { 0, 0.0f, 0.0f, 0, 0, 0, 0, 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct BoneWindModifierModelMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1 };
            object[] fieldDefaults = { 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct BoneWindModifiersMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 3, 1 };
            object[] fieldDefaults = { 0.0f, 0.0f };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct BountyMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1, 1 };
            object[] fieldDefaults = { 0, 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct BountySetMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1 };
            object[] fieldDefaults = { 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct BroadcastTextMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 3, 3, 1, 1, 1, 2, 1 };
            object[] fieldDefaults = { "", "", 0, 0, 0, 0, 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct CameraEffectMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1 };
            object[] fieldDefaults = { 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct CameraEffectEntryMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 };
            object[] fieldDefaults = { 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0, 0, 0, 0, 0, 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct CameraModeMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 3, 3, 1, 1, 1, 1, 1, 1, 1, 1, 1 };
            object[] fieldDefaults = { 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0, 0, 0, 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct CastableRaidBuffsMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1 };
            object[] fieldDefaults = { 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct CelestialBodyMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 2, 1, 1, 2, 2, 2, 1, 2, 1, 3, 1, 1, 1 };
            object[] fieldDefaults = { 0, 0, 0, 0, 0, 0, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0, 0 };
            return new DB6Meta(14, arraySizes, fieldDefaults);
        }
    }

    struct Cfg_CategoriesMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1, 1, 1 };
            object[] fieldDefaults = { "", 0, 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct Cfg_ConfigsMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1, 1 };
            object[] fieldDefaults = { 0.0f, 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct Cfg_RegionsMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1, 1, 1 };
            object[] fieldDefaults = { "", 0, 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct CharBaseInfoMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1 };
            object[] fieldDefaults = { 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct CharBaseSectionMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1 };
            object[] fieldDefaults = { 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct CharComponentTextureLayoutsMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1 };
            object[] fieldDefaults = { 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct CharComponentTextureSectionsMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1, 1, 1, 1, 1 };
            object[] fieldDefaults = { 0, 0, 0, 0, 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct CharHairGeosetsMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 };
            object[] fieldDefaults = { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct CharSectionsMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 3, 1, 1, 1, 1, 1, 1 };
            object[] fieldDefaults = { 0, 0, 0, 0, 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct CharShipmentMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1, 1, 1, 1, 1, 1, 1 };
            object[] fieldDefaults = { 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct CharShipmentContainerMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 };
            object[] fieldDefaults = { "", "", 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct CharStartOutfitMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 24, 1, 1, 1, 1, 1, 1 };
            object[] fieldDefaults = { 0, 0, 0, 0, 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct CharTitlesMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1, 1 };
            object[] fieldDefaults = { "", "", 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct CharacterFaceBoneSetMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1, 1 };
            object[] fieldDefaults = { 0, 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct CharacterFacialHairStylesMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 5, 1, 1, 1 };
            object[] fieldDefaults = { 0, 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct CharacterLoadoutMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1 };
            object[] fieldDefaults = { 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct CharacterLoadoutItemMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1 };
            object[] fieldDefaults = { 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct ChatChannelsMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1, 1 };
            object[] fieldDefaults = { 0, "", "", 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct ChatProfanityMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1 };
            object[] fieldDefaults = { "", 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct ChrClassRaceSexMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1, 1, 1, 1 };
            object[] fieldDefaults = { 0, 0, 0, 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct ChrClassTitleMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1 };
            object[] fieldDefaults = { "", "", 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct ChrClassUIDisplayMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1 };
            object[] fieldDefaults = { 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct ChrClassVillainMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1 };
            object[] fieldDefaults = { "", 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct ChrClassesMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 };
            object[] fieldDefaults = { "", "", "", "", "", 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            return new DB6Meta(18, arraySizes, fieldDefaults);
        }
    }

    struct ChrClassesXPowerTypesMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1 };
            object[] fieldDefaults = { 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct ChrRacesMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1, 1, 1, 1, 2, 1, 1, 1, 3, 3, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 3 };
            object[] fieldDefaults = { 0, "", "", "", "", "", "", "", 0, 0, 0.0f, 0.0f, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct ChrSpecializationMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 2, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 };
            object[] fieldDefaults = { 0, "", "", "", 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            return new DB6Meta(9, arraySizes, fieldDefaults);
        }
    }

    struct ChrUpgradeBucketMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1 };
            object[] fieldDefaults = { 0, 0, 0 };
            return new DB6Meta(2, arraySizes, fieldDefaults);
        }
    }

    struct ChrUpgradeBucketSpellMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1 };
            object[] fieldDefaults = { 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct ChrUpgradeTierMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1, 1 };
            object[] fieldDefaults = { "", 0, 0, 0 };
            return new DB6Meta(3, arraySizes, fieldDefaults);
        }
    }

    struct CinematicCameraMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 3, 1, 1 };
            object[] fieldDefaults = { 0, 0.0f, 0.0f, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct CinematicSequencesMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 8 };
            object[] fieldDefaults = { 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct CloakDampeningMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 5, 5, 2, 2, 1, 1, 1 };
            object[] fieldDefaults = { 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct CombatConditionMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1, 2, 2, 2, 2, 1, 2, 2, 1 };
            object[] fieldDefaults = { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct ComponentModelFileDataMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1, 1 };
            object[] fieldDefaults = { 0, 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct ComponentTextureFileDataMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1 };
            object[] fieldDefaults = { 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct ContributionMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1, 1, 4, 1 };
            object[] fieldDefaults = { 0, 0, "", "", 0, 0 };
            return new DB6Meta(0, arraySizes, fieldDefaults);
        }
    }

    struct ConversationLineMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1, 1, 1, 1, 1, 1 };
            object[] fieldDefaults = { 0, 0, 0, 0, 0, 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct CreatureMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 3, 1, 4, 4, 1, 1, 1, 1, 1, 1, 1, 1 };
            object[] fieldDefaults = { 0, 0, 0, 0.0f, "", "", "", "", 0, 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct CreatureDifficultyMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 7, 1, 1, 1, 1 };
            object[] fieldDefaults = { 0, 0, 0, 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct CreatureDispXUiCameraMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1 };
            object[] fieldDefaults = { 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct CreatureDisplayInfoMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1, 1, 1, 1, 1, 1, 3, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 };
            object[] fieldDefaults = { 0, 0.0f, 0, 0, 0, 0, 0, 0, 0, 0, 255, 0, 0.0f, 0, 0, 0, 0, 0, 0, 255, 0, 1.0f, 0 };
            return new DB6Meta(0, arraySizes, fieldDefaults);
        }
    }

    struct CreatureDisplayInfoCondMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 2, 2, 2, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 3 };
            object[] fieldDefaults = { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct CreatureDisplayInfoEvtMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1 };
            object[] fieldDefaults = { 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct CreatureDisplayInfoExtraMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 3, 1 };
            object[] fieldDefaults = { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct CreatureDisplayInfoTrnMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1, 1 };
            object[] fieldDefaults = { 0, 0, 0, 0.0f };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct CreatureFamilyMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1, 1, 2, 1, 1, 1, 1 };
            object[] fieldDefaults = { 0.0f, 0.0f, "", 0, 0, 0, 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct CreatureImmunitiesMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 2, 1, 1, 1, 1, 1, 1, 8, 16 };
            object[] fieldDefaults = { 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct CreatureModelDataMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1, 1, 1, 1, 1, 6, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 };
            object[] fieldDefaults = { 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct CreatureMovementInfoMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1 };
            object[] fieldDefaults = { 0.0f };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct CreatureSoundDataMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 5, 4, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 };
            object[] fieldDefaults = { 0.0f, 0.0f, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct CreatureTypeMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1 };
            object[] fieldDefaults = { "", 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct CreatureXContributionMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1 };
            object[] fieldDefaults = { 0, 0, 0 };
            return new DB6Meta(0, arraySizes, fieldDefaults);
        }
    }

    struct CriteriaMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 };
            object[] fieldDefaults = { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct CriteriaTreeMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1, 1, 1, 1, 1 };
            object[] fieldDefaults = { 0, "", 0, 0, 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct CriteriaTreeXEffectMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1 };
            object[] fieldDefaults = { 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct CurrencyCategoryMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1 };
            object[] fieldDefaults = { "", 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct CurrencyTypesMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 2, 1, 1, 1, 1, 1, 1, 1, 1 };
            object[] fieldDefaults = { "", "", 0, 0, 0, "", 0, 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct CurveMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1 };
            object[] fieldDefaults = { 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct CurvePointMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 2, 1, 1 };
            object[] fieldDefaults = { 0.0f, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct DeathThudLookupsMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1, 1 };
            object[] fieldDefaults = { 0, 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct DecalPropertiesMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 };
            object[] fieldDefaults = { 0, 0, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0, 0, 0, 0, 0, 0, 0 };
            return new DB6Meta(0, arraySizes, fieldDefaults);
        }
    }

    struct DeclinedWordMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1 };
            object[] fieldDefaults = { "", 0 };
            return new DB6Meta(1, arraySizes, fieldDefaults);
        }
    }

    struct DeclinedWordCasesMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1 };
            object[] fieldDefaults = { "", 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct DestructibleModelDataMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 };
            object[] fieldDefaults = { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct DeviceBlacklistMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1 };
            object[] fieldDefaults = { 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct DeviceDefaultSettingsMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1 };
            object[] fieldDefaults = { 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct DifficultyMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 };
            object[] fieldDefaults = { "", 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct DissolveEffectMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 };
            object[] fieldDefaults = { 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0, 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct DriverBlacklistMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1, 1, 1, 1, 1 };
            object[] fieldDefaults = { 0, 0, 0, 0, 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct DungeonEncounterMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1, 1, 1, 1, 1, 1, 1 };
            object[] fieldDefaults = { "", 0, 0, 0, 0, 0, 0, 0, 0 };
            return new DB6Meta(6, arraySizes, fieldDefaults);
        }
    }

    struct DungeonMapMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 2, 2, 1, 1, 1, 1, 1, 1 };
            object[] fieldDefaults = { 0.0f, 0.0f, 0, 0, 0, 0, 0, 0 };
            return new DB6Meta(7, arraySizes, fieldDefaults);
        }
    }

    struct DungeonMapChunkMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1, 1, 1 };
            object[] fieldDefaults = { 0.0f, 0, 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct DurabilityCostsMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 21, 8 };
            object[] fieldDefaults = { 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct DurabilityQualityMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1 };
            object[] fieldDefaults = { 0.0f };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct EdgeGlowEffectMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 };
            object[] fieldDefaults = { 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct EmotesMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1, 1, 1, 1, 1, 1, 1 };
            object[] fieldDefaults = { "", 0, 0, 0, 0, 0, 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct EmotesTextMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1 };
            object[] fieldDefaults = { "", 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct EmotesTextDataMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1 };
            object[] fieldDefaults = { "", 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct EmotesTextSoundMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1, 1, 1 };
            object[] fieldDefaults = { 0, 0, 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct EnvironmentalDamageMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1 };
            object[] fieldDefaults = { 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct ExhaustionMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1, 1, 1, 1, 1, 1 };
            object[] fieldDefaults = { 0, 0.0f, 0.0f, 0.0f, "", 0.0f, "", 0 };
            return new DB6Meta(7, arraySizes, fieldDefaults);
        }
    }

    struct FactionMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 4, 4, 2, 1, 1, 4, 1, 4, 4, 1, 1, 2, 1, 1, 1 };
            object[] fieldDefaults = { 0, 0, 0, 0.0f, "", "", 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            return new DB6Meta(0, arraySizes, fieldDefaults);
        }
    }

    struct FactionGroupMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1 };
            object[] fieldDefaults = { "", "", 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct FactionTemplateMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 4, 4, 1, 1, 1 };
            object[] fieldDefaults = { 0, 0, 0, 0, 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct FootprintTexturesMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1 };
            object[] fieldDefaults = { 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct FootstepTerrainLookupMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1, 1 };
            object[] fieldDefaults = { 0, 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct FriendshipRepReactionMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1 };
            object[] fieldDefaults = { "", 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct FriendshipReputationMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1, 1 };
            object[] fieldDefaults = { 0, "", 0, 0 };
            return new DB6Meta(3, arraySizes, fieldDefaults);
        }
    }

    struct FullScreenEffectMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 };
            object[] fieldDefaults = { 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0, 0, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0, 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct GMSurveyAnswersMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1 };
            object[] fieldDefaults = { "", 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct GMSurveyCurrentSurveyMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1 };
            object[] fieldDefaults = { 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct GMSurveyQuestionsMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1 };
            object[] fieldDefaults = { "" };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct GMSurveySurveysMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 15 };
            object[] fieldDefaults = { 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct GameObjectArtKitMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 3, 4 };
            object[] fieldDefaults = { "", "" };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct GameObjectDiffAnimMapMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1, 1 };
            object[] fieldDefaults = { 0, 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct GameObjectDisplayInfoMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 6, 1, 1, 1 };
            object[] fieldDefaults = { 0, 0.0f, 0.0f, 0.0f, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct GameObjectDisplayInfoXSoundKitMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1 };
            object[] fieldDefaults = { 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct GameObjectsMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 3, 4, 1, 8, 1, 1, 1, 1, 1, 1, 1, 1 };
            object[] fieldDefaults = { 0.0f, 0.0f, 0.0f, 0, "", 0, 0, 0, 0, 0, 0, 0 };
            return new DB6Meta(11, arraySizes, fieldDefaults);
        }
    }

    struct GameTipsMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1, 1 };
            object[] fieldDefaults = { "", 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct GarrAbilityMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1, 1, 1, 1, 1, 1 };
            object[] fieldDefaults = { "", "", 0, 0, 0, 0, 0, 0 };
            return new DB6Meta(7, arraySizes, fieldDefaults);
        }
    }

    struct GarrAbilityCategoryMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1 };
            object[] fieldDefaults = { "" };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct GarrAbilityEffectMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 };
            object[] fieldDefaults = { 0.0f, 0.0f, 0.0f, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            return new DB6Meta(11, arraySizes, fieldDefaults);
        }
    }

    struct GarrBuildingMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 };
            object[] fieldDefaults = { 0, 0, "", "", "", "", 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct GarrBuildingDoodadSetMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1, 1, 1 };
            object[] fieldDefaults = { 0, 0, 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct GarrBuildingPlotInstMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 2, 1, 1, 1, 1 };
            object[] fieldDefaults = { 0.0f, 0, 0, 0, 0 };
            return new DB6Meta(4, arraySizes, fieldDefaults);
        }
    }

    struct GarrClassSpecMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1, 1, 1, 1, 1, 1 };
            object[] fieldDefaults = { "", "", "", 0, 0, 0, 0, 0 };
            return new DB6Meta(7, arraySizes, fieldDefaults);
        }
    }

    struct GarrClassSpecPlayerCondMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1, 1, 1, 1 };
            object[] fieldDefaults = { 0, "", 0, 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct GarrEncounterMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1, 1, 1, 1, 1 };
            object[] fieldDefaults = { 0, "", 0.0f, 0.0f, 0, 0, 0 };
            return new DB6Meta(5, arraySizes, fieldDefaults);
        }
    }

    struct GarrEncounterSetXEncounterMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1 };
            object[] fieldDefaults = { 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct GarrEncounterXMechanicMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1 };
            object[] fieldDefaults = { 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct GarrFollItemSetMemberMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1, 1 };
            object[] fieldDefaults = { 0, 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct GarrFollSupportSpellMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1, 1 };
            object[] fieldDefaults = { 0, 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct GarrFollowerMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 };
            object[] fieldDefaults = { 0, 0, "", "", 0, 0, 0, 0, "", 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            return new DB6Meta(31, arraySizes, fieldDefaults);
        }
    }

    struct GarrFollowerLevelXPMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1, 1 };
            object[] fieldDefaults = { 0, 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct GarrFollowerQualityMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1, 1, 1, 1, 1 };
            object[] fieldDefaults = { 0, 0, 0, 0, 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct GarrFollowerSetXFollowerMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1 };
            object[] fieldDefaults = { 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct GarrFollowerTypeMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1, 1, 1, 1, 1 };
            object[] fieldDefaults = { 0, 0, 0, 0, 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct GarrFollowerUICreatureMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1, 1, 1, 1 };
            object[] fieldDefaults = { 0, 0.0f, 0, 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct GarrFollowerXAbilityMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1 };
            object[] fieldDefaults = { 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct GarrItemLevelUpgradeDataMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1, 1, 1 };
            object[] fieldDefaults = { 0, 0, 0, 0, 0 };
            return new DB6Meta(0, arraySizes, fieldDefaults);
        }
    }

    struct GarrMechanicMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1 };
            object[] fieldDefaults = { 0.0f, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct GarrMechanicSetXMechanicMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1 };
            object[] fieldDefaults = { 0, 0, 0 };
            return new DB6Meta(1, arraySizes, fieldDefaults);
        }
    }

    struct GarrMechanicTypeMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1, 1, 1 };
            object[] fieldDefaults = { "", "", 0, 0, 0 };
            return new DB6Meta(4, arraySizes, fieldDefaults);
        }
    }

    struct GarrMissionMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1, 1, 1, 2, 2, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 };
            object[] fieldDefaults = { 0, 0, "", "", "", 0.0f, 0.0f, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            return new DB6Meta(19, arraySizes, fieldDefaults);
        }
    }

    struct GarrMissionTextureMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 2, 1 };
            object[] fieldDefaults = { 0.0f, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct GarrMissionTypeMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1 };
            object[] fieldDefaults = { "", 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct GarrMissionXEncounterMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1, 1, 1 };
            object[] fieldDefaults = { 0, 0, 0, 0, 0 };
            return new DB6Meta(1, arraySizes, fieldDefaults);
        }
    }

    struct GarrMissionXFollowerMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1 };
            object[] fieldDefaults = { 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct GarrMssnBonusAbilityMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1, 1, 1 };
            object[] fieldDefaults = { 0.0f, 0, 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct GarrPlotMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1, 1, 1, 1, 2 };
            object[] fieldDefaults = { "", 0, 0, 0, 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct GarrPlotBuildingMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1 };
            object[] fieldDefaults = { 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct GarrPlotInstanceMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1 };
            object[] fieldDefaults = { "", 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct GarrPlotUICategoryMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1 };
            object[] fieldDefaults = { "", 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct GarrSiteLevelMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 2, 1, 1, 1, 1, 1, 1, 1, 1 };
            object[] fieldDefaults = { 0.0f, 0, 0, 0, 0, 0, 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct GarrSiteLevelPlotInstMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 2, 1, 1, 1 };
            object[] fieldDefaults = { 0.0f, 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct GarrSpecializationMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 2, 1, 1, 1, 1, 1 };
            object[] fieldDefaults = { 0, 0.0f, "", "", 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct GarrStringMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1 };
            object[] fieldDefaults = { "" };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct GarrTalentMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 };
            object[] fieldDefaults = { 0, "", "", 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            return new DB6Meta(7, arraySizes, fieldDefaults);
        }
    }

    struct GarrTalentTreeMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1, 1 };
            object[] fieldDefaults = { 0, 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct GarrTypeMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1, 1, 1 };
            object[] fieldDefaults = { 0, 0, 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct GarrUiAnimClassInfoMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1, 1 };
            object[] fieldDefaults = { 0, 0.0f, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct GarrUiAnimRaceInfoMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 };
            object[] fieldDefaults = { 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct GemPropertiesMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1 };
            object[] fieldDefaults = { 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct GlobalStringsMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1 };
            object[] fieldDefaults = { "", "", 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct GlyphBindableSpellMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1 };
            object[] fieldDefaults = { 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct GlyphExclusiveCategoryMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1 };
            object[] fieldDefaults = { "" };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct GlyphPropertiesMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1, 1 };
            object[] fieldDefaults = { 0, 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct GlyphRequiredSpecMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1 };
            object[] fieldDefaults = { 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct GroundEffectDoodadMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1, 1 };
            object[] fieldDefaults = { "", 0.0f, 0.0f, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct GroundEffectTextureMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 4, 4, 1, 1 };
            object[] fieldDefaults = { 0, 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct GroupFinderActivityMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 };
            object[] fieldDefaults = { "", "", 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct GroupFinderActivityGrpMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1 };
            object[] fieldDefaults = { "", 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct GroupFinderCategoryMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1 };
            object[] fieldDefaults = { "", 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct GuildColorBackgroundMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1 };
            object[] fieldDefaults = { 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct GuildColorBorderMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1 };
            object[] fieldDefaults = { 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct GuildColorEmblemMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1 };
            object[] fieldDefaults = { 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct GuildPerkSpellsMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1 };
            object[] fieldDefaults = { 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct HeirloomMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1, 1, 1, 2, 2, 1, 1, 1 };
            object[] fieldDefaults = { 0, "", 0, 0, 0, 0, 0, 0, 0, 0 };
            return new DB6Meta(9, arraySizes, fieldDefaults);
        }
    }

    struct HelmetAnimScalingMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1 };
            object[] fieldDefaults = { 0.0f, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct HelmetGeosetVisDataMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 9 };
            object[] fieldDefaults = { 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct HighlightColorMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1, 1, 1 };
            object[] fieldDefaults = { 0, 0, 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct HolidayDescriptionsMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1 };
            object[] fieldDefaults = { "" };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct HolidayNamesMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1 };
            object[] fieldDefaults = { "" };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct HolidaysMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 16, 1, 10, 1, 1, 10, 1, 1, 1, 1, 1 };
            object[] fieldDefaults = { 0, 0, "", 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            return new DB6Meta(0, arraySizes, fieldDefaults);
        }
    }

    struct HotfixMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1 };
            object[] fieldDefaults = { "", 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct ImportPriceArmorMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1, 1 };
            object[] fieldDefaults = { 0.0f, 0.0f, 0.0f, 0.0f };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct ImportPriceQualityMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1 };
            object[] fieldDefaults = { 0.0f };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct ImportPriceShieldMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1 };
            object[] fieldDefaults = { 0.0f };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct ImportPriceWeaponMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1 };
            object[] fieldDefaults = { 0.0f };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct InvasionClientDataMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 2, 1, 1, 1, 1, 1, 1, 1, 1 };
            object[] fieldDefaults = { "", 0.0f, 0, 0, 0, 0, 0, 0, 0, 0 };
            return new DB6Meta(2, arraySizes, fieldDefaults);
        }
    }

    struct ItemMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1, 1, 1, 1, 1, 1 };
            object[] fieldDefaults = { 0, 0, 0, 0, 0, 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct ItemAppearanceMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1, 1 };
            object[] fieldDefaults = { 0, 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct ItemAppearanceXUiCameraMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1 };
            object[] fieldDefaults = { 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct ItemArmorQualityMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 7, 1 };
            object[] fieldDefaults = { 0.0f, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct ItemArmorShieldMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 7, 1 };
            object[] fieldDefaults = { 0.0f, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct ItemArmorTotalMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1, 1, 1 };
            object[] fieldDefaults = { 0.0f, 0.0f, 0.0f, 0.0f, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct ItemBagFamilyMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1 };
            object[] fieldDefaults = { "" };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct ItemBonusMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 2, 1, 1, 1 };
            object[] fieldDefaults = { 0, 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct ItemBonusListLevelDeltaMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1 };
            object[] fieldDefaults = { 0, 0 };
            return new DB6Meta(1, arraySizes, fieldDefaults);
        }
    }

    struct ItemBonusTreeNodeMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1, 1, 1 };
            object[] fieldDefaults = { 0, 0, 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct ItemChildEquipmentMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1 };
            object[] fieldDefaults = { 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct ItemClassMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1, 1 };
            object[] fieldDefaults = { 0.0f, "", 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct ItemContextPickerEntryMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1, 1, 1 };
            object[] fieldDefaults = { 0, 0, 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct ItemCurrencyCostMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1 };
            object[] fieldDefaults = { 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct ItemDamageAmmoMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 7, 1 };
            object[] fieldDefaults = { 0.0f, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct ItemDamageOneHandMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 7, 1 };
            object[] fieldDefaults = { 0.0f, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct ItemDamageOneHandCasterMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 7, 1 };
            object[] fieldDefaults = { 0.0f, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct ItemDamageTwoHandMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 7, 1 };
            object[] fieldDefaults = { 0.0f, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct ItemDamageTwoHandCasterMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 7, 1 };
            object[] fieldDefaults = { 0.0f, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct ItemDisenchantLootMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1, 1, 1, 1 };
            object[] fieldDefaults = { 0, 0, 0, 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct ItemDisplayInfoMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 2, 2, 3, 3, 1, 1, 2, 1, 1, 1, 1, 1, 1, 1, 1 };
            object[] fieldDefaults = { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct ItemDisplayInfoMaterialResMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1 };
            object[] fieldDefaults = { 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct ItemDisplayXUiCameraMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1 };
            object[] fieldDefaults = { 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct ItemEffectMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1, 1, 1, 1, 1, 1, 1 };
            object[] fieldDefaults = { 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct ItemExtendedCostMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 5, 5, 5, 1, 5, 1, 1, 1, 1, 1 };
            object[] fieldDefaults = { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct ItemGroupSoundsMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 4 };
            object[] fieldDefaults = { 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct ItemLevelSelectorMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1 };
            object[] fieldDefaults = { 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct ItemLimitCategoryMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1 };
            object[] fieldDefaults = { "", 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct ItemLimitCategoryConditionMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1 };
            object[] fieldDefaults = { 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct ItemModifiedAppearanceMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1, 1, 1, 1 };
            object[] fieldDefaults = { 0, 0, 0, 0, 0, 0 };
            return new DB6Meta(5, arraySizes, fieldDefaults);
        }
    }

    struct ItemModifiedAppearanceExtraMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1, 1, 1 };
            object[] fieldDefaults = { 0, 0, 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct ItemNameDescriptionMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1 };
            object[] fieldDefaults = { "", 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct ItemPetFoodMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1 };
            object[] fieldDefaults = { "" };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct ItemPriceBaseMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1 };
            object[] fieldDefaults = { 0.0f, 0.0f, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct ItemRandomPropertiesMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 5 };
            object[] fieldDefaults = { "", 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct ItemRandomSuffixMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 5, 5 };
            object[] fieldDefaults = { "", 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct ItemRangedDisplayInfoMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1, 1 };
            object[] fieldDefaults = { 0, 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct ItemSearchNameMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 3, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 };
            object[] fieldDefaults = { "", 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct ItemSetMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 17, 1, 1, 1 };
            object[] fieldDefaults = { "", 0, 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct ItemSetSpellMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1, 1 };
            object[] fieldDefaults = { 0, 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct ItemSparseMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 3, 1, 1, 1, 1, 1, 1, 1, 1, 1, 10, 10, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 10, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 10, 1, 1, 1, 1, 1, 1, 3, 1, 1, 1, 1 };
            object[] fieldDefaults = { 0, 0.0f, 0.0f, 0, 0, 0, 0, 0, 0, 0, 0, 0.0f, 0.0f, "", "", "", "", "", 0, 0.0f, 0, 0.0f, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct ItemSpecMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1, 1, 1, 1 };
            object[] fieldDefaults = { 0, 0, 0, 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct ItemSpecOverrideMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1 };
            object[] fieldDefaults = { 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct ItemSubClassMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 };
            object[] fieldDefaults = { "", "", 0, 0, 0, 0, 0, 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct ItemSubClassMaskMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1 };
            object[] fieldDefaults = { 0, "", 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct ItemUpgradeMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1, 1, 1 };
            object[] fieldDefaults = { 0, 0, 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct ItemVisualEffectsMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1 };
            object[] fieldDefaults = { "" };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct ItemVisualsMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 5 };
            object[] fieldDefaults = { 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct ItemXBonusTreeMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1 };
            object[] fieldDefaults = { 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct JournalEncounterMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 2, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 };
            object[] fieldDefaults = { 0.0f, "", "", 0, 0, 0, 0, 0, 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct JournalEncounterCreatureMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1, 1, 1, 1, 1 };
            object[] fieldDefaults = { 0, 0, "", "", 0, 0, 0 };
            return new DB6Meta(6, arraySizes, fieldDefaults);
        }
    }

    struct JournalEncounterItemMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1, 1, 1, 1 };
            object[] fieldDefaults = { 0, 0, 0, 0, 0, 0 };
            return new DB6Meta(5, arraySizes, fieldDefaults);
        }
    }

    struct JournalEncounterSectionMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 };
            object[] fieldDefaults = { "", "", 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct JournalEncounterXDifficultyMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1 };
            object[] fieldDefaults = { 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct JournalInstanceMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 };
            object[] fieldDefaults = { 0, 0, 0, 0, "", "", 0, 0, 0, 0, 0 };
            return new DB6Meta(10, arraySizes, fieldDefaults);
        }
    }

    struct JournalItemXDifficultyMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1 };
            object[] fieldDefaults = { 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct JournalSectionXDifficultyMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1 };
            object[] fieldDefaults = { 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct JournalTierMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1 };
            object[] fieldDefaults = { "" };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct JournalTierXInstanceMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1 };
            object[] fieldDefaults = { 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct KeyChainMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 32 };
            object[] fieldDefaults = { 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct KeystoneAffixMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1 };
            object[] fieldDefaults = { "", "", 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct LanguageWordsMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1 };
            object[] fieldDefaults = { "", 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct LanguagesMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1 };
            object[] fieldDefaults = { "", 0 };
            return new DB6Meta(1, arraySizes, fieldDefaults);
        }
    }

    struct LfgDungeonExpansionMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1, 1, 1, 1, 1 };
            object[] fieldDefaults = { 0, 0, 0, 0, 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct LfgDungeonGroupMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1, 1 };
            object[] fieldDefaults = { "", 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct LfgDungeonsMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 };
            object[] fieldDefaults = { "", 0, "", "", 0.0f, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            return new DB6Meta(31, arraySizes, fieldDefaults);
        }
    }

    struct LfgDungeonsGroupingMapMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1 };
            object[] fieldDefaults = { 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct LfgRoleRequirementMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1 };
            object[] fieldDefaults = { 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct LightMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 3, 1, 1, 1, 8 };
            object[] fieldDefaults = { 0.0f, 0.0f, 0.0f, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct LightDataMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 };
            object[] fieldDefaults = { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0, 0, 0, 0, 0, 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct LightParamsMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1, 1, 1, 3, 1, 1, 1, 1, 1 };
            object[] fieldDefaults = { 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0, 0, 0, 0, 0 };
            return new DB6Meta(10, arraySizes, fieldDefaults);
        }
    }

    struct LightSkyboxMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1 };
            object[] fieldDefaults = { "", 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct LiquidMaterialMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1 };
            object[] fieldDefaults = { 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct LiquidObjectMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1, 1, 1 };
            object[] fieldDefaults = { 0.0f, 0.0f, 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct LiquidTypeMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1, 1, 1, 1, 1, 6, 2, 18, 4, 1, 1, 1, 1, 1, 1, 6, 1 };
            object[] fieldDefaults = { "", 0, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, "", 0, 0.0f, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct LoadingScreenTaxiSplinesMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 10, 10, 1, 1, 1 };
            object[] fieldDefaults = { 0.0f, 0.0f, 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct LoadingScreensMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1 };
            object[] fieldDefaults = { 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct LocaleMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1, 1 };
            object[] fieldDefaults = { 0, 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct LocationMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 3, 3 };
            object[] fieldDefaults = { 0.0f, 0.0f };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct LockMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 8, 8, 8, 8 };
            object[] fieldDefaults = { 0, 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct LockTypeMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1, 1, 1 };
            object[] fieldDefaults = { "", "", "", "", 0 };
            return new DB6Meta(4, arraySizes, fieldDefaults);
        }
    }

    struct LookAtControllerMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 };
            object[] fieldDefaults = { 0.0f, 0.0f, 0.0f, 0.0f, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct MailTemplateMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1 };
            object[] fieldDefaults = { "" };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct ManagedWorldStateMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 };
            object[] fieldDefaults = { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            return new DB6Meta(9, arraySizes, fieldDefaults);
        }
    }

    struct ManagedWorldStateBuffMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1, 1 };
            object[] fieldDefaults = { 0, 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct ManagedWorldStateInputMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1 };
            object[] fieldDefaults = { 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct ManifestInterfaceActionIconMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1 };
            object[] fieldDefaults = { 0 };
            return new DB6Meta(0, arraySizes, fieldDefaults);
        }
    }

    struct ManifestInterfaceDataMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1 };
            object[] fieldDefaults = { "", "" };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct ManifestInterfaceItemIconMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1 };
            object[] fieldDefaults = { 0 };
            return new DB6Meta(0, arraySizes, fieldDefaults);
        }
    }

    struct ManifestInterfaceTOCDataMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1 };
            object[] fieldDefaults = { "" };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct ManifestMP3Meta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1 };
            object[] fieldDefaults = { 0 };
            return new DB6Meta(0, arraySizes, fieldDefaults);
        }
    }

    struct MapMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 2, 1, 2, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 };
            object[] fieldDefaults = { "", 0, 0.0f, 0.0f, "", "", "", "", "", 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct MapCelestialBodyMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1 };
            object[] fieldDefaults = { 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct MapChallengeModeMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1, 3, 1 };
            object[] fieldDefaults = { 0, "", 0, 0, 0 };
            return new DB6Meta(0, arraySizes, fieldDefaults);
        }
    }

    struct MapDifficultyMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1, 1, 1, 1, 1, 1 };
            object[] fieldDefaults = { "", 0, 0, 0, 0, 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct MapDifficultyXConditionMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1, 1 };
            object[] fieldDefaults = { "", 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct MarketingPromotionsXLocaleMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1, 1, 1, 1, 1 };
            object[] fieldDefaults = { 0, 0, 0, 0, "", 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct MaterialMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1, 1 };
            object[] fieldDefaults = { 0, 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct MinorTalentMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1 };
            object[] fieldDefaults = { 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct ModelAnimCloakDampeningMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1 };
            object[] fieldDefaults = { 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct ModelFileDataMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1 };
            object[] fieldDefaults = { 0, 0, 0 };
            return new DB6Meta(1, arraySizes, fieldDefaults);
        }
    }

    struct ModelRibbonQualityMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1 };
            object[] fieldDefaults = { 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct ModifierTreeMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1, 1, 1, 1, 1 };
            object[] fieldDefaults = { 0, 0, 0, 0, 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct MountMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 };
            object[] fieldDefaults = { 0, "", "", "", 0.0f, 0, 0, 0, 0, 0, 0 };
            return new DB6Meta(8, arraySizes, fieldDefaults);
        }
    }

    struct MountCapabilityMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1, 1, 1, 1, 1, 1 };
            object[] fieldDefaults = { 0, 0, 0, 0, 0, 0, 0, 0 };
            return new DB6Meta(6, arraySizes, fieldDefaults);
        }
    }

    struct MountTypeXCapabilityMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1 };
            object[] fieldDefaults = { 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct MountXDisplayMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1 };
            object[] fieldDefaults = { 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct MovieMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1, 1 };
            object[] fieldDefaults = { 0, 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct MovieFileDataMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1 };
            object[] fieldDefaults = { 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct MovieVariationMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1 };
            object[] fieldDefaults = { 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct NPCSoundsMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 4 };
            object[] fieldDefaults = { 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct NameGenMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1 };
            object[] fieldDefaults = { "", 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct NamesProfanityMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1 };
            object[] fieldDefaults = { "", 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct NamesReservedMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1 };
            object[] fieldDefaults = { "" };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct NamesReservedLocaleMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1 };
            object[] fieldDefaults = { "", 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct NpcModelItemSlotDisplayInfoMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1 };
            object[] fieldDefaults = { 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct ObjectEffectMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 3, 1, 1, 1, 1, 1, 1, 1 };
            object[] fieldDefaults = { "", 0.0f, 0, 0, 0, 0, 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct ObjectEffectGroupMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1 };
            object[] fieldDefaults = { "" };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct ObjectEffectModifierMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 4, 1, 1, 1 };
            object[] fieldDefaults = { 0.0f, 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct ObjectEffectPackageMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1 };
            object[] fieldDefaults = { "" };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct ObjectEffectPackageElemMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1 };
            object[] fieldDefaults = { 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct OutlineEffectMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1, 1, 1, 1 };
            object[] fieldDefaults = { 0.0f, 0, 0, 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct OverrideSpellDataMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 10, 1, 1 };
            object[] fieldDefaults = { 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct PageTextMaterialMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1 };
            object[] fieldDefaults = { "" };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct PaperDollItemFrameMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1 };
            object[] fieldDefaults = { "", "", 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct ParagonReputationMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1 };
            object[] fieldDefaults = { 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct ParticleColorMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 3, 3, 3 };
            object[] fieldDefaults = { 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct PathMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1, 1, 1, 1, 1 };
            object[] fieldDefaults = { 0, 0, 0, 0, 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct PathNodeMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1, 1 };
            object[] fieldDefaults = { 0, 0, 0, 0 };
            return new DB6Meta(0, arraySizes, fieldDefaults);
        }
    }

    struct PathNodePropertyMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1, 1, 1 };
            object[] fieldDefaults = { 0, 0, 0, 0, 0 };
            return new DB6Meta(3, arraySizes, fieldDefaults);
        }
    }

    struct PathPropertyMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1, 1 };
            object[] fieldDefaults = { 0, 0, 0, 0 };
            return new DB6Meta(3, arraySizes, fieldDefaults);
        }
    }

    struct PhaseMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1 };
            object[] fieldDefaults = { 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct PhaseShiftZoneSoundsMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 };
            object[] fieldDefaults = { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct PhaseXPhaseGroupMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1 };
            object[] fieldDefaults = { 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct PlayerConditionMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1, 1, 1, 1, 1, 1, 2, 1, 4, 1, 1, 1, 1, 1, 1, 4, 4, 4, 1, 4, 4, 4, 2, 1, 4, 4, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 3, 1, 1, 1, 1, 1, 1, 4, 1, 1, 1, 4, 4, 4, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 3, 4, 4, 4, 1, 4, 1, 4, 6, 1, 1, 1, 2, 1 };
            object[] fieldDefaults = { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, "", 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct PositionerMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1, 1 };
            object[] fieldDefaults = { 0.0f, 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct PositionerStateMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1, 1, 1, 1, 1, 1 };
            object[] fieldDefaults = { 0.0f, 0, 0, 0, 0, 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct PositionerStateEntryMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 };
            object[] fieldDefaults = { 0.0f, 0.0f, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct PowerDisplayMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1, 1, 1 };
            object[] fieldDefaults = { "", 0, 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct PowerTypeMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 };
            object[] fieldDefaults = { "", "", 0.0f, 0.0f, 0, 0, 0, 0, 0, 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct PrestigeLevelInfoMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1, 1 };
            object[] fieldDefaults = { 0, "", 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct PvpBracketTypesMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 4 };
            object[] fieldDefaults = { 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct PvpDifficultyMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1, 1 };
            object[] fieldDefaults = { 0, 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct PvpItemMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1 };
            object[] fieldDefaults = { 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct PvpRewardMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1 };
            object[] fieldDefaults = { 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct PvpScalingEffectMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1 };
            object[] fieldDefaults = { 0.0f, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct PvpScalingEffectTypeMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1 };
            object[] fieldDefaults = { "" };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct PvpTalentMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1, 1, 1, 1, 1, 1, 1 };
            object[] fieldDefaults = { 0, 0, "", 0, 0, 0, 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct PvpTalentUnlockMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1 };
            object[] fieldDefaults = { 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct QuestFactionRewardMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 10 };
            object[] fieldDefaults = { 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct QuestFeedbackEffectMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1, 1, 1, 1 };
            object[] fieldDefaults = { 0, 0, 0, 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct QuestInfoMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1, 1 };
            object[] fieldDefaults = { "", 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct QuestLineMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1 };
            object[] fieldDefaults = { "" };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct QuestLineXQuestMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1 };
            object[] fieldDefaults = { 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct QuestMoneyRewardMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 10 };
            object[] fieldDefaults = { 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct QuestObjectiveMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1, 1, 1, 1, 1, 1 };
            object[] fieldDefaults = { 0, 0, "", 0, 0, 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct QuestPOIBlobMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1, 1, 1, 1, 1, 1 };
            object[] fieldDefaults = { 0, 0, 0, 0, 0, 0, 0, 0 };
            return new DB6Meta(0, arraySizes, fieldDefaults);
        }
    }

    struct QuestPOIPointMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1, 1 };
            object[] fieldDefaults = { 0, 0, 0, 0 };
            return new DB6Meta(3, arraySizes, fieldDefaults);
        }
    }

    struct QuestPackageItemMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1, 1 };
            object[] fieldDefaults = { 0, 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct QuestSortMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1 };
            object[] fieldDefaults = { "", 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct QuestV2Meta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1 };
            object[] fieldDefaults = { 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct QuestV2CliTaskMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1, 1, 1, 1, 1, 3, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 };
            object[] fieldDefaults = { 0, "", "", 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            return new DB6Meta(20, arraySizes, fieldDefaults);
        }
    }

    struct QuestXGroupActivityMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1 };
            object[] fieldDefaults = { 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct QuestXPMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 10 };
            object[] fieldDefaults = { 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct RacialMountsMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1 };
            object[] fieldDefaults = { 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct RandPropPointsMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 5, 5, 5 };
            object[] fieldDefaults = { 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct ResearchBranchMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1, 1, 1 };
            object[] fieldDefaults = { "", "", 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct ResearchFieldMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1 };
            object[] fieldDefaults = { "", 0, 0 };
            return new DB6Meta(2, arraySizes, fieldDefaults);
        }
    }

    struct ResearchProjectMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1, 1, 1, 1, 1, 1, 1 };
            object[] fieldDefaults = { "", "", 0, "", 0, 0, 0, 0, 0 };
            return new DB6Meta(7, arraySizes, fieldDefaults);
        }
    }

    struct ResearchSiteMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1, 1 };
            object[] fieldDefaults = { 0, "", 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct ResistancesMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1 };
            object[] fieldDefaults = { "", 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct RewardPackMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1, 1, 1, 1 };
            object[] fieldDefaults = { 0, 0.0f, 0, 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct RewardPackXCurrencyTypeMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1 };
            object[] fieldDefaults = { 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct RewardPackXItemMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1 };
            object[] fieldDefaults = { 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct RibbonQualityMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1, 1, 1 };
            object[] fieldDefaults = { 0.0f, 0.0f, 0.0f, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct RulesetItemUpgradeMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1 };
            object[] fieldDefaults = { 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct ScalingStatDistributionMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1 };
            object[] fieldDefaults = { 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct ScenarioMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1, 1 };
            object[] fieldDefaults = { "", 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct ScenarioEventEntryMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1 };
            object[] fieldDefaults = { 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct ScenarioStepMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1, 1, 1, 1, 1, 1, 1 };
            object[] fieldDefaults = { "", "", 0, 0, 0, 0, 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct SceneScriptMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1, 1 };
            object[] fieldDefaults = { "", "", 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct SceneScriptPackageMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1 };
            object[] fieldDefaults = { "" };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct SceneScriptPackageMemberMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1, 1 };
            object[] fieldDefaults = { 0, 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct ScheduledIntervalMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1, 1, 1 };
            object[] fieldDefaults = { 0, 0, 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct ScheduledWorldStateMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1, 1, 1, 1, 1, 1 };
            object[] fieldDefaults = { 0, 0, 0, 0, 0, 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct ScheduledWorldStateGroupMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1, 1, 1 };
            object[] fieldDefaults = { 0, 0, 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct ScheduledWorldStateXUniqCatMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1 };
            object[] fieldDefaults = { 0, 0, 0 };
            return new DB6Meta(0, arraySizes, fieldDefaults);
        }
    }

    struct ScreenEffectMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 4, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 };
            object[] fieldDefaults = { "", 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct ScreenLocationMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1 };
            object[] fieldDefaults = { "" };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct SeamlessSiteMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1 };
            object[] fieldDefaults = { 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct ServerMessagesMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1 };
            object[] fieldDefaults = { "" };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct ShadowyEffectMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 };
            object[] fieldDefaults = { 0, 0, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct SkillLineMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1, 1, 1, 1, 1, 1 };
            object[] fieldDefaults = { "", "", "", 0, 0, 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct SkillLineAbilityMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 };
            object[] fieldDefaults = { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct SkillRaceClassInfoMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1, 1, 1, 1, 1 };
            object[] fieldDefaults = { 0, 0, 0, 0, 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct SoundAmbienceMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 2, 1, 1 };
            object[] fieldDefaults = { 0, 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct SoundAmbienceFlavorMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1 };
            object[] fieldDefaults = { 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct SoundBusMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1, 1, 1, 1, 1, 1 };
            object[] fieldDefaults = { 0.0f, 0, 0, 0, 0, 0, 0, 0 };
            return new DB6Meta(7, arraySizes, fieldDefaults);
        }
    }

    struct SoundBusOverrideMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1, 1, 1, 1, 1 };
            object[] fieldDefaults = { 0, 0.0f, 0, 0, 0, 0, 0 };
            return new DB6Meta(0, arraySizes, fieldDefaults);
        }
    }

    struct SoundEmitterPillPointsMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 3, 1 };
            object[] fieldDefaults = { 0.0f, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct SoundEmittersMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 3, 3, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 };
            object[] fieldDefaults = { 0.0f, 0.0f, "", 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            return new DB6Meta(9, arraySizes, fieldDefaults);
        }
    }

    struct SoundFilterMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1 };
            object[] fieldDefaults = { "" };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct SoundFilterElemMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 9, 1, 1 };
            object[] fieldDefaults = { 0.0f, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct SoundKitMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 };
            object[] fieldDefaults = { "", 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0, 0, 0, 0, 0, 0, 0, 0 };
            return new DB6Meta(16, arraySizes, fieldDefaults);
        }
    }

    struct SoundKitAdvancedMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 };
            object[] fieldDefaults = { 0.0f, 0, 0, 0, 0, 0, 0, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0, 0, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct SoundKitChildMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1 };
            object[] fieldDefaults = { 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct SoundKitEntryMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1, 1 };
            object[] fieldDefaults = { 0, 0.0f, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct SoundKitFallbackMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1 };
            object[] fieldDefaults = { 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct SoundOverrideMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1, 1 };
            object[] fieldDefaults = { 0, 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct SoundProviderPreferencesMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 };
            object[] fieldDefaults = { "", 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0, 0, 0, 0, 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct SourceInfoMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1, 1 };
            object[] fieldDefaults = { 0, "", 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct SpamMessagesMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1 };
            object[] fieldDefaults = { "" };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct SpecializationSpellsMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1, 1, 1, 1 };
            object[] fieldDefaults = { 0, 0, "", 0, 0, 0 };
            return new DB6Meta(5, arraySizes, fieldDefaults);
        }
    }

    struct SpellMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1, 1, 1, 1, 1 };
            object[] fieldDefaults = { "", "", "", "", 0, 0, 0 };
            return new DB6Meta(5, arraySizes, fieldDefaults);
        }
    }

    struct SpellActionBarPrefMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1 };
            object[] fieldDefaults = { 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct SpellActivationOverlayMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1, 1, 4, 1, 1, 1 };
            object[] fieldDefaults = { 0, 0, 0, 0.0f, 0, 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct SpellAuraOptionsMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1, 1, 1, 1, 1, 1 };
            object[] fieldDefaults = { 0, 0, 0, 0, 0, 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct SpellAuraRestrictionsMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 };
            object[] fieldDefaults = { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct SpellAuraVisXChrSpecMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1 };
            object[] fieldDefaults = { 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct SpellAuraVisibilityMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1, 1 };
            object[] fieldDefaults = { 0, 0, 0, 0 };
            return new DB6Meta(3, arraySizes, fieldDefaults);
        }
    }

    struct SpellCastTimesMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1 };
            object[] fieldDefaults = { 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct SpellCastingRequirementsMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1, 1, 1, 1, 1 };
            object[] fieldDefaults = { 0, 0, 0, 0, 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct SpellCategoriesMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1, 1, 1, 1, 1, 1, 1 };
            object[] fieldDefaults = { 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct SpellCategoryMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1, 1, 1, 1 };
            object[] fieldDefaults = { "", 0, 0, 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct SpellChainEffectsMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 3, 3, 3, 3, 1, 1, 1, 3, 1, 1, 1, 11, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 };
            object[] fieldDefaults = { 0.0f, 0.0f, 0.0f, 0, 0, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0, 0.0f, 0.0f, "", "", 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct SpellClassOptionsMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 4, 1, 1 };
            object[] fieldDefaults = { 0, 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct SpellCooldownsMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1, 1, 1 };
            object[] fieldDefaults = { 0, 0, 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct SpellDescriptionVariablesMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1 };
            object[] fieldDefaults = { "" };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct SpellDispelTypeMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1, 1 };
            object[] fieldDefaults = { "", "", 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct SpellDurationMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1 };
            object[] fieldDefaults = { 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct SpellEffectMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 4, 1, 1, 1, 1, 1, 1, 2, 2, 2, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 };
            object[] fieldDefaults = { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0.0f, 0, 0.0f, 1.0f, 0, 0, 0, 0, 0.0f, 0.0f, 0, 0.0f, 0, 0.0f, 1.0f };
            return new DB6Meta(1, arraySizes, fieldDefaults);
        }
    }

    struct SpellEffectEmissionMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1, 1 };
            object[] fieldDefaults = { 0.0f, 0.0f, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct SpellEffectGroupSizeMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1 };
            object[] fieldDefaults = { 0, 0.0f };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct SpellEffectScalingMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1, 1 };
            object[] fieldDefaults = { 0.0f, 0.0f, 0.0f, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct SpellEquippedItemsMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1, 1 };
            object[] fieldDefaults = { 0, 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct SpellFlyoutMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1, 1, 1, 1 };
            object[] fieldDefaults = { 0, "", "", 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct SpellFlyoutItemMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1 };
            object[] fieldDefaults = { 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct SpellFocusObjectMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1 };
            object[] fieldDefaults = { "" };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct SpellInterruptsMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 2, 2, 1, 1 };
            object[] fieldDefaults = { 0, 0, 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct SpellItemEnchantmentMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 3, 1, 3, 1, 1, 3, 1, 1, 1, 1, 1, 1, 3, 1, 1, 1, 1, 1, 1 };
            object[] fieldDefaults = { 0, "", 0.0f, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct SpellItemEnchantmentConditionMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 5, 5, 5, 5, 5, 5 };
            object[] fieldDefaults = { 0, 0, 0, 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct SpellKeyboundOverrideMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1 };
            object[] fieldDefaults = { 0, "", 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct SpellLabelMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1 };
            object[] fieldDefaults = { 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct SpellLearnSpellMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1 };
            object[] fieldDefaults = { 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct SpellLevelsMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1, 1, 1, 1 };
            object[] fieldDefaults = { 0, 0, 0, 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct SpellMechanicMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1 };
            object[] fieldDefaults = { "" };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct SpellMiscMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 14, 1, 1, 1, 1, 1, 1, 1, 1 };
            object[] fieldDefaults = { 0, 0.0f, 0.0f, 0, 0, 0, 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct SpellMiscDifficultyMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1 };
            object[] fieldDefaults = { 0, 0, 0 };
            return new DB6Meta(2, arraySizes, fieldDefaults);
        }
    }

    struct SpellMissileMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 };
            object[] fieldDefaults = { 0, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct SpellMissileMotionMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1, 1 };
            object[] fieldDefaults = { "", "", 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct SpellPowerMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 };
            object[] fieldDefaults = { 0, 0, 0.0f, 0.0f, 0, 0.0f, 0, 0, 0, 0, 0, 0, 0, 0 };
            return new DB6Meta(8, arraySizes, fieldDefaults);
        }
    }

    struct SpellPowerDifficultyMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1 };
            object[] fieldDefaults = { 0, 0, 0 };
            return new DB6Meta(2, arraySizes, fieldDefaults);
        }
    }

    struct SpellProceduralEffectMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 4, 1, 1 };
            object[] fieldDefaults = { 0.0f, 0, 0 };
            return new DB6Meta(2, arraySizes, fieldDefaults);
        }
    }

    struct SpellProcsPerMinuteMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1 };
            object[] fieldDefaults = { 0.0f, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct SpellProcsPerMinuteModMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1, 1 };
            object[] fieldDefaults = { 0.0f, 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct SpellRadiusMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1, 1 };
            object[] fieldDefaults = { 0.0f, 0.0f, 0.0f, 0.0f };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct SpellRangeMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 2, 2, 1, 1, 1 };
            object[] fieldDefaults = { 0.0f, 0.0f, "", "", 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct SpellReagentsMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 8, 8 };
            object[] fieldDefaults = { 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct SpellReagentsCurrencyMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1 };
            object[] fieldDefaults = { 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct SpellScalingMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1, 1, 1 };
            object[] fieldDefaults = { 0, 0, 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct SpellShapeshiftMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 2, 2, 1 };
            object[] fieldDefaults = { 0, 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct SpellShapeshiftFormMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1, 1, 1, 1, 1, 1, 4, 8 };
            object[] fieldDefaults = { "", 0.0f, 0, 0, 0, 0, 0, 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct SpellSpecialUnitEffectMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1 };
            object[] fieldDefaults = { 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct SpellTargetRestrictionsMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1, 1, 1, 1, 1, 1 };
            object[] fieldDefaults = { 0, 0.0f, 0.0f, 0, 0, 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct SpellTotemsMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 2, 2 };
            object[] fieldDefaults = { 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct SpellVisualMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 3, 3, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 };
            object[] fieldDefaults = { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0.0f, 0.0f, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            return new DB6Meta(26, arraySizes, fieldDefaults);
        }
    }

    struct SpellVisualAnimMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1 };
            object[] fieldDefaults = { 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct SpellVisualColorEffectMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 };
            object[] fieldDefaults = { 0.0f, 0, 0.0f, 0, 0, 0, 0, 0, 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct SpellVisualEffectNameMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 };
            object[] fieldDefaults = { "", 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0, 0, 0, 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct SpellVisualKitMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1, 1, 1, 1 };
            object[] fieldDefaults = { 0, 0.0f, 0, 0, 0, 0 };
            return new DB6Meta(4, arraySizes, fieldDefaults);
        }
    }

    struct SpellVisualKitAreaModelMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1, 1, 1, 1 };
            object[] fieldDefaults = { 0, 0.0f, 0.0f, 0.0f, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct SpellVisualKitEffectMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1 };
            object[] fieldDefaults = { 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct SpellVisualKitModelAttachMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 3, 3, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 };
            object[] fieldDefaults = { 0, 0.0f, 0.0f, 0, 0, 0, 0, 0, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 1.0f, 0.0f, 65535, 65535, 65535, 0, 0, 0.0f };
            return new DB6Meta(6, arraySizes, fieldDefaults);
        }
    }

    struct SpellVisualMissileMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1, 3, 3, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 };
            object[] fieldDefaults = { 0, 0, 0, 0.0f, 0.0f, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            return new DB6Meta(13, arraySizes, fieldDefaults);
        }
    }

    struct SpellXSpellVisualMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 };
            object[] fieldDefaults = { 0, 0, 0, 1.0f, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            return new DB6Meta(2, arraySizes, fieldDefaults);
        }
    }

    struct StartupFilesMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1 };
            object[] fieldDefaults = { 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct Startup_StringsMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1 };
            object[] fieldDefaults = { "", "" };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct StationeryMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1 };
            object[] fieldDefaults = { "", 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct StringLookupsMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1 };
            object[] fieldDefaults = { "" };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct SummonPropertiesMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1, 1, 1 };
            object[] fieldDefaults = { 0, 0, 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct TactKeyMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 16 };
            object[] fieldDefaults = { 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct TactKeyLookupMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 8 };
            object[] fieldDefaults = { 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct TalentMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1, 1, 1, 1, 1, 2, 1 };
            object[] fieldDefaults = { 0, 0, "", 0, 0, 0, 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct TaxiNodesMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 3, 1, 2, 2, 1, 1, 1, 1, 1 };
            object[] fieldDefaults = { 0.0f, "", 0, 0.0f, 0, 0, 0, 0, 0 };
            return new DB6Meta(8, arraySizes, fieldDefaults);
        }
    }

    struct TaxiPathMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1, 1 };
            object[] fieldDefaults = { 0, 0, 0, 0 };
            return new DB6Meta(2, arraySizes, fieldDefaults);
        }
    }

    struct TaxiPathNodeMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 3, 1, 1, 1, 1, 1, 1, 1, 1 };
            object[] fieldDefaults = { 0.0f, 0, 0, 0, 0, 0, 0, 0, 0 };
            return new DB6Meta(8, arraySizes, fieldDefaults);
        }
    }

    struct TerrainMaterialMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1 };
            object[] fieldDefaults = { "", "", 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct TerrainTypeMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1, 1, 1 };
            object[] fieldDefaults = { "", 0, 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct TerrainTypeSoundsMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1 };
            object[] fieldDefaults = { "" };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct TextureBlendSetMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 3, 3, 3, 3, 3, 4, 1, 1, 1, 1 };
            object[] fieldDefaults = { 0, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0, 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct TextureFileDataMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1 };
            object[] fieldDefaults = { 0, 0, 0 };
            return new DB6Meta(2, arraySizes, fieldDefaults);
        }
    }

    struct TotemCategoryMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1 };
            object[] fieldDefaults = { "", 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct ToyMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1, 1, 1 };
            object[] fieldDefaults = { 0, "", 0, 0, 0 };
            return new DB6Meta(4, arraySizes, fieldDefaults);
        }
    }

    struct TradeSkillCategoryMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1, 1, 1 };
            object[] fieldDefaults = { "", 0, 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct TradeSkillItemMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1 };
            object[] fieldDefaults = { 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct TransformMatrixMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 3, 1, 1, 1, 1 };
            object[] fieldDefaults = { 0.0f, 0.0f, 0.0f, 0.0f, 0.0f };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct TransmogHolidayMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1 };
            object[] fieldDefaults = { 0, 0 };
            return new DB6Meta(0, arraySizes, fieldDefaults);
        }
    }

    struct TransmogSetMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 };
            object[] fieldDefaults = { "", 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            return new DB6Meta(4, arraySizes, fieldDefaults);
        }
    }

    struct TransmogSetGroupMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1 };
            object[] fieldDefaults = { "", 0 };
            return new DB6Meta(1, arraySizes, fieldDefaults);
        }
    }

    struct TransmogSetItemMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1, 1 };
            object[] fieldDefaults = { 0, 0, 0, 0 };
            return new DB6Meta(0, arraySizes, fieldDefaults);
        }
    }

    struct TransportAnimationMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 3, 1 };
            object[] fieldDefaults = { 0, 0, 0.0f, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct TransportPhysicsMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 };
            object[] fieldDefaults = { 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct TransportRotationMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 4 };
            object[] fieldDefaults = { 0, 0, 0.0f };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct TrophyMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1, 1 };
            object[] fieldDefaults = { "", 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct UiCamFbackTransmogChrRaceMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1, 1, 1 };
            object[] fieldDefaults = { 0, 0, 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct UiCamFbackTransmogWeaponMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1, 1 };
            object[] fieldDefaults = { 0, 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct UiCameraMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 3, 3, 3, 1, 1, 1, 1, 1 };
            object[] fieldDefaults = { "", 0.0f, 0.0f, 0.0f, 0, 0, 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct UiCameraTypeMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1 };
            object[] fieldDefaults = { "", 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct UiMapPOIMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 3, 1, 1, 1, 1, 1 };
            object[] fieldDefaults = { 0, 0.0f, 0, 0, 0, 0, 0 };
            return new DB6Meta(6, arraySizes, fieldDefaults);
        }
    }

    struct UiModelSceneMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1 };
            object[] fieldDefaults = { 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct UiModelSceneActorMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 3, 1, 1, 1, 1, 1, 1, 1, 1 };
            object[] fieldDefaults = { "", 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0, 0, 0, 0 };
            return new DB6Meta(7, arraySizes, fieldDefaults);
        }
    }

    struct UiModelSceneActorDisplayMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1, 1, 1 };
            object[] fieldDefaults = { 0.0f, 0.0f, 0.0f, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct UiModelSceneCameraMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 3, 3, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 };
            object[] fieldDefaults = { "", 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0, 0, 0, 0 };
            return new DB6Meta(14, arraySizes, fieldDefaults);
        }
    }

    struct UiTextureAtlasMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1 };
            object[] fieldDefaults = { 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct UiTextureAtlasMemberMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1, 1, 1, 1, 1 };
            object[] fieldDefaults = { "", 0, 0, 0, 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct UiTextureKitMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1 };
            object[] fieldDefaults = { "" };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct UnitBloodMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 5, 1, 1, 1, 1, 1, 1 };
            object[] fieldDefaults = { "", 0, 0, 0, 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct UnitBloodLevelsMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 3 };
            object[] fieldDefaults = { 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct UnitConditionMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 8, 1, 8, 8 };
            object[] fieldDefaults = { 0, 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct UnitPowerBarMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 6, 6, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 };
            object[] fieldDefaults = { 0.0f, 0.0f, 0, 0, "", "", "", "", 0.0f, 0.0f, 0, 0, 0, 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct UnitTestMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1, 1, 1 };
            object[] fieldDefaults = { "", "", 0, 0, 0 };
            return new DB6Meta(2, arraySizes, fieldDefaults);
        }
    }

    struct VehicleMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 2, 1, 1, 2, 1, 1, 8, 1, 3, 1, 1 };
            object[] fieldDefaults = { 0, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, "", "", "", 0.0f, 0.0f, 0, 0, 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct VehicleSeatMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1, 3, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 3, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 };
            object[] fieldDefaults = { 0, 0, 0, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct VehicleUIIndSeatMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1, 1 };
            object[] fieldDefaults = { 0.0f, 0.0f, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct VehicleUIIndicatorMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1 };
            object[] fieldDefaults = { "" };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct VideoHardwareMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 };
            object[] fieldDefaults = { "", "", 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            return new DB6Meta(14, arraySizes, fieldDefaults);
        }
    }

    struct VignetteMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1, 1, 1, 1, 1 };
            object[] fieldDefaults = { "", 0.0f, 0.0f, 0, 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct VocalUISoundsMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1, 2 };
            object[] fieldDefaults = { 0, 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct WMOAreaTableMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 };
            object[] fieldDefaults = { 0, "", 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            return new DB6Meta(13, arraySizes, fieldDefaults);
        }
    }

    struct WbAccessControlListMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1, 1, 1 };
            object[] fieldDefaults = { "", 0, 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct WbCertWhitelistMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1, 1 };
            object[] fieldDefaults = { "", 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct WeaponImpactSoundsMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1, 11, 11, 11, 11 };
            object[] fieldDefaults = { 0, 0, 0, 0, 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct WeaponSwingSounds2Meta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1 };
            object[] fieldDefaults = { 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct WeaponTrailMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1, 1, 3, 3, 3, 3, 3 };
            object[] fieldDefaults = { 0, 0.0f, 0.0f, 0.0f, 0, 0.0f, 0.0f, 0.0f, 0.0f };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct WeaponTrailModelDefMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1 };
            object[] fieldDefaults = { 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct WeaponTrailParamMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 };
            object[] fieldDefaults = { 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0, 0, 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct WeatherMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 2, 1, 3, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 };
            object[] fieldDefaults = { 0.0f, 0.0f, 0.0f, "", 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0, 0, 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct WindSettingsMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 3, 1, 1, 3, 1, 3, 1, 1, 1 };
            object[] fieldDefaults = { 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct WmoMinimapTextureMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1, 1, 1 };
            object[] fieldDefaults = { 0, 0, 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct WorldBossLockoutMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1 };
            object[] fieldDefaults = { "", 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct WorldChunkSoundsMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1, 1, 1, 1 };
            object[] fieldDefaults = { 0, 0, 0, 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct WorldEffectMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1, 1, 1, 1 };
            object[] fieldDefaults = { 0, 0, 0, 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct WorldElapsedTimerMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1 };
            object[] fieldDefaults = { "", 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct WorldMapAreaMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 };
            object[] fieldDefaults = { "", 0.0f, 0.0f, 0.0f, 0.0f, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            return new DB6Meta(15, arraySizes, fieldDefaults);
        }
    }

    struct WorldMapContinentMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 2, 1, 2, 2, 1, 1, 1, 1, 1, 1, 1 };
            object[] fieldDefaults = { 0.0f, 0.0f, 0.0f, 0.0f, 0, 0, 0, 0, 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct WorldMapOverlayMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1, 1, 1, 4, 1, 1, 1, 1, 1, 1, 1, 1 };
            object[] fieldDefaults = { 0, "", 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            return new DB6Meta(0, arraySizes, fieldDefaults);
        }
    }

    struct WorldMapTransformsMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 6, 2, 1, 1, 1, 1, 1, 1, 1 };
            object[] fieldDefaults = { 0.0f, 0.0f, 0.0f, 0, 0, 0, 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct WorldSafeLocsMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 3, 1, 1, 1 };
            object[] fieldDefaults = { 0.0f, 0.0f, "", 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct WorldStateExpressionMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1 };
            object[] fieldDefaults = { "" };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct WorldStateUIMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 3, 1, 1, 1, 1 };
            object[] fieldDefaults = { "", "", "", "", "", "", 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            return new DB6Meta(15, arraySizes, fieldDefaults);
        }
    }

    struct WorldStateZoneSoundsMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1, 1, 1, 1, 1, 1 };
            object[] fieldDefaults = { 0, 0, 0, 0, 0, 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct World_PVP_AreaMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1, 1, 1, 1, 1 };
            object[] fieldDefaults = { 0, 0, 0, 0, 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct ZoneIntroMusicTableMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1, 1 };
            object[] fieldDefaults = { "", 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct ZoneLightMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 1, 1 };
            object[] fieldDefaults = { "", 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct ZoneLightPointMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 2, 1, 1 };
            object[] fieldDefaults = { 0.0f, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }

    struct ZoneMusicMeta
    {
        public static DB6Meta Instance()
        {
            byte[] arraySizes = { 1, 2, 2, 2 };
            object[] fieldDefaults = { "", 0, 0, 0 };
            return new DB6Meta(-1, arraySizes, fieldDefaults);
        }
    }
}
