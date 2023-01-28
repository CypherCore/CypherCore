// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

namespace Game.Entities
{
    public class PlayerCreateInfoAction
	{
		public uint Action { get; set; }

        public byte Button { get; set; }
        public byte Type { get; set; }

        public PlayerCreateInfoAction() : this(0, 0, 0)
		{
		}

		public PlayerCreateInfoAction(byte _button, uint _action, byte _type)
		{
			Button = _button;
			Type   = _type;
			Action = _action;
		}
	}
}