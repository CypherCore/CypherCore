// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Warrior
{
	//214871 - Odyn's fury
	[SpellScript(214871)]
	internal class spell_warr_odyns_fury : AuraScript, IHasAuraEffects
	{
		public List<IAuraEffectHandler> AuraEffects { get; } = new List<IAuraEffectHandler>();

		private void Absorb(AuraEffect UnnamedParameter, DamageInfo UnnamedParameter2, ref double absorbAmount)
		{
			absorbAmount = 0;
		}

		public override void Register()
		{
			AuraEffects.Add(new AuraEffectAbsorbHandler(Absorb, 0));
		}
	}
}