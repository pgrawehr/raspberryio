namespace Unosquare.RaspberryIO.Playground.Peripherals
{
    using System;
    using Abstractions;
    using Swan.Logging;
    using Unosquare.RaspberryIO.Peripherals;

    public static partial class Peripherals
    {
        /// <summary>
        /// For this test, connect the ultrasonic sensor to pins 23 and 24.
        /// See http://sensorkit.joy-it.net/index.php?title=KY-050_Ultraschallabstandssensor for a cabling diagram using the KY-050 sensor module
        /// together with a KY-051 voltage translator. But note that this example uses GPIO.23 and GPIO.24 instead of GPIO.17 and GPIO.27 there. 
        /// </summary>
        public static void TestUltrasonicSensor()
        {
            ConsoleColor color;

            using (var sensor = new UltrasonicHcsr04(Pi.Gpio[BcmPin.Gpio23], Pi.Gpio[BcmPin.Gpio24]))
            {
                sensor.OnDataAvailable += (s, e) =>
                {
                    Console.Clear();

                    if (!e.IsValid)
                    {
                        Terminal.WriteLine("Sensor could not be read (distance to close?).");
                    }
                    else if (e.HasObstacles)
                    {
                        if (e.Distance <= 10)
                            color = ConsoleColor.DarkRed;
                        else if (e.Distance <= 20)
                            color = ConsoleColor.DarkYellow;
                        else if (e.Distance <= 30)
                            color = ConsoleColor.Yellow;
                        else if (e.Distance <= 40)
                            color = ConsoleColor.Green;
                        else if (e.Distance <= 50)
                            color = ConsoleColor.Blue;
                        else
                            color = ConsoleColor.White;

                        var distance = e.Distance < 57 ? e.Distance : 58;

                        Terminal.Write($"{new string('█', (int)distance)}", color, true);
                        Terminal.Write("--------------------------------------------------------->", color, true);
                        Terminal.Write("          10        20        30        40        50       cm", color, true);
                        Terminal.Write($"Obstacle detected at {e.Distance:N2}cm / {e.DistanceInch:N2}in", color, true);
                    }
                    else
                    {
                        Terminal.WriteLine("No obstacles detected.");
                    }

                    Terminal.WriteLine(ExitMessage);
                };

                sensor.Start();
                while (true)
                {
                    var input = Console.ReadKey(true).Key;
                    if (input != ConsoleKey.Escape) continue;

                    break;
                }
            }
        }
    }
}
