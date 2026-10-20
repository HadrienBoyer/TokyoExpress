namespace Tessera
{
    // Relabelling of Sylves.FTHexPrismDir to fit Tessera conventiosn better
    // Rotating Y+ to Z-  (and Z+ to Y+), like XZModifer
    public enum SylvesTrianglePrismDir
    {
        BackRight,
        Back,
        BackLeft,
        ForwardLeft,
        Forward,
        ForwardRight,
        Up,
        Down,
    }

    public enum SylvesTrianglePrismCorner
    {
        DownForwardRight,
        DownBackRight,
        DownBack,
        DownBackLeft,
        DownForwardLeft,
        DownForward,
        UpForwardRight,
        UpBackRight,
        UpBack,
        UpBackLeft,
        UpForwardLeft,
        UpForward,
    }
}
