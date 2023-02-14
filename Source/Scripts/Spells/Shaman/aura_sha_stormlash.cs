// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Shaman
{
	// 195255 - Stormlash
	[SpellScript(195255)]
	public class aura_sha_stormlash : AuraScript, IHasAuraEffects
	{
		public List<IAuraEffectHandler> AuraEffects { get; } = new();

		private void HandleProc(AuraEffect UnnamedParameter, ProcEventInfo UnnamedParameter2)
		{
			var caster = GetCaster();

			if (caster != null)
				caster.CastSpell(caster, ShamanSpells.SPELL_SHAMAN_STORMLASH_BUFF, true);
		}

		public override void Register()
		{
			AuraEffects.Add(new AuraEffectProcHandler(HandleProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
		}
	}
}