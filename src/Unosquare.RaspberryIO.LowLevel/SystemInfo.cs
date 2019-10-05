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
            lock (Lock)
            {
                if (_revGetted) return _boardRevision;
                var val = WiringPi.PiBoardRev();
                _boardRevision = val == 1 ? BoardRevision.Rev1 : BoardRevision.Rev2;
                _revGetted = true;
            }

            return _boardRevision;
        }
    }
}
