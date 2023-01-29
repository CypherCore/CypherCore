using System.Collections.Generic;
using System.Numerics;
using Framework.Constants;

namespace Game.Entities;

public unsafe class AreaTriggerCreateProperties
{
    public int AnimId { get; set; }
    public uint AnimKitId { get; set; }

    public uint DecalPropertiesId { get; set; }
    public AreaTriggerScaleInfo ExtraScale { get; set; } = new();
    public uint FacingCurveId { get; set; }

    public uint Id { get; set; }
    public uint MorphCurveId { get; set; }

    public uint MoveCurveId { get; set; }
    public AreaTriggerOrbitInfo OrbitInfo { get; set; }

    public AreaTriggerScaleInfo OverrideScale { get; set; } = new();
    public List<Vector2> PolygonVertices { get; set; } = new();
    public List<Vector2> PolygonVerticesTarget { get; set; } = new();
    public uint ScaleCurveId { get; set; }

    public uint ScriptId { get; set; }

    public AreaTriggerShapeInfo Shape { get; set; } = new();
    public List<Vector3> SplinePoints { get; set; } = new();
    public AreaTriggerTemplate Template { get; set; }

    public uint TimeToTarget { get; set; }
    public uint TimeToTargetScale { get; set; }

    public AreaTriggerCreateProperties()
    {
        // legacy code from before it was known what each curve field does
        ExtraScale.Raw.Data[5] = 1065353217;
        // also OverrideActive does nothing on ExtraScale
        ExtraScale.Structured.OverrideActive = 1;
    }

    public bool HasSplines()
    {
        return SplinePoints.Count >= 2;
    }

    public float GetMaxSearchRadius()
    {
        if (Shape.TriggerType == AreaTriggerTypes.Polygon)
        {
            Position center = new(0.0f, 0.0f);
            float maxSearchRadius = 0.0f;

            foreach (var vertice in PolygonVertices)
            {
                float pointDist = center.GetExactDist2d(vertice.X, vertice.Y);

                if (pointDist > maxSearchRadius)
                    maxSearchRadius = pointDist;
            }

            return maxSearchRadius;
        }

        return Shape.GetMaxSearchRadius();
    }
}