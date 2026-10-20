namespace Tessera
{
    /// <summary>
    /// Records some useful stats about the run
    /// </summary>
    internal class TesseraStats
    {
        /// <summary>
        /// Set this to true to log additional stats after generation.
        /// </summary>
        public static bool DebugMode = false;

        // Fundamental timings
        public double createHelperTime;
        public double initializeTime;
        public double createPropagatorTime;
        public double initialConstraintsTime;
        public double skyboxTime;
        public double banBigTilesTime;
        public double runTime;
        public double postProcessTime;

        public double totalTime => createHelperTime 
            + initializeTime 
            + createPropagatorTime 
            + initialConstraintsTime 
            + skyboxTime 
            + banBigTilesTime 
            + runTime 
            + postProcessTime;

        // Other details of the run
        public int modelTileCount;
        public int cellCount;

        // Details of the run
        public int stepCount;
    }
}
