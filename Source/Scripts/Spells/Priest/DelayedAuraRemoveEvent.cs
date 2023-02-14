// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Dynamic;
using Game.Entities;

namespace Scripts.Spells.Priest;

public class DelayedAuraRemoveEvent : BasicEvent
{
	public DelayedAuraRemoveEvent(Unit owner, uint spellId)
	{
		this._owner   = owner;
		this._spellId = spellId;
	}

	public override bool Execute(ulong UnnamedParameter, uint UnnamedParameter2)
	{
		_owner.RemoveAurasDueToSpell(_spellId);

		return true;
	}

	private readonly Unit _owner;
	private readonly uint _spellId;
}