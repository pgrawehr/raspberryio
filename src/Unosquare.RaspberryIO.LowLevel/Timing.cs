namespace Unosquare.RaspberryIO.LowLevel
{
    using RaspberryIO.Abstractions;
    using System;
    using System.Threading;
    using System.Diagnostics;

    /// <summary>
    /// Provides access to timing and threading properties and methods.
    /// </summary>
    public class Timing : ITiming
    {
        private Stopwatch m_stopWatch;
        public Timing()
        {
            m_stopWatch = new Stopwatch();
            m_stopWatch.Start();
        }

        /// <inheritdoc />
        /// <summary>
        /// This returns a number representing the number of milliseconds since the system started
        /// </summary>
        public long Milliseconds
        {
            get
            {
                return (uint)m_stopWatch.ElapsedMilliseconds;
            }
        }

        /// <summary>
        /// This returns a number representing the number of microseconds since your
        /// program initialized the GPIO controller
        /// It returns an unsigned 32-bit number which wraps after approximately 71 minutes.
        /// </summary>
        public long Microseconds
        {
            get
            {
                long elapsed = m_stopWatch.ElapsedTicks;
                long micros = elapsed / (Stopwatch.Frequency * 1000000); // Convert to microseconds (1E-6 seconds)
                return micros;
            }
        }

        /// <inheritdoc cref="ITiming.SleepMilliseconds(uint)" />
        public static void Sleep(uint millis)
        {
            int sleepTime = (int)millis;
            if (millis > int.MaxValue)
            {
                throw new NotSupportedException($"Maximum sleep time is {int.MaxValue}ms");
            }
            Thread.Sleep(sleepTime);
        }

        /// <inheritdoc />
        public void SleepMilliseconds(uint millis) => Sleep(millis);

        /// <summary>
        /// This performs busy-wating for the given number of microseconds.
        /// If the value is larger than 1ms, an ordinary sleep is used first. 
        /// </summary>
        /// <param name="micros">Number of microseconds to sleep. This method should be used with small numbers only. </param>
        public void SleepMicroseconds(uint micros)
        {
            if (micros > 1000)
            {
                Sleep(micros / 1000);
                micros = micros % 1000;
            }
            TimeSpan startValue = m_stopWatch.Elapsed;
            TimeSpan end = startValue + TimeSpan.FromMilliseconds(micros / 1000);
            TimeSpan curValue;
            do
            {
                curValue = m_stopWatch.Elapsed;
            } while (curValue < end);
        }
    }
}
