using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Warlock
{
	[SpellScript(37377, "spell_warl_t4_2p_bonus_shadow", false, WarlockSpells.FLAMESHADOW)] // 37377 - Shadowflame
	[SpellScript(39437, "spell_warl_t4_2p_bonus_fire", false, WarlockSpells.SHADOWFLAME)]   // 39437 - Shadowflame Hellfire and RoF
	internal class spell_warl_t4_2p_bonus : AuraScript, IHasAuraEffects
	{
		private readonly uint _triggerSpell;

		public spell_warl_t4_2p_bonus(uint triggerSpell)
		{
			_triggerSpell = triggerSpell;
		}

		public List<IAuraEffectHandler> AuraEffects { get; } = new();

		public override bool Validate(SpellInfo spellInfo)
		{
			return ValidateSpellInfo(_triggerSpell);
		}

		public override void Register()
		{
			AuraEffects.Add(new AuraEffectProcHandler(HandleProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
		}

		private void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
		{
			PreventDefaultAction();
			var caster = eventInfo.GetActor();
			caster.CastSpell(caster, _triggerSpell, new CastSpellExtraArgs(aurEff));
		}
	}
}