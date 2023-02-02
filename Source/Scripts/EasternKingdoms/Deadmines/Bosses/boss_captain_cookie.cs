using System;
using Framework.Constants;
using Game.AI;
using Game.Entities;
using Game.Scripting;

namespace Scripts.EasternKingdoms.Deadmines.Bosses
{
    [CreatureScript(47739)]
    public class boss_captain_cookie : BossAI
    {
        public const int POINT_MOVE = 1;
        public static readonly uint[] ThrowFoodSpells = { eSpell.SPELL_THROW_FOOD_TARGETING_CORN, eSpell.SPELL_THROW_FOOD_TARGETING_ROTTEN_CORN, eSpell.SPELL_THROW_FOOD_TARGETING_MELON, eSpell.SPELL_THROW_FOOD_TARGETING_ROTTEN_MELON, eSpell.SPELL_THROW_FOOD_TARGETING_STEAK, eSpell.SPELL_THROW_FOOD_TARGETING_ROTTEN_STEAK, eSpell.SPELL_THROW_FOOD_TARGETING_MYSTERY_MEAT, eSpell.SPELL_THROW_FOOD_TARGETING_ROTTEN_MM, eSpell.SPELL_THROW_FOOD_TARGETING_LOAF, eSpell.SPELL_THROW_FOOD_TARGETING_ROTTEN_LOAF, eSpell.SPELL_THROW_FOOD_TARGETING_BUN, eSpell.SPELL_THROW_FOOD_TARGETING_ROTTEN_BUN };
        public static readonly Position NotePos = new Position(-74.3611f, -820.014f, 40.3714f, 0.0f);
        public static readonly Position[] CookiesPos =
        {
            new Position(-67.435249f, -822.357178f, 40.861347f, 0.0f),
            new Position(-64.2552f, -820.245f, 41.17154f, 0.0f)
        };
        public static readonly Position MovePos = new Position(-71.292213f, -819.792297f, 40.51f, 0.04f);

        public struct eSpell
        {
            public const uint SPELL_WHO_IS_THAT = 89339;
            public const uint SPELL_SETIATED = 89267;
            public const uint SPELL_SETIATED_H = 92834;
            public const uint SPELL_NAUSEATED = 89732;
            public const uint SPELL_NAUSEATED_H = 92066;
            public const uint SPELL_ROTTEN_AURA = 89734;
            public const uint SPELL_ROTTEN_AURA_H = 95513;
            public const uint SPELL_ROTTEN_AURA_DMG = 89734;
            public const uint SPELL_ROTTEN_AURA_DMG_H = 92065;
            public const uint SPELL_CAULDRON = 89250;
            public const uint SPELL_CAULDRON_VISUAL = 89251;
            public const uint SPELL_CAULDRON_FIRE = 89252;


            public const uint SPELL_THROW_FOOD_TARGETING_CORN = 89268;
            public const uint SPELL_THROW_FOOD_TARGETING_ROTTEN_CORN = 89740;
            public const uint SPELL_THROW_FOOD_TARGETING_MELON = 90561;
            public const uint SPELL_THROW_FOOD_TARGETING_ROTTEN_MELON = 90582;
            public const uint SPELL_THROW_FOOD_TARGETING_STEAK = 90562;
            public const uint SPELL_THROW_FOOD_TARGETING_ROTTEN_STEAK = 90583;
            public const uint SPELL_THROW_FOOD_TARGETING_MYSTERY_MEAT = 90563;
            public const uint SPELL_THROW_FOOD_TARGETING_ROTTEN_MM = 90584;
            public const uint SPELL_THROW_FOOD_TARGETING_LOAF = 90564;
            public const uint SPELL_THROW_FOOD_TARGETING_ROTTEN_LOAF = 90585;
            public const uint SPELL_THROW_FOOD_TARGETING_BUN = 90565;
            public const uint SPELL_THROW_FOOD_TARGETING_ROTTEN_BUN = 90586;


            public const uint SPELL_THROW_FOOD = 89263;
            public const uint SPELL_THROW_FOOD_FORCE = 89269;
            public const uint SPELL_THROW_FOOD_BACK = 89264;
            public const uint SPELL_THROW_FOOD_01 = 90557;
            public const uint SPELL_THROW_FOOD_02 = 90560;
            public const uint SPELL_THROW_FOOD_03 = 90603;
            public const uint SPELL_THROW_FOOD_04 = 89739;
            public const uint SPELL_THROW_FOOD_05 = 90605;
            public const uint SPELL_THROW_FOOD_06 = 90556;
            public const uint SPELL_THROW_FOOD_07 = 90680;
            public const uint SPELL_THROW_FOOD_08 = 90559;
            public const uint SPELL_THROW_FOOD_09 = 90602;
            public const uint SPELL_THROW_FOOD_10 = 89263;
            public const uint SPELL_THROW_FOOD_11 = 90604;
            public const uint SPELL_THROW_FOOD_12 = 90555;
            public const uint SPELL_THROW_FOOD_13 = 90606;
        }

        public struct Adds
        {
            public const uint NPC_BABY_MURLOC = 48672;

            public const uint NPC_CAULDRON = 47754;

            public const uint NPC_BUN = 48301;
            public const uint NPC_MISTERY_MEAT = 48297;
            public const uint NPC_BREAD_LOAF = 48300;
            public const uint NPC_STEAK = 48296;
            public const uint NPC_CORN = 48006;
            public const uint NPC_MELON = 48294;

