namespace Unosquare.RaspberryIO.LowLevel
{
    using System;
    using System.Threading.Tasks;
    using RaspberryIO.Abstractions;
    using RaspberryIO.Abstractions.Native;
    using Swan.Diagnostics;
    using Definitions = RaspberryIO.Abstractions.Definitions;

    /// <summary>
    /// Represents a GPIO Pin, its location and its capabilities.
    /// Full pin reference available here:
    /// http://pinout.xyz/pinout/pin31_gpio6 and  http://wiringpi.com/pins/.
    /// </summary>
    public sealed partial class GpioPin : IGpioPin, IDisposable
    {
        #region Property Backing

        private static readonly int[] GpioToWiringPi;

        private static readonly int[] GpioToWiringPiR1 =
        {
            8, 9, -1, -1, 7, -1, -1, 11, 10, 13, 12, 14, -1, -1, 15, 16, -1, 0, 1, -1, -1, 2, 3, 4, 5, 6, -1, -1, -1, -1, -1, -1,
        };

        private static readonly int[] GpioToWiringPiR2 =
        {
            30, 31, 8, 9, 7, 21, 22, 11, 10, 13, 12, 14, 26, 23, 15, 16, 27, 0, 1, 24, 28, 29, 3, 4, 5, 6, 25, 2, 17, 18, 19, 20,
        };

        private readonly object _syncLock = new object();
        private readonly GpioController m_controller;
        private GpioPinDriveMode _pinMode;
        private GpioPinResistorPullMode _resistorPullMode;
        private int _pwmRegister;
        private PwmMode _pwmMode = PwmMode.Balanced;
        private uint _pwmRange = 1024;
        private int _pwmClockDivisor = 1;
        private int _softPwmValue = -1;
        private int _softToneFrequency = -1;
        private bool m_pinOpen;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="GpioPin"/> class.
        /// </summary>
        /// <param name="bcmPinNumber">The BCM pin number.</param>
        internal GpioPin(BcmPin bcmPinNumber, GpioController controller)
        {
            BcmPin = bcmPinNumber;
            m_controller = controller;
            BcmPinNumber = (int)bcmPinNumber;

            PhysicalPinNumber = Definitions.BcmToPhysicalPinNumber(SystemInfo.GetBoardRevision(), bcmPinNumber);
            Header = (BcmPinNumber >= 28 && BcmPinNumber <= 31) ? GpioHeader.P5 : GpioHeader.P1;
            m_pinOpen = false;
            _resistorPullMode = GpioPinResistorPullMode.Off;
        }

        #endregion

        #region Pin Properties

        /// <inheritdoc />
        public BcmPin BcmPin { get; }

        /// <inheritdoc />
        public int BcmPinNumber { get; }

        /// <inheritdoc />
        public int PhysicalPinNumber { get; }
        
        /// <inheritdoc />
        public GpioHeader Header { get; }

        /// <summary>
        /// Gets the friendly name of the pin.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Gets the hardware mode capabilities of this pin.
        /// </summary>
        public PinCapability Capabilities { get; private set; }

        /// <inheritdoc />
        public bool Value
        {
            get => Read();
            set => Write(value);
        }

        #endregion

        #region Hardware-Specific Properties

        /// <inheritdoc />
        /// <exception cref="T:System.NotSupportedException">Thrown when a pin does not support the given operation mode.</exception>
        public GpioPinDriveMode PinMode
        {
            get
            {
                EnsurePinOpen();
                return _pinMode;
            }

            set
            {
                lock (_syncLock)
                {
                    EnsurePinOpen();
                    var mode = value;
                    if ((mode == GpioPinDriveMode.GpioClock && !HasCapability(PinCapability.GPCLK)) ||
                        (mode == GpioPinDriveMode.PwmOutput && !HasCapability(PinCapability.PWM)) ||
                        (mode == GpioPinDriveMode.Input && !HasCapability(PinCapability.GP)) ||
                        (mode == GpioPinDriveMode.Output && !HasCapability(PinCapability.GP)))
                    {
                        throw new NotSupportedException(
                            $"Pin {BcmPinNumber} '{Name}' does not support mode '{mode}'. Pin capabilities are limited to: {Capabilities}");
                    }

                    m_controller.SetPinMode(BcmPinNumber, mode, InputPullMode);
                    _pinMode = mode;
                }
            }
        }

