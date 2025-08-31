// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.DataStorage;
using Game.Entities;
using Game.Maps;
using Game.Spells;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Game.AI
{
    public class ScriptedAI : CreatureAI
    {
        Difficulty _difficulty;
        bool _isCombatMovementAllowed;

        public ScriptedAI(Creature creature) : base(creature)
        {
            _isCombatMovementAllowed = true;
            _difficulty = me.GetMap().GetDifficultyID();
        }

        public void AttackStartNoMove(Unit target)
        {
            if (target == null)
                return;

            if (me.Attack(target, true))
                DoStartNoMovement(target);
        }

        // Called before JustEngagedWith even before the creature is in combat.
        public override void AttackStart(Unit target)
        {
            if (IsCombatMovementAllowed())
                base.AttackStart(target);
            else
                AttackStartNoMove(target);
        }

        //Called at World update tick
        public override void UpdateAI(uint diff)
        {
            //Check if we have a current target
            UpdateVictim();
        }

        //Start movement toward victim
        public void DoStartMovement(Unit target, float distance = 0.0f, float angle = 0.0f)
        {
            if (target != null)
                me.StartDefaultCombatMovement(target, distance, angle);
        }

        //Start no movement on victim
        public void DoStartNoMovement(Unit target)
        {
            if (target == null)
                return;

            me.GetMotionMaster().MoveIdle();
        }

        //Stop attack of current victim
        public void DoStopAttack()
        {
            if (me.GetVictim() != null)
                me.AttackStop();
        }

        //Cast spell by spell info
        public void DoCastSpell(Unit target, SpellInfo spellInfo, bool triggered = false)
        {
            if (target == null || me.IsNonMeleeSpellCast(false))
                return;

            me.StopMoving();
            me.CastSpell(target, spellInfo.Id, triggered);
        }

        //Plays a sound to all nearby players
        public static void DoPlaySoundToSet(WorldObject source, uint soundId)
        {
            if (source == null)
                return;

            if (!CliDB.SoundKitStorage.ContainsKey(soundId))
            {
                Log.outError(LogFilter.ScriptsAi, $"ScriptedAI::DoPlaySoundToSet: Invalid soundId {soundId} used in DoPlaySoundToSet (Source: {source.GetGUID()})");
                return;
            }

            source.PlayDirectSound(soundId);
        }

        /// <summary>
        /// Add specified amount of threat directly to victim (ignores redirection effects) - also puts victim in combat and engages them if necessary
        /// </summary>
        /// <param name="victim"></param>
        /// <param name="amount"></param>
        /// <param name="who"></param>
        public void AddThreat(Unit victim, float amount, Unit who = null)
        {
            if (victim == null)
                return;

            if (who == null)
                who = me;

            who.GetThreatManager().AddThreat(victim, amount, null, true, true);
        }

        /// <summary>
        /// Adds/removes the specified percentage from the specified victim's threat (to who, or me if not specified)
        /// </summary>
        /// <param name="victim"></param>
        /// <param name="pct"></param>
        /// <param name="who"></param>
        public void ModifyThreatByPercent(Unit victim, int pct, Unit who = null)
        {
            if (victim == null)
                return;

            if (who == null)
                who = me;

            who.GetThreatManager().ModifyThreatByPercent(victim, pct);
        }

        /// <summary>
        /// Resets the victim's threat level to who (or me if not specified) to zero
        /// </summary>
        /// <param name="victim"></param>
        /// <param name="who"></param>
        public void ResetThreat(Unit victim, Unit who)
        {
            if (victim == null)
                return;

            if (who == null)
                who = me;

            who.GetThreatManager().ResetThreat(victim);
        }

        /// <summary>
        /// Resets the specified unit's threat list (me if not specified) - does not delete entries, just sets their threat to zero
        /// </summary>
        /// <param name="who"></param>
        public void ResetThreatList(Unit who = null)
        {
            if (who == null)
                who = me;

            who.GetThreatManager().ResetAllThreat();
        }

        /// <summary>
        /// Returns the threat level of victim towards who (or me if not specified)
        /// </summary>
        /// <param name="victim"></param>
        /// <param name="who"></param>
        /// <returns></returns>
        public float GetThreat(Unit victim, Unit who = null)
        {
            if (victim == null)
                return 0.0f;

            if (who == null)
                who = me;

            return who.GetThreatManager().GetThreat(victim);
        }

        /// <summary>
        /// Stops combat, ignoring restrictions, for the given creature
        /// </summary>
        /// <param name="who"></param>
        /// <param name="reset"></param>
        void ForceCombatStop(Creature who, bool reset = true)
        {
            if (who == null || !who.IsInCombat())
                return;

            who.CombatStop(true);
            who.DoNotReacquireSpellFocusTarget();
            who.GetMotionMaster().Clear(MovementGeneratorPriority.Normal);

            if (reset)
            {
                who.LoadCreaturesAddon();

                if (!me.IsTapListNotClearedOnEvade())
                    who.SetTappedBy(null);

                who.ResetPlayerDamageReq();
                who.SetLastDamagedTime(0);
                who.SetCannotReachTarget(false);
            }
        }

        /// <summary>
        /// Stops combat, ignoring restrictions, for the found creatures
        /// </summary>
        /// <param name="entry"></param>
        /// <param name="maxSearchRange"></param>
        /// <param name="samePhase"></param>
        /// <param name="reset"></param>
        void ForceCombatStopForCreatureEntry(uint entry, float maxSearchRange = 250.0f, bool samePhase = true, bool reset = true)
        {
            Log.outDebug(LogFilter.ScriptsAi, $"BossAI::ForceStopCombatForCreature: called on {me.GetGUID()}. Debug info: {me.GetDebugInfo()}");

            List<Creature> creatures = new();
            AllCreaturesOfEntryInRange check = new(me, entry, maxSearchRange);
            CreatureListSearcher searcher = new(me, creatures, check);

            if (!samePhase)
                PhasingHandler.SetAlwaysVisible(me, true, false);

            Cell.VisitGridObjects(me, searcher, maxSearchRange);

            if (!samePhase)
                PhasingHandler.SetAlwaysVisible(me, false, false);

            foreach (Creature creature in creatures)
                ForceCombatStop(creature, reset);
        }

        /// <summary>
        /// Stops combat, ignoring restrictions, for the found creatures
        /// </summary>
        /// <param name="creatureEntries"></param>
        /// <param name="maxSearchRange"></param>
        /// <param name="samePhase"></param>
        /// <param name="reset"></param>
        void ForceCombatStopForCreatureEntry(List<uint> creatureEntries, float maxSearchRange = 250.0f, bool samePhase = true, bool reset = true)
        {
            foreach (var entry in creatureEntries)
                ForceCombatStopForCreatureEntry(entry, maxSearchRange, samePhase, reset);
        }

        //Spawns a creature relative to me
        public Creature DoSpawnCreature(uint entry, float offsetX, float offsetY, float offsetZ, float angle, TempSummonType type, TimeSpan despawntime)
        {
            return me.SummonCreature(entry, me.GetPositionX() + offsetX, me.GetPositionY() + offsetY, me.GetPositionZ() + offsetZ, angle, type, despawntime);
        }

        //Returns spells that meet the specified criteria from the creatures spell list
        public SpellInfo SelectSpell(Unit target, SpellSchoolMask school, Mechanics mechanic, SelectTargetType targets, float rangeMin, float rangeMax, SelectEffect effect)
        {
            //No target so we can't cast
            if (target == null)
                return null;

            //Silenced so we can't cast
            if (me.IsSilenced(school != 0 ? school : SpellSchoolMask.Magic))
                return null;

            //Using the extended script system we first create a list of viable spells
            SpellInfo[] apSpell = new SpellInfo[SharedConst.MaxCreatureSpells];

            uint spellCount = 0;

            //Check if each spell is viable(set it to null if not)
            for (uint i = 0; i < SharedConst.MaxCreatureSpells; i++)
            {
                SpellInfo tempSpell = Global.SpellMgr.GetSpellInfo(me.m_spells[i], me.GetMap().GetDifficultyID());
                AISpellInfoType aiSpell = GetAISpellInfo(me.m_spells[i], me.GetMap().GetDifficultyID());

                //This spell doesn't exist
                if (tempSpell == null || aiSpell == null)
                    continue;

                // Targets and Effects checked first as most used restrictions
                //Check the spell targets if specified
                if (targets != 0 && !Convert.ToBoolean(aiSpell.Targets & (1 << ((int)targets - 1))))
                    continue;

                //Check the type of spell if we are looking for a specific spell type
                if (effect != 0 && !Convert.ToBoolean(aiSpell.Effects & (1 << ((int)effect - 1))))
                    continue;

                //Check for school if specified
                if (school != 0 && (tempSpell.SchoolMask & school) == 0)
                    continue;

                //Check for spell mechanic if specified
                if (mechanic != 0 && tempSpell.Mechanic != mechanic)
                    continue;

                // Continue if we don't have the mana to actually cast this spell
                bool hasPower = true;
                foreach (SpellPowerCost cost in tempSpell.CalcPowerCost(me, tempSpell.GetSchoolMask()))
                {
                    if (cost.Amount > me.GetPower(cost.Power))
                    {
                        hasPower = false;
                        break;
                    }
                }

                if (!hasPower)
                    continue;

                //Check if the spell meets our range requirements
                if (rangeMin != 0 && me.GetSpellMinRangeForTarget(target, tempSpell) < rangeMin)
                    continue;

                if (rangeMax != 0 && me.GetSpellMaxRangeForTarget(target, tempSpell) > rangeMax)
                    continue;

                //Check if our target is in range
                if (me.IsWithinDistInMap(target, me.GetSpellMinRangeForTarget(target, tempSpell)) || !me.IsWithinDistInMap(target, me.GetSpellMaxRangeForTarget(target, tempSpell)))
                    continue;

                //All good so lets add it to the spell list
                apSpell[spellCount] = tempSpell;
                ++spellCount;
            }

            //We got our usable spells so now lets randomly pick one
            if (spellCount == 0)
                return null;

            return apSpell[RandomHelper.IRand(0, (int)(spellCount - 1))];
        }

        public void DoTeleportTo(float x, float y, float z, uint time = 0)
        {
            me.Relocate(x, y, z);
            float speed = me.GetDistance(x, y, z) / (time * 0.001f);
            me.MonsterMoveWithSpeed(x, y, z, speed);
        }

        public void DoTeleportTo(float[] position)
        {
            me.NearTeleportTo(position[0], position[1], position[2], position[3]);
        }

        //Teleports a player without dropping threat (only teleports to same map)
        public void DoTeleportPlayer(Unit unit, float x, float y, float z, float o)
        {
            if (unit == null)
                return;
            Player player = unit.ToPlayer();
            if (player != null)
                player.TeleportTo(unit.GetMapId(), x, y, z, o, TeleportToOptions.NotLeaveCombat);
            else
                Log.outError(LogFilter.ScriptsAi, $"ScriptedAI::DoTeleportPlayer: Creature {me.GetGUID()} Tried to teleport non-player unit ({unit.GetGUID()}) to X: {x} Y: {y} Z: {z} O: {o}. Aborted.");
        }

        public void DoTeleportAll(float x, float y, float z, float o)
        {
            Map map = me.GetMap();
            if (!map.IsDungeon())
                return;

            var PlayerList = map.GetPlayers();
            foreach (var player in PlayerList)
                if (player.IsAlive())
                    player.TeleportTo(me.GetMapId(), x, y, z, o, TeleportToOptions.NotLeaveCombat);

        }

        //Returns friendly unit with the most amount of hp missing from max hp
        public Unit DoSelectLowestHpFriendly(float range, uint minHPDiff = 1)
        {
            var u_check = new MostHPMissingInRange<Unit>(me, range, minHPDiff);
            var searcher = new UnitLastSearcher(me, u_check);
            Cell.VisitAllObjects(me, searcher, range);

            return searcher.GetResult();
        }

        //Returns a list of friendly CC'd units within range
        public List<Creature> DoFindFriendlyCC(float range)
        {
            List<Creature> list = new();
            var u_check = new FriendlyCCedInRange(me, range);
            var searcher = new CreatureListSearcher(me, list, u_check);
            Cell.VisitAllObjects(me, searcher, range);

            return list;
        }

        //Returns a list of all friendly units missing a specific buff within range
        public List<Creature> DoFindFriendlyMissingBuff(float range, uint spellId)
        {
            List<Creature> list = new();
            var u_check = new FriendlyMissingBuffInRange(me, range, spellId);
            var searcher = new CreatureListSearcher(me, list, u_check);
            Cell.VisitAllObjects(me, searcher, range);

            return list;
        }

        //Return a player with at least minimumRange from me
        public Player GetPlayerAtMinimumRange(float minimumRange)
        {
            var check = new PlayerAtMinimumRangeAway(me, minimumRange);
            var searcher = new PlayerSearcher(me, check);
            Cell.VisitWorldObjects(me, searcher, minimumRange);

            return searcher.GetResult();
        }

        public void SetEquipmentSlots(bool loadDefault, int mainHand = -1, int offHand = -1, int ranged = -1)
        {
            if (loadDefault)
            {
                me.LoadEquipment(me.GetOriginalEquipmentId(), true);
                return;
            }

            if (mainHand >= 0)
                me.SetVirtualItem(0, (uint)mainHand);

            if (offHand >= 0)
                me.SetVirtualItem(1, (uint)offHand);

            if (ranged >= 0)
                me.SetVirtualItem(2, (uint)ranged);
        }

        // Used to control if MoveChase() is to be used or not in AttackStart(). Some creatures does not chase victims
        // NOTE: If you use SetCombatMovement while the creature is in combat, it will do NOTHING - This only affects AttackStart
        //       You should make the necessary to make it happen so.
        //       Remember that if you modified _isCombatMovementAllowed (e.g: using SetCombatMovement) it will not be reset at Reset().
        //       It will keep the last value you set.
        public void SetCombatMovement(bool allowMovement)
        {
            _isCombatMovementAllowed = allowMovement;
        }

        public static Creature GetClosestCreatureWithEntry(WorldObject source, uint entry, float maxSearchRange, bool alive = true)
        {
            return source.FindNearestCreature(entry, maxSearchRange, alive);
        }

        public static Creature GetClosestCreatureWithOptions(WorldObject source, float maxSearchRange, FindCreatureOptions options)
        {
            return source.FindNearestCreatureWithOptions(maxSearchRange, options);
        }

        public static GameObject GetClosestGameObjectWithEntry(WorldObject source, uint entry, float maxSearchRange, bool spawnedOnly = true)
        {
            return source.FindNearestGameObject(entry, maxSearchRange, spawnedOnly);
        }

        public bool HealthBelowPct(int pct) { return me.HealthBelowPct(pct); }

        public bool HealthAbovePct(int pct) { return me.HealthAbovePct(pct); }

        public bool IsLFR()
        {
            return me.GetMap().IsLFR();
        }

        public bool IsNormal()
        {
            return me.GetMap().IsNormal();
        }

        public bool IsHeroic()
        {
            return me.GetMap().IsHeroic();
        }

        public bool IsMythic()
        {
            return me.GetMap().IsMythic();
        }

        public bool IsMythicPlus()
        {
            return me.GetMap().IsMythicPlus();
        }

        public bool IsHeroicOrHigher()
        {
            return me.GetMap().IsHeroicOrHigher();
        }

        public bool IsTimewalking()
        {
            return me.GetMap().IsTimewalking();
        }

        public bool IsCombatMovementAllowed() { return _isCombatMovementAllowed; }

        // return the dungeon or raid difficulty
        public Difficulty GetDifficulty() { return _difficulty; }

        // return true for 25 man or 25 man heroic mode
        public bool Is25ManRaid() { return _difficulty == Difficulty.Raid25N || _difficulty == Difficulty.Raid25HC; }

        public T DungeonMode<T>(T normal5, T heroic10)
        {
            return _difficulty switch
            {
                Difficulty.Normal => normal5,
                _ => heroic10,
            };
        }

        public T RaidMode<T>(T normal10, T normal25)
        {
            return _difficulty switch
            {
                Difficulty.Raid10N => normal10,
                _ => normal25,
            };
        }

        public T RaidMode<T>(T normal10, T normal25, T heroic10, T heroic25)
        {
            return _difficulty switch
            {
                Difficulty.Raid10N => normal10,
                Difficulty.Raid25N => normal25,
                Difficulty.Raid10HC => heroic10,
                _ => heroic25,
            };
        }
    }

    public class BossAI : ScriptedAI
    {
        public InstanceScript instance;
        public SummonList summons;
        uint _bossId;

        public BossAI(Creature creature, uint bossId) : base(creature)
        {
            instance = creature.GetInstanceScript();
            summons = new SummonList(creature);
            _bossId = bossId;

            if (instance != null)
                SetBoundary(instance.GetBossBoundary(bossId));

            _scheduler.SetValidator(() => !me.HasUnitState(UnitState.Casting));
        }

        public void _Reset()
        {
            if (!me.IsAlive())
                return;

            me.ResetLootMode();
            _events.Reset();
            summons.DespawnAll();
            _scheduler.CancelAll();
            if (instance != null && instance.GetBossState(_bossId) != EncounterState.Done)
                instance.SetBossState(_bossId, EncounterState.NotStarted);
        }

        public void _JustDied()
        {
            _events.Reset();
            summons.DespawnAll();
            _scheduler.CancelAll();
            if (instance != null)
                instance.SetBossState(_bossId, EncounterState.Done);
        }

        public void _JustEngagedWith(Unit who)
        {
            if (instance != null)
            {
                // bosses do not respawn, check only on enter combat
                if (!instance.CheckRequiredBosses(_bossId, who.ToPlayer()))
                {
                    EnterEvadeMode(EvadeReason.SequenceBreak);
                    return;
                }
                instance.SetBossState(_bossId, EncounterState.InProgress);
            }

            me.SetActive(true);
            ScheduleTasks();
        }

        public void TeleportCheaters()
        {
            float x, y, z;
            me.GetPosition(out x, out y, out z);

            foreach (var pair in me.GetCombatManager().GetPvECombatRefs())
            {
                Unit target = pair.Value.GetOther(me);
                if (target.IsControlledByPlayer() && !IsInBoundary(target))
                    target.NearTeleportTo(x, y, z, 0);
            }
        }

        void ForceCombatStopForCreatureEntry(uint entry, float maxSearchRange = 250.0f, bool reset = true)
        {
            Log.outDebug(LogFilter.ScriptsAi, $"BossAI::ForceStopCombatForCreature: called on {me.GetGUID()}. Debug info: {me.GetDebugInfo()}");

            List<Creature> creatures = me.GetCreatureListWithEntryInGrid(entry, maxSearchRange);
            foreach (Creature creature in creatures)
            {
                creature.CombatStop(true);
                creature.DoNotReacquireSpellFocusTarget();
                creature.GetMotionMaster().Clear(MovementGeneratorPriority.Normal);

                if (reset)
                {
                    creature.LoadCreaturesAddon();
                    creature.SetTappedBy(null);
                    creature.ResetPlayerDamageReq();
                    creature.SetLastDamagedTime(0);
                    creature.SetCannotReachTarget(false);
                }
            }
        }

        public override void JustSummoned(Creature summon)
        {
            summons.Summon(summon);
            if (me.IsEngaged())
                DoZoneInCombat(summon);
        }

        public override void SummonedCreatureDespawn(Creature summon)
        {
            summons.Despawn(summon);
        }

        public override void UpdateAI(uint diff)
        {
            if (!UpdateVictim())
                return;

            _events.Update(diff);

            if (me.HasUnitState(UnitState.Casting))
                return;


            _events.ExecuteEvents(eventId =>
            {
                ExecuteEvent(eventId);

                if (me.HasUnitState(UnitState.Casting))
                    return;
            });
        }

        public void _DespawnAtEvade()
        {
            _DespawnAtEvade(TimeSpan.FromSeconds(30));
        }

        public void _DespawnAtEvade(TimeSpan delayToRespawn, Creature who = null)
        {
            if (delayToRespawn < TimeSpan.FromSeconds(2))
            {
                Log.outError(LogFilter.ScriptsAi, $"BossAI::_DespawnAtEvade: called with delay of {delayToRespawn} seconds, defaulting to 2 (me: {me.GetGUID()})");
                delayToRespawn = TimeSpan.FromSeconds(2);
            }

            if (who == null)
                who = me;

            TempSummon whoSummon = who.ToTempSummon();
            if (whoSummon != null)
            {
                Log.outWarn(LogFilter.ScriptsAi, $"BossAI::_DespawnAtEvade: called on a temporary summon (who: {who.GetGUID()})");
                whoSummon.UnSummon();
                return;
            }

            who.DespawnOrUnsummon(TimeSpan.Zero, delayToRespawn);

            if (instance != null && who == me)
                instance.SetBossState(_bossId, EncounterState.Fail);
        }

        public virtual void ExecuteEvent(uint eventId) { }

        public virtual void ScheduleTasks() { }

        public override void Reset() { _Reset(); }
        public override void JustEngagedWith(Unit who) { _JustEngagedWith(who); }
        public override void JustDied(Unit killer) { _JustDied(); }
        public override void JustReachedHome() { _JustReachedHome(); }

        public override bool CanAIAttack(Unit victim) { return IsInBoundary(victim); }

        public void _JustReachedHome() { me.SetActive(false); }

        public uint GetBossId() { return _bossId; }
    }

    public class WorldBossAI : ScriptedAI
    {
        SummonList summons;

        public WorldBossAI(Creature creature) : base(creature)
        {
            summons = new SummonList(creature);
        }

        void _Reset()
        {
            if (!me.IsAlive())
                return;

            _events.Reset();
            summons.DespawnAll();
        }

        void _JustDied()
        {
            _events.Reset();
            summons.DespawnAll();
        }

        void _JustEngagedWith()
        {
            Unit target = SelectTarget(SelectTargetMethod.Random, 0, 0.0f, true);
            if (target != null)
                AttackStart(target);
        }

        public override void JustSummoned(Creature summon)
        {
            summons.Summon(summon);
            Unit target = SelectTarget(SelectTargetMethod.Random, 0, 0.0f, true);
            if (target != null)
                summon.GetAI().AttackStart(target);
        }

        public override void SummonedCreatureDespawn(Creature summon)
        {
            summons.Despawn(summon);
        }

        public override void UpdateAI(uint diff)
        {
            if (!UpdateVictim())
                return;

            _events.Update(diff);

            if (me.HasUnitState(UnitState.Casting))
                return;

            _events.ExecuteEvents(eventId =>
            {
                ExecuteEvent(eventId);

                if (me.HasUnitState(UnitState.Casting))
                    return;
            });
        }

        // Hook used to execute events scheduled into EventMap without the need
        // to override UpdateAI
        // note: You must re-schedule the event within this method if the event
        // is supposed to run more than once
        public virtual void ExecuteEvent(uint eventId) { }

        public override void Reset() { _Reset(); }

        public override void JustEngagedWith(Unit who) { _JustEngagedWith(); }

        public override void JustDied(Unit killer) { _JustDied(); }
    }

    public class SummonList : List<ObjectGuid>
    {
        Creature _me;

        public SummonList(Creature creature)
        {
            _me = creature;
        }

        public void Summon(Creature summon) { Add(summon.GetGUID()); }

        public void DoZoneInCombat(uint entry = 0)
        {
            foreach (var id in this)
            {
                Creature summon = ObjectAccessor.GetCreature(_me, id);
                if (summon != null && summon.IsAIEnabled() && (entry == 0 || summon.GetEntry() == entry))
                {
                    summon.GetAI().DoZoneInCombat();
                }
            }
        }

        public void DespawnEntry(uint entry)
        {
            foreach (var id in this)
            {
                Creature summon = ObjectAccessor.GetCreature(_me, id);
                if (summon == null)
                    Remove(id);
                else if (summon.GetEntry() == entry)
                {
                    Remove(id);
                    summon.DespawnOrUnsummon();
                }
            }
        }

        public void DespawnAll()
        {
            while (!this.Empty())
            {
                Creature summon = ObjectAccessor.GetCreature(_me, this.FirstOrDefault());
                RemoveAt(0);
                if (summon != null)
                    summon.DespawnOrUnsummon();
            }
        }

        public void Despawn(Creature summon) { Remove(summon.GetGUID()); }

        public void DespawnIf(ICheck<ObjectGuid> predicate)
        {
            this.RemoveAll(predicate);
        }

        public void DespawnIf(Predicate<ObjectGuid> predicate)
        {
            RemoveAll(predicate);
        }

        public void RemoveNotExisting()
        {
            foreach (var id in this)
            {
                if (ObjectAccessor.GetCreature(_me, id) == null)
                    Remove(id);
            }
        }

        public void DoAction(int info, ICheck<ObjectGuid> predicate, ushort max = 0)
        {
            // We need to use a copy of SummonList here, otherwise original SummonList would be modified
            List<ObjectGuid> listCopy = new(this);
            listCopy.RandomResize(predicate.Invoke, max);
            DoActionImpl(info, listCopy);
        }

        public void DoAction(int info, Predicate<ObjectGuid> predicate, ushort max = 0)
        {
            // We need to use a copy of SummonList here, otherwise original SummonList would be modified
            List<ObjectGuid> listCopy = new(this);
            listCopy.RandomResize(predicate, max);
            DoActionImpl(info, listCopy);
        }

        public bool HasEntry(uint entry)
        {
            foreach (var id in this)
            {
                Creature summon = ObjectAccessor.GetCreature(_me, id);
                if (summon != null && summon.GetEntry() == entry)
                    return true;
            }

            return false;
        }

        void DoActionImpl(int action, List<ObjectGuid> summons)
        {
            foreach (var guid in summons)
            {
                Creature summon = ObjectAccessor.GetCreature(_me, guid);
                if (summon != null && summon.IsAIEnabled())
                    summon.GetAI().DoAction(action);
            }
        }
    }

    public class EntryCheckPredicate : ICheck<ObjectGuid>
    {
        uint _entry;

        public EntryCheckPredicate(uint entry)
        {
            _entry = entry;
        }

        public bool Invoke(ObjectGuid guid) { return guid.GetEntry() == _entry; }
    }
}
