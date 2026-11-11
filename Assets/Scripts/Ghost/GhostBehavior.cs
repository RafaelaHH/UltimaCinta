using System;

namespace UltimaCinta.Ghost
{
    [Flags]
    public enum GhostBehavior
    {
        None = 0,
        Slow = 1 << 0,
        Resilient = 1 << 1,
        HuntsBySound = 1 << 2,
        Crawls = 1 << 3,
        HuntsOnlyInDarkness = 1 << 4,
        DrainsBatteryNearby = 1 << 5,
        RepelledByLight = 1 << 6,
        RepelledByGarlic = 1 << 7,
        RepelledByFlashlight = 1 << 8,
        DoesNotAppearInMirrors = 1 << 9,
        ThrowsObjects = 1 << 10,
        SlamsDoors = 1 << 11,
        FrequentShortHunts = 1 << 12,
        NoiseReducesSanity = 1 << 13,
        PassesThroughWalls = 1 << 14,
        TeleportsNearPlayers = 1 << 15,
        Fast = 1 << 16,
        Silent = 1 << 17,
        StoppedBySalt = 1 << 18
    }
}