        /// <summary>
        /// Gets the interrupt callback. Returns null if no interrupt
        /// has been registered.
        /// </summary>
        public Action InterruptCallback { get; private set; }

        /// <summary>
        /// The interrupt callback that takes additional parameters. 
        /// </summary>
        public Action<int, int, uint> InterruptCallback2 { get; private set; }

        /// <summary>
        /// Gets the interrupt edge detection mode.
        /// </summary>
        public EdgeDetection InterruptEdgeDetection { get; private set; }

        /// <summary>
        /// Determines whether the specified capability has capability.
        /// </summary>
        /// <param name="capability">The capability.</param>
        /// <returns>
        ///   <c>true</c> if the specified capability has capability; otherwise, <c>false</c>.
        /// </returns>
        public bool HasCapability(PinCapability capability) =>
            (Capabilities & capability) == capability;

        #endregion

        #region Hardware PWM Members

        /// <summary>
        /// Set the pull mode of the current pin (up, down or open).
        /// This implicitly sets the pinmode to Input. 
        /// </summary>
        public GpioPinResistorPullMode InputPullMode
        {
            get => PinMode == GpioPinDriveMode.Input ? _resistorPullMode : GpioPinResistorPullMode.Off;

            set
            {
                lock (_syncLock)
                {
                    EnsurePinOpen();
                    m_controller.SetPinMode(BcmPinNumber, GpioPinDriveMode.Input, value);
                    PinMode = GpioPinDriveMode.Input;
                    _resistorPullMode = value;
                }
            }
        }

        #endregion
        
        #region Output Mode (Write) Members

        /// <inheritdoc />
        public void Write(GpioPinValue value)
        {
            lock (_syncLock)
            {
                if (PinMode != GpioPinDriveMode.Output)
                {
                    throw new InvalidOperationException(
                        $"Unable to write to pin {BcmPinNumber} because operating mode is {PinMode}."
                        + $" Writes are only allowed if {nameof(PinMode)} is set to {GpioPinDriveMode.Output}");
                }

                m_controller.SetPinValue(BcmPinNumber, value);
            }
        }

        /// <summary>
        /// Writes the value asynchronously.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>The awaitable task.</returns>
        public Task WriteAsync(GpioPinValue value) => Task.Run(() => { Write(value); });

        /// <summary>
        /// Writes the specified bit value.
        /// This method performs a digital write.
        /// </summary>
        /// <param name="value">if set to <c>true</c> [value].</param>
        public void Write(bool value)
            => Write(value ? GpioPinValue.High : GpioPinValue.Low);

        /// <summary>
        /// Writes the specified bit value.
        /// This method performs a digital write.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>
        /// The awaitable task.
        /// </returns>
        public Task WriteAsync(bool value) => Task.Run(() => { Write(value); });

        /// <summary>
        /// Writes the specified value. 0 for low, any other value for high
        /// This method performs a digital write.
        /// </summary>
        /// <param name="value">The value.</param>
        public void Write(int value) => Write(value != 0 ? GpioPinValue.High : GpioPinValue.Low);

        /// <summary>
        /// Writes the specified value. 0 for low, any other value for high
        /// This method performs a digital write.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>The awaitable task.</returns>
        public Task WriteAsync(int value) => Task.Run(() => { Write(value); });

