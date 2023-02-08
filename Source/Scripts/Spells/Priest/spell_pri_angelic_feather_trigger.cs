using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Priest;

[Script] // 121536 - Angelic Feather talent
internal class spell_pri_angelic_feather_trigger : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();

	public override bool Validate(SpellInfo spellInfo)
	{
		return ValidateSpellInfo(PriestSpells.AngelicFeatherAreatrigger);
	}

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleEffectDummy, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHit));
	}

	private void HandleEffectDummy(uint effIndex)
	{
		Position destPos = GetHitDest().GetPosition();
		float    radius  = GetEffectInfo().CalcRadius();

		// Caster is prioritary
		if (GetCaster().IsWithinDist2d(destPos, radius))
		{
			GetCaster().CastSpell(GetCaster(), PriestSpells.AngelicFeatherAura, true);
		}
		else
		{
			CastSpellExtraArgs args = new(TriggerCastFlags.FullMask);
			args.CastDifficulty = GetCastDifficulty();
			GetCaster().CastSpell(destPos, PriestSpells.AngelicFeatherAreatrigger, args);
		}
	}
}