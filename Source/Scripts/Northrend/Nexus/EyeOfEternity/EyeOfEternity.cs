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
using Framework.GameMath;
using Game.Entities;
using Game.Maps;
using Game.Scripting;
using System.Collections.Generic;
using System.Linq;

namespace Scripts.Northrend.Nexus.EyeOfEternity
{
    struct EyeOfEternityConst
    {
        public const uint MaxEncounter = 1;
        public const uint EventFocusingIris = 20711;
    }

    struct InstanceData
    {
        public const uint MalygosEvent = 1;
        public const uint VortexHandling = 2;
        public const uint PowerSparksHandling = 3;
        public const uint RespawnIris = 4;
    }

    struct InstanceData64
    {
        public const uint Trigger = 0;
        public const uint Malygos = 1;
        public const uint Platform = 2;
        public const uint AlexstraszaBunnyGUID = 3;
        public const uint HeartOfMagicGUID = 4;
        public const uint FocusingIrisGUID = 5;
        public const uint GiftBoxBunnyGUID = 6;
    }

    struct InstanceNpcs
    {
        public const uint Malygos = 28859;
        public const uint VortexTrigger = 30090;
        public const uint PortalTrigger = 30118;
        public const uint PowerSpark = 30084;
        public const uint HoverDiskMelee = 30234;
        public const uint HoverDiskCaster = 30248;
        public const uint ArcaneOverload = 30282;
        public const uint WyrmrestSkytalon = 30161;
        public const uint Alexstrasza = 32295;
        public const uint AlexstraszaBunny = 31253;
        public const uint AlexstraszasGift = 32448;
        public const uint SurgeOfPower = 30334;
    }

    struct InstanceGameObjects
    {
        public const uint NexusRaidPlatform = 193070;
        public const uint ExitPortal = 193908;
        public const uint FocusingIris10 = 193958;
        public const uint FocusingIris25 = 193960;
        public const uint AlexstraszaSGift10 = 193905;
        public const uint AlexstraszaSGift25 = 193967;
        public const uint HeartOfMagic10 = 194158;
        public const uint HeartOfMagic25 = 194159;
    }

    struct InstanceSpells
    {
        public const uint Vortex4 = 55853; // Damage | Used To Enter To The Vehicle
        public const uint Vortex5 = 56263; // Damage | Used To Enter To The Vehicle
        public const uint PortalOpened = 61236;
        public const uint RideRedDragonTriggered = 56072;
        public const uint IrisOpened = 61012; // Visual When Starting Encounter
        public const uint SummomRedDragonBuddy = 56070;
    }

    [Script]
    class instance_eye_of_eternity : InstanceMapScript
    {
        public instance_eye_of_eternity() : base("instance_eye_of_eternity", 616) { }

        class instance_eye_of_eternity_InstanceMapScript : InstanceScript
        {
            public instance_eye_of_eternity_InstanceMapScript(InstanceMap map) : base(map)
            {
                SetHeaders("EOE");
                SetBossNumber(EyeOfEternityConst.MaxEncounter);
            }

            public override void OnPlayerEnter(Player player)
            {
                if (GetBossState(0) == EncounterState.Done)
                    player.CastSpell(player, InstanceSpells.SummomRedDragonBuddy, true);
            }

            public override bool SetBossState(uint type, EncounterState state)
            {
                if (!base.SetBossState(type, state))
                    return false;

                if (type == InstanceData.MalygosEvent)
                {
                    if (state == EncounterState.Fail)
                    {
                        foreach (var triggerGuid in portalTriggers)
                        {
                            Creature trigger = instance.GetCreature(triggerGuid);
                            if (trigger)
                            {
                                // just in case
                                trigger.RemoveAllAuras();
                                trigger.GetAI().Reset();
                            }
                        }

                        SpawnGameObject(InstanceGameObjects.ExitPortal, exitPortalPosition);

                        GameObject platform = instance.GetGameObject(platformGUID);
                        if (platform)
                            platform.RemoveFlag(GameObjectFields.Flags, GameObjectFlags.Destroyed);
                    }
                    else if (state == EncounterState.Done)
                        SpawnGameObject(InstanceGameObjects.ExitPortal, exitPortalPosition);
                }
                return true;
            }

            // @todo this should be handled in map, maybe add a summon function in map
            // There is no other way afaik...
            void SpawnGameObject(uint entry, Position pos)
            {
                GameObject go = GameObject.CreateGameObject(entry, instance, pos, Quaternion.fromEulerAnglesZYX(pos.GetOrientation(), 0.0f, 0.0f), 255, GameObjectState.Ready);
                if (go)
                    instance.AddToMap(go);
            }

            public override void OnGameObjectCreate(GameObject go)
            {
                switch (go.GetEntry())
                {
                    case InstanceGameObjects.NexusRaidPlatform:
                        platformGUID = go.GetGUID();
                        break;
                    case InstanceGameObjects.FocusingIris10:
                    case InstanceGameObjects.FocusingIris25:
                        irisGUID = go.GetGUID();
                        focusingIrisPosition = go.GetPosition();
                        break;
                    case InstanceGameObjects.ExitPortal:
                        exitPortalGUID = go.GetGUID();
                        exitPortalPosition = go.GetPosition();
                        break;
                    case InstanceGameObjects.HeartOfMagic10:
                    case InstanceGameObjects.HeartOfMagic25:
                        heartOfMagicGUID = go.GetGUID();
                        break;
                    default:
                        break;
                }
            }

