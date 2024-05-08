﻿namespace WoTE.Content.NPCs.EoL
{
    /// <summary>
    /// A representation of one of the Empress' AI states.
    /// </summary>
    public enum EmpressAIType
    {
        Awaken,

        // Attack states.
        SequentialDashes,
        BasicPrismaticBolts,
        ButterflyBurstDashes,
        OutwardRainbows,
        ConvergingTerraprismas,
        PrismaticBoltSpin,
        TwirlingPetalSun,

        // Intermediate states.
        Teleport,
        ResetCycle,

        // Useful count constant.
        Count
    }
}
