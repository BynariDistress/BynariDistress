/// <summary>
/// Progressive damage stages for destructible objects.
/// Mirrors BBC2's surface-damage → partial-collapse → full-collapse flow.
/// </summary>
public enum DestructionStage
{
    /// Object is completely intact. Original material / no cracks.
    Intact     = 0,

    /// 75 % health remaining. Surface crack texture swapped in.
    /// Small surface debris may chip off at hit point.
    Damaged    = 1,

    /// 50 % health remaining. Structural cracks deepen.
    /// Medium chunks begin breaking away near impact zones.
    Critical   = 2,

    /// 25 % health remaining. Structure is heavily compromised.
    /// Large chunks release; nearby objects may be destabilised.
    Structural = 3,

    /// 0 % health. Full collapse – all remaining chunks released with physics.
    Destroyed  = 4,
}
