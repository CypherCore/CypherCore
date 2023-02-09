using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Warrior
{
	[Script] // 32215 - Victorious State
	internal class spell_warr_victorious_state : AuraScript, IHasAuraEffects
	{
		public List<IAuraEffectHandler> AuraEffects { get; } = new();

		public override bool Validate(SpellInfo spellInfo)
		{
			return ValidateSpellInfo(WarriorSpells.IMPENDING_VICTORY);
		}

		public override void Register()
		{
			AuraEffects.Add(new AuraEffectProcHandler(HandleOnProc, 0, AuraType.ProcTriggerSpell, AuraScriptHookType.EffectProc));
		}

		private void HandleOnProc(AuraEffect aurEff, ProcEventInfo procInfo)
		{
			if (procInfo.GetActor().GetTypeId() == TypeId.Player &&
			    procInfo.GetActor().ToPlayer().GetPrimarySpecialization() == TalentSpecialization.WarriorFury)
				PreventDefaultAction();

			procInfo.GetActor().GetSpellHistory().ResetCooldown(WarriorSpells.IMPENDING_VICTORY, true);
		}
	}
}