namespace Unosquare.RaspberryIO.LowLevel
{
    using RaspberryIO.Abstractions;
    using Swan;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Threading.Tasks;
    
    /// <summary>
    /// Represents the Raspberry Pi GPIO controller
    /// as an IReadOnlyCollection of GpioPins.
    /// 
    /// Low level operations are accomplished by using the Wiring Pi library.
    /// </summary>
    public sealed class GpioController : IGpioController, IDisposable
    {
        private static readonly object SyncRoot = new object();
        private static readonly EnumMapper<GpioPinDriveMode, System.Device.Gpio.PinMode> PinModeMap;
        private Dictionary<int, GpioPin> m_pins;
        private System.Device.Gpio.GpioController m_gpioController;

        #region Constructors and Initialization

        /// <summary>
        /// Initializes static members of the <see cref="GpioController"/> class.
        /// </summary>
        static GpioController()
        {
            PinModeMap = new EnumMapper<GpioPinDriveMode, System.Device.Gpio.PinMode>();
            PinModeMap.Add(GpioPinDriveMode.Input, System.Device.Gpio.PinMode.Input);
            PinModeMap.Add(GpioPinDriveMode.Output, System.Device.Gpio.PinMode.Output);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GpioController"/> class.
        /// </summary>
        /// <exception cref="System.Exception">Unable to initialize the GPIO controller.</exception>
        internal GpioController()
        {
            m_gpioController = new System.Device.Gpio.GpioController(System.Device.Gpio.PinNumberingScheme.Logical);
            m_pins = new Dictionary<int, GpioPin>();
            Dictionary<int, GpioPin> headerP1 = new Dictionary<int, GpioPin>();
            Dictionary<int, GpioPin> headerP5 = new Dictionary<int, GpioPin>();

            for (int i = 0; i <= 30; i++)
            {
                var pin = new GpioPin((BcmPin)i, this);
                m_pins.Add(i, pin);
                if (i <= 26)
                {
                    headerP1.Add(i, pin);
                }
                else
                {
                    headerP5.Add(i, pin);
                }
            }

            Pins = new ReadOnlyCollection<GpioPin>(m_pins.Select(x => x.Value).ToList());
            HeaderP1 = new ReadOnlyDictionary<int, GpioPin>(headerP1);
            HeaderP5 = new ReadOnlyDictionary<int, GpioPin>(headerP5);
        }

        public void Dispose()
        {
            if (m_gpioController != null)
            {
                foreach(var p in m_pins)
                {
                    p.Value.Dispose();
                }
                m_gpioController.Dispose();
            }
            m_gpioController = null;
        }

        /// <summary>
        /// Determines if the underlying GPIO controller has been initialized properly.
        /// </summary>
        /// <value>
        /// <c>true</c> if the controller is properly initialized; otherwise, <c>false</c>.
        /// </value>
        public static bool IsInitialized
        {
            get
            {
                lock (SyncRoot)
                {
                    return Mode != ControllerMode.NotInitialized;
                }
            }
        }

        /// <inheritdoc />
        /// <summary>
        /// Gets the number of registered pins in the controller.
        /// </summary>
        public int Count => Pins.Count;

        /// <summary>
        /// Gets or sets the initialization mode.
        /// </summary>
        private static ControllerMode Mode { get; set; } = ControllerMode.NotInitialized;

        #endregion

        #region Pin Addressing

        /// <summary>
        /// Gets the PWM base frequency (in Hz).
        /// </summary>
        public int PwmBaseFrequency => 19200000;

        /// <summary>
        /// Gets a red-only collection of all pins.
        /// </summary>
        public ReadOnlyCollection<GpioPin> Pins
        {
            get;
        }

        internal void SetPinMode(int bcmPinNumber, GpioPinDriveMode mode, GpioPinResistorPullMode pullMode)
        {
            if (mode != GpioPinDriveMode.Output && mode != GpioPinDriveMode.Input)
            {
                throw new NotImplementedException();
            }
            if (mode == GpioPinDriveMode.Output)
            {
                m_gpioController.SetPinMode(bcmPinNumber, PinModeMap.Get(mode));
            }
            else
            {
                System.Device.Gpio.PinMode setmode = System.Device.Gpio.PinMode.Input;
                switch(pullMode)
                {
                    case GpioPinResistorPullMode.Off:
                        setmode = System.Device.Gpio.PinMode.Input;
                        break;
                    case GpioPinResistorPullMode.PullDown:
                        setmode = System.Device.Gpio.PinMode.InputPullDown;
                        break;
                    case GpioPinResistorPullMode.PullUp:
                        setmode = System.Device.Gpio.PinMode.InputPullUp;
                        break;
                }
                m_gpioController.SetPinMode(bcmPinNumber, setmode);
            }
        }

        internal void SetPinValue(int bcmPinNumber, GpioPinValue value)
        {
            m_gpioController.Write(bcmPinNumber, value == GpioPinValue.High ? System.Device.Gpio.PinValue.High : System.Device.Gpio.PinValue.Low);
        }

        internal bool GetPinValue(int bcmPinNumber)
        {
            var v = m_gpioController.Read(bcmPinNumber);
            return v == System.Device.Gpio.PinValue.High;
        }

        /// <summary>
        /// Provides all the pins on Header P1 of the Pi as a lookup by physical header pin number.
        /// This header is the main header and it is the one commonly used.
        /// </summary>
        public ReadOnlyDictionary<int, GpioPin> HeaderP1 { get; }

        /// <summary>
        /// Provides all the pins on Header P5 of the Pi as a lookup by physical header pin number.
        /// This header is the secondary header and it is rarely used.
        /// </summary>
        public ReadOnlyDictionary<int, GpioPin> HeaderP5 { get; }

        #endregion

        #region Indexers

        /// <inheritdoc />
        public IGpioPin this[BcmPin bcmPin] => Pins[(int)bcmPin];

        /// <inheritdoc />
        public IGpioPin this[int bcmPinNumber]
        {
            get
            {
                if (!Enum.IsDefined(typeof(BcmPin), bcmPinNumber))
                    throw new IndexOutOfRangeException($"Pin {bcmPinNumber} is not registered in the GPIO controller.");

                return Pins[bcmPinNumber];
            }
        }

        /// <inheritdoc />
        public IGpioPin this[P1 pinNumber] => HeaderP1[(int)pinNumber];

        /// <inheritdoc />
        public IGpioPin this[P5 pinNumber] => HeaderP5[(int)pinNumber];

        #endregion

        #region IReadOnlyCollection Implementation

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>
        /// A <see cref="T:System.Collections.Generic.IEnumerator`1" /> that can be used to iterate through the collection.
        /// </returns>
        public IEnumerator<GpioPin> GetEnumerator() => Pins.GetEnumerator();

        /// <inheritdoc />
        IEnumerator<IGpioPin> IEnumerable<IGpioPin>.GetEnumerator() => Pins.GetEnumerator();

        /// <inheritdoc />
        IEnumerator IEnumerable.GetEnumerator() => Pins.GetEnumerator();

        #endregion

        #region Pin Operations

        /// <summary>
        /// Closes the pin.
        /// Note that this is called from GpioPin.Dispose and therefore must be save from incorrect disposal order
        /// </summary>
        /// <param name="pin">Pin number</param>
        public void Close(int pin)
        {
            if (m_gpioController != null)
            {
                m_gpioController.ClosePin(pin);
            }
        }

        public void Open(int pin)
        {
            m_gpioController.OpenPin(pin);
        }

        public PinCapability GetCapabilities(int pin)
        {
            PinCapability pc = PinCapability.None;
            if (m_gpioController.IsPinModeSupported(pin, System.Device.Gpio.PinMode.Input) && m_gpioController.IsPinModeSupported(pin, System.Device.Gpio.PinMode.Output))
            {
                // Other modes not supported trough this interface
                pc = PinCapability.GP;
            }
            return pc;
        }

        public void RegisterCallback(int pin, EdgeDetection edge, System.Device.Gpio.PinChangeEventHandler eventHandler)
        {
            if (edge == EdgeDetection.FallingEdge || edge == EdgeDetection.FallingAndRisingEdge)
            {
                m_gpioController.RegisterCallbackForPinValueChangedEvent(pin, System.Device.Gpio.PinEventTypes.Falling, eventHandler);
            }
            if (edge == EdgeDetection.RisingEdge || edge == EdgeDetection.FallingAndRisingEdge)
            {
                m_gpioController.RegisterCallbackForPinValueChangedEvent(pin, System.Device.Gpio.PinEventTypes.Rising, eventHandler);
            }
        }

        public void UnregisterCallback(int pin, System.Device.Gpio.PinChangeEventHandler eventHandler)
        {
            try
            {
                m_gpioController.UnregisterCallbackForPinValueChangedEvent(pin, eventHandler);
            }
            catch (InvalidOperationException)
            {
                // Ignore. Removing Callbacks that weren't registered shouldn't be an issue. 
            }
        }
        #endregion
    }
}
