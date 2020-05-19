using System;
using System.Collections.Generic;
using Game.Entities;
using Framework.Constants;
using Game.Scripting;
using Game.AI;
using Game.Spells;

namespace Scripts.Northrend.FrozenHalls.ForgeOfSouls.DevourerOfSouls
{
    struct TextIds
    {
        public const uint SayFaceAggro = 0;
        public const byte SayFaceAngerSlay = 1;
        public const byte SayFaceSorrowSlay = 2;
        public const byte SayFaceDesireSlay = 3;
        public const uint SayFaceDeath = 4;
        public const uint EmoteMirroredSoul = 5;
        public const uint EmoteUnleashSoul = 6;
        public const uint SayFaceUnleashSoul = 7;
        public const uint EmoteWailingSoul = 8;
        public const uint SayFaceWailingSoul = 9;

        public const uint SayJainaOutro = 0;
        public const uint SaySylvanasOutro = 0;
    }

    struct SpellIds
    {
        public const uint PhantomBlast = 68982;
        public const uint MirroredSoulProcAura = 69023;
        public const uint MirroredSoulDamage = 69034;
        public const uint MirroredSoulTargetSelector = 69048;
        public const uint MirroredSoulBuff = 69051;
        public const uint WellOfSouls = 68820;
        public const uint UnleashedSouls = 68939;
        public const uint WailingSoulsStarting = 68912;  // Initial Spell Cast At Begining Of Wailing Souls Phase
        public const uint WailingSoulsBeam = 68875;  // The Beam Visual
        public const uint WailingSouls = 68873;  // The Actual Spell
        //    68871; 68873; 68875; 68876; 68899; 68912; 70324;
        // 68899 Trigger 68871
    }

    struct ModelIds
    {
        public const uint Anger = 30148;
        public const uint Sorrow = 30149;
        public const uint Desire = 30150;
    }

    struct Misc
    {
        public const uint DataThreeFaced = 1;

        public static outroPosition[] outroPositions =
        {
            new outroPosition(CreatureIds.Champion1Alliance, CreatureIds.Champion1Horde, new Position(5590.47f, 2427.79f, 705.935f, 0.802851f)),
            new outroPosition(CreatureIds.Champion1Alliance, CreatureIds.Champion1Horde, new Position(5593.59f, 2428.34f, 705.935f, 0.977384f)),
            new outroPosition(CreatureIds.Champion1Alliance, CreatureIds.Champion1Horde, new Position(5600.81f, 2429.31f, 705.935f, 0.890118f)),
            new outroPosition(CreatureIds.Champion1Alliance, CreatureIds.Champion1Horde, new Position(5600.81f, 2421.12f, 705.935f, 0.890118f)),
            new outroPosition(CreatureIds.Champion1Alliance, CreatureIds.Champion1Horde, new Position(5601.43f, 2426.53f, 705.935f, 0.890118f)),
            new outroPosition(CreatureIds.Champion1Alliance, CreatureIds.Champion1Horde, new Position(5601.55f, 2418.36f, 705.935f, 1.15192f)),
            new outroPosition(CreatureIds.Champion1Alliance, CreatureIds.Champion1Horde, new Position(5598, 2429.14f, 705.935f, 1.0472f)),
            new outroPosition(CreatureIds.Champion1Alliance, CreatureIds.Champion1Horde, new Position(5594.04f, 2424.87f, 705.935f, 1.15192f)),
            new outroPosition(CreatureIds.Champion1Alliance, CreatureIds.Champion1Horde, new Position(5597.89f, 2421.54f, 705.935f, 0.610865f)),
            new outroPosition(CreatureIds.Champion2Alliance, CreatureIds.Champion2Horde, new Position(5598.57f, 2434.62f, 705.935f, 1.13446f)),
            new outroPosition(CreatureIds.Champion2Alliance, CreatureIds.Champion2Horde, new Position(5585.46f, 2417.99f, 705.935f, 1.06465f)),
            new outroPosition(CreatureIds.Champion2Alliance, CreatureIds.Champion2Horde, new Position(5605.81f, 2428.42f, 705.935f, 0.820305f)),
            new outroPosition(CreatureIds.Champion2Alliance, CreatureIds.Champion2Horde, new Position(5591.61f, 2412.66f, 705.935f, 0.925025f)),
            new outroPosition(CreatureIds.Champion2Alliance, CreatureIds.Champion2Horde, new Position(5593.9f, 2410.64f, 705.935f, 0.872665f)),
            new outroPosition(CreatureIds.Champion2Alliance, CreatureIds.Champion2Horde, new Position(5586.76f, 2416.73f, 705.935f, 0.942478f)),
            new outroPosition(CreatureIds.Champion2Alliance, CreatureIds.Champion3Horde, new Position(5592.23f, 2419.14f, 705.935f, 0.855211f)),
            new outroPosition(CreatureIds.Champion2Alliance, CreatureIds.Champion3Horde, new Position(5594.61f, 2416.87f, 705.935f, 0.907571f)),
            new outroPosition(CreatureIds.Champion2Alliance, CreatureIds.Champion3Horde, new Position(5589.77f, 2421.03f, 705.935f, 0.855211f)),

            new outroPosition(CreatureIds.Koreln, CreatureIds.Loralen, new Position(5602.58f, 2435.95f, 705.935f, 0.959931f)),
            new outroPosition(CreatureIds.Elandra, CreatureIds.Kalira, new Position(5606.13f, 2433.16f, 705.935f, 0.785398f)),
            new outroPosition(CreatureIds.JainaPart2, CreatureIds.SylvanasPart2, new Position(5606.12f, 2436.6f, 705.935f, 0.890118f)),
        };

