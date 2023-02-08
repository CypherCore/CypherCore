using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.DeathKnight;

[SpellScript(new uint[] { 48263, 48265, 48266 })]
public class spell_dk_presence_AuraScript : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects => new List<IAuraEffectHandler>();

	public override bool Validate(SpellInfo UnnamedParameter)
	{
		if (!Global.SpellMgr.HasSpellInfo(DeathKnightSpells.SPELL_DK_FROST_PRESENCE, Difficulty.None) || 
		    !Global.SpellMgr.HasSpellInfo(DeathKnightSpells.SPELL_DK_UNHOLY_PRESENCE, Difficulty.None) || 
		    !Global.SpellMgr.HasSpellInfo(DeathKnightSpells.SPELL_DK_IMPROVED_FROST_PRESENCE, Difficulty.None) || 
		    !Global.SpellMgr.HasSpellInfo(DeathKnightSpells.SPELL_DK_IMPROVED_UNHOLY_PRESENCE, Difficulty.None) || 
		    !Global.SpellMgr.HasSpellInfo(DeathKnightSpells.SPELL_DK_IMPROVED_UNHOLY_PRESENCE_TRIGGERED, Difficulty.None) || 
		    !Global.SpellMgr.HasSpellInfo(DeathKnightSpells.SPELL_DK_IMPROVED_FROST_PRESENCE_TRIGGERED, Difficulty.None))
		{
			return false;
		}

		return true;
	}

	private void HandleImprovedFrostPresence(AuraEffect UnnamedParameter, AuraEffectHandleModes UnnamedParameter2)
	{
		Unit       target    = GetTarget();
		AuraEffect impAurEff = target.GetAuraEffect(DeathKnightSpells.SPELL_DK_IMPROVED_FROST_PRESENCE, 0);
		if (impAurEff != null)
		{
			impAurEff.SetAmount(impAurEff.CalculateAmount(GetCaster()));
		}
	}

	private void HandleImprovedUnholyPresence(AuraEffect aurEff, AuraEffectHandleModes UnnamedParameter)
	{
		Unit       target    = GetTarget();
		AuraEffect impAurEff = target.GetAuraEffect(DeathKnightSpells.SPELL_DK_IMPROVED_UNHOLY_PRESENCE, 0);
		if (impAurEff != null) 
		{
			if (!target.HasAura(DeathKnightSpells.SPELL_DK_IMPROVED_UNHOLY_PRESENCE_TRIGGERED))
			{
				target.CastSpell(target, DeathKnightSpells.SPELL_DK_IMPROVED_UNHOLY_PRESENCE_TRIGGERED, new CastSpellExtraArgs(TriggerCastFlags.FullMask).AddSpellMod(SpellValueMod.BasePoint0, (int)impAurEff.GetAmount()).SetTriggeringAura(aurEff));
			}
		}
	}

	private void HandleEffectRemove(AuraEffect UnnamedParameter, AuraEffectHandleModes UnnamedParameter2)
	{
		Unit       target    = GetTarget();
		AuraEffect impAurEff = target.GetAuraEffect(DeathKnightSpells.SPELL_DK_IMPROVED_FROST_PRESENCE, 0);
		if (impAurEff != null)
		{
			impAurEff.SetAmount(0);
		}
		target.RemoveAura(DeathKnightSpells.SPELL_DK_IMPROVED_UNHOLY_PRESENCE_TRIGGERED);
	}

	public override void Register()
	{
		if (ScriptSpellId == DeathKnightSpells.SPELL_DK_FROST_PRESENCE)
		{
			AuraEffects.Add(new AuraEffectApplyHandler(HandleImprovedFrostPresence, 0, AuraType.Any, AuraEffectHandleModes.Real));
		}
		if (ScriptSpellId == DeathKnightSpells.SPELL_DK_UNHOLY_PRESENCE)
		{
			AuraEffects.Add(new AuraEffectApplyHandler(HandleImprovedUnholyPresence, 0, AuraType.Any, AuraEffectHandleModes.Real));
		}

		AuraEffects.Add(new AuraEffectApplyHandler(HandleEffectRemove, 0, AuraType.Any, AuraEffectHandleModes.Real, AuraScriptHookType.EffectAfterRemove));
	}
}