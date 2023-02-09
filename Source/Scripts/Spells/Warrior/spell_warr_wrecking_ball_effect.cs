using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Warrior
{
	// 215570 - Wrecking Ball
	[SpellScript(215570)]
	public class spell_warr_wrecking_ball_effect : AuraScript, IHasAuraEffects
	{
		public List<IAuraEffectHandler> AuraEffects => new();

		private void HandleProc(AuraEffect UnnamedParameter, ProcEventInfo UnnamedParameter2)
		{
			var caster = GetCaster();

			if (caster != null)
				if (caster.HasAura(WarriorSpells.WRECKING_BALL_EFFECT))
					caster.RemoveAura(WarriorSpells.WRECKING_BALL_EFFECT);
		}

		public override void Register()
		{
			AuraEffects.Add(new AuraEffectProcHandler(HandleProc, 0, AuraType.AddPctModifier, AuraScriptHookType.EffectProc));
		}
	}
}