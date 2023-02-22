// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Framework.Constants;
using Game.AI;
using Game.Entities;
using Game.Maps;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.EasternKingdoms.BlackrockMountain.BlackwingLair.Razorgore
{
    internal struct SpellIds
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

    internal struct TextIds
    {
        public const uint SayEggsBroken1 = 0;
        public const uint SayEggsBroken2 = 1;
        public const uint SayEggsBroken3 = 2;
        public const uint SayDeath = 3;
    }

    internal struct CreatureIds
    {
        public const uint EliteDrachkin = 12422;
        public const uint EliteWarrior = 12458;
        public const uint Warrior = 12416;
        public const uint Mage = 12420;
        public const uint Warlock = 12459;
    }

    internal struct GameObjectIds
    {
        public const uint Egg = 177807;
    }

    [Script]
    internal class boss_razorgore : BossAI
    {
        private bool secondPhase;

        public boss_razorgore(Creature creature) : base(creature, DataTypes.RazorgoreTheUntamed)
        {
            Initialize();
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

        public override void DoAction(int action)
        {
            if (action == BWLMisc.ActionPhaseTwo)
                DoChangePhase();
        }

        public override void DamageTaken(Unit who, ref double damage, DamageEffectType damageType, SpellInfo spellInfo = null)
        {
            // @todo this is wrong - razorgore should still take Damage, he should just nuke the whole room and respawn if he dies during P1
            if (!secondPhase)
                damage = 0;
        }

        public override void UpdateAI(uint diff)
        {
            if (!UpdateVictim())
                return;

            _scheduler.Update(diff, () => DoMeleeAttackIfReady());
        }

        private void Initialize()
        {
            secondPhase = false;
        }

        private void DoChangePhase()
        {
            _scheduler.Schedule(TimeSpan.FromSeconds(15),
                                task =>
                                {
                                    DoCastVictim(SpellIds.Cleave);
                                    task.Repeat(TimeSpan.FromSeconds(7), TimeSpan.FromSeconds(10));
                                });

            _scheduler.Schedule(TimeSpan.FromSeconds(35),
                                task =>
                                {
                                    DoCastVictim(SpellIds.Warstomp);
                                    task.Repeat(TimeSpan.FromSeconds(15), TimeSpan.FromSeconds(25));
                                });

            _scheduler.Schedule(TimeSpan.FromSeconds(7),
                                task =>
                                {
                                    DoCastVictim(SpellIds.Fireballvolley);
                                    task.Repeat(TimeSpan.FromSeconds(12), TimeSpan.FromSeconds(15));
                                });

            _scheduler.Schedule(TimeSpan.FromSeconds(12),
                                task =>
                                {
                                    DoCastVictim(SpellIds.Conflagration);
                                    task.Repeat(TimeSpan.FromSeconds(30));
                                });

            secondPhase = true;
            me.RemoveAllAuras();
            me.SetFullHealth();
        }
    }

    [Script]
    internal class go_orb_of_domination : GameObjectAI
    {
        private readonly InstanceScript instance;

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
    internal class spell_egg_event : SpellScript, ISpellOnHit
    {
        public void OnHit()
        {
            InstanceScript instance = GetCaster().GetInstanceScript();

            instance?.SetData(BWLMisc.DataEggEvent, (uint)EncounterState.Special);
        }
    }
}