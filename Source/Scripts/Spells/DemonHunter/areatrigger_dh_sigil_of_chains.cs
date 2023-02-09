using Game.AI;
using Game.Entities;
using Game.Scripting;
using Game.Spells;

namespace Scripts.Spells.DemonHunter;

[Script] // 202138 - Sigil of Chains
internal class areatrigger_dh_sigil_of_chains : AreaTriggerAI
{
	public areatrigger_dh_sigil_of_chains(AreaTrigger at) : base(at)
	{
	}

	public override void OnRemove()
	{
		var caster = at.GetCaster();

		if (caster != null)
		{
			caster.CastSpell(at.GetPosition(), DemonHunterSpells.SigilOfChainsVisual, new CastSpellExtraArgs());
			caster.CastSpell(at.GetPosition(), DemonHunterSpells.SigilOfChainsTargetSelect, new CastSpellExtraArgs());
		}
	}
}