        public static Position CrucibleSummonPos = new Position(5672.294f, 2520.686f, 713.4386f, 0.9599311f);
    }

    struct outroPosition
    {
        public outroPosition(uint allianceEntry, uint hordeEntry, Position movePosition)
        {
            Entry = new uint[2];
            Entry[0] = allianceEntry;
            Entry[1] = hordeEntry;

            MovePosition = movePosition;
        }

        public uint[] Entry;
        public Position MovePosition;
    }

    [Script]
    class boss_devourer_of_souls : BossAI
    {
        public boss_devourer_of_souls(Creature creature) : base(creature, DataType.DevourerOfSouls)
        {
            Initialize();
            beamAngle = 0.0f;
            beamAngleDiff = 0.0f;
            wailingSoulTick = 0;
        }

        void Initialize()
        {
            threeFaced = true;
        }

        public override void Reset()
        {
            _Reset();
            me.SetControlled(false, UnitState.Root);
            me.SetDisplayId(ModelIds.Anger);
            me.SetReactState(ReactStates.Aggressive);

            Initialize();
        }

        public override void EnterCombat(Unit who)
        {
            _EnterCombat();
            Talk(TextIds.SayFaceAggro);

            if (!me.FindNearestCreature(CreatureIds.CrucibleOfSouls, 60)) // Prevent double spawn
                me.GetMap().SummonCreature(CreatureIds.CrucibleOfSouls, Misc.CrucibleSummonPos);

            _scheduler.SetValidator(() => !me.HasUnitState(UnitState.Casting));

            _scheduler.Schedule(TimeSpan.FromSeconds(5), task =>
            {
                DoCastVictim(SpellIds.PhantomBlast);
                task.Repeat(TimeSpan.FromSeconds(5));
            });

            _scheduler.Schedule(TimeSpan.FromSeconds(8), task =>
            {
                DoCastAOE(SpellIds.MirroredSoulTargetSelector);
                Talk(TextIds.EmoteMirroredSoul);
                task.Repeat(TimeSpan.FromSeconds(15), TimeSpan.FromSeconds(30));
            });

            _scheduler.Schedule(TimeSpan.FromSeconds(30), task =>
            {
                Unit target = SelectTarget(SelectAggroTarget.Random, 0);
                if (target)
                    DoCast(target, SpellIds.WellOfSouls);
                task.Repeat(TimeSpan.FromSeconds(20));
            });

            _scheduler.Schedule(TimeSpan.FromSeconds(20), task =>
            {
                Unit target = SelectTarget(SelectAggroTarget.Random, 0);
                if (target)
                    DoCast(target, SpellIds.UnleashedSouls);
                me.SetDisplayId(ModelIds.Sorrow);
                Talk(TextIds.SayFaceUnleashSoul);
                Talk(TextIds.EmoteUnleashSoul);
                task.Repeat(TimeSpan.FromSeconds(30));
                task.Schedule(TimeSpan.FromSeconds(5), () => me.SetDisplayId(ModelIds.Anger));
            });

            _scheduler.Schedule(TimeSpan.FromSeconds(60), TimeSpan.FromSeconds(70), task =>
            {
                me.SetDisplayId(ModelIds.Desire);
                Talk(TextIds.SayFaceWailingSoul);
                Talk(TextIds.EmoteWailingSoul);
                DoCast(me, SpellIds.WailingSoulsStarting);
                Unit target = SelectTarget(SelectAggroTarget.Random, 0);
                if (target)
                {
                    me.SetFacingToObject(target);
                    DoCast(me, SpellIds.WailingSoulsBeam);
                }

                beamAngle = me.GetOrientation();

                beamAngleDiff = (float)Math.PI / 30.0f; // PI/2 in 15 sec = PI/30 per tick
                if (RandomHelper.RAND(true, false))
                    beamAngleDiff = -beamAngleDiff;

                me.InterruptNonMeleeSpells(false);
                me.SetReactState(ReactStates.Passive);

                //Remove any target
                me.SetTarget(ObjectGuid.Empty);

                me.GetMotionMaster().Clear();
                me.SetControlled(true, UnitState.Root);

                wailingSoulTick = 15;

                _scheduler.DelayAll(TimeSpan.FromSeconds(18)); // no other events during wailing souls

                // first one after 3 secs.
                _scheduler.Schedule(TimeSpan.FromSeconds(3), tickTask =>
                {
                    beamAngle += beamAngleDiff;
                    me.SetFacingTo(beamAngle);
                    me.StopMoving();

                    DoCast(me, SpellIds.WailingSouls);

                    if (--wailingSoulTick != 0)
                        tickTask.Repeat(TimeSpan.FromSeconds(1));
                    else
                    {
                        me.SetReactState(ReactStates.Aggressive);
                        me.SetDisplayId(ModelIds.Anger);
                        me.SetControlled(false, UnitState.Root);
                        me.GetMotionMaster().MoveChase(me.GetVictim());
                        task.Repeat(TimeSpan.FromSeconds(60), TimeSpan.FromSeconds(70));
                    }
                });
            });
        }

