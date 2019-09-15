using Swan.Diagnostics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Unosquare.RaspberryIO.Abstractions;

namespace Unosquare.RaspberryIO.Peripherals.AnalogDigitalConverter
{
    /// <summary>
    /// Support for the Ads1115 Analog Digital Converter.
    /// Also available as pre-configured package KY-053 from joy-it.
    /// </summary>
    public class AdcAds1115 : IDisposable
    {
        private readonly object m_lock;

        // Read with 20Hz
        private readonly TimeSpan ReadTime = TimeSpan.FromSeconds(0.05);
        private bool m_disposed;
        private CancellationTokenSource m_cancellationToken;
        private float m_currentGain;
        private int m_currentGainCode;
        private int m_currentDataRateCode;

        public const int ADS1X15_DEFAULT_ADDRESS = (0x48);
        public const int ADS1X15_POINTER_CONVERSION = (0x00);
        public const int ADS1X15_POINTER_CONFIG = (0x01);
        public const int ADS1X15_CONFIG_OS_SINGLE = (0x8000);
        public const int ADS1X15_CONFIG_MUX_OFFSET = (12);
        public const int ADS1X15_CONFIG_COMP_QUE_DISABLE = (0x0003);
        private readonly List<(float, int)> m_configGainList = new List<(float, int)> {
            (2f / 3f, 0x0000),
            (1,   0x0200),
            (2,   0x0400),
            (4,   0x0600),
            (8,   0x0800),
            (16,  0x0A00),
        };

        private readonly List<(int, int)> m_configDataRates = new List<(int, int)>
        {
            (8, 0x0000),
            (16, 0x0020),
            (32, 0x0040),
            (64, 0x0060),
            (128, 0x0080),
            (250, 0x00A0),
            (475, 0x00C0),
            (860, 0x00E0),
        };

        public enum Mode
        {
            Continuous = 0x0,
            Single = 0x0100,
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AdcAds1115"/> class.
        /// Initializes a new instance of the ADC converter class.
        /// </summary>
        /// <param name="device">The device.</param>
        public AdcAds1115(II2CDevice device)
        {
            Device = device;

            m_lock = new object();
            m_disposed = false;
            m_currentGain = 1.0f;
            m_currentDataRateCode = 0x0080;
            SetGain(1.0f);
        }

        /// <summary>
        /// Occurs when [data available].
        /// </summary>
        public event EventHandler<AdcEventArgs> DataAvailable;

        private II2CDevice Device
        {
            get;
        }

        private Thread ReadWorker
        {
            get;
            set;
        }

        public float Gain
        {
            get
            {
                return m_currentGain;
            }
        }

        /// <summary>
        /// Gets a value indicating whether this instance is running.
        /// </summary>
        /// <value><c>true</c> if this instance is running; otherwise, <c>false</c>.</value>
        public bool IsRunning
        {
            get
            {
                var r = ReadWorker; // Atomic get
                return r != null && r.ThreadState == ThreadState.Running;
            }
        }

        public IEnumerable<float> PossibleGains
        {
            get
            {
                return m_configGainList.Select(x => x.Item1);
            }
        }

        /// <summary>
        /// Starts the instance.
        /// </summary>
        public void Start(CancellationToken cancellationToken)
        {
            lock (m_lock)
            {
                if (ReadWorker != null)
                {
                    throw new InvalidOperationException("Sensor retrieve task is already running");
                }
                m_cancellationToken = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                ReadWorker = new Thread(Run);
                ReadWorker.Start();
            }
        }

        /// <summary>
        /// Sets the given gain.
        /// </summary>
        /// <param name="gain">New gain value. Must be a value from the <see cref="PossibleGains"/> list.</param>
        /// <exception cref="InvalidOperationException">The given value is not one of the valid gain values</exception>
        public void SetGain(float gain)
        {
            int bitValue = m_configGainList.First(x => Math.Abs(x.Item1 - gain) < 1E-5).Item2;
            m_currentGainCode = bitValue;
            m_currentGain = gain;
        }

        /// <summary>
        /// Runs this instance.
        /// </summary>
        private void Run()
        {
            var timer = new HighResolutionTimer();
            var lastElapsedTime = TimeSpan.FromSeconds(0);

            while (IsRunning)
            {
                if (lastElapsedTime < ReadTime)
                {
                    Thread.Sleep(ReadTime - lastElapsedTime);
                }

                timer.Start();

                var sensorData = RetrieveSensorData();
                lastElapsedTime = timer.Elapsed;

                if (IsRunning)
                {
                    DataAvailable?.Invoke(this, sensorData);
                }

                timer.Reset();
            }
        }

        /// <summary>
        /// Retrieve the data capted by the sensor.
        /// </summary>
        /// <returns> Data calculated by KY-053 ADC. </returns>
        private AdcEventArgs RetrieveSensorData()
        {
            int p0 = Read(0, false);

            return new AdcEventArgs(p0, 0, 0, 0);
        }

        /// <summary>
        /// Stops the continuous reads.
        /// </summary>
        private void StopContinuousReads()
        {
            lock (m_lock)
            {
                if (ReadWorker != null)
                {
                    ReadWorker.Join();
                    ReadWorker = null;
                }
            }
        }

        private int Read(int pin, bool isDifferential)
        {
            if (isDifferential)
            {
                return Read(pin);
            }
            else
            {
                return Read(pin + 4);
            }
        }

        private int Read(int pin)
        {
            int config = ADS1X15_CONFIG_OS_SINGLE;
            config |= (pin & 0x07) << ADS1X15_CONFIG_MUX_OFFSET;
            config |= m_currentGainCode;
            config |= (int)Mode.Single;
            config |= m_currentDataRateCode;
            config |= ADS1X15_CONFIG_COMP_QUE_DISABLE;
            Device.WriteAddressWord(ADS1X15_POINTER_CONFIG, (ushort)config);
            // TODO: Add timeout
            while (!ConversionComplete())
            {
                Thread.Sleep(1);
            }

            return ReadWord(ADS1X15_POINTER_CONVERSION);
        }

        /// <summary>
        /// Return status of ADC conversion.
        /// True = Device is ready.
        /// </summary>
        private bool ConversionComplete()
        {
            // OS is bit 15
            // OS = 0: Device is currently performing a conversion
            // OS = 1: Device is not currently performing a conversion
            return (ReadWord(ADS1X15_POINTER_CONFIG) & 0x8000) == 0x8000;
        }

        /// <summary>
        /// Reads the word.
        /// </summary>
        /// <param name="register">The register.</param>
        /// <returns>System.Int32.</returns>
        private int ReadWord(int register)
        {
            var h = Device.ReadAddressByte(register);
            var l = Device.ReadAddressByte(register + 1);
            return (h << 8) + l;
        }

        /// <summary>
        /// Reads the word for i2C.
        /// </summary>
        /// <param name="register">The register.</param>
        /// <returns>System.Int32.</returns>
        private int ReadWord2C(int register)
        {
            var value = ReadWord(register);
            return (value & 0x8000) != 0 ? -1 * (0x10000 - value) : value;
        }

        public void Dispose()
        {
            Dispose(true);
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        private void Dispose(bool disposing)
        {
            if (m_disposed)
            {
                return;
            }

            if (disposing)
            {
                StopContinuousReads();
            }

            ReadWorker = null;
            m_disposed = true;
        }
    }
}
