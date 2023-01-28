// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;

namespace Game.Entities
{
    public class EquipmentSetInfo
	{
		public enum EquipmentSetType
		{
			Equipment = 0,
			Transmog = 1
		}

		public EquipmentSetData Data { get; set; }

        public EquipmentSetUpdateState State { get; set; }

        public EquipmentSetInfo()
		{
			State = EquipmentSetUpdateState.New;
			Data  = new EquipmentSetData();
		}

		// Data sent in EquipmentSet related packets
		public class EquipmentSetData
		{
			public int[] Appearances { get; set; } = new int[EquipmentSlot.End]; // ItemModifiedAppearanceID
			public int AssignedSpecIndex { get; set; } = -1;                     // Index of character specialization that this set is automatically equipped for
			public int[] Enchants { get; set; } = new int[2];                    // SpellItemEnchantmentID
			public ulong Guid { get; set; }                                     // Set Identifier
            public uint IgnoreMask { get; set; }                               // Mask of EquipmentSlot
            public ObjectGuid[] Pieces { get; set; } = new ObjectGuid[EquipmentSlot.End];
			public int SecondaryShoulderApparanceID { get; set; } // Secondary shoulder appearance
            public int SecondaryShoulderSlot { get; set; }       // Always 2 if secondary shoulder apperance is used
            public int SecondaryWeaponAppearanceID { get; set; }  // For legion artifacts: linked child Item appearance
            public int SecondaryWeaponSlot { get; set; }         // For legion artifacts: which Slot is used by child Item
            public string SetIcon { get; set; } = "";
			public uint SetID { get; set; } // Index
            public string SetName { get; set; } = "";
			public EquipmentSetType Type { get; set; }
        }
	}
}