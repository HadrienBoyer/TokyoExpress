namespace Tessera
{
    // Relabelling of Sylves.PTHexPrismDir to fit Tessera conventiosn better
    // Rotating Y+ to Z-  (and Z+ to Y+), like XZModifer
    public enum SylvesHexPrismDir
    {
        Right,
        BackRight,
        BackLeft,
        Left,
        ForwardLeft,
        ForwardRight,
        Up,
        Down,
    }

    public enum SylvesHexPrismCorner
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
