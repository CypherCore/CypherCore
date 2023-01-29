using System.Collections.Generic;
using Game.Entities;

namespace Game.Achievements;

public class CriteriaDataSet
{
    private uint _criteriaId;
    private readonly List<CriteriaData> _storage = new();

    public void Add(CriteriaData data)
    {
        _storage.Add(data);
    }

    public bool Meets(Player source, WorldObject target, uint miscValue = 0, uint miscValue2 = 0)
    {
        foreach (var data in _storage)
            if (!data.Meets(_criteriaId, source, target, miscValue, miscValue2))
                return false;

        return true;
    }

    public void SetCriteriaId(uint id)
    {
        _criteriaId = id;
    }
}