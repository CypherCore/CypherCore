// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

namespace Game
{
    public class PlayerChoiceResponse
    {
        public PlayerChoiceResponseMawPower? MawPower;
        public string Answer { get; set; }
        public string ButtonTooltip { get; set; }
        public int ChoiceArtFileId { get; set; }

        public string Confirmation { get; set; }
        public string Description { get; set; }
        public int Flags { get; set; }
        public byte GroupID { get; set; }
        public string Header { get; set; }
        public int ResponseId { get; set; }
        public ushort ResponseIdentifier { get; set; }
        public PlayerChoiceResponseReward Reward { get; set; }
        public uint? RewardQuestID { get; set; }
        public uint SoundKitID { get; set; }
        public string SubHeader { get; set; }
        public uint UiTextureAtlasElementID { get; set; }
        public int UiTextureKitID { get; set; }
        public uint WidgetSetID { get; set; }
    }
}