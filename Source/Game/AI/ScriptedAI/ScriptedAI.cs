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
        public ScriptedAI(Creature creature) : base(creature)
        {
            _isCombatMovementAllowed = true;
            _isHeroic = me.GetMap().IsHeroic();
            _difficulty = me.GetMap().GetDifficultyID();
        }

        void AttackStartNoMove(Unit target)
        {
            if (target == null)
                return;

            if (me.Attack(target, true))
                DoStartNoMovement(target);
        }

        // Called before EnterCombat even before the creature is in combat.
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
            if (!UpdateVictim())
                return;

            DoMeleeAttackIfReady();
        }

        //Start movement toward victim
        public void DoStartMovement(Unit target, float distance = 0.0f, float angle = 0.0f)
        {
            if (target != null)
                me.GetMotionMaster().MoveChase(target, distance, angle);
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
        void DoCastSpell(Unit target, SpellInfo spellInfo, bool triggered = false)
        {
            if (target == null || me.IsNonMeleeSpellCast(false))
                return;

            me.StopMoving();
            me.CastSpell(target, spellInfo, triggered ? TriggerCastFlags.FullMask : TriggerCastFlags.None);
        }

        //Plays a sound to all nearby players
        public void DoPlaySoundToSet(WorldObject source, uint soundId)
        {
            if (source == null)
                return;

            if (!CliDB.SoundKitStorage.ContainsKey(soundId))
            {
                Log.outError(LogFilter.Scripts, "Invalid soundId {0} used in DoPlaySoundToSet (Source: TypeId {1}, GUID {2})", soundId, source.GetTypeId(), source.GetGUID().ToString());
                return;
            }

            source.PlayDirectSound(soundId);
        }

        //Spawns a creature relative to me
        public Creature DoSpawnCreature(uint entry, float offsetX, float offsetY, float offsetZ, float angle, TempSummonType type, uint despawntime)
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
            if (me.HasFlag(UnitFields.Flags, UnitFlags.Silenced))
                return null;

            //Using the extended script system we first create a list of viable spells
            SpellInfo[] apSpell = new SpellInfo[SharedConst.MaxCreatureSpells];

            uint spellCount = 0;

            //Check if each spell is viable(set it to null if not)
            for (uint i = 0; i < SharedConst.MaxCreatureSpells; i++)
            {
                SpellInfo tempSpell = Global.SpellMgr.GetSpellInfo(me.m_spells[i]);

                //This spell doesn't exist
                if (tempSpell == null)
                    continue;

                // Targets and Effects checked first as most used restrictions
                //Check the spell targets if specified
                if (targets != 0 && !Convert.ToBoolean(Global.ScriptMgr.spellSummaryStorage[me.m_spells[i]].Targets & (1 << ((int)targets - 1))))
                    continue;

                //Check the type of spell if we are looking for a specific spell type
                if (effect != 0 && !Convert.ToBoolean(Global.ScriptMgr.spellSummaryStorage[me.m_spells[i]].Effects & (1 << ((int)effect - 1))))
                    continue;

                //Check for school if specified
                if (school != 0 && (tempSpell.SchoolMask & school) == 0)
                    continue;

                //Check for spell mechanic if specified
                if (mechanic != 0 && tempSpell.Mechanic != mechanic)
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

        //Drops all threat to 0%. Does not remove players from the threat list
        public void DoResetThreat()
        {
            if (!me.CanHaveThreatList() || me.GetThreatManager().isThreatListEmpty())
            {
                Log.outError(LogFilter.Scripts, "DoResetThreat called for creature that either cannot have threat list or has empty threat list (me entry = {0})", me.GetEntry());
                return;
            }

            var threatlist = me.GetThreatManager().getThreatList();

            foreach (var refe in threatlist)
            {
                Unit unit = Global.ObjAccessor.GetUnit(me, refe.getUnitGuid());
                if (unit != null && DoGetThreat(unit) != 0)
                    DoModifyThreatPercent(unit, -100);
            }
        }

        public float DoGetThreat(Unit unit)
        {
            if (unit == null)
                return 0.0f;
            return me.GetThreatManager().getThreat(unit);
        }

        public void DoModifyThreatPercent(Unit unit, int pct)
        {
            if (unit == null)
                return;
            me.GetThreatManager().modifyThreatPercent(unit, pct);
        }

        void DoTeleportTo(float x, float y, float z, uint time = 0)
        {
            me.Relocate(x, y, z);
            float speed = me.GetDistance(x, y, z) / (time * 0.001f);
            me.MonsterMoveWithSpeed(x, y, z, speed);
        }

        void DoTeleportTo(float[] position)
        {
            me.NearTeleportTo(position[0], position[1], position[2], position[3]);
        }

        //Teleports a player without dropping threat (only teleports to same map)
        void DoTeleportPlayer(Unit unit, float x, float y, float z, float o)
        {
            if (unit == null)
                return;
            Player player = unit.ToPlayer();
            if (player != null)
                player.TeleportTo(unit.GetMapId(), x, y, z, o, TeleportToOptions.NotLeaveCombat);
            else
                Log.outError(LogFilter.Scripts, "Creature {0} (Entry: {1}) Tried to teleport non-player unit (Type: {2} GUID: {3}) to X: {4} Y: {5} Z: {6} O: {7}. Aborted.",
                    me.GetGUID(), me.GetEntry(), unit.GetTypeId(), unit.GetGUID(), x, y, z, o);
        }

        void DoTeleportAll(float x, float y, float z, float o)
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

            return searcher.GetTarget();
        }

        //Returns a list of friendly CC'd units within range
        List<Creature> DoFindFriendlyCC(float range)
        {
            List<Creature> list = new List<Creature>();
            var u_check = new FriendlyCCedInRange(me, range);
            var searcher = new CreatureListSearcher(me, list, u_check);
            Cell.VisitAllObjects(me, searcher, range);

            return list;
        }

        //Returns a list of all friendly units missing a specific buff within range
        public List<Creature> DoFindFriendlyMissingBuff(float range, uint spellId)
        {
            List<Creature> list = new List<Creature>();
            var u_check = new FriendlyMissingBuffInRange(me, range, spellId);
            var searcher = new CreatureListSearcher(me, list, u_check);
            Cell.VisitAllObjects(me, searcher, range);

            return list;
        }

        //Return a player with at least minimumRange from me
        Player GetPlayerAtMinimumRange(float minimumRange)
        {
            var check = new PlayerAtMinimumRangeAway(me, minimumRange);
            var searcher = new PlayerSearcher(me, check);
            Cell.VisitWorldObjects(me, searcher, minimumRange);

            return searcher.GetTarget();
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

        // Called at any Damage from any attacker (before damage apply)
        public override void DamageTaken(Unit attacker, ref uint damage) { }

        //Called at creature death
        public override void JustDied(Unit killer) { }

        //Called at creature killing another unit
        public override void KilledUnit(Unit victim) { }

        // Called when the creature summon successfully other creature
        public override void JustSummoned(Creature summon) { }

        // Called when a summoned creature is despawned
        public override void SummonedCreatureDespawn(Creature summon) { }

        // Called when hit by a spell
        public override void SpellHit(Unit caster, SpellInfo spell) { }

        // Called when spell hits a target
        public override void SpellHitTarget(Unit target, SpellInfo spell) { }

        // Called when AI is temporarily replaced or put back when possess is applied or removed
        public virtual void OnPossess(bool apply) { }

        public static Creature GetClosestCreatureWithEntry(WorldObject source, uint entry, float maxSearchRange, bool alive = true)
        {
            return source.FindNearestCreature(entry, maxSearchRange, alive);
        }

        public GameObject GetClosestGameObjectWithEntry(WorldObject source, uint entry, float maxSearchRange)
        {
            return source.FindNearestGameObject(entry, maxSearchRange);
        }

        //Called at creature reset either by death or evade
        public override void Reset() { }

        //Called at creature aggro either by MoveInLOS or Attack Start
        public override void EnterCombat(Unit victim) { }

        public bool HealthBelowPct(int pct) { return me.HealthBelowPct(pct); }
        public bool HealthAbovePct(int pct) { return me.HealthAbovePct(pct); }

        public bool IsCombatMovementAllowed() { return _isCombatMovementAllowed; }

        // return true for heroic mode. i.e.
        //   - for dungeon in mode 10-heroic,
        //   - for raid in mode 10-Heroic
        //   - for raid in mode 25-heroic
        // DO NOT USE to check raid in mode 25-normal.
        public bool IsHeroic() { return _isHeroic; }

        // return the dungeon or raid difficulty
        public Difficulty GetDifficulty() { return _difficulty; }

        // return true for 25 man or 25 man heroic mode
        public bool Is25ManRaid() { return _difficulty == Difficulty.Raid25N || _difficulty == Difficulty.Raid25HC; }

        public T DungeonMode<T>(T normal5, T heroic10)
        {
            switch (_difficulty)
            {
                case Difficulty.Normal:
                    return normal5;
                case Difficulty.Heroic:
                default:
                    return heroic10;
            }
        }

        public T RaidMode<T>(T normal10, T normal25)
        {
            switch (_difficulty)
            {
                case Difficulty.Raid10N:
                    return normal10;
                case Difficulty.Raid25N:
                default:
                    return normal25;
            }
        }
        public T RaidMode<T>(T normal10, T normal25, T heroic10, T heroic25)
        {
            switch (_difficulty)
            {
                case Difficulty.Raid10N:
                    return normal10;
                case Difficulty.Raid25N:
                    return normal25;
                case Difficulty.Raid10HC:
                    return heroic10;
                case Difficulty.Raid25HC:
                default:
                    return heroic25;
            }
        }

        Difficulty _difficulty;
        bool _isCombatMovementAllowed;
        bool _isHeroic;
    }

    public class BossAI : ScriptedAI
    {
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

            me.SetCombatPulseDelay(0);
            me.ResetLootMode();
            _events.Reset();
            summons.DespawnAll();
            _scheduler.CancelAll();
            if (instance != null)
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

        public void _EnterCombat()
        {
            if (instance != null)
            {
                // bosses do not respawn, check only on enter combat
                if (!instance.CheckRequiredBosses(_bossId))
                {
                    EnterEvadeMode(EvadeReason.SequenceBreak);
                    return;
                }
                instance.SetBossState(_bossId, EncounterState.InProgress);
            }

            me.SetCombatPulseDelay(5);
            me.setActive(true);
            DoZoneInCombat();
            ScheduleTasks();
        }

        void TeleportCheaters()
        {
            float x, y, z;
            me.GetPosition(out x, out y, out z);

            var threatList = me.GetThreatManager().getThreatList();
            foreach (var refe in threatList)
            {
                Unit target = refe.getTarget();
                if (target)
                    if (target.IsTypeId(TypeId.Player) && !CheckBoundary(target))
                        target.NearTeleportTo(x, y, z, 0);
            }
        }

        public override void JustSummoned(Creature summon)
        {
            summons.Summon(summon);
            if (me.IsInCombat())
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

            DoMeleeAttackIfReady();
        }

        public void _DespawnAtEvade(uint delayToRespawn = 30, Creature who = null)
        {
            if (delayToRespawn < 2)
            {
                Log.outError(LogFilter.Scripts, "_DespawnAtEvade called with delay of {0} seconds, defaulting to 2.", delayToRespawn);
                delayToRespawn = 2;
            }

            if (!who)
                who = me;

            TempSummon whoSummon = who.ToTempSummon();
            if (whoSummon)
            {
                Log.outWarn(LogFilter.ScriptsAi, "_DespawnAtEvade called on a temporary summon.");
                whoSummon.UnSummon();
                return;
            }

            who.DespawnOrUnsummon(0, TimeSpan.FromSeconds(delayToRespawn));

            if (instance != null && who == me)
                instance.SetBossState(_bossId, EncounterState.Fail);
        }

        public virtual void ExecuteEvent(uint eventId) { }

        public virtual void ScheduleTasks() { }

        public override void Reset() { _Reset(); }
        public override void EnterCombat(Unit who) { _EnterCombat(); }
        public override void JustDied(Unit killer) { _JustDied(); }
        public override void JustReachedHome() { _JustReachedHome(); }

        public override bool CanAIAttack(Unit victim) { return CheckBoundary(victim); }

        public void _JustReachedHome() { me.setActive(false); }

        public InstanceScript instance;
        public SummonList summons;
        uint _bossId;
    }

    public class WorldBossAI : ScriptedAI
    {
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

        void _EnterCombat()
        {
            Unit target = SelectTarget(SelectAggroTarget.Random, 0, 0.0f, true);
            if (target)
                AttackStart(target);
        }

        public override void JustSummoned(Creature summon)
        {
            summons.Summon(summon);
            Unit target = SelectTarget(SelectAggroTarget.Random, 0, 0.0f, true);
            if (target)
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

            DoMeleeAttackIfReady();
        }

        // Hook used to execute events scheduled into EventMap without the need
        // to override UpdateAI
        // note: You must re-schedule the event within this method if the event
        // is supposed to run more than once
        public virtual void ExecuteEvent(uint eventId) { }

        public override void Reset() { _Reset(); }

        public override void EnterCombat(Unit who) { _EnterCombat(); }

        public override void JustDied(Unit killer) { _JustDied(); }

        SummonList summons;
    }

    public class SummonList : List<ObjectGuid>
    {
        public SummonList(Creature creature)
        {
            me = creature;
        }

        public void Summon(Creature summon) { Add(summon.GetGUID()); }

        public void DoZoneInCombat(uint entry = 0, float maxRangeToNearestTarget = 250.0f)
        {
            foreach (var id in this)
            {
                Creature summon = ObjectAccessor.GetCreature(me, id);
                if (summon && summon.IsAIEnabled && (entry == 0 || summon.GetEntry() == entry))
                {
                    summon.GetAI().DoZoneInCombat(null, maxRangeToNearestTarget);
                }
            }
        }

        public void DespawnEntry(uint entry)
        {
            foreach (var id in this)
            {
                Creature summon = ObjectAccessor.GetCreature(me, id);
                if (!summon)
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
                Creature summon = ObjectAccessor.GetCreature(me, this.FirstOrDefault());
                RemoveAt(0);
                if (summon)
                    summon.DespawnOrUnsummon();
            }
        }

        public void Despawn(Creature summon) { Remove(summon.GetGUID()); }

        public void DespawnIf(Predicate<ObjectGuid> predicate)
        {
            RemoveAll(predicate);
        }

        public void RemoveNotExisting()
        {
            foreach (var id in this)
            {
                if (!ObjectAccessor.GetCreature(me, id))
                    Remove(id);
            }
        }

        public void DoAction(int info, Predicate<ObjectGuid> predicate, ushort max = 0)
        {
            // We need to use a copy of SummonList here, otherwise original SummonList would be modified
            List<ObjectGuid> listCopy = new List<ObjectGuid>(this);
            listCopy.RandomResize(predicate, max);
            DoActionImpl(info, listCopy);
        }

        public bool HasEntry(uint entry)
        {
            foreach (var id in this)
            {
                Creature summon = ObjectAccessor.GetCreature(me, id);
                if (summon && summon.GetEntry() == entry)
                    return true;
            }

            return false;
        }

        void DoActionImpl(int action, List<ObjectGuid> summons)
        {
            foreach (var guid in summons)
            {
                Creature summon = ObjectAccessor.GetCreature(me, guid);
                if (summon && summon.IsAIEnabled)
                    summon.GetAI().DoAction(action);
            }
        }

        Creature me;
    }
}