        /// <summary>
        /// Writes the specified value as an analog level.
        /// You will need to register additional analog modules to enable this function for devices such as the Gertboard.
        /// </summary>
        /// <param name="value">The value.</param>
        public void WriteLevel(int value)
        {
            lock (_syncLock)
            {
                if (PinMode != GpioPinDriveMode.Output)
                {
                    throw new InvalidOperationException(
                        $"Unable to write to pin {BcmPinNumber} because operating mode is {PinMode}."
                        + $" Writes are only allowed if {nameof(PinMode)} is set to {GpioPinDriveMode.Output}");
                }

                throw new NotSupportedException("Operation not supported on a Raspberry Pi");
            }
        }

        /// <summary>
        /// Writes the specified value as an analog level.
        /// You will need to register additional analog modules to enable this function for devices such as the Gertboard.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>The awaitable task.</returns>
        public Task WriteLevelAsync(int value) => Task.Run(() => { WriteLevel(value); });

        #endregion

        #region Input Mode (Read) Members

        /// <summary>
        /// Wait for specific pin status.
        /// </summary>
        /// <param name="status">status to check.</param>
        /// <param name="timeOutMillisecond">timeout to reach status.</param>
        /// <returns>true/false.</returns>
        public bool WaitForValue(GpioPinValue status, int timeOutMillisecond)
        {
            if (PinMode != GpioPinDriveMode.Input)
            {
                throw new InvalidOperationException(
                    $"Unable to read from pin {BcmPinNumber} because operating mode is {PinMode}."
                    + $" Reads are only allowed if {nameof(PinMode)} is set to {GpioPinDriveMode.Input}");
            }

            var hrt = new HighResolutionTimer();
            hrt.Start();
            do
            {
                if (ReadValue() == status)
                    return true;
            }
            while (hrt.ElapsedMilliseconds <= timeOutMillisecond);

            return false;
        }

        /// <summary>
        /// Reads the digital value on the pin as a boolean value.
        /// </summary>
        /// <returns>The state of the pin.</returns>
        public bool Read()
        {
            lock (_syncLock)
            {
                if (PinMode != GpioPinDriveMode.Input && PinMode != GpioPinDriveMode.Output)
                {
                    throw new InvalidOperationException(
                        $"Unable to read from pin {BcmPinNumber} because operating mode is {PinMode}."
                        + $" Reads are only allowed if {nameof(PinMode)} is set to {GpioPinDriveMode.Input} or {GpioPinDriveMode.Output}");
                }

                return m_controller.GetPinValue(BcmPinNumber);
            }
        }

        /// <summary>
        /// Reads the digital value on the pin as a boolean value.
        /// </summary>
        /// <returns>The state of the pin.</returns>
        public Task<bool> ReadAsync() => Task.Run(Read);

        /// <summary>
        /// Reads the digital value on the pin as a High or Low value.
        /// </summary>
        /// <returns>The state of the pin.</returns>
        public GpioPinValue ReadValue()
            => Read() ? GpioPinValue.High : GpioPinValue.Low;

        /// <summary>
        /// Reads the digital value on the pin as a High or Low value.
        /// </summary>
        /// <returns>The state of the pin.</returns>
        public Task<GpioPinValue> ReadValueAsync() => Task.Run(ReadValue);

        /// <summary>
        /// Reads the analog value on the pin.
        /// This returns the value read on the supplied analog input pin. You will need to register
        /// additional analog modules to enable this function for devices such as the Gertboard,
        /// quick2Wire analog board, etc.
        /// </summary>
        /// <returns>The analog level.</returns>
        /// <exception cref="InvalidOperationException">When the pin mode is not configured as an input.</exception>
        public int ReadLevel()
        {
            lock (_syncLock)
            {
                if (PinMode != GpioPinDriveMode.Input)
                {
                    throw new InvalidOperationException(
                        $"Unable to read from pin {BcmPinNumber} because operating mode is {PinMode}."
                        + $" Reads are only allowed if {nameof(PinMode)} is set to {GpioPinDriveMode.Input}");
                }

                throw new NotSupportedException("Operation not supported on a Raspberry Pi");
            }
        }

