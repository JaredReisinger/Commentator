// Guids.cs
// MUST match guids.h
using System;

namespace Spudnoggin.Commentator
{
    static class GuidList
    {
        public const string guidCommentatorPkgString = "8c308e81-b885-44b1-b2c6-f8b3e14cc6c3";
        public const string guidCommentatorCmdSetString = "757260c9-0fbd-4ce3-a50b-2e0803d1d766";

        public static readonly Guid guidCommentatorCmdSet = new Guid(guidCommentatorCmdSetString);

        public const string CommentatorServiceString = "e0cacd03-d64f-4e01-aea4-633c0df2265d";
    };
}