using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Unosquare.RaspberryIO.Abstractions;
using Unosquare.RaspberryIO.Peripherals;
using Unosquare.RaspberryIO.Peripherals.AnalogDigitalConverter;

namespace Unosquare.RaspberryIO.Playground.Peripherals
{
    public static partial class Peripherals
    {
        /// <summary>
        /// Test the ADS1115 ADC (or the KY-053 sensor module) together with a two-axis analog joystick (KY-023 module)
        /// Connect the joystick with the ADC as described in http://sensorkit.joy-it.net/index.php?title=KY-023_Joystick_Modul_(XY-Achsen).
        /// Note that I2C-Bus support must be enabled in config. 
        /// </summary>
        public static void TestJoystick()
        {
            Console.Clear();

            // Add device
            var device = Pi.I2C.AddDevice(0x48);

            // The joystick module we're testing has also an extra digital signal when it is pressed down
            var inputPin = Pi.Gpio[Abstractions.BcmPin.Gpio24];

            inputPin.InputPullMode = GpioPinResistorPullMode.PullUp;
            inputPin.PinMode = GpioPinDriveMode.Input;

            Terminal.WriteLine(ExitMessage);
            using (var adc = new AdcAds1115(device))
            {
                // Present info to screen
                adc.DataAvailable +=
                    (s, e) =>
                    {
                        bool pressed = !inputPin.Read();
                        Terminal.WriteLine($"Button {(pressed ? "pressed" : "not pressed")}\nX:{e.A0}\nY:{e.A1}"); 
                    };

                adc.Start(CancellationToken.None);
                while (true)
                {
                    var input = Console.ReadKey(true).Key;
                    if (input != ConsoleKey.Escape)
                        continue;

                    break;
                }
            }
        }
    }
}
