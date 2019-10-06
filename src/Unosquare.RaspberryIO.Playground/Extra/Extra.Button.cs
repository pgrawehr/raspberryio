namespace Unosquare.RaspberryIO.Playground.Extra
{
    using Abstractions;
    using System;
    using Unosquare.RaspberryIO.Peripherals;

    public static partial class Extra
    {
        public static void TestButton()
        {
            Console.Clear();

            Console.WriteLine("I - Input, O - Output, U - Up, D - Down");
            Console.WriteLine("Press ESC to quit");
            var inputPin = Pi.Gpio[BcmPin.Gpio06];
            inputPin.PinMode = GpioPinDriveMode.Input;
            inputPin.InputPullMode = GpioPinResistorPullMode.PullUp;
            var button = new Button(inputPin, GpioPinResistorPullMode.PullUp);

            button.Pressed += (s, e) => LogMessageOnEvent("Pressed");
            button.Released += (s, e) => LogMessageOnEvent("Released");

            while (true)
            {
                var input = Console.ReadKey(true).Key;

                if (input == ConsoleKey.Escape)
                    break;

                if (input == ConsoleKey.I)
                {
                    inputPin.PinMode = GpioPinDriveMode.Input;
                }
                if (input == ConsoleKey.O)
                {
                    inputPin.PinMode = GpioPinDriveMode.Output;
                }
                if (input == ConsoleKey.U)
                {
                    inputPin.InputPullMode = GpioPinResistorPullMode.PullUp;
                }
                if (input == ConsoleKey.D)
                {
                    inputPin.InputPullMode = GpioPinResistorPullMode.PullDown;
                }
            }
            button.Dispose();
        }

        private static void LogMessageOnEvent(string message)
        {
            Console.Clear();
            Console.WriteLine(message);
            Console.WriteLine(ExitMessage);
        }
    }
}
