﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Framework.Constants;
using Game.AI;
using Game.Entities;
using Game.Maps;
using Game.Scripting;
using Game.Spells;
using static Scripts.EasternKingdoms.Deadmines.Bosses.boss_foe_reaper_5000;

namespace Scripts.EasternKingdoms.Deadmines.NPC
{
    [CreatureScript(47404)]
    public class npc_defias_watcher : ScriptedAI
    {
        public npc_defias_watcher(Creature creature) : base(creature)
        {
            Instance = creature.GetInstanceScript();
            Status = false;
        }

        public InstanceScript Instance;
        public bool Status;

        public override void Reset()
        {
            if (!me)
            {
                return;
            }

            me.SetPower(PowerType.Energy, 100);
            me.SetMaxPower(PowerType.Energy, 100);
            me.SetPowerType(PowerType.Energy);
            if (Status == true)
            {
                if (!me.HasAura(eSpell.SPELL_ON_FIRE))
                {
                    me.AddAura(eSpell.SPELL_ON_FIRE, me);
                }
                me.SetFaction(35);
            }
        }

        public override void JustEnteredCombat(Unit who)
        {
        }

        public override void JustDied(Unit killer)
        {
            if (!me || Status == true)
            {
                return;
            }

            Energizing();
        }

        public void Energizing()
        {
            Status = true;
            me.SetHealth(15);
            me.SetRegenerateHealth(false);
            me.SetFaction(35);
            me.AddAura(eSpell.SPELL_ON_FIRE, me);
            me.CastSpell(me, eSpell.SPELL_ON_FIRE);
            me.SetInCombatWithZone();
 
            Creature reaper = me.FindNearestCreature(DMCreatures.NPC_FOE_REAPER_5000, 200.0f);
            if (reaper != null)
            {
                me.CastSpell(reaper, eSpell.SPELL_ENERGIZE);
            }
        }

        public override void DamageTaken(Unit attacker, ref uint damage, DamageEffectType damageType, SpellInfo spellInfo = null)
        {
            if (!me || damage <= 0 || Status == true)
            {
                return;
            }

            if (me.GetHealth() - damage <= me.GetMaxHealth() * 0.10)
            {
                damage = 0;
                Energizing();
            }
        }

        public override void UpdateAI(uint diff)
        {
            if (!UpdateVictim())
            {
                return;
            }

            DoMeleeAttackIfReady();
        }
    }
}