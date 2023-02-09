using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Warlock
{
	// 212580 - Eye of the Observer
	[SpellScript(212580)]
	public class spell_warlock_eye_of_the_observer : AuraScript, IHasAuraEffects
	{
		public List<IAuraEffectHandler> AuraEffects => new();

		private void OnProc(AuraEffect UnnamedParameter, ProcEventInfo eventInfo)
		{
			PreventDefaultAction();
			var caster = GetCaster();
			var actor  = eventInfo.GetActor();

			if (caster == null || actor == null)
				return;

			caster.CastSpell(actor, WarlockSpells.LASERBEAM, new CastSpellExtraArgs(TriggerCastFlags.FullMask).AddSpellMod(SpellValueMod.BasePoint0, (int)actor.CountPctFromMaxHealth(5)));
		}

		public override void Register()
		{
			AuraEffects.Add(new AuraEffectProcHandler(OnProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
		}
	}
}