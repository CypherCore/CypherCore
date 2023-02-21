// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.DeathKnight;

[SpellScript(276023)]
public class spell_dk_harbinger_of_doom_aura : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects { get; } = new List<IAuraEffectHandler>();

	private void OnApply(AuraEffect UnnamedParameter, AuraEffectHandleModes UnnamedParameter2)
	{

		var caster = GetCaster();
		if (caster != null) {
			var player = caster.ToPlayer();
			if (player != null) {
				Spell spell = GetTarget().FindCurrentSpellBySpellId(DeathKnightSpells.DEATH_COIL_SUDDEN_DOOM);
				spell.m_spellInfo.ProcBasePPM = MathFunctions.CalculatePct(spell.m_spellInfo.ProcBasePPM, 100-30);
            }
		}
		
	}

    private void OnRemove(AuraEffect UnnamedParameter, AuraEffectHandleModes UnnamedParameter2)
    {

        Spell spell = GetTarget().FindCurrentSpellBySpellId(DeathKnightSpells.DEATH_COIL_SUDDEN_DOOM);
        spell.m_spellInfo.ProcBasePPM = Global.SpellMgr.GetSpellInfo(DeathKnightSpells.DEATH_COIL_SUDDEN_DOOM).ProcBasePPM;
    }

    public override void Register()
	{
        AuraEffects.Add(new AuraEffectApplyHandler(OnRemove, 0, AuraType.Dummy, AuraEffectHandleModes.Real, AuraScriptHookType.EffectRemove));
        AuraEffects.Add(new AuraEffectApplyHandler(OnApply, 0, AuraType.Dummy, AuraEffectHandleModes.Real));
    }
}