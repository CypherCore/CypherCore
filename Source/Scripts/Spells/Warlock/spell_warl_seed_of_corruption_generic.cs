// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Warlock
{
	// 32863 - Seed of Corruption
	// 36123 - Seed of Corruption
	// 38252 - Seed of Corruption
	// 39367 - Seed of Corruption
	// 44141 - Seed of Corruption
	// 70388 - Seed of Corruption
	[SpellScript(new uint[]
	             {
		             32863, 36123, 38252, 39367, 44141, 70388
	             })] // Monster spells, triggered only on amount drop (not on death)
	internal class spell_warl_seed_of_corruption_generic : AuraScript, IHasAuraEffects
	{
		public List<IAuraEffectHandler> AuraEffects { get; } = new();

		public override bool Validate(SpellInfo spellInfo)
		{
			return ValidateSpellInfo(WarlockSpells.SEED_OF_CORRUPTION_GENERIC);
		}

		public override void Register()
		{
			AuraEffects.Add(new AuraEffectProcHandler(HandleProc, 1, AuraType.Dummy, AuraScriptHookType.EffectProc));
		}

		private void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
		{
			PreventDefaultAction();
			var damageInfo = eventInfo.GetDamageInfo();

			if (damageInfo == null ||
			    damageInfo.GetDamage() == 0)
				return;

			var amount = aurEff.GetAmount() - (int)damageInfo.GetDamage();

			if (amount > 0)
			{
				aurEff.SetAmount(amount);

				return;
			}

			Remove();

			var caster = GetCaster();

			if (!caster)
				return;

			caster.CastSpell(eventInfo.GetActionTarget(), WarlockSpells.SEED_OF_CORRUPTION_GENERIC, new CastSpellExtraArgs(aurEff));
		}
	}
}