// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using System;

namespace Game.Entities
{
    class MirrorTimer
    {
        int m_value;
        int m_maxValue;
        int m_scale = -1;
        int m_spellId;
        MirrorTimerFlags m_flags;

        static int ExpiredTickPeriod = 1 * Time.InMilliseconds;
        PeriodicTimer m_expiredTick = new(ExpiredTickPeriod, ExpiredTickPeriod);

        public bool IsActive() { return m_maxValue > 0; }
        public bool IsRegenerating() { return m_scale > 0; }

        public int GetValue() { return m_value; }
        public void SetValue(int value)
        {
            if (IsActive() && value != GetValue())
                m_flags |= MirrorTimerFlags.Changed;

            m_value = value;
        }

        public int GetMaxValue() { return m_maxValue; }
        public void SetMaxValue(int maxValue)
        {
            if (maxValue == 0)
                Stop();

            if (!IsActive())
                return;

            if (maxValue != GetMaxValue())
                m_flags |= MirrorTimerFlags.Changed;

            m_maxValue = maxValue;
            SetValue(Math.Min(GetValue(), GetMaxValue()));
        }

        public int GetScale() { return m_scale; }
        public void SetScale(int scale)
        {
            if (scale == 0)
            {
                SetPaused(true);
                return;
            }

            if (IsActive() && scale != GetScale())
                m_flags |= MirrorTimerFlags.Changed;

            m_scale = scale;
        }

        public int GetSpellId() { return m_spellId; }

        public bool IsPaused() { return m_flags.HasFlag(MirrorTimerFlags.Paused) && !IsRegenerating(); }
        public void SetPaused(bool state)
        {
            if (IsActive() && state != IsPaused())
                m_flags |= MirrorTimerFlags.PausedChanged;

            if (state)
                m_flags |= MirrorTimerFlags.Paused;
            else
                m_flags &= ~MirrorTimerFlags.Paused;
        }

        public bool IsChanged() { return m_flags.HasFlag(MirrorTimerFlags.Changed); }
        public bool IsPausedChanged() { return m_flags.HasFlag(MirrorTimerFlags.PausedChanged); }
        public void ClearChanged() { m_flags &= ~(MirrorTimerFlags.Changed | MirrorTimerFlags.PausedChanged); }

        public void Start(int maxValue, int spellId)
        {
            if (m_scale < 0)
            {
                m_value = maxValue;
                m_maxValue = maxValue;
                m_spellId = spellId;
                m_flags |= MirrorTimerFlags.Changed;
                m_expiredTick.SetPeriodic(ExpiredTickPeriod, ExpiredTickPeriod);
            }
            else
                Stop();
        }

        public void Start(int value, int maxValue, int spellId)
        {
            Start(maxValue, spellId);

            if (IsActive())
                m_value = value;
        }

        public void Stop()
        {
            if (!IsActive())
                return;

            m_value = 0;
            m_maxValue = 0;
            m_flags = MirrorTimerFlags.Changed;
        }

        public enum UpdateResult
        {
            Inactive,
            Decreased,
            DecreasedExpired,
            ExpiredTicked,
            Regenerated
        }

        public UpdateResult Update(uint diff)
        {
            if (!IsActive() || IsPaused())
                return UpdateResult.Inactive;

            int delta = (int)diff * GetScale();

            if (delta < 0)      // Timer running out
            {
                if (GetValue() > -delta)
                {
                    m_value += delta;
                    return UpdateResult.Decreased;
                }

                if (m_value == 0)   // subsequent ticks after expiration
                {
                    if (!m_expiredTick.Update((int)diff))
                        return UpdateResult.DecreasedExpired;
                }
                else
                    m_value = 0;

                return UpdateResult.ExpiredTicked;
            }
            else                // Timer regenerating
            {
                if (GetValue() + delta < GetMaxValue())
                {
                    m_value += delta;
                    m_expiredTick.SetPeriodic(ExpiredTickPeriod, ExpiredTickPeriod);
                }
                else
                    Stop();

                return UpdateResult.Regenerated;
            }
        }
    }
}
