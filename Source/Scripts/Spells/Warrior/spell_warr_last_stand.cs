// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Warrior
{
	/// Updated 8.3.7
	// 12975 - Last Stand
	[SpellScript(12975)]
	public class spell_warr_last_stand : SpellScript, IHasSpellEffects
	{
		public List<ISpellEffect> SpellEffects { get; } = new();

		public override bool Validate(SpellInfo UnnamedParameter)
		{
			return ValidateSpellInfo(WarriorSpells.LAST_STAND_TRIGGERED);
		}

		private void HandleDummy(uint UnnamedParameter)
		{
			var caster = GetCaster();
			var args   = new CastSpellExtraArgs(TriggerCastFlags.FullMask);
			args.AddSpellMod(SpellValueMod.BasePoint0, (int)caster.CountPctFromMaxHealth(GetEffectValue()));
			caster.CastSpell(caster, WarriorSpells.LAST_STAND_TRIGGERED, args);
		}

		public override void Register()
		{
			SpellEffects.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHit));
		}
	}
}