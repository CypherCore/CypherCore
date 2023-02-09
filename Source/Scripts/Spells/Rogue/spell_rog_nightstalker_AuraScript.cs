using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Rogue;

[SpellScript(14062)]
public class spell_rog_nightstalker_AuraScript : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects => new List<IAuraEffectHandler>();

	private void HandleRemove(AuraEffect UnnamedParameter, AuraEffectHandleModes UnnamedParameter2)
	{
		var caster = GetCaster();
		if (caster)
		{
			if (caster.HasAura(RogueSpells.SPELL_ROGUE_SPELL_NIGHTSTALKER_DAMAGE_DONE))
				caster.RemoveAura(RogueSpells.SPELL_ROGUE_SPELL_NIGHTSTALKER_DAMAGE_DONE);

			if (caster.HasAura(RogueSpells.SPELL_ROGUE_SHADOW_FOCUS_EFFECT))
			{
				caster.RemoveAura(RogueSpells.SPELL_ROGUE_SHADOW_FOCUS_EFFECT);
			}
		}
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectApplyHandler(HandleRemove, 0, AuraType.ModShapeshift, AuraEffectHandleModes.Real, AuraScriptHookType.EffectAfterRemove));
	}
}