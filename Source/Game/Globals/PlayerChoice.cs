// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

namespace Game
{
    public class PlayerChoiceResponseRewardItem
    {
        public uint Id;
        public List<uint> BonusListIDs = new();
        public int Quantity;

        public PlayerChoiceResponseRewardItem() { }
        public PlayerChoiceResponseRewardItem(uint id, List<uint> bonusListIDs, int quantity)
        {
            Id = id;
            BonusListIDs = bonusListIDs;
            Quantity = quantity;
        }
    }

    public class PlayerChoiceResponseRewardEntry
    {
        public uint Id;
        public int Quantity;

        public PlayerChoiceResponseRewardEntry(uint id, int quantity)
        {
            Id = id;
            Quantity = quantity;
        }
    }

    public class PlayerChoiceResponseReward
    {
        public int TitleId;
        public int PackageId;
        public int SkillLineId;
        public uint SkillPointCount;
        public uint ArenaPointCount;
        public uint HonorPointCount;
        public ulong Money;
        public uint Xp;

        public List<PlayerChoiceResponseRewardItem> Items = new();
        public List<PlayerChoiceResponseRewardEntry> Currency = new();
        public List<PlayerChoiceResponseRewardEntry> Faction = new();
        public List<PlayerChoiceResponseRewardItem> ItemChoices = new();
    }

    public struct PlayerChoiceResponseMawPower
    {
        public int TypeArtFileID;
        public int? Rarity;
        public int SpellID;
        public int MaxStacks;
    }

    public enum PlayerChoiceResponseFlags
    {
        None = 0x000,
        DisabledButton = 0x001,    // Disables single button
        DesaturateArt = 0x002,
        DisabledOption = 0x004,    // Disables the entire group of options
        ConsolidateWidgets = 0x020,
        ShowCheckmark = 0x040,
        HideButtonShowText = 0x080,
        Selected = 0x100,
    }

    public class PlayerChoiceResponse
    {
        public int ResponseId;
        public int ChoiceArtFileId;
        public PlayerChoiceResponseFlags Flags;
        public uint WidgetSetID;
        public uint UiTextureAtlasElementID;
        public uint SoundKitID;
        public byte GroupID;
        public int UiTextureKitID;
        public string Answer;
        public string Header;
        public string SubHeader;
        public string ButtonTooltip;
        public string Description;
        public string Confirmation;
        public PlayerChoiceResponseReward Reward;
        public uint? RewardQuestID;
        public PlayerChoiceResponseMawPower? MawPower;
    }

    public class PlayerChoice
    {
        public int ChoiceId;
        public int UiTextureKitId;
        public uint SoundKitId;
        public uint CloseSoundKitId;
        public TimeSpan Duration;
        public string Question;
        public string PendingChoiceText;
        public List<PlayerChoiceResponse> Responses = new();
        public bool InfiniteRange;
        public bool HideWarboardHeader;
        public bool KeepOpenAfterChoice;
        public bool ShowChoicesAsList;
        public bool ForceDontShowChoicesAsList;

        public uint MaxResponses;

        public uint ScriptId;

        public PlayerChoiceResponse GetResponse(int responseId)
        {
            return Responses.Find(playerChoiceResponse => playerChoiceResponse.ResponseId == responseId);
        }
    }
}
