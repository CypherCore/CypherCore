// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Items;

[Script]
internal class spell_item_shadows_fate : AuraScript, IAuraOnProc
{
	public void OnProc(ProcEventInfo procInfo)
	{
		PreventDefaultAction();

		var caster = procInfo.GetActor();
		var target = GetCaster();

		if (!caster ||
		    !target)
			return;

		caster.CastSpell(target, ItemSpellIds.SoulFeast, new CastSpellExtraArgs(TriggerCastFlags.FullMask));
	}
}