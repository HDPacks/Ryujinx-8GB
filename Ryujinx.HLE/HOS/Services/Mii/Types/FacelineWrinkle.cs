﻿namespace Ryujinx.HLE.HOS.Services.Mii.Types
{
    enum FacelineWrinkle : byte
    {
        None,
        TearTroughs,
        FacialPain,
        Cheeks,
        Folds,
        UnderTheEyes,
        SplitChin,
        Chin,
        BrowDroop,
        MouthFrown,
        CrowsFeet,
        FoldsCrowsFrown,

        Min = 0,
        Max = 11
    }
}
