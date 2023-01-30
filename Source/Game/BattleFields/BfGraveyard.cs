// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;

namespace Game.BattleFields
{
    public class BfGraveyard
    {
        private readonly List<ObjectGuid> _resurrectQueue = new();
        private readonly ObjectGuid[] _spiritGuide = new ObjectGuid[SharedConst.PvpTeamsCount];

        private uint _controlTeam;
        private uint _graveyardId;

        public BfGraveyard(BattleField battlefield)
        {
            Bf = battlefield;
            _graveyardId = 0;
            _controlTeam = TeamId.Neutral;
            _spiritGuide[0] = ObjectGuid.Empty;
            _spiritGuide[1] = ObjectGuid.Empty;
        }

        protected BattleField Bf { get; set; }

        public void Initialize(uint startControl, uint graveyardId)
        {
            _controlTeam = startControl;
            _graveyardId = graveyardId;
        }

        public void SetSpirit(Creature spirit, int teamIndex)
        {
            if (!spirit)
            {
                Log.outError(LogFilter.Battlefield, "BfGraveyard:SetSpirit: Invalid Spirit.");

                return;
            }

            _spiritGuide[teamIndex] = spirit.GetGUID();
            spirit.SetReactState(ReactStates.Passive);
        }

        public float GetDistance(Player player)
        {
            WorldSafeLocsEntry safeLoc = Global.ObjectMgr.GetWorldSafeLoc(_graveyardId);

            return player.GetDistance2d(safeLoc.Loc.GetPositionX(), safeLoc.Loc.GetPositionY());
        }

        public void AddPlayer(ObjectGuid playerGuid)
        {
            if (!_resurrectQueue.Contains(playerGuid))
            {
                _resurrectQueue.Add(playerGuid);
                Player player = Global.ObjAccessor.FindPlayer(playerGuid);

                if (player)
                    player.CastSpell(player, BattlegroundConst.SpellWaitingForResurrect, true);
            }
        }

        public void RemovePlayer(ObjectGuid playerGuid)
        {
            _resurrectQueue.Remove(playerGuid);

            Player player = Global.ObjAccessor.FindPlayer(playerGuid);

            if (player)
                player.RemoveAurasDueToSpell(BattlegroundConst.SpellWaitingForResurrect);
        }

        public void Resurrect()
        {
            if (_resurrectQueue.Empty())
                return;

            foreach (var guid in _resurrectQueue)
            {
                // Get player object from his Guid
                Player player = Global.ObjAccessor.FindPlayer(guid);

                if (!player)
                    continue;

                // Check  if the player is in world and on the good graveyard
                if (player.IsInWorld)
                {
                    Creature spirit = Bf.GetCreature(_spiritGuide[_controlTeam]);

                    if (spirit)
                        spirit.CastSpell(spirit, BattlegroundConst.SpellSpiritHeal, true);
                }

                // Resurect player
                player.CastSpell(player, BattlegroundConst.SpellResurrectionVisual, true);
                player.ResurrectPlayer(1.0f);
                player.CastSpell(player, 6962, true);
                player.CastSpell(player, BattlegroundConst.SpellSpiritHealMana, true);

                player.SpawnCorpseBones(false);
            }

            _resurrectQueue.Clear();
        }

        // For changing graveyard control
        public void GiveControlTo(uint team)
        {
            // Guide switching
            // Note: Visiblity changes are made by phasing
            /*if (_SpiritGuide[1 - team])
			    _SpiritGuide[1 - team].SetVisible(false);
			if (_SpiritGuide[team])
			    _SpiritGuide[team].SetVisible(true);*/

            _controlTeam = team;
            // Teleport to other graveyard, player witch were on this graveyard
            RelocateDeadPlayers();
        }

        public bool HasNpc(ObjectGuid guid)
        {
            if (_spiritGuide[TeamId.Alliance].IsEmpty() ||
                _spiritGuide[TeamId.Horde].IsEmpty())
                return false;

            if (!Bf.GetCreature(_spiritGuide[TeamId.Alliance]) ||
                !Bf.GetCreature(_spiritGuide[TeamId.Horde]))
                return false;

            return (_spiritGuide[TeamId.Alliance] == guid || _spiritGuide[TeamId.Horde] == guid);
        }

        // Check if a player is in this graveyard's ressurect queue
        public bool HasPlayer(ObjectGuid guid)
        {
            return _resurrectQueue.Contains(guid);
        }

        // Get the graveyard's ID.
        public uint GetGraveyardId()
        {
            return _graveyardId;
        }

        public uint GetControlTeamId()
        {
            return _controlTeam;
        }

        private void RelocateDeadPlayers()
        {
            WorldSafeLocsEntry closestGrave = null;

            foreach (var guid in _resurrectQueue)
            {
                Player player = Global.ObjAccessor.FindPlayer(guid);

                if (!player)
                    continue;

                if (closestGrave != null)
                {
                    player.TeleportTo(closestGrave.Loc);
                }
                else
                {
                    closestGrave = Bf.GetClosestGraveYard(player);

                    if (closestGrave != null)
                        player.TeleportTo(closestGrave.Loc);
                }
            }
        }
    }
}