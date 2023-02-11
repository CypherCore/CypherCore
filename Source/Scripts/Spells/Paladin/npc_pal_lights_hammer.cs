﻿using Framework.Constants;
using Game.AI;
using Game.Entities;
using Game.Scripting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scripts.Spells.Paladin
{
    // Light's Hammer
    // NPC Id - 59738
    [CreatureScript(59738)]
    public class npc_pal_lights_hammer : ScriptedAI
    {
        public npc_pal_lights_hammer(Creature creature) : base(creature)
        {
        }

        public override void Reset()
        {
            me.CastSpell(me, PaladinSpells.SPELL_PALADIN_LIGHT_HAMMER_COSMETIC, true);
            me.SetUnitFlag(UnitFlags.NonAttackable | UnitFlags.Uninteractible | UnitFlags.RemoveClientControl);
        }
    }
}