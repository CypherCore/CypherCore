using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Warrior
{
    // Ravager - 152277
    // Ravager - 228920
    [SpellScript(new uint[] { 152277, 228920 })]
    public class aura_warr_ravager : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> Effects => new List<IAuraEffectHandler>();

        private void OnApply(AuraEffect UnnamedParameter, AuraEffectHandleModes UnnamedParameter2)
        {
            Player player = GetTarget().ToPlayer();
            if (player != null)
            {
                if (player.GetPrimarySpecialization() == (uint)TalentSpecialization.WarriorProtection)
                {
                    player.CastSpell(player, WarriorSpells.RAVAGER_PARRY, true);
                }
            }
        }

        private void OnTick(AuraEffect UnnamedParameter)
        {
            Creature creature = GetTarget().GetSummonedCreatureByEntry(WarriorSpells.NPC_WARRIOR_RAVAGER);
            if (creature != null)
            {
                GetTarget().CastSpell(creature.GetPosition(), WarriorSpells.RAVAGER_DAMAGE, true);
            }
        }

        public override void Register()
        {
            Effects.Add(new EffectApplyHandler(this.OnApply, 0, AuraType.Dummy, AuraEffectHandleModes.RealOrReapplyMask));
            Effects.Add(new EffectPeriodicHandler(this.OnTick, 2, AuraType.PeriodicDummy));
        }
    }
}
