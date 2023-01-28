// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Framework.Constants;

namespace Game.Entities
{
    public class VoidStorageItem
	{
		public uint ArtifactKnowledgeLevel { get; set; }
        public List<uint> BonusListIDs { get; set; } = new();
		public ItemContext Context { get; set; }
        public ObjectGuid CreatorGuid;
		public uint FixedScalingLevel { get; set; }
        public uint ItemEntry { get; set; }

        public ulong ItemId { get; set; }
        public uint RandomBonusListId { get; set; }

        public VoidStorageItem(ulong id, uint entry, ObjectGuid creator, uint randomBonusListId, uint fixedScalingLevel, uint artifactKnowledgeLevel, ItemContext context, List<uint> bonuses)
		{
			ItemId                 = id;
			ItemEntry              = entry;
			CreatorGuid            = creator;
			RandomBonusListId      = randomBonusListId;
			FixedScalingLevel      = fixedScalingLevel;
			ArtifactKnowledgeLevel = artifactKnowledgeLevel;
			Context                = context;

			foreach (var value in bonuses)
				BonusListIDs.Add(value);
		}
	}
}