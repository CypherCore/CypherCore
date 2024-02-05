// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

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

            _scheduler.Update(diff);
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
                if (razorgore != null)
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