            public override void OnCreatureCreate(Creature creature)
            {
                switch (creature.GetEntry())
                {
                    case InstanceNpcs.VortexTrigger:
                        vortexTriggers.Add(creature.GetGUID());
                        break;
                    case InstanceNpcs.Malygos:
                        malygosGUID = creature.GetGUID();
                        break;
                    case InstanceNpcs.PortalTrigger:
                        portalTriggers.Add(creature.GetGUID());
                        break;
                    case InstanceNpcs.AlexstraszaBunny:
                        alexstraszaBunnyGUID = creature.GetGUID();
                        break;
                    case InstanceNpcs.AlexstraszasGift:
                        giftBoxBunnyGUID = creature.GetGUID();
                        break;
                }
            }

            public override void OnUnitDeath(Unit unit)
            {
                if (!unit.IsTypeId(TypeId.Player))
                    return;

                // Player continues to be moving after death no matter if spline will be cleared along with all movements,
                // so on next world tick was all about delay if box will pop or not (when new movement will be registered)
                // since in EoE you never stop falling. However root at this precise* moment works,
                // it will get cleared on release. If by any chance some lag happen "Reload()" and "RepopMe()" works,
                // last test I made now gave me 50/0 of this bug so I can't do more about it.
                unit.SetControlled(true, UnitState.Root);
            }

            public override void ProcessEvent(WorldObject obj, uint eventId)
            {
                if (eventId == EyeOfEternityConst.EventFocusingIris)
                {
                    Creature alexstraszaBunny = instance.GetCreature(alexstraszaBunnyGUID);
                    if (alexstraszaBunny)
                        alexstraszaBunny.CastSpell(alexstraszaBunny, InstanceSpells.IrisOpened);

                    GameObject iris = instance.GetGameObject(irisGUID);
                    if (iris)
                        iris.SetFlag(GameObjectFields.Flags, GameObjectFlags.InUse);

                    Creature malygos = instance.GetCreature(malygosGUID);
                    if (malygos)
                        malygos.GetAI().DoAction(0); // ACTION_LAND_ENCOUNTER_START

                    GameObject exitPortal = instance.GetGameObject(exitPortalGUID);
                    if (exitPortal)
                        exitPortal.Delete();
                }
            }

            void VortexHandling()
            {
                Creature malygos = instance.GetCreature(malygosGUID);
                if (malygos)
                {
                    var threatList = malygos.GetThreatManager().getThreatList();
                    foreach (var guid in vortexTriggers)
                    {
                        if (threatList.Empty())
                            return;

                        byte counter = 0;
                        Creature trigger = instance.GetCreature(guid);
                        if (trigger)
                        {
                            // each trigger have to cast the spell to 5 players.
                            foreach (var refe in threatList)
                            {
                                if (counter >= 5)
                                    break;

                                Unit target = refe.getTarget();
                                if (target)
                                {
                                    Player player = target.ToPlayer();

                                    if (!player || player.IsGameMaster() || player.HasAura(InstanceSpells.Vortex4))
                                        continue;

                                    player.CastSpell(trigger, InstanceSpells.Vortex4, true);
                                    counter++;
                                }
                            }
                        }
                    }
                }
            }

            void PowerSparksHandling()
            {
                bool next = (lastPortalGUID == portalTriggers.Last() || !lastPortalGUID.IsEmpty() ? true : false);

                foreach (var guid in portalTriggers)
                {
                    if (next)
                    {
                        Creature trigger = instance.GetCreature(guid);
                        if (trigger)
                        {
                            lastPortalGUID = trigger.GetGUID();
                            trigger.CastSpell(trigger, InstanceSpells.PortalOpened, true);
                            return;
                        }
                    }

                    if (guid == lastPortalGUID)
                        next = true;
                }
            }

            public override void SetData(uint data, uint value)
            {
                switch (data)
                {
                    case InstanceData.VortexHandling:
                        VortexHandling();
                        break;
                    case InstanceData.PowerSparksHandling:
                        PowerSparksHandling();
                        break;
                    case InstanceData.RespawnIris:
                        SpawnGameObject(instance.GetDifficultyID() == Difficulty.Raid10N ? InstanceGameObjects.FocusingIris10 : InstanceGameObjects.FocusingIris25, focusingIrisPosition);
                        break;
                }
            }

            public override ObjectGuid GetGuidData(uint data)
            {
                switch (data)
                {
                    case InstanceData64.Trigger:
                        return vortexTriggers.First();
                    case InstanceData64.Malygos:
                        return malygosGUID;
                    case InstanceData64.Platform:
                        return platformGUID;
                    case InstanceData64.AlexstraszaBunnyGUID:
                        return alexstraszaBunnyGUID;
                    case InstanceData64.HeartOfMagicGUID:
                        return heartOfMagicGUID;
                    case InstanceData64.FocusingIrisGUID:
                        return irisGUID;
                    case InstanceData64.GiftBoxBunnyGUID:
                        return giftBoxBunnyGUID;
                }

                return ObjectGuid.Empty;
            }

            List<ObjectGuid> vortexTriggers = new List<ObjectGuid>();
            List<ObjectGuid> portalTriggers = new List<ObjectGuid>();
            ObjectGuid malygosGUID;
            ObjectGuid irisGUID;
            ObjectGuid lastPortalGUID;
            ObjectGuid platformGUID;
            ObjectGuid exitPortalGUID;
            ObjectGuid heartOfMagicGUID;
            ObjectGuid alexstraszaBunnyGUID;
            ObjectGuid giftBoxBunnyGUID;
            Position focusingIrisPosition;
            Position exitPortalPosition;
        }

        public override InstanceScript GetInstanceScript(InstanceMap map)
        {
            return new instance_eye_of_eternity_InstanceMapScript(map);
        }
    }
}