        /// <summary>
        /// Reads the analog value on the pin.
        /// This returns the value read on the supplied analog input pin. You will need to register
        /// additional analog modules to enable this function for devices such as the Gertboard,
        /// quick2Wire analog board, etc.
        /// </summary>
        /// <returns>The analog level.</returns>
        public Task<int> ReadLevelAsync() => Task.Run(ReadLevel);

        #endregion

        #region Interrupts

        /// <summary>
        /// Registers an interrupt callback when a pin changes.
        /// </summary>
        public void RegisterInterruptCallback(EdgeDetection edgeDetection, Action callback)
        {
            if (callback == null)
                throw new ArgumentNullException(nameof(callback));

            if (PinMode != GpioPinDriveMode.Input)
            {
                throw new InvalidOperationException(
                    $"Unable to {nameof(RegisterInterruptCallback)} for pin {BcmPinNumber} because operating mode is {PinMode}."
                    + $" Calling {nameof(RegisterInterruptCallback)} is only allowed if {nameof(PinMode)} is set to {GpioPinDriveMode.Input}");
            }
            if (InterruptCallback != null || InterruptCallback2 != null)
            {
                throw new NotSupportedException("Only one callback per pin is allowed");
            }
            lock (_syncLock)
            {
                m_controller.RegisterCallback(BcmPinNumber, edgeDetection, CallbackHandler);
                InterruptEdgeDetection = edgeDetection;
                InterruptCallback = callback;
            }
        }

        private void CallbackHandler(object sender, System.Device.Gpio.PinValueChangedEventArgs e)
        {
            InterruptCallback?.Invoke();
        }

        private void CallbackHandler2(object sender, System.Device.Gpio.PinValueChangedEventArgs e)
        {
            InterruptCallback2?.Invoke(e.PinNumber, Read() ? 1 : 0, 0);
        }

        /// <summary>
        /// Deregisters all callbacks for this pin. 
        /// </summary>
        public void UnregisterInterruptCallback()
        {
            m_controller.UnregisterCallback(BcmPinNumber, CallbackHandler);
            m_controller.UnregisterCallback(BcmPinNumber, CallbackHandler2);
            InterruptCallback = null;
            InterruptCallback2 = null;
        }

        public void Dispose()
        {
            m_controller.Close(BcmPinNumber);
        }

        private void EnsurePinOpen()
        {
            if (!m_pinOpen)
            {
                m_controller.Open(BcmPinNumber);
                Capabilities = PinCapability.GP;
                m_pinOpen = true;
            }
        }

        /// <inheritdoc />
        public void RegisterInterruptCallback(EdgeDetection edgeDetection, Action<int, int, uint> callback)
        {
            if (callback == null)
                throw new ArgumentNullException(nameof(callback));

            if (PinMode != GpioPinDriveMode.Input)
            {
                throw new InvalidOperationException(
                    $"Unable to {nameof(RegisterInterruptCallback)} for pin {BcmPinNumber} because operating mode is {PinMode}."
                    + $" Calling {nameof(RegisterInterruptCallback)} is only allowed if {nameof(PinMode)} is set to {GpioPinDriveMode.Input}");
            }
            if (InterruptCallback != null || InterruptCallback2 != null)
            {
                throw new NotSupportedException("Only one callback per pin is allowed");
            }
            lock (_syncLock)
            {
                m_controller.RegisterCallback(BcmPinNumber, edgeDetection, CallbackHandler2);
                InterruptEdgeDetection = edgeDetection;
                InterruptCallback2 = callback;
            }
        }

        public IPwmDevice CreatePwmDevice()
        {
            var pwm = System.Device.Pwm.PwmChannel.Create(0, 0);
            return new HardwarePwmChannel(pwm);
        }

        internal static WiringPiPin BcmToWiringPiPinNumber(BcmPin pin) =>
            (WiringPiPin)GpioToWiringPi[(int)pin];

        #endregion
    }
}
