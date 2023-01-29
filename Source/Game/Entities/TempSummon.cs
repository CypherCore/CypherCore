// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Framework.Constants;
using Game.DataStorage;
using Game.Maps;

namespace Game.Entities
{
    public class TempSummon : Creature
    {
        private bool _canFollowOwner;
        private uint? _creatureIdVisibleToSummoner;
        private uint? _displayIdVisibleToSummoner;
        private uint _lifetime;

        public SummonPropertiesRecord Properties { get; set; }
        private ObjectGuid _summonerGUID;
        private uint _timer;
        private TempSummonType _type;

        public TempSummon(SummonPropertiesRecord properties, WorldObject owner, bool isWorldObject) : base(isWorldObject)
        {
            Properties = properties;
            _type = TempSummonType.ManualDespawn;

            _summonerGUID = owner != null ? owner.GetGUID() : ObjectGuid.Empty;
            UnitTypeMask |= UnitTypeMask.Summon;
            _canFollowOwner = true;
        }

        public WorldObject GetSummoner()
        {
            return !_summonerGUID.IsEmpty() ? Global.ObjAccessor.GetWorldObject(this, _summonerGUID) : null;
        }

        public Unit GetSummonerUnit()
        {
            WorldObject summoner = GetSummoner();

            if (summoner != null)
                return summoner.ToUnit();

            return null;
        }

        public Creature GetSummonerCreatureBase()
        {
            return !_summonerGUID.IsEmpty() ? ObjectAccessor.GetCreature(this, _summonerGUID) : null;
        }

        public GameObject GetSummonerGameObject()
        {
            WorldObject summoner = GetSummoner();

            if (summoner != null)
                return summoner.ToGameObject();

            return null;
        }

        public override void Update(uint diff)
        {
            base.Update(diff);

            if (DeathState == DeathState.Dead)
            {
                UnSummon();

                return;
            }

            switch (_type)
            {
                case TempSummonType.ManualDespawn:
                case TempSummonType.DeadDespawn:
                    break;
                case TempSummonType.TimedDespawn:
                    {
                        if (_timer <= diff)
                        {
                            UnSummon();

                            return;
                        }

                        _timer -= diff;

                        break;
                    }
                case TempSummonType.TimedDespawnOutOfCombat:
                    {
                        if (!IsInCombat())
                        {
                            if (_timer <= diff)
                            {
                                UnSummon();

                                return;
                            }

                            _timer -= diff;
                        }
                        else if (_timer != _lifetime)
                        {
                            _timer = _lifetime;
                        }

                        break;
                    }

                case TempSummonType.CorpseTimedDespawn:
                    {
                        if (DeathState == DeathState.Corpse)
                        {
                            if (_timer <= diff)
                            {
                                UnSummon();

                                return;
                            }

                            _timer -= diff;
                        }

                        break;
                    }
                case TempSummonType.CorpseDespawn:
                    {
                        // if DeathState is DEAD, CORPSE was skipped
                        if (DeathState == DeathState.Corpse)
                        {
                            UnSummon();

                            return;
                        }

                        break;
                    }
                case TempSummonType.TimedOrCorpseDespawn:
                    {
                        if (DeathState == DeathState.Corpse)
                        {
                            UnSummon();

                            return;
                        }

                        if (!IsInCombat())
                        {
                            if (_timer <= diff)
                            {
                                UnSummon();

                                return;
                            }
                            else
                            {
                                _timer -= diff;
                            }
                        }
                        else if (_timer != _lifetime)
                        {
                            _timer = _lifetime;
                        }

                        break;
                    }
                case TempSummonType.TimedOrDeadDespawn:
                    {
                        if (!IsInCombat() &&
                            IsAlive())
                        {
                            if (_timer <= diff)
                            {
                                UnSummon();

                                return;
                            }
                            else
                            {
                                _timer -= diff;
                            }
                        }
                        else if (_timer != _lifetime)
                        {
                            _timer = _lifetime;
                        }

                        break;
                    }
                default:
                    UnSummon();
                    Log.outError(LogFilter.Unit, "Temporary summoned creature (entry: {0}) have unknown Type {1} of ", GetEntry(), _type);

                    break;
            }
        }

