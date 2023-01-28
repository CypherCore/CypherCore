// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Framework.Constants;

namespace Game.Entities
{
    public class SpecializationInfo
	{
		public byte ActiveGroup { get; set; }
        public List<uint>[] Glyphs { get; set; } = new List<uint>[PlayerConst.MaxSpecializations];
		public uint[][] PvpTalents { get; set; } = new uint[PlayerConst.MaxSpecializations][];
		public uint ResetTalentsCost { get; set; }
        public long ResetTalentsTime { get; set; }

        public Dictionary<uint, PlayerSpellState>[] Talents { get; set; } = new Dictionary<uint, PlayerSpellState>[PlayerConst.MaxSpecializations];

		public SpecializationInfo()
		{
			for (byte i = 0; i < PlayerConst.MaxSpecializations; ++i)
			{
				Talents[i]    = new Dictionary<uint, PlayerSpellState>();
				PvpTalents[i] = new uint[PlayerConst.MaxPvpTalentSlots];
				Glyphs[i]     = new List<uint>();
			}
		}
	}
}