        public override void KilledUnit(Unit victim)
        {
            if (victim.GetTypeId() != TypeId.Player)
                return;

            byte textId = 0;
            switch (me.GetDisplayId())
            {
                case ModelIds.Anger:
                    textId = TextIds.SayFaceAngerSlay;
                    break;
                case ModelIds.Sorrow:
                    textId = TextIds.SayFaceSorrowSlay;
                    break;
                case ModelIds.Desire:
                    textId = TextIds.SayFaceDesireSlay;
                    break;
                default:
                    break;
            }

            if (textId != 0)
                Talk(textId);
        }

        public override void JustDied(Unit killer)
        {
            _JustDied();

            Position spawnPoint = new Position(5618.139f, 2451.873f, 705.854f, 0);

            Talk(TextIds.SayFaceDeath);

            int entryIndex;
            if (instance.GetData(DataType.TeamInInstance) == (uint)Team.Alliance)
                entryIndex = 0;
            else
                entryIndex = 1;

            for (var i = 0; Misc.outroPositions[i].Entry[entryIndex] != 0; ++i)
            {
                Creature summon = me.SummonCreature(Misc.outroPositions[i].Entry[entryIndex], spawnPoint, TempSummonType.DeadDespawn);
                if (summon)
                {
                    summon.GetMotionMaster().MovePoint(0, Misc.outroPositions[i].MovePosition);
                    if (summon.GetEntry() == CreatureIds.JainaPart2)
                        summon.GetAI().Talk(TextIds.SayJainaOutro);
                    else if (summon.GetEntry() == CreatureIds.SylvanasPart2)
                        summon.GetAI().Talk(TextIds.SaySylvanasOutro);
                }
            }
        }