        public virtual void InitStats(uint duration)
        {
            Cypher.Assert(!IsPet());

            _timer = duration;
            _lifetime = duration;

            if (_type == TempSummonType.ManualDespawn)
                _type = (duration == 0) ? TempSummonType.DeadDespawn : TempSummonType.TimedDespawn;

            Unit owner = GetSummonerUnit();

            if (owner != null &&
                IsTrigger() &&
                Spells[0] != 0)
                if (owner.IsTypeId(TypeId.Player))
                    ControlledByPlayer = true;

            if (owner != null &&
                owner.IsPlayer())
            {
                CreatureSummonedData summonedData = Global.ObjectMgr.GetCreatureSummonedData(GetEntry());

                if (summonedData != null)
                {
                    _creatureIdVisibleToSummoner = summonedData.CreatureIDVisibleToSummoner;

                    if (summonedData.CreatureIDVisibleToSummoner.HasValue)
                    {
                        CreatureTemplate creatureTemplateVisibleToSummoner = Global.ObjectMgr.GetCreatureTemplate(summonedData.CreatureIDVisibleToSummoner.Value);
                        _displayIdVisibleToSummoner = ObjectManager.ChooseDisplayId(creatureTemplateVisibleToSummoner, null).CreatureDisplayID;
                    }
                }
            }

            if (Properties == null)
                return;

            if (owner != null)
            {
                int slot = Properties.Slot;

                if (slot > 0)
                {
                    if (!owner.SummonSlot[slot].IsEmpty() &&
                        owner.SummonSlot[slot] != GetGUID())
                    {
                        Creature oldSummon = GetMap().GetCreature(owner.SummonSlot[slot]);

                        if (oldSummon != null &&
                            oldSummon.IsSummon())
                            oldSummon.ToTempSummon().UnSummon();
                    }

                    owner.SummonSlot[slot] = GetGUID();
                }

                if (!Properties.GetFlags().HasFlag(SummonPropertiesFlags.UseCreatureLevel))
                    SetLevel(owner.GetLevel());
            }

            uint faction = Properties.Faction;

            if (owner && Properties.GetFlags().HasFlag(SummonPropertiesFlags.UseSummonerFaction)) // TODO: Determine priority between faction and flag
                faction = owner.GetFaction();

            if (faction != 0)
                SetFaction(faction);

            if (Properties.GetFlags().HasFlag(SummonPropertiesFlags.SummonFromBattlePetJournal))
                RemoveNpcFlag(NPCFlags.WildBattlePet);
        }

        public virtual void InitSummon()
        {
            WorldObject owner = GetSummoner();

            if (owner != null)
            {
                if (owner.IsCreature())
                    owner.ToCreature().GetAI()?.JustSummoned(this);
                else if (owner.IsGameObject())
                    owner.ToGameObject().GetAI()?.JustSummoned(this);

                if (IsAIEnabled())
                    GetAI().IsSummonedBy(owner);
            }
        }

        public override void UpdateObjectVisibilityOnCreate()
        {
            List<WorldObject> objectsToUpdate = new();
            objectsToUpdate.Add(this);

            SmoothPhasing smoothPhasing = GetSmoothPhasing();

            if (smoothPhasing != null)
            {
                SmoothPhasingInfo infoForSeer = smoothPhasing.GetInfoForSeer(GetDemonCreatorGUID());

                if (infoForSeer != null &&
                    infoForSeer.ReplaceObject.HasValue &&
                    smoothPhasing.IsReplacing(infoForSeer.ReplaceObject.Value))
                {
                    WorldObject original = Global.ObjAccessor.GetWorldObject(this, infoForSeer.ReplaceObject.Value);

                    if (original != null)
                        objectsToUpdate.Add(original);
                }
            }

            VisibleChangesNotifier notifier = new(objectsToUpdate);
            Cell.VisitWorldObjects(this, notifier, GetVisibilityRange());
        }

