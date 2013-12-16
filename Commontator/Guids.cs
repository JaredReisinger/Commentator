// Guids.cs
// MUST match guids.h
using System;

namespace Spudnoggin.Commontator
{
    static class GuidList
    {
        public const string guidCommontatorPkgString = "8c308e81-b885-44b1-b2c6-f8b3e14cc6c3";
        public const string guidCommontatorCmdSetString = "757260c9-0fbd-4ce3-a50b-2e0803d1d766";

        public static readonly Guid guidCommontatorCmdSet = new Guid(guidCommontatorCmdSetString);

        public const string CommontatorServiceString = "e0cacd03-d64f-4e01-aea4-633c0df2265d";
    };
}