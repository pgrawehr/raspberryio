namespace Unosquare.RaspberryIO.LowLevel
{
    using RaspberryIO.Abstractions;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Device.I2c;

    /// <inheritdoc />
    /// <summary>
    /// A simple wrapper for the I2c bus on the Raspberry Pi.
    /// </summary>
    public class I2CBus : II2CBus
    {
        // TODO: It would be nice to integrate i2c device detection.
        private static readonly object SyncRoot = new object();
        private readonly Dictionary<int, II2CDevice> _devices = new Dictionary<int, II2CDevice>();

        /// <inheritdoc />
        public ReadOnlyCollection<II2CDevice> Devices
        {
            get
            {
                lock (SyncRoot)
                    return new ReadOnlyCollection<II2CDevice>(_devices.Values.ToArray());
            }
        }

        /// <inheritdoc />
        public II2CDevice this[int deviceId] => GetDeviceById(deviceId);

        /// <inheritdoc />
        public II2CDevice GetDeviceById(int deviceId)
        {
            lock (SyncRoot)
                return _devices[deviceId];
        }

        /// <inheritdoc />
        /// <exception cref="KeyNotFoundException">When the device file descriptor is not found.</exception>
        public II2CDevice AddDevice(int deviceId)
        {
            lock (SyncRoot)
            {
                if (_devices.ContainsKey(deviceId))
                    return _devices[deviceId];

                var fileDescriptor = CreateAndOpenDevice(deviceId);
                if (fileDescriptor == null)
                    throw new KeyNotFoundException($"Device with id {deviceId} could not be registered with the I2C bus.");

                var device = new I2CDevice(deviceId, fileDescriptor);
                _devices[deviceId] = device;
                return device;
            }
        }

        private I2cDevice CreateAndOpenDevice(int deviceId)
        {
            // Default bus address is 1 for all recent Pis (although the PI4 has more I2C interfaces, which I haven't seen in use so far)
            I2cConnectionSettings connectionSettings = new I2cConnectionSettings(1, deviceId);
            var device = I2cDevice.Create(connectionSettings);
            return device;
        }
    }
}
