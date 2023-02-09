using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Generic;

[Script]
internal class spell_steal_essence_visual : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects { get; } = new();

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectApplyHandler(HandleRemove, 0, AuraType.Dummy, AuraEffectHandleModes.Real, AuraScriptHookType.EffectAfterRemove));
	}

	private void HandleRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
	{
		Unit caster = GetCaster();

		if (caster != null)
		{
			caster.CastSpell(caster, GenericSpellIds.CreateToken, true);
			Creature soulTrader = caster.ToCreature();

			soulTrader?.GetAI().Talk(TextIds.SayCreateToken);
		}
	}
}