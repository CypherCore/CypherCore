// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Mage;

[Script] // 342247 - Alter Time Active
internal class spell_mage_alter_time_active : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();

	public override bool Validate(SpellInfo spellInfo)
	{
		return ValidateSpellInfo(MageSpells.AlterTimeAura, MageSpells.ArcaneAlterTimeAura);
	}

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(RemoveAlterTimeAura, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHit));
	}

	private void RemoveAlterTimeAura(uint effIndex)
	{
		var unit = GetCaster();
		unit.RemoveAura(MageSpells.AlterTimeAura, ObjectGuid.Empty, 0, AuraRemoveMode.Expire);
		unit.RemoveAura(MageSpells.ArcaneAlterTimeAura, ObjectGuid.Empty, 0, AuraRemoveMode.Expire);
	}
}