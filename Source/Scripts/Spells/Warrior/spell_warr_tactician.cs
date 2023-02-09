using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Warrior
{
	//184783
	[SpellScript(184783)]
	public class spell_warr_tactician : AuraScript, IHasAuraEffects
	{
		public List<IAuraEffectHandler> AuraEffects => new();

		private void HandleEffectProc(AuraEffect UnnamedParameter, ProcEventInfo procInfo)
		{
			PreventDefaultAction();
			var rageSpent = 0;

			var caster = GetCaster();

			if (caster != null)
				if (procInfo.GetSpellInfo() != null)
				{
					foreach (var cost in procInfo.GetSpellInfo().CalcPowerCost(caster, procInfo.GetSpellInfo().GetSchoolMask()))
					{
						if (cost.Power != PowerType.Rage)
							continue;

						rageSpent = cost.Amount;
					}

					if (RandomHelper.randChance((rageSpent / 10) * 1.40))
					{
						caster.GetSpellHistory().ResetCooldown(WarriorSpells.COLOSSUS_SMASH, true);
						caster.GetSpellHistory().ResetCooldown(WarriorSpells.MORTAL_STRIKE, true);
						caster.CastSpell(caster, WarriorSpells.TACTICIAN_CD, true);
					}
				}
		}

		public override void Register()
		{
			AuraEffects.Add(new AuraEffectProcHandler(HandleEffectProc, 0, AuraType.ProcTriggerSpell, AuraScriptHookType.EffectProc));
		}
	}
}