namespace Unosquare.RaspberryIO.Playground
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Swan.Logging;
    using WiringPi;

    /// <summary>
    /// Main entry point class.
    /// </summary>
    public static partial class Program
    {
        private static readonly Dictionary<ConsoleKey, string> MainOptions = new Dictionary<ConsoleKey, string>
        {
            // Module Control Items
            { ConsoleKey.S, "System" },
            { ConsoleKey.P, "Peripherals" },
            { ConsoleKey.X, "Extra examples" },
        };

        /// <summary>
        /// Defines the entry point of the application.
        /// </summary>
        /// <returns>A task representing the program.</returns>
        public static void Main()
        {
            $"Starting program at {DateTime.Now}".Info();

            Pi.Init<BootstrapWiringPi>();

            var exit = false;
            do
            {
                Console.Clear();
                var mainOption = Terminal.ReadPrompt("Main options", MainOptions, "Esc to exit this program");

                switch (mainOption.Key)
                {
                    case ConsoleKey.S:
                        SystemTests.ShowMenu();
                        break;
                    case ConsoleKey.P:
                        Peripherals.Peripherals.ShowMenu();
                        break;
                    case ConsoleKey.X:
                        Extra.Extra.ShowMenu();
                        break;                        
                    case ConsoleKey.Escape:
                        exit = true;
                        break;
                }
            }
            while (!exit);

            Console.Clear();
            Console.ResetColor();
        }
    }
}
