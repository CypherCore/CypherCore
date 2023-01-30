// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.BattleGrounds;
using Game.Entities;

namespace Game.Maps
{
    public class BattlegroundMap : Map
    {
        private Battleground _bg;

        public BattlegroundMap(uint id, uint expiry, uint InstanceId, Difficulty spawnMode)
            : base(id, expiry, InstanceId, spawnMode)
        {
            InitVisibilityDistance();
        }

        public override void InitVisibilityDistance()
        {
            _VisibleDistance = IsBattleArena() ? Global.WorldMgr.GetMaxVisibleDistanceInArenas() : Global.WorldMgr.GetMaxVisibleDistanceInBG();
            _VisibilityNotifyPeriod = IsBattleArena() ? Global.WorldMgr.GetVisibilityNotifyPeriodInArenas() : Global.WorldMgr.GetVisibilityNotifyPeriodInBG();
        }

        public override TransferAbortParams CannotEnter(Player player)
        {
            if (player.GetMap() == this)
            {
                Log.outError(LogFilter.Maps, "BGMap:CannotEnter - player {0} is already in map!", player.GetGUID().ToString());
                Cypher.Assert(false);

                return new TransferAbortParams(TransferAbortReason.Error);
            }

            if (player.GetBattlegroundId() != GetInstanceId())
                return new TransferAbortParams(TransferAbortReason.LockedToDifferentInstance);

            return base.CannotEnter(player);
        }

        public override bool AddPlayerToMap(Player player, bool initPlayer = true)
        {
            player.InstanceValid = true;

            return base.AddPlayerToMap(player, initPlayer);
        }

        public override void RemovePlayerFromMap(Player player, bool remove)
        {
            Log.outInfo(LogFilter.Maps,
                        "MAP: Removing player '{0}' from bg '{1}' of map '{2}' before relocating to another map",
                        player.GetName(),
                        GetInstanceId(),
                        GetMapName());

            base.RemovePlayerFromMap(player, remove);
        }

        public void SetUnload()
        {
            _unloadTimer = 1;
        }

        public override void RemoveAllPlayers()
        {
            if (HavePlayers())
                foreach (Player player in _activePlayers)
                    if (!player.IsBeingTeleportedFar())
                        player.TeleportTo(player.GetBattlegroundEntryPoint());
        }

        public Battleground GetBG()
        {
            return _bg;
        }

        public void SetBG(Battleground bg)
        {
            _bg = bg;
        }
    }
}