namespace Unosquare.RaspberryIO.Playground.Extra
{
    using Abstractions;
    using Swan;
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    public static partial class Extra
    {
        public static void TestLedBlinking()
        {
            using var cancellationTokenSource = new CancellationTokenSource();
            var task = Blink(cancellationTokenSource.Token);

            while (true)
            {
                var input = Console.ReadKey(true).Key;

                if (input != ConsoleKey.Escape)
                    continue;
                cancellationTokenSource.Cancel();
                break;
            }

            task.Wait();
        }

        public static void TestLedDimming(bool hardware)
        {
            using var cancellationTokenSource = new CancellationTokenSource();
            var task = hardware ? DimHardware(cancellationTokenSource.Token) : DimSoftware(cancellationTokenSource.Token);

            while (true)
            {
                if (Console.KeyAvailable)
                {
                    var input = Console.ReadKey(true).Key;

                    if (input != ConsoleKey.Escape)
                        continue;
                    cancellationTokenSource.Cancel();
                    break;
                }
                else
                {
                    // Task got an exception?
                    if (task.IsCompleted)
                    {
                        break;
                    }
                    Thread.Sleep(100);
                }
            }

            task.Wait(cancellationTokenSource.Token);
        }

        /// <summary>
        /// For this test, connect an LED to Gpio13 and ground. (don't forget the resistor!).
        /// </summary>
        private static Task Blink(CancellationToken cancellationToken)
        {
            return Task.Run(() =>
            {
                Console.Clear();
                var blinkingPin = Pi.Gpio[BcmPin.Gpio13];

                // Configure the pin as an output
                blinkingPin.PinMode = GpioPinDriveMode.Output;

                // perform writes to the pin by toggling the isOn variable
                var isOn = false;
                while (!cancellationToken.IsCancellationRequested)
                {
                    isOn = !isOn;
                    blinkingPin.Write(isOn);
                    var ledState = isOn ? "on" : "off";
                    Console.Clear();
                    Console.WriteLine($"Blinking {ledState}");
                    Console.WriteLine(ExitMessage);
                    Thread.Sleep(500);
                }

                blinkingPin.Write(0);
            }, cancellationToken);
        }

        private static Task DimHardware(CancellationToken cancellationToken) =>
            Task.Run(async () =>
            {
                Console.Clear();
                Console.WriteLine("Hardware Dimming");
                Terminal.WriteLine(ExitMessage);

                var pin = Pi.Gpio[BcmPin.Gpio13];
                // pin.PinMode = GpioPinDriveMode.Output;
                var pinPwmDriver = pin.CreatePwmDevice(false);
                pinPwmDriver.SetDutyCycle(0.5, 400);
                pinPwmDriver.Enabled = true;

                while (!cancellationToken.IsCancellationRequested)
                {
                    for (var x = 0; x <= 100; x++)
                    {
                        pinPwmDriver.SetDutyCycle(x);
                        await Task.Delay(10, cancellationToken);
                    }

                    for (var x = 0; x <= 100; x++)
                    {
                        pinPwmDriver.SetDutyCycle(x);
                        await Task.Delay(10, cancellationToken);
                    }
                }

                pinPwmDriver.Dispose();
                pin.PinMode = GpioPinDriveMode.Output;
                pin.Write(0);
            }, cancellationToken);

        /// <summary>
        /// For this test, connect a three-color LED to Gpio23, Gpio24, Gpio25 and ground. Tested with the KY-016 Module, works also with the KY-011 Module (which has only 2 colors).
        /// Don't forget the resistor(s)!
        /// </summary>
        private static Task DimSoftware(CancellationToken cancellationToken) =>
            Task.Run(async () =>
            {
                Console.Clear();
                Console.WriteLine("Software Dimming");
                Terminal.WriteLine(ExitMessage);

                var pinGreen = Pi.Gpio[BcmPin.Gpio23];
                var pinRed = Pi.Gpio[BcmPin.Gpio24];
                var pinBlue = Pi.Gpio[BcmPin.Gpio25];

                pinGreen.PinMode = GpioPinDriveMode.Output;
                var greenPwm = pinGreen.CreatePwmDevice(true);
                greenPwm.SetDutyCycle(0.5, 400);
                greenPwm.Enabled = true;

                pinRed.PinMode = GpioPinDriveMode.Output;
                var redPwm = pinRed.CreatePwmDevice(true);
                redPwm.SetDutyCycle(0.5, 400);
                redPwm.Enabled = true;

                pinBlue.PinMode = GpioPinDriveMode.Output;
                var bluePwm = pinBlue.CreatePwmDevice(true);
                bluePwm.SetDutyCycle(0.5, 400);
                bluePwm.Enabled = true;

                int channel = 0;

                while (!cancellationToken.IsCancellationRequested)
                {
                    IPwmDevice pwm = null;
                    if (channel == 0)
                    {
                        pwm = greenPwm;
                    }
                    else if (channel == 1)
                    {
                        pwm = redPwm;
                    }
                    else
                    {
                        pwm = bluePwm;
                    }
                    channel = (channel + 1) % 3;

                    for (var x = 0; x <= 100; x++)
                    {
                        pwm.SetDutyCycle(x);
                        await Task.Delay(50, cancellationToken);
                    }

                    for (var x = 100; x >= 0; x--)
                    {
                        pwm.SetDutyCycle(x);
                        await Task.Delay(50, cancellationToken);
                    }
                }

                bluePwm.Dispose();
                greenPwm.Dispose();
                redPwm.Dispose();

                pinGreen.Write(0);
                pinRed.Write(0);
                pinBlue.Write(0);
                Terminal.WriteLine("End of task");
            }, cancellationToken);
    }
}
