/*
 * Copyright (C) 2012-2020 CypherCore <http://github.com/CypherCore>
 * 
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */

using Framework.Constants;
using Game.AI;
using Game.Entities;
using Game.Maps;
using Game.Scripting;
using Game.Spells;
using System;

namespace Scripts.EasternKingdoms.BlackrockMountain.BlackwingLair.Razorgore
{
    struct SpellIds
    {
        // @todo orb uses the wrong spell, this needs sniffs
        public const uint Mindcontrol = 42013;
        public const uint Channel = 45537;
        public const uint EggDestroy = 19873;

        public const uint Cleave = 22540;
        public const uint Warstomp = 24375;
        public const uint Fireballvolley = 22425;
        public const uint Conflagration = 23023;
    }

    struct TextIds
    {
        public const uint SayEggsBroken1 = 0;
        public const uint SayEggsBroken2 = 1;
        public const uint SayEggsBroken3 = 2;
        public const uint SayDeath = 3;
    }

    struct CreatureIds
    {
        public const uint EliteDrachkin = 12422;
        public const uint EliteWarrior = 12458;
        public const uint Warrior = 12416;
        public const uint Mage = 12420;
        public const uint Warlock = 12459;
    }

    struct GameObjectIds
    {
        public const uint Egg = 177807;
    }

    [Script]
    class boss_razorgore : BossAI
    {
        bool secondPhase;

        public boss_razorgore(Creature creature) : base(creature, DataTypes.RazorgoreTheUntamed)
        {
            Initialize();
        }

        void Initialize()
        {
            secondPhase = false;
        }

        public override void Reset()
        {
            _Reset();

            Initialize();
            instance.SetData(BWLMisc.DataEggEvent, (uint)EncounterState.NotStarted);
        }

        public override void JustDied(Unit killer)
        {
            _JustDied();
            Talk(TextIds.SayDeath);

            instance.SetData(BWLMisc.DataEggEvent, (uint)EncounterState.NotStarted);
        }

        void DoChangePhase()
        {
            _scheduler.Schedule(TimeSpan.FromSeconds(15), task =>
            {
                DoCastVictim(SpellIds.Cleave);
                task.Repeat(TimeSpan.FromSeconds(7), TimeSpan.FromSeconds(10));
            });
            _scheduler.Schedule(TimeSpan.FromSeconds(35), task =>
            {
                DoCastVictim(SpellIds.Warstomp);
                task.Repeat(TimeSpan.FromSeconds(15), TimeSpan.FromSeconds(25));
            });
            _scheduler.Schedule(TimeSpan.FromSeconds(7), task =>
            {
                DoCastVictim(SpellIds.Fireballvolley);
                task.Repeat(TimeSpan.FromSeconds(12), TimeSpan.FromSeconds(15));
            });
            _scheduler.Schedule(TimeSpan.FromSeconds(12), task =>
            {
                DoCastVictim(SpellIds.Conflagration);
                task.Repeat(TimeSpan.FromSeconds(30));
            });

            secondPhase = true;
            me.RemoveAllAuras();
            me.SetFullHealth();
        }

        public override void DoAction(int action)
        {
            if (action == BWLMisc.ActionPhaseTwo)
                DoChangePhase();
        }

        public override void DamageTaken(Unit who, ref uint damage, DamageEffectType damageType, SpellInfo spellInfo = null)
        {
            // @todo this is wrong - razorgore should still take damage, he should just nuke the whole room and respawn if he dies during P1
            if (!secondPhase)
                damage = 0;
        }

        public override void UpdateAI(uint diff)
        {
            if (!UpdateVictim())
                return;

            _scheduler.Update(diff, () => DoMeleeAttackIfReady());
        }
    }

    [Script]
    class go_orb_of_domination : GameObjectAI
    {
        InstanceScript instance;

        public go_orb_of_domination(GameObject go) : base(go)
        {
            instance = go.GetInstanceScript();
        }

        public override bool OnGossipHello(Player player)
        {
            if (instance.GetData(BWLMisc.DataEggEvent) != (uint)EncounterState.Done)
            {
                Creature razorgore = instance.GetCreature(DataTypes.RazorgoreTheUntamed);
                if (razorgore)
                {
                    razorgore.Attack(player, true);
                    player.CastSpell(razorgore, SpellIds.Mindcontrol);
                }
            }
            return true;
        }
    }

    [Script] // 19873 - Destroy Egg
    class spell_egg_event : SpellScript
    {
        void HandleOnHit()
        {
            InstanceScript instance = GetCaster().GetInstanceScript();
            if (instance != null)
                instance.SetData(BWLMisc.DataEggEvent, (uint)EncounterState.Special);
        }

        public override void Register()
        {
            OnHit.Add(new HitHandler(HandleOnHit));
        }
    }
}

