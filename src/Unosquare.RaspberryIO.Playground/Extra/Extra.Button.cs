namespace Unosquare.RaspberryIO.Playground.Extra
{
    using System;
    using Swan;
    using Swan.Logging;
    using Abstractions;
    using Unosquare.RaspberryIO.Peripherals;

    public static partial class Extra
    {
        public static void TestButton()
        {
            Console.Clear();

            Console.WriteLine("Testing Button");
            var inputPin = Pi.Gpio[BcmPin.Gpio24];
            var button = new Button(inputPin, GpioPinResistorPullMode.PullUp);

            button.Pressed += (s, e) => LogMessageOnEvent("Pressed");
            button.Released += (s, e) => LogMessageOnEvent("Released");

            while (true)
            {
                var input = Console.ReadKey(true).Key;

                if (input != ConsoleKey.Escape) continue;

                break;
            }
        }

        private static void LogMessageOnEvent(string message)
        {
            Console.Clear();
            Terminal.WriteLine(message);
            Terminal.WriteLine(ExitMessage);
        }
    }
}
