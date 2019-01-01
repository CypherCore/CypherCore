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

namespace Scripts.Northrend.Nexus.Nexus
{
    struct MagusTelestraConst
    {
        //Spells
        public const uint SpellIceNova = 47772;
        public const uint SpellIceNovaH = 56935;
        public const uint SpellFirebomb = 47773;
        public const uint SpellFirebombH = 56934;
        public const uint SpellGravityWell = 47756;
        public const uint SpellTelestraBack = 47714;
        public const uint SpellFireMagusVisual = 47705;
        public const uint SpellFrostMagusVisual = 47706;
        public const uint SpellArcaneMagusVisual = 47704;
        public const uint SpellWearChristmasHat = 61400;

        //Npcs
        public const uint NpcFireMagus = 26928;
        public const uint NpcFrostMagus = 26930;
        public const uint NpcArcaneMagus = 26929;

        //Texts
        public const uint SayAggro = 0;
        public const uint SayKill = 1;
        public const uint SayDeath = 2;
        public const uint SayMerge = 3;
        public const uint SaySplit = 4;

        //Misc
        public const uint DataSplitPersonality = 1;
        public const ushort GameEventWinterVeil = 2;
    }

    [Script]
    class boss_magus_telestra : ScriptedAI
    {
        public boss_magus_telestra(Creature creature) : base(creature)
        {
            instance = creature.GetInstanceScript();
            bFireMagusDead = false;
            bFrostMagusDead = false;
            bArcaneMagusDead = false;
            uiIsWaitingToAppearTimer = 0;
        }

        void Initialize()
        {
            Phase = 0;

            uiIceNovaTimer = 7 * Time.InMilliseconds;
            uiFireBombTimer = 0;
            uiGravityWellTimer = 15 * Time.InMilliseconds;
            uiCooldown = 0;

            for (byte n = 0; n < 3; ++n)
                time[n] = 0;

            splitPersonality = 0;
            bIsWaitingToAppear = false;
        }

        public override void Reset()
        {
            Initialize();

            me.RemoveFlag(UnitFields.Flags, UnitFlags.NotSelectable);

            instance.SetBossState(DataTypes.MagusTelestra, EncounterState.NotStarted);

            if (IsHeroic() && Global.GameEventMgr.IsActiveEvent(MagusTelestraConst.GameEventWinterVeil) && !me.HasAura(MagusTelestraConst.SpellWearChristmasHat))
                me.AddAura(MagusTelestraConst.SpellWearChristmasHat, me);
        }

        public override void EnterCombat(Unit who)
        {
            Talk(MagusTelestraConst.SayAggro);

            instance.SetBossState(DataTypes.MagusTelestra, EncounterState.InProgress);
        }

        public override void JustDied(Unit killer)
        {
            Talk(MagusTelestraConst.SayDeath);

            instance.SetBossState(DataTypes.MagusTelestra, EncounterState.Done);
        }

        public override void KilledUnit(Unit who)
        {
            if (who.IsTypeId(TypeId.Player))
                Talk(MagusTelestraConst.SayKill);
        }

        public override uint GetData(uint type)
        {
            if (type == MagusTelestraConst.DataSplitPersonality)
                return splitPersonality;

            return 0;
        }

