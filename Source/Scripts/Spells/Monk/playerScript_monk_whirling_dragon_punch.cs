using System;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IPlayer;
using Game.Spells;

namespace Scripts.Spells.Monk;

[Script]
public class playerScript_monk_whirling_dragon_punch : ScriptObjectAutoAdd, IPlayerOnCooldownEnd, IPlayerOnCooldownStart, IPlayerOnChargeRecoveryTimeStart
{
	public Class PlayerClass => Class.Monk;

	public playerScript_monk_whirling_dragon_punch() : base("playerScript_monk_whirling_dragon_punch")
	{
	}

	public void OnCooldownEnd(Player player, SpellInfo spellInfo, uint itemId, uint categoryId)
	{
		if (spellInfo.Id == MonkSpells.SPELL_MONK_FISTS_OF_FURY)
			player.RemoveAura(MonkSpells.SPELL_MONK_WHIRLING_DRAGON_PUNCH_CASTER_AURA);
	}

	public void OnCooldownStart(Player player, SpellInfo spellInfo, uint itemId, uint categoryId, TimeSpan cooldown, ref DateTime cooldownEnd, ref DateTime categoryEnd, ref bool onHold)
	{
		if (spellInfo.Id == MonkSpells.SPELL_MONK_FISTS_OF_FURY)
		{
			SpellInfo risingSunKickInfo = Global.SpellMgr.GetSpellInfo(MonkSpells.SPELL_MONK_RISING_SUN_KICK, Difficulty.None);
			ApplyCasterAura(player, (int)cooldown.TotalMilliseconds, player.GetSpellHistory().GetChargeRecoveryTime(risingSunKickInfo.ChargeCategoryId));
		}
	}

	public void OnChargeRecoveryTimeStart(Player player, uint chargeCategoryId, ref int chargeRecoveryTime)
	{
		SpellInfo risingSunKickInfo = Global.SpellMgr.GetSpellInfo(MonkSpells.SPELL_MONK_RISING_SUN_KICK, Difficulty.None);
		if (risingSunKickInfo.ChargeCategoryId == chargeCategoryId)
		{
			ApplyCasterAura(player, chargeRecoveryTime, (int)player.GetSpellHistory().GetRemainingCooldown(Global.SpellMgr.GetSpellInfo(MonkSpells.SPELL_MONK_FISTS_OF_FURY, Difficulty.None)).TotalMilliseconds);
		}
	}

	private void ApplyCasterAura(Player player, int cooldown1, int cooldown2)
	{
		if (cooldown1 > 0 && cooldown2 > 0)
		{
			uint whirlingDragonPunchAuraDuration = (uint)Math.Min(cooldown1, cooldown2);
			player.CastSpell(player, MonkSpells.SPELL_MONK_WHIRLING_DRAGON_PUNCH_CASTER_AURA, true);

			Aura aura = player.GetAura(MonkSpells.SPELL_MONK_WHIRLING_DRAGON_PUNCH_CASTER_AURA);
			if (aura != null)
			{
				aura.SetDuration(whirlingDragonPunchAuraDuration);
			}
		}
	}
}