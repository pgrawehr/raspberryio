namespace Unosquare.RaspberryIO.LowLevel
{
    using RaspberryIO.Abstractions;
    using System;

    /// <summary>
    /// Provides access to timing and threading properties and methods.
    /// </summary>
    public class Timing : ITiming
    {
        /// <inheritdoc />
        /// <summary>
        /// This returns a number representing the number of milliseconds since the system started
        /// It returns an unsigned 32-bit number which wraps after 49 days.
        /// </summary>
        public uint Milliseconds
        {
            get
            {
                return (uint)Environment.TickCount;
            }
        }

        /// <inheritdoc />
        /// <summary>
        /// This returns a number representing the number of microseconds since your
        /// program initialized the GPIO controller
        /// It returns an unsigned 32-bit number which wraps after approximately 71 minutes.
        /// </summary>
        public uint Microseconds => WiringPi.Micros();

        /// <inheritdoc cref="ITiming.SleepMilliseconds(uint)" />
        public static void Sleep(uint millis) => WiringPi.Delay(millis);

        /// <inheritdoc />
        public void SleepMilliseconds(uint millis) => Sleep(millis);

        /// <inheritdoc />
        public void SleepMicroseconds(uint micros) => WiringPi.DelayMicroseconds(micros);
    }
}
