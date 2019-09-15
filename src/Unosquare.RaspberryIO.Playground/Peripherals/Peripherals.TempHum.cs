namespace Unosquare.RaspberryIO.Playground.Peripherals
{
    using System;
    using Abstractions;
    using Unosquare.RaspberryIO.Peripherals;

    public static partial class Peripherals
    {
        /// <summary>
        /// Tests the temperature and hummidity sensor.
        /// Attach Dht11 (Joy-it name KY-015) to Gpio23, see also http://sensorkit.joy-it.net/index.php?title=KY-015_Kombi-Sensor_Temperatur%2BFeuchtigkeit
        /// </summary>
        public static void TestTempSensor()
        {
            Console.Clear();

            using (var sensor = DhtSensor.Create(DhtType.Dht11, Pi.Gpio[BcmPin.Gpio23]))
            {
                var totalReadings = 0.0;
                var validReadings = 0.0;

                sensor.OnDataAvailable += (s, e) =>
                {
                    totalReadings++;
                    if (!e.IsValid) return;

                    Console.Clear();
                    validReadings++;
                    Console.WriteLine($"Temperature: \n {e.Temperature:0.00}°C \n {e.TemperatureFahrenheit:0.00}°F  \n Humidity: {e.HumidityPercentage:P0}\n" +
                        $"Data set {validReadings}/{totalReadings}\n\n");
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
