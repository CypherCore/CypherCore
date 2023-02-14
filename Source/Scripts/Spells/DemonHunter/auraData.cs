// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Entities;

namespace Scripts.Spells.DemonHunter;

public class auraData
{
	public auraData(uint id, ObjectGuid casterGUID)
	{
		m_id         = id;
		m_casterGuid = casterGUID;
	}

	public uint m_id;
	public ObjectGuid m_casterGuid = new();
}