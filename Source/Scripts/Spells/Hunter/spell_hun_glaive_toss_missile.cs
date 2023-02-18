// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Hunter;

[SpellScript(new uint[]
             {
	             120755, 120756
             })]
public class spell_hun_glaive_toss_missile : SpellScript, ISpellOnHit, ISpellAfterCast
{
	public void AfterCast()
	{
		if (GetSpellInfo().Id == HunterSpells.GLAIVE_TOSS_RIGHT)
		{
			var plr = GetCaster().ToPlayer();

			if (plr != null)
			{
				plr.CastSpell(plr, HunterSpells.GLAIVE_TOSS_DAMAGE_AND_SNARE_RIGHT, true);
			}
			else if (GetOriginalCaster())
			{
				var caster = GetOriginalCaster().ToPlayer();

				if (caster != null)
					caster.CastSpell(caster, HunterSpells.GLAIVE_TOSS_DAMAGE_AND_SNARE_RIGHT, true);
			}
		}
		else
		{
			var plr = GetCaster().ToPlayer();

			if (plr != null)
			{
				plr.CastSpell(plr, HunterSpells.GLAIVE_TOSS_DAMAGE_AND_SNARE_LEFT, true);
			}
			else if (GetOriginalCaster())
			{
				var caster = GetOriginalCaster().ToPlayer();

				if (caster != null)
					caster.CastSpell(caster, HunterSpells.GLAIVE_TOSS_DAMAGE_AND_SNARE_LEFT, true);
			}
		}

		var target = GetExplTargetUnit();

		if (target != null)
			if (GetCaster() == GetOriginalCaster())
				GetCaster().AddAura(HunterSpells.GLAIVE_TOSS_AURA, target);
	}

	public void OnHit()
	{
		if (GetSpellInfo().Id == HunterSpells.GLAIVE_TOSS_RIGHT)
		{
			var caster = GetCaster();

			if (caster != null)
			{
				var target = GetHitUnit();

				if (target != null)
					if (caster == GetOriginalCaster())
						target.CastSpell(caster, HunterSpells.GLAIVE_TOSS_LEFT, new CastSpellExtraArgs(TriggerCastFlags.FullMask).SetOriginalCaster(caster.GetGUID()));
			}
		}
		else
		{
			var caster = GetCaster();

			if (caster != null)
			{
				var target = GetHitUnit();

				if (target != null)
					if (caster == GetOriginalCaster())
						target.CastSpell(caster, HunterSpells.GLAIVE_TOSS_RIGHT, new CastSpellExtraArgs(TriggerCastFlags.FullMask).SetOriginalCaster(caster.GetGUID()));
			}
		}
	}
}