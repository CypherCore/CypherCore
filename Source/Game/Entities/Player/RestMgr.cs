using Framework.Constants;

namespace Game.Entities
{
    public class RestMgr
    {
        public RestMgr(Player player)
        {
            _player = player;
        }

        public void SetRestBonus(RestTypes restType, float restBonus)
        {
            byte rest_rested_offset;
            byte rest_state_offset;
            ActivePlayerFields next_level_xp_field;
            bool affectedByRaF = false;

            switch (restType)
            {
                case RestTypes.XP:
                    // Reset restBonus (XP only) for max level players
                    if (_player.getLevel() >= WorldConfig.GetIntValue(WorldCfg.MaxPlayerLevel))
                        restBonus = 0;

                    rest_rested_offset = PlayerFieldOffsets.RestRestedXp;
                    rest_state_offset = PlayerFieldOffsets.RestStateXp;
                    next_level_xp_field = ActivePlayerFields.NextLevelXp;
                    affectedByRaF = true;
                    break;
                case RestTypes.Honor:
                    // Reset restBonus (Honor only) for players with max honor level.
                    if (_player.IsMaxHonorLevel())
                        restBonus = 0;

                    rest_rested_offset = PlayerFieldOffsets.RestRestedHonor;
                    rest_state_offset = PlayerFieldOffsets.RestStateHonor;
                    next_level_xp_field = ActivePlayerFields.HonorNextLevel;
                    break;
                default:
                    return;
            }

            if (restBonus < 0)
                restBonus = 0;

            float rest_bonus_max = (float)(_player.GetUInt32Value(next_level_xp_field)) * 1.5f / 2;

            if (restBonus > rest_bonus_max)
                _restBonus[(int)restType] = rest_bonus_max;
            else
                _restBonus[(int)restType] = restBonus;

            // update data for client
            if (affectedByRaF && _player.GetsRecruitAFriendBonus(true) && (_player.GetSession().IsARecruiter() || _player.GetSession().GetRecruiterId() != 0))
                _player.SetUInt32Value(ActivePlayerFields.RestInfo + rest_state_offset, (uint)PlayerRestState.RAFLinked);
            else
            {
                if (_restBonus[(int)restType] > 10)
                    _player.SetUInt32Value(ActivePlayerFields.RestInfo + rest_state_offset, (uint)PlayerRestState.Rested);
                else if (_restBonus[(int)restType] <= 1)
                    _player.SetUInt32Value(ActivePlayerFields.RestInfo + rest_state_offset, (uint)PlayerRestState.NotRAFLinked);
            }

            // RestTickUpdate
            _player.SetUInt32Value(ActivePlayerFields.RestInfo + rest_rested_offset, (uint)_restBonus[(int)restType]);
        }

        public void AddRestBonus(RestTypes restType, float restBonus)
        {
            // Don't add extra rest bonus to max level players. Note: Might need different condition in next expansion for honor XP (PLAYER_LEVEL_MIN_HONOR perhaps).
            if (_player.getLevel() >= WorldConfig.GetIntValue(WorldCfg.MaxPlayerLevel))
                restBonus = 0;

            float totalRestBonus = GetRestBonus(restType) + restBonus;
            SetRestBonus(restType, totalRestBonus);
        }

        public void SetRestFlag(RestFlag restFlag, uint triggerId = 0)
        {
            RestFlag oldRestMask = _restFlagMask;
            _restFlagMask |= restFlag;

            if (oldRestMask == 0 && _restFlagMask != 0) // only set flag/time on the first rest state
            {
                _restTime = Time.UnixTime;
                _player.SetFlag(PlayerFields.Flags, PlayerFlags.Resting);
            }

            if (triggerId != 0)
                _innAreaTriggerId = triggerId;
        }

        public void RemoveRestFlag(RestFlag restFlag)
        {
            RestFlag oldRestMask = _restFlagMask;
            _restFlagMask &= ~restFlag;

            if (oldRestMask != 0 && _restFlagMask == 0) // only remove flag/time on the last rest state remove
            {
                _restTime = 0;
                _player.RemoveFlag(PlayerFields.Flags, PlayerFlags.Resting);
            }
        }

        public uint GetRestBonusFor(RestTypes restType, uint xp)
        {
            uint rested_bonus = (uint)GetRestBonus(restType); // xp for each rested bonus

            if (rested_bonus > xp) // max rested_bonus == xp or (r+x) = 200% xp
                rested_bonus = xp;

            SetRestBonus(restType, GetRestBonus(restType) - rested_bonus);

            Log.outDebug(LogFilter.Player, "RestMgr.GetRestBonus: Player '{0}' ({1}) gain {2} xp (+{3} Rested Bonus). Rested points={4}",
                _player.GetGUID().ToString(), _player.GetName(), xp + rested_bonus, rested_bonus, GetRestBonus(restType));
            return rested_bonus;
        }

        public void Update(uint now)
        {
            if (RandomHelper.randChance(3) && _restTime > 0) // freeze update
            {
                long timeDiff = now - _restTime;
                if (timeDiff >= 10)
                {
                    _restTime = now;

                    float bubble = 0.125f * WorldConfig.GetFloatValue(WorldCfg.RateRestIngame);
                    AddRestBonus(RestTypes.XP, timeDiff * CalcExtraPerSec(RestTypes.XP, bubble));
                }
            }
        }

        public void LoadRestBonus(RestTypes restType, PlayerRestState state, float restBonus)
        {
            _restBonus[(int)restType] = restBonus;
            _player.SetUInt32Value(ActivePlayerFields.RestInfo + (int)restType * 2, (uint)state);
            _player.SetUInt32Value(ActivePlayerFields.RestInfo + (int)restType * 2 + 1, (uint)restBonus);
        }

        public float CalcExtraPerSec(RestTypes restType, float bubble)
        {
            switch (restType)
            {
                case RestTypes.Honor:
                    return (_player.GetUInt32Value(ActivePlayerFields.HonorNextLevel)) / 72000.0f * bubble;
                case RestTypes.XP:
                    return (_player.GetUInt32Value(ActivePlayerFields.NextLevelXp)) / 72000.0f * bubble;
                default:
                    return 0.0f;
            }
        }

        public float GetRestBonus(RestTypes restType) { return _restBonus[(int)restType]; }
        public bool HasRestFlag(RestFlag restFlag) { return (_restFlagMask & restFlag) != 0; }
        public uint GetInnTriggerId() { return _innAreaTriggerId; }

        Player _player;
        long _restTime;
        uint _innAreaTriggerId;
        float[] _restBonus = new float[(int)RestTypes.Max];
        RestFlag _restFlagMask;
    }
}
