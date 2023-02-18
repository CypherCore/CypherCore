// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Warrior
{
	//18499
	[SpellScript(18499)]
	public class spell_warr_berzerker_rage : AuraScript, IHasAuraEffects
	{
		public List<IAuraEffectHandler> AuraEffects { get; } = new List<IAuraEffectHandler>();

		private void HandleRemove(AuraEffect UnnamedParameter, AuraEffectHandleModes UnnamedParameter2)
		{
			var caster = GetCaster();
			caster.RemoveAura(WarriorSpells.UNCHACKLED_FURY);
		}

		public override void Register()
		{
			AuraEffects.Add(new AuraEffectApplyHandler(HandleRemove, 0, AuraType.AddPctModifier, AuraEffectHandleModes.Real, AuraScriptHookType.EffectRemove));
		}
	}
}