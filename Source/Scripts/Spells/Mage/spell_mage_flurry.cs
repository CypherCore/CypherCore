// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Mage;

[SpellScript(44614)]
public class spell_mage_flurry : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();

	private void HandleDummy(int effIndex)
	{
		var caster     = GetCaster();
		var target     = GetHitUnit();
		var isImproved = false;

		if (caster == null || target == null)
			return;

		if (caster.HasAura(MageSpells.BRAIN_FREEZE_AURA))
		{
			caster.RemoveAura(MageSpells.BRAIN_FREEZE_AURA);

			if (caster.HasSpell(MageSpells.BRAIN_FREEZE_IMPROVED))
				isImproved = true;
		}

		var targetGuid = target.GetGUID();

		if (targetGuid != ObjectGuid.Empty)
			for (byte i = 1; i < 3; ++i) // basepoint value is 3 all the time, so, set it 3 because sometimes it won't read
				caster.m_Events.AddEventAtOffset(() =>
				                                 {
					                                 if (caster != null)
					                                 {
						                                 var target = ObjectAccessor.Instance.GetUnit(caster, targetGuid);

						                                 if (target != null)
						                                 {
							                                 caster.CastSpell(target, MageSpells.FLURRY_VISUAL, false);

							                                 if (isImproved)
								                                 caster.CastSpell(target, MageSpells.FLURRY_CHILL_PROC, false);
						                                 }
					                                 }
				                                 },
				                                 TimeSpan.FromMilliseconds(i * 250));
	}

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
	}
}