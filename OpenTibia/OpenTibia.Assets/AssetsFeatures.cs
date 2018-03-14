using System;

namespace OpenTibia.Assets
{
    [Flags]
    public enum AssetsFeatures
    {
        None = 0,
        PatternsZ = 1 << 0,
        Extended = 1 << 1,
        FramesDuration = 1 << 2,
        FrameGroups = 1 << 3,
        Transparency = 1 << 4
    }
}
