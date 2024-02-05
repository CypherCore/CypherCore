// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.AI;
using Game.Entities;
using Game.Maps;
using Game.Scripting;
using Game.Spells;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Scripts.EasternKingdoms.Karazhan.Moroes
{
    struct SpellIds
    {
        public const uint Vanish = 29448;
        public const uint Garrote = 37066;
        public const uint Blind = 34694;
        public const uint Gouge = 29425;
        public const uint Frenzy = 37023;

        // Adds
        public const uint Manaburn = 29405;
        public const uint Mindfly = 29570;
        public const uint Swpain = 34441;
        public const uint Shadowform = 29406;

        public const uint Hammerofjustice = 13005;
        public const uint Judgementofcommand = 29386;
        public const uint Sealofcommand = 29385;

        public const uint Dispelmagic = 15090;
        public const uint Greaterheal = 29564;
        public const uint Holyfire = 29563;
        public const uint Pwshield = 29408;

        public const uint Cleanse = 29380;
        public const uint Greaterblessofmight = 29381;
        public const uint Holylight = 29562;
        public const uint Divineshield = 41367;

        public const uint Hamstring = 9080;
        public const uint Mortalstrike = 29572;
        public const uint Whirlwind = 29573;

        public const uint Disarm = 8379;
        public const uint Heroicstrike = 29567;
        public const uint Shieldbash = 11972;
        public const uint Shieldwall = 29390;
    }

    struct TextIds
    {
        public const uint SayAggro = 0;
        public const uint SaySpecial = 1;
        public const uint SayKill = 2;
        public const uint SayDeath = 3;
    }

    struct MiscConst
    {
        public const uint GroupNonEnrage = 1;

        public static Position[] Locations =
        {
            new Position(-10991.0f, -1884.33f, 81.73f, 0.614315f),
            new Position(-10989.4f, -1885.88f, 81.73f, 0.904913f),
            new Position(-10978.1f, -1887.07f, 81.73f, 2.035550f),
            new Position(-10975.9f, -1885.81f, 81.73f, 2.253890f)
        };

        public static uint[] Adds =
        {
            17007,
            19872,
            19873,
            19874,
            19875,
            19876,
        };
    }

    [Script]
    class boss_moroes : BossAI
    {
        public ObjectGuid[] AddGUID = new ObjectGuid[4];
        uint[] AddId = new uint[4];

        bool InVanish;
        bool Enrage;

        public boss_moroes(Creature creature) : base(creature, DataTypes.Moroes)
        {
            Initialize();
        }

        void Initialize()
        {
            Enrage = false;
            InVanish = false;
        }

        public override void Reset()
        {
            Initialize();
            if (me.IsAlive())
                SpawnAdds();

            instance.SetBossState(DataTypes.Moroes, EncounterState.NotStarted);
        }

        public override void JustEngagedWith(Unit who)
        {
            base.JustEngagedWith(who);

            _scheduler.Schedule(TimeSpan.FromSeconds(5), MiscConst.GroupNonEnrage, task =>
            {
                for (byte i = 0; i < 4; ++i)
                {
                    if (!AddGUID[i].IsEmpty())
                    {
                        Creature temp = ObjectAccessor.GetCreature(me, AddGUID[i]);
                        if (temp != null && temp.IsAlive())
                            if (temp.GetVictim() == null)
                                temp.GetAI().AttackStart(me.GetVictim());
                    }
                }
                task.Repeat();
            });
            _scheduler.Schedule(TimeSpan.FromSeconds(23), MiscConst.GroupNonEnrage, task =>
            {
                DoCastVictim(SpellIds.Gouge);
                task.Repeat(TimeSpan.FromSeconds(40));
            });
            _scheduler.Schedule(TimeSpan.FromSeconds(30), MiscConst.GroupNonEnrage, task =>
            {
                DoCast(me, SpellIds.Vanish);
                me.SetCanMelee(false);
                InVanish = true;

                task.Schedule(TimeSpan.FromSeconds(5), garroteTask =>
                {
                    Talk(TextIds.SaySpecial);

                    Unit target = SelectTarget(SelectTargetMethod.Random, 0, 100, true);
                    if (target != null)
                        target.CastSpell(target, SpellIds.Garrote, true);

                    InVanish = false;
                    me.SetCanMelee(true);
                });

                task.Repeat();
            });
            _scheduler.Schedule(TimeSpan.FromSeconds(35), MiscConst.GroupNonEnrage, task =>
            {
                Unit target = SelectTarget(SelectTargetMethod.MinDistance, 0, 0.0f, true, false);
                if (target != null)
                    DoCast(target, SpellIds.Blind);
                task.Repeat(TimeSpan.FromSeconds(40));
            });

            Talk(TextIds.SayAggro);
            AddsAttack();
        }

        public override void KilledUnit(Unit victim)
        {
            Talk(TextIds.SayKill);
        }

        public override void JustDied(Unit killer)
        {
            Talk(TextIds.SayDeath);

            base.JustDied(killer);

            DeSpawnAdds();

            //remove aura from spell Garrote when Moroes dies
            instance.DoRemoveAurasDueToSpellOnPlayers(SpellIds.Garrote);
        }

        void SpawnAdds()
        {
            DeSpawnAdds();

            if (isAddlistEmpty())
            {
                List<uint> AddList = MiscConst.Adds.ToList();
                AddList.RandomResize(4);
                
                for (var i = 0; i < 4; ++i)
                {
                    Creature creature = me.SummonCreature(AddList[i], MiscConst.Locations[i], TempSummonType.CorpseTimedDespawn, TimeSpan.FromSeconds(10));
                    if (creature != null)
                    {
                        AddGUID[i] = creature.GetGUID();
                        AddId[i] = AddList[i];
                    }
                }
            }
            else
            {
                for (byte i = 0; i < 4; ++i)
                {
                    Creature creature = me.SummonCreature(AddId[i], MiscConst.Locations[i], TempSummonType.CorpseTimedDespawn, TimeSpan.FromSeconds(10));
                    if (creature != null)
                        AddGUID[i] = creature.GetGUID();
                }
            }
        }

        bool isAddlistEmpty()
        {
            for (byte i = 0; i < 4; ++i)
                if (AddId[i] == 0)
                    return true;

            return false;
        }

        void DeSpawnAdds()
        {
            for (byte i = 0; i < 4; ++i)
            {
                if (!AddGUID[i].IsEmpty())
                {
                    Creature temp = ObjectAccessor.GetCreature(me, AddGUID[i]);
                    if (temp != null)
                        temp.DespawnOrUnsummon();
                }
            }
        }

        void AddsAttack()
        {
            for (byte i = 0; i < 4; ++i)
            {
                if (!AddGUID[i].IsEmpty())
                {
                    Creature temp = ObjectAccessor.GetCreature((me), AddGUID[i]);
                    if (temp != null && temp.IsAlive())
                    {
                        temp.GetAI().AttackStart(me.GetVictim());
                        DoZoneInCombat(temp);
                    }
                    else
                        EnterEvadeMode();
                }
            }
        }

        public override void UpdateAI(uint diff)
        {
            if (!UpdateVictim())
                return;

            if (!Enrage && HealthBelowPct(30))
            {
                DoCast(me, SpellIds.Frenzy);
                Enrage = true;
                _scheduler.CancelGroup(MiscConst.GroupNonEnrage);
            }

            _scheduler.Update(diff);
        }
    }

    class boss_moroes_guest : ScriptedAI
    {
        InstanceScript instance;

        ObjectGuid[] GuestGUID = new ObjectGuid[4];

        public boss_moroes_guest(Creature creature) : base(creature)
        {
            instance = creature.GetInstanceScript();
        }

        public override void Reset()
        {
            instance.SetBossState(DataTypes.Moroes, EncounterState.NotStarted);
        }

        public void AcquireGUID()
        {
            Creature Moroes = ObjectAccessor.GetCreature(me, instance.GetGuidData(DataTypes.Moroes));
            if (Moroes != null)
            {
                for (byte i = 0; i < 4; ++i)
                {
                    ObjectGuid Guid = Moroes.GetAI<boss_moroes>().AddGUID[i];
                    if (!Guid.IsEmpty())
                        GuestGUID[i] = Guid;
                }
            }
        }

        public Unit SelectGuestTarget()
        {
            ObjectGuid TempGUID = GuestGUID[RandomHelper.Rand32() % 4];
            if (!TempGUID.IsEmpty())
            {
                Unit unit = Global.ObjAccessor.GetUnit(me, TempGUID);
                if (unit != null && unit.IsAlive())
                    return unit;
            }

            return me;
        }

        public override void UpdateAI(uint diff)
        {
            if (instance.GetBossState(DataTypes.Moroes) != EncounterState.InProgress)
                EnterEvadeMode();
        }
    }

    [Script]
    class boss_baroness_dorothea_millstipe : boss_moroes_guest
    {
        //Shadow Priest
        public boss_baroness_dorothea_millstipe(Creature creature) : base(creature)
        {
            Initialize();
        }

        void Initialize()
        {
            ManaBurn_Timer = 7000;
            MindFlay_Timer = 1000;
            ShadowWordPain_Timer = 6000;
        }

        uint ManaBurn_Timer;
        uint MindFlay_Timer;
        uint ShadowWordPain_Timer;

        public override void Reset()
        {
            Initialize();

            DoCast(me, SpellIds.Shadowform, new CastSpellExtraArgs(true));

            base.Reset();
        }

        public override void UpdateAI(uint diff)
        {
            if (!UpdateVictim())
                return;

            base.UpdateAI(diff);

            if (MindFlay_Timer <= diff)
            {
                DoCastVictim(SpellIds.Mindfly);
                MindFlay_Timer = 12000;                         // 3 sec channeled
            }
            else MindFlay_Timer -= diff;

            if (ManaBurn_Timer <= diff)
            {
                Unit target = SelectTarget(SelectTargetMethod.Random, 0, 100, true);
                if (target != null)
                    if (target.GetPowerType() == PowerType.Mana)
                        DoCast(target, SpellIds.Manaburn);
                ManaBurn_Timer = 5000;                          // 3 sec cast
            }
            else ManaBurn_Timer -= diff;

            if (ShadowWordPain_Timer <= diff)
            {
                Unit target = SelectTarget(SelectTargetMethod.Random, 0, 100, true);
                if (target != null)
                {
                    DoCast(target, SpellIds.Swpain);
                    ShadowWordPain_Timer = 7000;
                }
            }
            else ShadowWordPain_Timer -= diff;
        }
    }

    [Script]
    class boss_baron_rafe_dreuger : boss_moroes_guest
    {
        //Retr Pally
        public boss_baron_rafe_dreuger(Creature creature) : base(creature)
        {
            Initialize();
        }

        void Initialize()
        {
            HammerOfJustice_Timer = 1000;
            SealOfCommand_Timer = 7000;
            JudgementOfCommand_Timer = SealOfCommand_Timer + 29000;
        }

        uint HammerOfJustice_Timer;
        uint SealOfCommand_Timer;
        uint JudgementOfCommand_Timer;

        public override void Reset()
        {
            Initialize();

            base.Reset();
        }

        public override void UpdateAI(uint diff)
        {
            if (!UpdateVictim())
                return;

            base.UpdateAI(diff);

            if (SealOfCommand_Timer <= diff)
            {
                DoCast(me, SpellIds.Sealofcommand);
                SealOfCommand_Timer = 32000;
                JudgementOfCommand_Timer = 29000;
            }
            else SealOfCommand_Timer -= diff;

            if (JudgementOfCommand_Timer <= diff)
            {
                DoCastVictim(SpellIds.Judgementofcommand);
                JudgementOfCommand_Timer = SealOfCommand_Timer + 29000;
            }
            else JudgementOfCommand_Timer -= diff;

            if (HammerOfJustice_Timer <= diff)
            {
                DoCastVictim(SpellIds.Hammerofjustice);
                HammerOfJustice_Timer = 12000;
            }
            else HammerOfJustice_Timer -= diff;
        }
    }

    [Script]
    class boss_lady_catriona_von_indi : boss_moroes_guest
    {
        //Holy Priest
        public boss_lady_catriona_von_indi(Creature creature) : base(creature)
        {
            Initialize();
        }

        void Initialize()
        {
            DispelMagic_Timer = 11000;
            GreaterHeal_Timer = 1500;
            HolyFire_Timer = 5000;
            PowerWordShield_Timer = 1000;
        }

        uint DispelMagic_Timer;
        uint GreaterHeal_Timer;
        uint HolyFire_Timer;
        uint PowerWordShield_Timer;

        public override void Reset()
        {
            Initialize();

            AcquireGUID();

            base.Reset();
        }

        public override void UpdateAI(uint diff)
        {
            if (!UpdateVictim())
                return;

            base.UpdateAI(diff);

            if (PowerWordShield_Timer <= diff)
            {
                DoCast(me, SpellIds.Pwshield);
                PowerWordShield_Timer = 15000;
            }
            else PowerWordShield_Timer -= diff;

            if (GreaterHeal_Timer <= diff)
            {
                Unit target = SelectGuestTarget();

                DoCast(target, SpellIds.Greaterheal);
                GreaterHeal_Timer = 17000;
            }
            else GreaterHeal_Timer -= diff;

            if (HolyFire_Timer <= diff)
            {
                DoCastVictim(SpellIds.Holyfire);
                HolyFire_Timer = 22000;
            }
            else HolyFire_Timer -= diff;

            if (DispelMagic_Timer <= diff)
            {
                Unit target = RandomHelper.RAND(SelectGuestTarget(), SelectTarget(SelectTargetMethod.Random, 0, 100, true));
                if (target != null)
                    DoCast(target, SpellIds.Dispelmagic);

                DispelMagic_Timer = 25000;
            }
            else DispelMagic_Timer -= diff;
        }
    }

    [Script]
    class boss_lady_keira_berrybuck : boss_moroes_guest
    {
        //Holy Pally
        public boss_lady_keira_berrybuck(Creature creature) : base(creature)
        {
            Initialize();
        }

        void Initialize()
        {
            Cleanse_Timer = 13000;
            GreaterBless_Timer = 1000;
            HolyLight_Timer = 7000;
            DivineShield_Timer = 31000;
        }

        uint Cleanse_Timer;
        uint GreaterBless_Timer;
        uint HolyLight_Timer;
        uint DivineShield_Timer;

        public override void Reset()
        {
            Initialize();

            AcquireGUID();

            base.Reset();
        }

        public override void UpdateAI(uint diff)
        {
            if (!UpdateVictim())
                return;

            base.UpdateAI(diff);

            if (DivineShield_Timer <= diff)
            {
                DoCast(me, SpellIds.Divineshield);
                DivineShield_Timer = 31000;
            }
            else DivineShield_Timer -= diff;

            if (HolyLight_Timer <= diff)
            {
                Unit target = SelectGuestTarget();

                DoCast(target, SpellIds.Holylight);
                HolyLight_Timer = 10000;
            }
            else HolyLight_Timer -= diff;

            if (GreaterBless_Timer <= diff)
            {
                Unit target = SelectGuestTarget();

                DoCast(target, SpellIds.Greaterblessofmight);

                GreaterBless_Timer = 50000;
            }
            else GreaterBless_Timer -= diff;

            if (Cleanse_Timer <= diff)
            {
                Unit target = SelectGuestTarget();

                DoCast(target, SpellIds.Cleanse);

                Cleanse_Timer = 10000;
            }
            else Cleanse_Timer -= diff;
        }
    }

    [Script]
    class boss_lord_robin_daris : boss_moroes_guest
    {
        //Arms Warr
        public boss_lord_robin_daris(Creature creature) : base(creature)
        {
            Initialize();
        }

        void Initialize()
        {
            Hamstring_Timer = 7000;
            MortalStrike_Timer = 10000;
            WhirlWind_Timer = 21000;
        }

        uint Hamstring_Timer;
        uint MortalStrike_Timer;
        uint WhirlWind_Timer;

        public override void Reset()
        {
            Initialize();

            base.Reset();
        }

        public override void UpdateAI(uint diff)
        {
            if (!UpdateVictim())
                return;

            base.UpdateAI(diff);

            if (Hamstring_Timer <= diff)
            {
                DoCastVictim(SpellIds.Hamstring);
                Hamstring_Timer = 12000;
            }
            else Hamstring_Timer -= diff;

            if (MortalStrike_Timer <= diff)
            {
                DoCastVictim(SpellIds.Mortalstrike);
                MortalStrike_Timer = 18000;
            }
            else MortalStrike_Timer -= diff;

            if (WhirlWind_Timer <= diff)
            {
                DoCast(me, SpellIds.Whirlwind);
                WhirlWind_Timer = 21000;
            }
            else WhirlWind_Timer -= diff;
        }
    }

    [Script]
    class boss_lord_crispin_ference : boss_moroes_guest
    {
        //Arms Warr
        public boss_lord_crispin_ference(Creature creature) : base(creature)
        {
            Initialize();
        }

        void Initialize()
        {
            Disarm_Timer = 6000;
            HeroicStrike_Timer = 10000;
            ShieldBash_Timer = 8000;
            ShieldWall_Timer = 4000;
        }

        uint Disarm_Timer;
        uint HeroicStrike_Timer;
        uint ShieldBash_Timer;
        uint ShieldWall_Timer;

        public override void Reset()
        {
            Initialize();

            base.Reset();
        }

        public override void UpdateAI(uint diff)
        {
            if (!UpdateVictim())
                return;

            base.UpdateAI(diff);

            if (Disarm_Timer <= diff)
            {
                DoCastVictim(SpellIds.Disarm);
                Disarm_Timer = 12000;
            }
            else Disarm_Timer -= diff;

            if (HeroicStrike_Timer <= diff)
            {
                DoCastVictim(SpellIds.Heroicstrike);
                HeroicStrike_Timer = 10000;
            }
            else HeroicStrike_Timer -= diff;

            if (ShieldBash_Timer <= diff)
            {
                DoCastVictim(SpellIds.Shieldbash);
                ShieldBash_Timer = 13000;
            }
            else ShieldBash_Timer -= diff;

            if (ShieldWall_Timer <= diff)
            {
                DoCast(me, SpellIds.Shieldwall);
                ShieldWall_Timer = 21000;
            }
            else ShieldWall_Timer -= diff;
        }
    }
}