            public const uint NPC_ROTTEN_SNEAK = 48295;
            public const uint NPC_ROTTEN_CORN = 48276;
            public const uint NPC_ROTTEN_LOAF = 48299;
            public const uint NPC_ROTTEN_MELON = 48293;
            public const uint NPC_ROTTEN_MISTERY_MEAT = 48298;
            public const uint NPC_ROTTEN_BUN = 48302;
        }

        public struct BossEvents
        {
            public const uint EVENT_THROW_FOOD = 1;
            public const uint EVENT_CAULDRON_1 = 2;
            public const uint EVENT_CAULDRON_2 = 3;
            public const uint EVENT_MOVE = 4;
        }


        public boss_captain_cookie(Creature pCreature) : base(pCreature, DMData.DATA_COOKIE)
        {
            me.ApplySpellImmune(0, SpellImmunity.Effect, SpellEffectName.KnockBack, true);
            me.ApplySpellImmune(0, SpellImmunity.Mechanic, Mechanics.Grip, true);
            me.ApplySpellImmune(0, SpellImmunity.Mechanic, Mechanics.Stun, true);
            me.ApplySpellImmune(0, SpellImmunity.Mechanic, Mechanics.Fear, true);
            me.ApplySpellImmune(0, SpellImmunity.Mechanic, Mechanics.Root, true);
            me.ApplySpellImmune(0, SpellImmunity.Mechanic, Mechanics.Freeze, true);
            me.ApplySpellImmune(0, SpellImmunity.Mechanic, Mechanics.Polymorph, true);
            me.ApplySpellImmune(0, SpellImmunity.Mechanic, Mechanics.Horror, true);
            me.ApplySpellImmune(0, SpellImmunity.Mechanic, Mechanics.Sapped, true);
            me.ApplySpellImmune(0, SpellImmunity.Mechanic, Mechanics.Charm, true);
            me.ApplySpellImmune(0, SpellImmunity.Mechanic, Mechanics.Disoriented, true);
            me.ApplySpellImmune(0, SpellImmunity.State, AuraType.ModConfuse, true);
            me.SetActive(true);
        }

        public override void Reset()
        {
            _Reset();
            me.SetReactState(ReactStates.Aggressive);
            DoCast(eSpell.SPELL_WHO_IS_THAT);
            me.SetUnitFlag(UnitFlags.Uninteractible);
        }

        public override void MoveInLineOfSight(Unit who)
        {
            if (instance.GetBossState(DMData.DATA_RIPSNARL) != EncounterState.Done)
            {
                return;
            }

            if (me.GetDistance(who) > 5.0f)
            {
                return;
            }

            base.MoveInLineOfSight(who);
        }

        public override void JustEnteredCombat(Unit who)
        {
            me.RemoveAura(eSpell.SPELL_WHO_IS_THAT);
            me.RemoveUnitFlag(UnitFlags.Uninteractible);
            me.AttackStop();
            me.SetReactState(ReactStates.Passive);
 
            _events.ScheduleEvent(BossEvents.EVENT_MOVE, TimeSpan.FromMilliseconds(1000));

            DoZoneInCombat();
            instance.SetBossState(DMData.DATA_COOKIE, EncounterState.InProgress);
        }

        public override void MovementInform(MovementGeneratorType type, uint data)
        {
            if (type == MovementGeneratorType.Point)
            {
                if (data == POINT_MOVE)
                {
                    _events.ScheduleEvent(BossEvents.EVENT_CAULDRON_1, TimeSpan.FromMilliseconds(2000));
                }
            }
        }

        public override void JustDied(Unit killer)
        {
            base.JustDied(killer);

            if (IsHeroic())
            {
                me.SummonCreature(DMCreatures.NPC_VANESSA_NOTE, NotePos);
            }
        }

        public override void UpdateAI(uint diff)
        {
            if (!UpdateVictim())
            {
                return;
            }

            _events.Update(diff);

            if (me.HasUnitState(UnitState.Casting))
            {
                return;
            }

            uint eventId;
            while ((eventId = _events.ExecuteEvent()) != 0)
            {
                switch (eventId)
                {
                    case BossEvents.EVENT_MOVE:
                        me.GetMotionMaster().MovePoint(POINT_MOVE, MovePos);
                        break;
                    case BossEvents.EVENT_CAULDRON_1:
                        me.CastSpell(CookiesPos[0].GetPositionX(), CookiesPos[0].GetPositionY(), CookiesPos[0].GetPositionZ(), eSpell.SPELL_CAULDRON, true);
                        _events.ScheduleEvent(BossEvents.EVENT_CAULDRON_2, TimeSpan.FromMilliseconds(2000));
                        break;
                    case BossEvents.EVENT_CAULDRON_2:
                        {
                            Creature pCauldron = me.FindNearestCreature(Adds.NPC_CAULDRON, 20.0f);
                            if (pCauldron != null)
                            {
                                me.GetMotionMaster().MoveJump(pCauldron.GetPosition(), 5, 10);
                            }
                            _events.ScheduleEvent(BossEvents.EVENT_THROW_FOOD, TimeSpan.FromMilliseconds(3000));
                            break;
                        }
                    case BossEvents.EVENT_THROW_FOOD:
                        DoCastAOE(ThrowFoodSpells[RandomHelper.URand(0, 11)]);
                        _events.ScheduleEvent(BossEvents.EVENT_THROW_FOOD, TimeSpan.FromMilliseconds(4000));
                        break;
                }
            }
        }
    }
}
