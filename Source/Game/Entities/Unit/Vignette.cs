// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.DataStorage;
using Game.Maps;
using Game.Networking.Packets;

namespace Game.Entities
{
    static class Vignettes
    {
        public static void UpdatePosition(VignetteData vignette, WorldObject owner)
        {
            vignette.Position = owner.GetPosition();
            WmoLocation wmoLocation = owner.GetCurrentWmo();
            if (wmoLocation != null)
            {
                vignette.WMOGroupID = (uint)wmoLocation.GroupId;
                vignette.WMODoodadPlacementID = wmoLocation.UniqueId;
            }
        }

        public static void UpdateHealth(VignetteData vignette, Unit owner)
        {
            vignette.HealthPercent = (float)(owner.GetHealth()) / (float)(owner.GetMaxHealth()); // converted to percentage in lua
        }

        public static void SendVignetteUpdate(VignetteData vignette, WorldObject owner)
        {
            if (!owner.IsInWorld)
                return;

            VignetteUpdate vignetteUpdate = new();
            vignette.FillPacket(vignetteUpdate.Updated);
            vignetteUpdate.Write();

            var sender = (Player receiver) =>
            {
                if (CanSee(receiver, vignette))
                    receiver.SendPacket(vignetteUpdate);
            };

            Player playerOwner = owner.ToPlayer();
            if (playerOwner != null)
                sender(playerOwner);

            MessageDistDeliverer notifier = new(owner, sender, owner.GetVisibilityRange());
            Cell.VisitWorldObjects(owner, notifier, owner.GetVisibilityRange());
        }

        public static void SendVignetteAdded(VignetteData vignette, WorldObject owner)
        {
            if (!owner.IsInWorld)
                return;

            VignetteUpdate vignetteUpdate = new();
            vignette.FillPacket(vignetteUpdate.Added);
            vignetteUpdate.Write();

            var sender = (Player receiver) =>
            {
                if (CanSee(receiver, vignette))
                    receiver.SendPacket(vignetteUpdate);
            };

            MessageDistDeliverer notifier = new(owner, sender, owner.GetVisibilityRange());
            Cell.VisitWorldObjects(owner, notifier, owner.GetVisibilityRange());
        }

        public static VignetteData Create(VignetteRecord vignetteData, WorldObject owner)
        {
            VignetteData vignette = new();
            vignette.Guid = ObjectGuid.Create(HighGuid.Vignette, owner.GetMapId(), vignetteData.ID, owner.GetMap().GenerateLowGuid(HighGuid.Vignette));
            vignette.Object = owner.GetGUID();
            vignette.Position = owner.GetPosition();
            vignette.Data = vignetteData;
            vignette.ZoneID = owner.GetZoneId(); // not updateable
            UpdatePosition(vignette, owner);
            Unit unitOwner = owner.ToUnit();
            if (unitOwner != null)
                UpdateHealth(vignette, unitOwner);

            if (vignetteData.IsInfiniteAOI())
                owner.GetMap().AddInfiniteAOIVignette(vignette);
            else
                SendVignetteAdded(vignette, owner);

            return vignette;
        }

        public static void Update(VignetteData vignette, WorldObject owner)
        {
            UpdatePosition(vignette, owner);
            Unit unitOwner = owner.ToUnit();
            if (unitOwner != null)
                UpdateHealth(vignette, unitOwner);

            if (vignette.Data.IsInfiniteAOI())
                vignette.NeedUpdate = true;
            else
                SendVignetteUpdate(vignette, owner);
        }

        public static void Remove(VignetteData vignette, WorldObject owner)
        {
            if (vignette.Data.IsInfiniteAOI())
                owner.GetMap().RemoveInfiniteAOIVignette(vignette);
            else
            {
                VignetteUpdate vignetteUpdate = new();
                vignetteUpdate.Removed.Add(vignette.Guid);
                owner.SendMessageToSet(vignetteUpdate, true);
            }
        }

        public static bool CanSee(Player player, VignetteData vignette)
        {
            if (vignette.Data.HasFlag(VignetteFlags.ZoneInfiniteAOI))
                if (vignette.ZoneID != player.GetZoneId())
                    return false;

            if (vignette.Data.VisibleTrackingQuestID != 0)
                if (player.IsQuestRewarded(vignette.Data.VisibleTrackingQuestID))
                    return false;

            if (!ConditionManager.IsPlayerMeetingCondition(player, vignette.Data.PlayerConditionID))
                return false;

            return true;
        }
    }

    public class VignetteData
    {
        public ObjectGuid Guid;
        public ObjectGuid Object;
        public Position Position;
        public VignetteRecord Data;
        public uint ZoneID;
        public uint WMOGroupID;
        public uint WMODoodadPlacementID;
        public float HealthPercent = 1.0f;
        public bool NeedUpdate;

        public void FillPacket(VignetteDataSet dataSet)
        {
            dataSet.IDs.Add(Guid);

            VignetteDataPkt data = new();
            data.ObjGUID = Object;
            data.Position = Position;
            data.VignetteID = (int)Data.ID;
            data.ZoneID = ZoneID;
            data.WMOGroupID = WMOGroupID;
            data.WMODoodadPlacementID = WMODoodadPlacementID;
            data.HealthPercent = HealthPercent;

            dataSet.Data.Add(data);
        }
    }
}