        public override void UpdateObjectVisibilityOnDestroy()
        {
            List<WorldObject> objectsToUpdate = new();
            objectsToUpdate.Add(this);

            WorldObject original = null;
            SmoothPhasing smoothPhasing = GetSmoothPhasing();

            if (smoothPhasing != null)
            {
                SmoothPhasingInfo infoForSeer = smoothPhasing.GetInfoForSeer(GetDemonCreatorGUID());

                if (infoForSeer != null &&
                    infoForSeer.ReplaceObject.HasValue &&
                    smoothPhasing.IsReplacing(infoForSeer.ReplaceObject.Value))
                    original = Global.ObjAccessor.GetWorldObject(this, infoForSeer.ReplaceObject.Value);

                if (original != null)
                {
                    objectsToUpdate.Add(original);

                    // disable replacement without removing - it is still needed for next step (visibility update)
                    SmoothPhasing originalSmoothPhasing = original.GetSmoothPhasing();

                    originalSmoothPhasing?.DisableReplacementForSeer(GetDemonCreatorGUID());
                }
            }

            VisibleChangesNotifier notifier = new(objectsToUpdate);
            Cell.VisitWorldObjects(this, notifier, GetVisibilityRange());

            if (original != null) // original is only != null when it was replaced
            {
                SmoothPhasing originalSmoothPhasing = original.GetSmoothPhasing();

                originalSmoothPhasing?.ClearViewerDependentInfo(GetDemonCreatorGUID());
            }
        }

        public void SetTempSummonType(TempSummonType type)
        {
            _type = type;
        }

        public virtual void UnSummon(uint msTime = 0)
        {
            if (msTime != 0)
            {
                ForcedUnsummonDelayEvent pEvent = new(this);

                Events.AddEvent(pEvent, Events.CalculateTime(TimeSpan.FromMilliseconds(msTime)));

                return;
            }

            Cypher.Assert(!IsPet());

            if (IsPet())
            {
                ToPet().Remove(PetSaveMode.NotInSlot);
                Cypher.Assert(!IsInWorld);

                return;
            }

            WorldObject owner = GetSummoner();

            if (owner != null)
            {
                if (owner.IsCreature())
                    owner.ToCreature().GetAI()?.SummonedCreatureDespawn(this);
                else if (owner.IsGameObject())
                    owner.ToGameObject().GetAI()?.SummonedCreatureDespawn(this);
            }

            AddObjectToRemoveList();
        }

        public override void RemoveFromWorld()
        {
            if (!IsInWorld)
                return;

            if (Properties != null)
            {
                int slot = Properties.Slot;

                if (slot > 0)
                {
                    Unit owner = GetSummonerUnit();

                    if (owner != null)
                        if (owner.SummonSlot[slot] == GetGUID())
                            owner.SummonSlot[slot].Clear();
                }
            }

            if (!GetOwnerGUID().IsEmpty())
                Log.outError(LogFilter.Unit, "Unit {0} has owner Guid when removed from world", GetEntry());

            base.RemoveFromWorld();
        }

        public override string GetDebugInfo()
        {
            return $"{base.GetDebugInfo()}\nTempSummonType : {GetSummonType()} Summoner: {GetSummonerGUID()} Timer: {GetTimer()}";
        }

        public override void SaveToDB(uint mapid, List<Difficulty> spawnDifficulties)
        {
        }

        public ObjectGuid GetSummonerGUID()
        {
            return _summonerGUID;
        }

        private TempSummonType GetSummonType()
        {
            return _type;
        }

        public uint GetTimer()
        {
            return _timer;
        }

        public uint? GetCreatureIdVisibleToSummoner()
        {
            return _creatureIdVisibleToSummoner;
        }

        public uint? GetDisplayIdVisibleToSummoner()
        {
            return _displayIdVisibleToSummoner;
        }

        public bool CanFollowOwner()
        {
            return _canFollowOwner;
        }

        public void SetCanFollowOwner(bool can)
        {
            _canFollowOwner = can;
        }
    }
}