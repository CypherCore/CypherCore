// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Framework.Constants;
using Game.DataStorage;
using Game.Entities;
using Game.Maps;
using Game.Maps.Checks;
using Game.Maps.Notifiers;
using Game.Spells;

namespace Game.AI
{
    public class ScriptedAI : CreatureAI
    {
        private readonly Difficulty _difficulty;
        private readonly bool _isHeroic;
        private bool _isCombatMovementAllowed;

        public ScriptedAI(Creature creature) : base(creature)
        {
            _isCombatMovementAllowed = true;
            _isHeroic = me.GetMap().IsHeroic();
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
            //Check if we have a current Target
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
        public void DoCastSpell(Unit target, SpellInfo spellInfo, bool triggered = false)
        {
            if (target == null ||
                me.IsNonMeleeSpellCast(false))
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
        ///  Add specified amount of threat directly to victim (ignores redirection effects) - also puts victim in combat and engages them if necessary
        /// </summary>
        /// <param Name="victim"></param>
        /// <param Name="amount"></param>
        /// <param Name="who"></param>
        public void AddThreat(Unit victim, float amount, Unit who = null)
        {
            if (!victim)
                return;

            if (!who)
                who = me;

            who.GetThreatManager().AddThreat(victim, amount, null, true, true);
        }

        /// <summary>
        ///  Adds/removes the specified percentage from the specified victim's threat (to who, or me if not specified)
        /// </summary>
        /// <param Name="victim"></param>
        /// <param Name="pct"></param>
        /// <param Name="who"></param>
        public void ModifyThreatByPercent(Unit victim, int pct, Unit who = null)
        {
            if (!victim)
                return;

            if (!who)
                who = me;

            who.GetThreatManager().ModifyThreatByPercent(victim, pct);
        }

        /// <summary>
        ///  Resets the victim's threat level to who (or me if not specified) to zero
        /// </summary>
        /// <param Name="victim"></param>
        /// <param Name="who"></param>
        public void ResetThreat(Unit victim, Unit who)
        {
            if (!victim)
                return;

            if (!who)
                who = me;

            who.GetThreatManager().ResetThreat(victim);
        }

        /// <summary>
        ///  Resets the specified unit's threat list (me if not specified) - does not delete entries, just sets their threat to zero
        /// </summary>
        /// <param Name="who"></param>
        public void ResetThreatList(Unit who = null)
        {
            if (!who)
                who = me;

            who.GetThreatManager().ResetAllThreat();
        }

        /// <summary>
        ///  Returns the threat level of victim towards who (or me if not specified)
        /// </summary>
        /// <param Name="victim"></param>
        /// <param Name="who"></param>
        /// <returns></returns>
        public float GetThreat(Unit victim, Unit who = null)
        {
            if (!victim)
                return 0.0f;

            if (!who)
                who = me;

            return who.GetThreatManager().GetThreat(victim);
        }

        //Spawns a creature relative to me
        public Creature DoSpawnCreature(uint entry, float offsetX, float offsetY, float offsetZ, float angle, TempSummonType type, TimeSpan despawntime)
        {
            return me.SummonCreature(entry, me.GetPositionX() + offsetX, me.GetPositionY() + offsetY, me.GetPositionZ() + offsetZ, angle, type, despawntime);
        }

        //Returns spells that meet the specified criteria from the creatures spell list
        public SpellInfo SelectSpell(Unit target, SpellSchoolMask school, Mechanics mechanic, SelectTargetType targets, float rangeMin, float rangeMax, SelectEffect effect)
        {
            //No Target so we can't cast
            if (target == null)
                return null;

            //Silenced so we can't cast
            if (me.HasUnitFlag(UnitFlags.Silenced))
                return null;

            //Using the extended script system we first create a list of viable spells
            SpellInfo[] apSpell = new SpellInfo[SharedConst.MaxCreatureSpells];

            uint spellCount = 0;

            //Check if each spell is viable(set it to null if not)
            for (uint i = 0; i < SharedConst.MaxCreatureSpells; i++)
            {
                SpellInfo tempSpell = Global.SpellMgr.GetSpellInfo(me.Spells[i], me.GetMap().GetDifficultyID());
                AISpellInfoType aiSpell = GetAISpellInfo(me.Spells[i], me.GetMap().GetDifficultyID());

                //This spell doesn't exist
                if (tempSpell == null ||
                    aiSpell == null)
                    continue;

                // Targets and Effects checked first as most used restrictions
                //Check the spell targets if specified
                if (targets != 0 &&
                    !Convert.ToBoolean(aiSpell.Targets & (1 << ((int)targets - 1))))
                    continue;

                //Check the Type of spell if we are looking for a specific spell Type
                if (effect != 0 &&
                    !Convert.ToBoolean(aiSpell.Effects & (1 << ((int)effect - 1))))
                    continue;

                //Check for school if specified
                if (school != 0 &&
                    (tempSpell.SchoolMask & school) == 0)
                    continue;

                //Check for spell mechanic if specified
                if (mechanic != 0 &&
                    tempSpell.Mechanic != mechanic)
                    continue;

                // Continue if we don't have the mana to actually cast this spell
                bool hasPower = true;

                foreach (SpellPowerCost cost in tempSpell.CalcPowerCost(me, tempSpell.GetSchoolMask()))
                    if (cost.Amount > me.GetPower(cost.Power))
                    {
                        hasPower = false;

                        break;
                    }

                if (!hasPower)
                    continue;

                //Check if the spell meets our range requirements
                if (rangeMin != 0 &&
                    me.GetSpellMinRangeForTarget(target, tempSpell) < rangeMin)
                    continue;

                if (rangeMax != 0 &&
                    me.GetSpellMaxRangeForTarget(target, tempSpell) > rangeMax)
                    continue;

                //Check if our Target is in range
                if (me.IsWithinDistInMap(target, me.GetSpellMinRangeForTarget(target, tempSpell)) ||
                    !me.IsWithinDistInMap(target, me.GetSpellMaxRangeForTarget(target, tempSpell)))
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

            return searcher.GetTarget();
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

        public bool HealthBelowPct(int pct)
        {
            return me.HealthBelowPct(pct);
        }

        public bool HealthAbovePct(int pct)
        {
            return me.HealthAbovePct(pct);
        }

        public bool IsCombatMovementAllowed()
        {
            return _isCombatMovementAllowed;
        }

        // return true for heroic mode. i.e.
        //   - for dungeon in mode 10-heroic,
        //   - for raid in mode 10-Heroic
        //   - for raid in mode 25-heroic
        // DO NOT USE to check raid in mode 25-normal.
        public bool IsHeroic()
        {
            return _isHeroic;
        }

        // return the dungeon or raid difficulty
        public Difficulty GetDifficulty()
        {
            return _difficulty;
        }

        // return true for 25 man or 25 man heroic mode
        public bool Is25ManRaid()
        {
            return _difficulty == Difficulty.Raid25N || _difficulty == Difficulty.Raid25HC;
        }

        public T DungeonMode<T>(T normal5, T heroic10)
        {
            return _difficulty switch
            {
                Difficulty.Normal => normal5,
                _ => heroic10
            };
        }

        public T RaidMode<T>(T normal10, T normal25)
        {
            return _difficulty switch
            {
                Difficulty.Raid10N => normal10,
                _ => normal25
            };
        }

        public T RaidMode<T>(T normal10, T normal25, T heroic10, T heroic25)
        {
            return _difficulty switch
            {
                Difficulty.Raid10N => normal10,
                Difficulty.Raid25N => normal25,
                Difficulty.Raid10HC => heroic10,
                _ => heroic25
            };
        }

        /// <summary>
        ///  Stops combat, ignoring restrictions, for the given creature
        /// </summary>
        /// <param Name="who"></param>
        /// <param Name="reset"></param>
        private void ForceCombatStop(Creature who, bool reset = true)
        {
            if (who == null ||
                !who.IsInCombat())
                return;

            who.CombatStop(true);
            who.DoNotReacquireSpellFocusTarget();
            who.GetMotionMaster().Clear(MovementGeneratorPriority.Normal);

            if (reset)
            {
                who.LoadCreaturesAddon();
                who.SetTappedBy(null);
                who.ResetPlayerDamageReq();
                who.SetLastDamagedTime(0);
                who.SetCannotReachTarget(false);
            }
        }

        /// <summary>
        ///  Stops combat, ignoring restrictions, for the found creatures
        /// </summary>
        /// <param Name="entry"></param>
        /// <param Name="maxSearchRange"></param>
        /// <param Name="samePhase"></param>
        /// <param Name="reset"></param>
        private void ForceCombatStopForCreatureEntry(uint entry, float maxSearchRange = 250.0f, bool samePhase = true, bool reset = true)
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
        ///  Stops combat, ignoring restrictions, for the found creatures
        /// </summary>
        /// <param Name="creatureEntries"></param>
        /// <param Name="maxSearchRange"></param>
        /// <param Name="samePhase"></param>
        /// <param Name="reset"></param>
        private void ForceCombatStopForCreatureEntry(List<uint> creatureEntries, float maxSearchRange = 250.0f, bool samePhase = true, bool reset = true)
        {
            foreach (var entry in creatureEntries)
                ForceCombatStopForCreatureEntry(entry, maxSearchRange, samePhase, reset);
        }
    }
}