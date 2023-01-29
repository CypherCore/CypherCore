using System;
using System.Collections.Generic;
using Framework.Constants;

namespace Game.Entities;

public class CreatureGroup
{
    private readonly ulong _leaderSpawnId;
    private readonly List<ulong> _memberIds = new();
    private readonly Dictionary<Creature, FormationInfo> _members = new();
    private bool _engaging;
    private bool _formed;
    private Creature _leader;

    public CreatureGroup(ulong leaderSpawnId)
    {
        _leaderSpawnId = leaderSpawnId;
    }

    public void AddMember(Creature member)
    {
        Log.outDebug(LogFilter.Unit, "CreatureGroup.AddMember: Adding {0}.", member.GetGUID().ToString());

        //Check if it is a leader
        if (member.GetSpawnId() == _leaderSpawnId)
        {
            Log.outDebug(LogFilter.Unit, "{0} is formation leader. Adding group.", member.GetGUID().ToString());
            _leader = member;
        }

        // formation must be registered at this point
        FormationInfo formationInfo = FormationMgr.GetFormationInfo(member.GetSpawnId());
        _members.Add(member, formationInfo);
        _memberIds.Add(member.GetSpawnId());

        bool allmembers = true;

        foreach (var mem in FormationMgr.GetMembers(_leaderSpawnId))
            if (!_memberIds.Contains(mem))
            {
                allmembers = false;
                break;
            }

        _formed = allmembers;
        member.SetFormation(this);
    }

    public void RemoveMember(Creature member)
    {
        if (_leader == member)
            _leader = null;

        _members.Remove(member);
        member.SetFormation(null);
        _formed = false;
    }

    public void MemberEngagingTarget(Creature member, Unit target)
    {
        // used to prevent recursive calls
        if (_engaging)
            return;

        GroupAIFlags groupAI = (GroupAIFlags)FormationMgr.GetFormationInfo(member.GetSpawnId()).GroupAI;

        if (groupAI == 0)
            return;

        if (member == _leader)
        {
            if (!groupAI.HasFlag(GroupAIFlags.MembersAssistLeader))
                return;
        }
        else if (!groupAI.HasFlag(GroupAIFlags.LeaderAssistsMember))
        {
            return;
        }

        _engaging = true;

        foreach (var pair in _members)
        {
            Creature other = pair.Key;

            // Skip self
            if (other == member)
                continue;

            if (!other.IsAlive())
                continue;

            if (((other != _leader && groupAI.HasFlag(GroupAIFlags.MembersAssistLeader)) || (other == _leader && groupAI.HasFlag(GroupAIFlags.LeaderAssistsMember))) &&
                other.IsValidAttackTarget(target))
                other.EngageWithTarget(target);
        }

        _engaging = false;
    }

    public void FormationReset(bool dismiss)
    {
        foreach (var creature in _members.Keys)
            if (creature != _leader &&
                creature.IsAlive())
                creature.GetMotionMaster().MoveIdle();

        //_formed = !dismiss;
    }

    public void LeaderStartedMoving()
    {
        if (_leader == null)
            return;

        foreach (var pair in _members)
        {
            Creature member = pair.Key;

            if (member == _leader ||
                !member.IsAlive() ||
                member.IsEngaged() ||
                !pair.Value.GroupAI.HasAnyFlag((uint)GroupAIFlags.IdleInFormation))
                continue;

            float angle = pair.Value.FollowAngle + MathF.PI; // for some reason, someone thought it was a great idea to invert relativ angles...
            float dist = pair.Value.FollowDist;

            if (!member.HasUnitState(UnitState.FollowFormation))
                member.GetMotionMaster().MoveFormation(_leader, dist, angle, pair.Value.LeaderWaypointIDs[0], pair.Value.LeaderWaypointIDs[1]);
        }
    }

    public bool CanLeaderStartMoving()
    {
        foreach (var pair in _members)
            if (pair.Key != _leader && pair.Key.IsAlive())
                if (pair.Key.IsEngaged() || pair.Key.IsReturningHome())
                    return false;

        return true;
    }

    public Creature GetLeader()
    {
        return _leader;
    }

    public ulong GetLeaderSpawnId()
    {
        return _leaderSpawnId;
    }

    public bool IsEmpty()
    {
        return _members.Empty();
    }

    public bool IsFormed()
    {
        return _formed;
    }

    public bool IsLeader(Creature creature)
    {
        return _leader == creature;
    }

    public bool HasMember(Creature member)
    {
        return _members.ContainsKey(member);
    }
}