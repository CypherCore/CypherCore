using Framework.Constants;

namespace Game.Entities;

public class CreatureMovementData
{
    public CreatureChaseMovementType Chase { get; set; }
    public CreatureFlightMovementType Flight { get; set; }
    public CreatureGroundMovementType Ground { get; set; }
    public uint InteractionPauseTimer { get; set; }
    public CreatureRandomMovementType Random { get; set; }
    public bool Rooted { get; set; }
    public bool Swim { get; set; }

    public CreatureMovementData()
    {
        Ground = CreatureGroundMovementType.Run;
        Flight = CreatureFlightMovementType.None;
        Swim = true;
        Rooted = false;
        Chase = CreatureChaseMovementType.Run;
        Random = CreatureRandomMovementType.Walk;
        InteractionPauseTimer = WorldConfig.GetUIntValue(WorldCfg.CreatureStopForPlayer);
    }

    public bool IsGroundAllowed()
    {
        return Ground != CreatureGroundMovementType.None;
    }

    public bool IsSwimAllowed()
    {
        return Swim;
    }

    public bool IsFlightAllowed()
    {
        return Flight != CreatureFlightMovementType.None;
    }

    public bool IsRooted()
    {
        return Rooted;
    }

    public CreatureChaseMovementType GetChase()
    {
        return Chase;
    }

    public CreatureRandomMovementType GetRandom()
    {
        return Random;
    }

    public uint GetInteractionPauseTimer()
    {
        return InteractionPauseTimer;
    }

    public override string ToString()
    {
        return $"Ground: {Ground}, Swim: {Swim}, Flight: {Flight} {(Rooted ? ", Rooted" : "")}, Chase: {Chase}, Random: {Random}, InteractionPauseTimer: {InteractionPauseTimer}";
    }
}