        public override void UpdateAI(uint diff)
        {
            //Return since we have no target
            if (!UpdateVictim())
                return;

            if (bIsWaitingToAppear)
            {
                me.StopMoving();
                me.AttackStop();
                if (uiIsWaitingToAppearTimer <= diff)
                {
                    me.CastSpell(me, 47714, true);
                    me.RemoveFlag(UnitFields.Flags, UnitFlags.NotSelectable);
                    bIsWaitingToAppear = false;
                    InVanish = false;
                    me.SendAIReaction(AiReaction.Hostile);
                }
                else
                    uiIsWaitingToAppearTimer -= diff;

                return;
            }

            if ((Phase == 1) || (Phase == 3))
            {
                if (bFireMagusDead && bFrostMagusDead && bArcaneMagusDead)
                {
                    for (byte n = 0; n < 3; ++n)
                        time[n] = 0;

                    me.GetMotionMaster().Clear();
                    DoCast(me, MagusTelestraConst.SpellTelestraBack);
                    if (Phase == 1)
                        Phase = 2;
                    if (Phase == 3)
                        Phase = 4;
                    bIsWaitingToAppear = true;
                    uiIsWaitingToAppearTimer = 4 * Time.InMilliseconds;
                    Talk(MagusTelestraConst.SayMerge);
                }
                else
                    return;
            }

            if ((Phase == 0) && HealthBelowPct(50))
            {
                InVanish = true;
                Phase = 1;
                me.CastStop();
                me.RemoveAllAuras();
                me.CastSpell(me, 47710, false);
                me.SetFlag(UnitFields.Flags, UnitFlags.NotSelectable);
                bFireMagusDead = false;
                bFrostMagusDead = false;
                bArcaneMagusDead = false;
                Talk(MagusTelestraConst.SaySplit);
                return;
            }

            if (IsHeroic() && (Phase == 2) && HealthBelowPct(10))
            {
                InVanish = true;
                Phase = 3;
                me.CastStop();
                me.RemoveAllAuras();
                me.SetFlag(UnitFields.Flags, UnitFlags.NotSelectable);
                bFireMagusDead = false;
                bFrostMagusDead = false;
                bArcaneMagusDead = false;
                Talk(MagusTelestraConst.SaySplit);
                return;
            }

            if (uiCooldown != 0)
            {
                if (uiCooldown <= diff)
                    uiCooldown = 0;
                else
                {
                    uiCooldown -= diff;
                    return;
                }
            }

            if (uiIceNovaTimer <= diff)
            {
                Unit target = SelectTarget(SelectAggroTarget.Random, 0);
                if (target)
                {
                    DoCast(target, MagusTelestraConst.SpellIceNova, false);
                    uiCooldown = 1500;
                }
                uiIceNovaTimer = 15 * Time.InMilliseconds;
            }
            else uiIceNovaTimer -= diff;

            if (uiGravityWellTimer <= diff)
            {
                Unit target = me.GetVictim();
                if (target)
                {
                    DoCast(target, MagusTelestraConst.SpellGravityWell);
                    uiCooldown = 6 * Time.InMilliseconds;
                }
                uiGravityWellTimer = 15 * Time.InMilliseconds;
            }
            else uiGravityWellTimer -= diff;

            if (uiFireBombTimer <= diff)
            {
                Unit target = SelectTarget(SelectAggroTarget.Random, 0);
                if (target)
                {
                    DoCast(target, MagusTelestraConst.SpellFirebomb, false);
                    uiCooldown = 2 * Time.InMilliseconds;
                }
                uiFireBombTimer = 2 * Time.InMilliseconds;
            }
            else uiFireBombTimer -= diff;

            if (!InVanish)
                DoMeleeAttackIfReady();
        }

        public override void SummonedCreatureDies(Creature summon, Unit killer)
        {
            if (summon.IsAlive())
                return;

            switch (summon.GetEntry())
            {
                case MagusTelestraConst.NpcFireMagus:
                    bFireMagusDead = true;
                    break;
                case MagusTelestraConst.NpcFrostMagus:
                    bFrostMagusDead = true;
                    break;
                case MagusTelestraConst.NpcArcaneMagus:
                    bArcaneMagusDead = true;
                    break;
            }

            byte i = 0;
            while (time[i] != 0)
                ++i;

            time[i] = Global.WorldMgr.GetGameTime();
            if (i == 2 && (time[2] - time[1] < 5) && (time[1] - time[0] < 5))
                ++splitPersonality;
        }

        InstanceScript instance;

        bool bFireMagusDead;
        bool bFrostMagusDead;
        bool bArcaneMagusDead;
        bool bIsWaitingToAppear;
        bool InVanish;

        uint uiIsWaitingToAppearTimer;
        uint uiIceNovaTimer;
        uint uiFireBombTimer;
        uint uiGravityWellTimer;
        uint uiCooldown;

        byte Phase;
        byte splitPersonality;
        long[] time = new long[3];
    }

    [Script]
    class achievement_split_personality : AchievementCriteriaScript
    {
        public achievement_split_personality() : base("achievement_split_personality") { }

        public override bool OnCheck(Player player, Unit target)
        {
            if (!target)
                return false;

            Creature Telestra = target.ToCreature();
            if (Telestra)
                if (Telestra.GetAI().GetData(MagusTelestraConst.DataSplitPersonality) == 2)
                    return true;

            return false;
        }
    }
}