        public override void SpellHitTarget(Unit target, SpellInfo spell)
        {
            if (spell.Id == SpellIds.PhantomBlast)
                threeFaced = false;
        }

        public override uint GetData(uint type)
        {
            if (type == Misc.DataThreeFaced)
                return threeFaced ? 1 : 0u;

            return 0;
        }

        public override void UpdateAI(uint diff)
        {
            // Return since we have no target
            if (!UpdateVictim())
                return;

            _scheduler.Update(diff);

            if (me.HasUnitState(UnitState.Casting))
                return;

            DoMeleeAttackIfReady();
        }

        bool threeFaced;

        // wailing soul event
        float beamAngle;
        float beamAngleDiff;
        sbyte wailingSoulTick;
    }

    [Script] // 69051 - Mirrored Soul
    class spell_devourer_of_souls_mirrored_soul : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.MirroredSoulProcAura);
        }

        void HandleScript(uint effIndex)
        {
            Unit target = GetHitUnit();
            if (target)
                target.CastSpell(GetCaster(), SpellIds.MirroredSoulProcAura, true);
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new EffectHandler(HandleScript, 0, SpellEffectName.ScriptEffect));
        }
    }

    [Script] // 69023 - Mirrored Soul (Proc)
    class spell_devourer_of_souls_mirrored_soul_proc : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.MirroredSoulDamage);
        }

        bool CheckProc(ProcEventInfo eventInfo)
        {
            return GetCaster() && GetCaster().IsAlive();
        }

        void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            PreventDefaultAction();
            DamageInfo damageInfo = eventInfo.GetDamageInfo();
            if (damageInfo == null || damageInfo.GetDamage() == 0)
                return;

            int damage = (int)MathFunctions.CalculatePct(damageInfo.GetDamage(), 45);
            GetTarget().CastCustomSpell(SpellIds.MirroredSoulDamage, SpellValueMod.BasePoint0, damage, GetCaster(), true);
        }

        public override void Register()
        {
            DoCheckProc.Add(new CheckProcHandler(CheckProc));
            OnEffectProc.Add(new EffectProcHandler(HandleProc, 0, AuraType.ProcTriggerSpell));
        }
    }

    [Script] // 69048 - Mirrored Soul (Target Selector)
    class spell_devourer_of_souls_mirrored_soul_target_selector : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.MirroredSoulBuff);
        }

        void FilterTargets(List<WorldObject> targets)
        {
            if (targets.Empty())
                return;

            WorldObject target = targets.SelectRandom();
            targets.Clear();
            targets.Add(target);
        }

        void HandleScript(uint effIndex)
        {
            Unit target = GetHitUnit();
            if (target)
                GetCaster().CastSpell(target, SpellIds.MirroredSoulBuff, false);
        }

        public override void Register()
        {
            OnObjectAreaTargetSelect.Add(new ObjectAreaTargetSelectHandler(FilterTargets, 0, Targets.UnitSrcAreaEntry));
            OnEffectHitTarget.Add(new EffectHandler(HandleScript, 0, SpellEffectName.ScriptEffect));
        }
    }

    [Script]
    class achievement_three_faced : AchievementCriteriaScript
    {
        public achievement_three_faced() : base("achievement_three_faced") { }

        public override bool OnCheck(Player player, Unit target)
        {
            if (!target)
                return false;

            Creature devourer = target.ToCreature();
            if (devourer)
                if (devourer.GetAI().GetData(Misc.DataThreeFaced) != 0)
                    return true;

            return false;
        }
    }
}
