using Framework.Constants;

namespace Game.Entities
{
    public class RestMgr
    {
        Player _player;
        long _restTime;
        uint _innAreaTriggerId;
        float[] _restBonus = new float[(int)RestTypes.Max];
        RestFlag _restFlagMask;

        public RestMgr(Player player)
        {
            _player = player;
        }

        public void SetRestBonus(RestTypes restType, float restBonus)
        {
            uint next_level_xp;
            bool affectedByRaF = false;

            switch (restType)
            {
                case RestTypes.XP:
                    // Reset restBonus (XP only) for max level players
                    if (_player.GetLevel() >= WorldConfig.GetIntValue(WorldCfg.MaxPlayerLevel))
                        restBonus = 0;

                    next_level_xp = _player.m_activePlayerData.NextLevelXP;
                    affectedByRaF = true;
                    break;
                case RestTypes.Honor:
                    // Reset restBonus (Honor only) for players with max honor level.
                    if (_player.IsMaxHonorLevel())
                        restBonus = 0;

                    next_level_xp = _player.m_activePlayerData.HonorNextLevel;
                    break;
                default:
                    return;
            }

            float rest_bonus_max = next_level_xp * 1.5f / 2;

            if (restBonus < 0)
                restBonus = 0;

            if (restBonus > rest_bonus_max)
                restBonus = rest_bonus_max;

            _restBonus[(int)restType] = restBonus;

            uint oldBonus = (uint)_restBonus[(int)restType];
            if (oldBonus == restBonus)
                return;

            // update data for client
            if (affectedByRaF && _player.GetsRecruitAFriendBonus(true) && (_player.GetSession().IsARecruiter() || _player.GetSession().GetRecruiterId() != 0))
                _player.SetRestState(restType, PlayerRestState.RAFLinked);
            else
            {
                if (_restBonus[(int)restType] > 10)
                    _player.SetRestState(restType, PlayerRestState.Rested);
                else if (_restBonus[(int)restType] <= 1)
                    _player.SetRestState(restType, PlayerRestState.NotRAFLinked);
            }

            // RestTickUpdate
            _player.SetRestThreshold(restType, (uint)_restBonus[(int)restType]);
        }

        public void AddRestBonus(RestTypes restType, float restBonus)
        {
            // Don't add extra rest bonus to max level players. Note: Might need different condition in next expansion for honor XP (PLAYER_LEVEL_MIN_HONOR perhaps).
            if (_player.GetLevel() >= WorldConfig.GetIntValue(WorldCfg.MaxPlayerLevel))
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
                _player.AddPlayerFlag(PlayerFlags.Resting);
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
                _player.RemovePlayerFlag(PlayerFlags.Resting);
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
            _player.SetRestState(restType, state);
            _player.SetRestThreshold(restType, (uint)restBonus);
        }

        public float CalcExtraPerSec(RestTypes restType, float bubble)
        {
            switch (restType)
            {
                case RestTypes.Honor:
                    return _player.m_activePlayerData.HonorNextLevel / 72000.0f * bubble;
                case RestTypes.XP:
                    return _player.m_activePlayerData.NextLevelXP / 72000.0f * bubble;
                default:
                    return 0.0f;
            }
        }

        public float GetRestBonus(RestTypes restType) { return _restBonus[(int)restType]; }
        public bool HasRestFlag(RestFlag restFlag) { return (_restFlagMask & restFlag) != 0; }
        public uint GetInnTriggerId() { return _innAreaTriggerId; }
    }
}
