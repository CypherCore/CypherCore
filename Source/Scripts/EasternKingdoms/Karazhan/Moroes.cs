/*
 * Copyright (C) 2012-2019 CypherCore <http://github.com/CypherCore>
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
using System.Collections.Generic;

namespace Scripts.EasternKingdoms.Karazhan.Moroes
{
    struct Misc
    {
        public static Position[] Locations =
        {
            new Position(-10991.0f, -1884.33f, 81.73f, 0.614315f),
            new Position(-10989.4f, -1885.88f, 81.73f, 0.904913f),
            new Position(-10978.1f, -1887.07f, 81.73f, 2.035550f),
            new Position(-10975.9f, -1885.81f, 81.73f, 2.253890f),
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

    struct TextIds
    {
        public const uint Aggro = 0;
        public const uint Special = 1;
        public const uint Kill = 2;
        public const uint Death = 3;
    }

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

    [Script]
    public class boss_moroes : ScriptedAI
    {
        public boss_moroes(Creature creature) : base(creature)
        {
            instance = creature.GetInstanceScript();
        }

        public override void Reset()
        {
            Vanish_Timer = 30000;
            Blind_Timer = 35000;
            Gouge_Timer = 23000;
            Wait_Timer = 0;
            CheckAdds_Timer = 5000;

            Enrage = false;
            InVanish = false;
            if (me.IsAlive())
                SpawnAdds();

            instance.SetData(karazhanConst.BossMoroes, (uint)EncounterState.NotStarted);
        }

        void StartEvent()
        {
            instance.SetData(karazhanConst.BossMoroes, (uint)EncounterState.InProgress);

            DoZoneInCombat();
        }

        public override void EnterCombat(Unit who)
        {
            StartEvent();

            Talk(TextIds.Aggro);
            AddsAttack();
            DoZoneInCombat();
        }

        public override void KilledUnit(Unit victim)
        {
            Talk(TextIds.Kill);
        }

        public override void JustDied(Unit killer)
        {
            Talk(TextIds.Death);

            instance.SetData(karazhanConst.BossMoroes, (uint)EncounterState.Done);

            DeSpawnAdds();

            //remove aura from spell Garrote when Moroes dies
            instance.DoRemoveAurasDueToSpellOnPlayers(SpellIds.Garrote);
        }

        void SpawnAdds()
        {
            DeSpawnAdds();

            if (isAddlistEmpty())
            {
                List<uint> AddList = new List<uint>();

                for (byte i = 0; i < 6; ++i)
                    AddList.Add(Misc.Adds[i]);

                AddList.RandomResize(4);

                byte c = 0;
                for (var i = 0; i != AddList.Count && c < 4; ++i, ++c)
                {
                    uint entry = AddList[i];
                    Creature creature = me.SummonCreature(entry, Misc.Locations[c], TempSummonType.CorpseTimedDespawn, 10000);
                    if (creature)
                    {
                        AddGUID[c] = creature.GetGUID();
                        AddId[c] = entry;
                    }
                }
            }
            else
            {
                for (byte i = 0; i < 4; ++i)
                {
                    Creature creature = me.SummonCreature(AddId[i], Misc.Locations[i], TempSummonType.CorpseTimedDespawn, 10000);
                    if (creature)
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
                    if (temp)
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
                    if (temp && temp.IsAlive())
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

            if (instance.GetData(karazhanConst.BossMoroes) == 0)
            {
                EnterEvadeMode();
                return;
            }

            if (!Enrage && HealthBelowPct(30))
            {
                DoCast(me, SpellIds.Frenzy);
                Enrage = true;
            }

            if (CheckAdds_Timer <= diff)
            {
                for (byte i = 0; i < 4; ++i)
                {
                    if (!AddGUID[i].IsEmpty())
                    {
                        Creature temp = ObjectAccessor.GetCreature((me), AddGUID[i]);
                        if (temp && temp.IsAlive())
                            if (!temp.GetVictim())
                                temp.GetAI().AttackStart(me.GetVictim());
                    }
                }
                CheckAdds_Timer = 5000;
            }
            else CheckAdds_Timer -= diff;

            if (!Enrage)
            {
                //Cast Vanish, then Garrote random victim
                if (Vanish_Timer <= diff)
                {
                    DoCast(me, SpellIds.Vanish);
                    InVanish = true;
                    Vanish_Timer = 30000;
                    Wait_Timer = 5000;
                }
                else Vanish_Timer -= diff;

                if (Gouge_Timer <= diff)
                {
                    DoCastVictim(SpellIds.Gouge);
                    Gouge_Timer = 40000;
                }
                else Gouge_Timer -= diff;

                if (Blind_Timer <= diff)
                {
                    List<Unit> targets = SelectTargetList(5, SelectAggroTarget.Random, me.GetCombatReach() * 5, true);
                    foreach (var i in targets)
                    {

                        if (!me.IsWithinMeleeRange(i))
                        {
                            DoCast(i, SpellIds.Blind);
                            break;
                        }
                    }
                    Blind_Timer = 40000;
                }
                else
                    Blind_Timer -= diff;
            }

            if (InVanish)
            {
                if (Wait_Timer <= diff)
                {
                    Talk(TextIds.Special);

                    Unit target = SelectTarget(SelectAggroTarget.Random, 0, 100, true);
                    if (target)
                        target.CastSpell(target, SpellIds.Garrote, true);

                    InVanish = false;
                }
                else
                    Wait_Timer -= diff;
            }

            if (!InVanish)
                DoMeleeAttackIfReady();
        }

        InstanceScript instance;

        public ObjectGuid[] AddGUID = new ObjectGuid[4];

        uint Vanish_Timer;
        uint Blind_Timer;
        uint Gouge_Timer;
        uint Wait_Timer;
        uint CheckAdds_Timer;
        uint[] AddId = new uint[4];

        bool InVanish;
        bool Enrage;
    }

    class boss_moroes_guestAI : ScriptedAI
    {
        public boss_moroes_guestAI(Creature creature) : base(creature)
        {
            instance = creature.GetInstanceScript();
        }

        public override void Reset()
        {
            instance.SetData(karazhanConst.BossMoroes, (uint)EncounterState.NotStarted);
        }

        public void AcquireGUID()
        {
            Creature Moroes = ObjectAccessor.GetCreature(me, instance.GetGuidData(DataTypes.Moroes));
            if (Moroes)
            {
                for (byte i = 0; i < 4; ++i)
                {
                    ObjectGuid GUID = ((boss_moroes)Moroes.GetAI()).AddGUID[i];
                    if (!GUID.IsEmpty())
                        GuestGUID[i] = GUID;
                }
            }

        }

        public Unit SelectGuestTarget()
        {
            ObjectGuid TempGUID = GuestGUID[RandomHelper.Rand32() % 4];
            if (!TempGUID.IsEmpty())
            {
                Unit unit = Global.ObjAccessor.GetUnit(me, TempGUID);
                if (unit && unit.IsAlive())
                    return unit;
            }

            return me;
        }

        public override void UpdateAI(uint diff)
        {
            if (instance.GetData(karazhanConst.BossMoroes) == 0)
                EnterEvadeMode();

            DoMeleeAttackIfReady();
        }

        InstanceScript instance;

        ObjectGuid[] GuestGUID = new ObjectGuid[4];
    }

    [Script]
    class boss_baroness_dorothea_millstipe : boss_moroes_guestAI
    {
        //Shadow Priest
        public boss_baroness_dorothea_millstipe(Creature creature) : base(creature) { }

        uint ManaBurn_Timer;
        uint MindFlay_Timer;
        uint ShadowWordPain_Timer;

        public override void Reset()
        {
            ManaBurn_Timer = 7000;
            MindFlay_Timer = 1000;
            ShadowWordPain_Timer = 6000;

            DoCast(me, SpellIds.Shadowform, true);

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
                Unit target = SelectTarget(SelectAggroTarget.Random, 0, 100, true);
                if (target)
                    if (target.GetPowerType() == PowerType.Mana)
                        DoCast(target, SpellIds.Manaburn);
                ManaBurn_Timer = 5000;                          // 3 sec cast
            }
            else ManaBurn_Timer -= diff;

            if (ShadowWordPain_Timer <= diff)
            {
                Unit target = SelectTarget(SelectAggroTarget.Random, 0, 100, true);
                if (target)
                {
                    DoCast(target, SpellIds.Swpain);
                    ShadowWordPain_Timer = 7000;
                }
            }
            else ShadowWordPain_Timer -= diff;
        }
    }

    [Script]
    class boss_baron_rafe_dreuger : boss_moroes_guestAI
    {
        //Retr Pally
        public boss_baron_rafe_dreuger(Creature creature) : base(creature) { }

        uint HammerOfJustice_Timer;
        uint SealOfCommand_Timer;
        uint JudgementOfCommand_Timer;

        public override void Reset()
        {
            HammerOfJustice_Timer = 1000;
            SealOfCommand_Timer = 7000;
            JudgementOfCommand_Timer = SealOfCommand_Timer + 29000;

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
    class boss_lady_catriona_von_indi : boss_moroes_guestAI
    {
        //Holy Priest
        public boss_lady_catriona_von_indi(Creature creature) : base(creature) { }

        uint DispelMagic_Timer;
        uint GreaterHeal_Timer;
        uint HolyFire_Timer;
        uint PowerWordShield_Timer;

        public override void Reset()
        {
            DispelMagic_Timer = 11000;
            GreaterHeal_Timer = 1500;
            HolyFire_Timer = 5000;
            PowerWordShield_Timer = 1000;

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
                Unit target = RandomHelper.RAND(SelectGuestTarget(), SelectTarget(SelectAggroTarget.Random, 0, 100, true));
                if (target)
                    DoCast(target, SpellIds.Dispelmagic);

                DispelMagic_Timer = 25000;
            }
            else DispelMagic_Timer -= diff;
        }
    }

    [Script]
    class boss_lady_keira_berrybuck : boss_moroes_guestAI
    {
        //Holy Pally
        public boss_lady_keira_berrybuck(Creature creature) : base(creature) { }

        uint Cleanse_Timer;
        uint GreaterBless_Timer;
        uint HolyLight_Timer;
        uint DivineShield_Timer;

        public override void Reset()
        {
            Cleanse_Timer = 13000;
            GreaterBless_Timer = 1000;
            HolyLight_Timer = 7000;
            DivineShield_Timer = 31000;

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
    class boss_lord_robin_daris : boss_moroes_guestAI
    {
        //Arms Warr
        public boss_lord_robin_daris(Creature creature) : base(creature) { }

        uint Hamstring_Timer;
        uint MortalStrike_Timer;
        uint WhirlWind_Timer;

        public override void Reset()
        {
            Hamstring_Timer = 7000;
            MortalStrike_Timer = 10000;
            WhirlWind_Timer = 21000;

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
    class boss_lord_crispin_ference : boss_moroes_guestAI
    {
        //Arms Warr
        public boss_lord_crispin_ference(Creature creature) : base(creature) { }

        uint Disarm_Timer;
        uint HeroicStrike_Timer;
        uint ShieldBash_Timer;
        uint ShieldWall_Timer;

        public override void Reset()
        {
            Disarm_Timer = 6000;
            HeroicStrike_Timer = 10000;
            ShieldBash_Timer = 8000;
            ShieldWall_Timer = 4000;

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
