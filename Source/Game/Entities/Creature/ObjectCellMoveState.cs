// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

namespace Game.Entities
{
    public enum ObjectCellMoveState
	{
		None,    // not in move list
		Active,  // in move list
		Inactive // in move list but should not move
	}
}