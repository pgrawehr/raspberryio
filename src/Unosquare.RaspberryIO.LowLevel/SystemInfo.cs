namespace Unosquare.RaspberryIO.LowLevel
{
    using RaspberryIO.Abstractions;
    using System;

    /// <summary>
    /// Represents the WiringPi system info.
    /// </summary>
    /// <seealso cref="ISystemInfo" />
    public class SystemInfo : ISystemInfo
    {
        private static readonly object Lock = new object();
        private static bool _revGetted;
        private static BoardRevision _boardRevision = BoardRevision.Rev2;

        /// <inheritdoc />
        public BoardRevision BoardRevision => GetBoardRevision();

        /// <inheritdoc />
        public Version LibraryVersion
        {
            get
            {
                // TODO: Implement
               return new Version(1, 0);
            }
        }

        internal static BoardRevision GetBoardRevision()
        {
            // I don't know how to detect this here, but Rev1 boards should be quite obsolete meanwhile
            return BoardRevision.Rev2;
        }
    }
}
