using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Warlock
{
    // 3026 - Use Soulstone
    [SpellScript(3026)]
    public class spell_warlock_use_soulstone : SpellScript, IHasSpellEffects
    {
        public List<ISpellEffect> SpellEffects => new List<ISpellEffect>();

        private void HandleHit(uint effIndex)
        {
            PreventHitDefaultEffect(effIndex);
            Player player = GetCaster().ToPlayer();
            if (player == null)
            {
                return;
            }
            Unit originalCaster = GetOriginalCaster();

            // already have one active request
            if (player.IsResurrectRequested())
            {
                return;
            }

            int healthPct = GetSpellInfo().GetEffect(1).CalcValue(originalCaster);
            int manaPct = GetSpellInfo().GetEffect(0).CalcValue(originalCaster);

            var health = player.CountPctFromMaxHealth(healthPct);
            int mana = 0;
            if (player.GetMaxPower(PowerType.Mana) > 0)
            {
                mana = MathFunctions.CalculatePct(player.GetMaxPower(PowerType.Mana), manaPct);
            }

            player.ResurrectPlayer(0.0f);
            player.SetHealth(health);
            player.SetPower(PowerType.Mana, mana);
            player.SetPower(PowerType.Rage, 0);
            player.SetPower(PowerType.Energy, player.GetMaxPower(PowerType.Energy));
            player.SetPower(PowerType.Focus, 0);
            player.SpawnCorpseBones();
        }

        public override void Register()
        {
            SpellEffects.Add(new EffectHandler(HandleHit, 0, SpellEffectName.SelfResurrect, SpellScriptHookType.EffectHit));
        }
    }
}
