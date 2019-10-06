namespace Unosquare.RaspberryIO.LowLevel
{
    using RaspberryIO.Abstractions;
    using RaspberryIO.Abstractions.Native;
    using System;
    using System.Buffers.Binary;
    using System.Threading.Tasks;

    /// <summary>
    /// Represents a device on the I2C Bus.
    /// </summary>
    public sealed class I2CDevice : II2CDevice
    {
        private readonly object _syncLock = new object();
        private System.Device.I2c.I2cDevice m_device;

        /// <summary>
        /// Initializes a new instance of the <see cref="I2CDevice"/> class.
        /// </summary>
        /// <param name="deviceId">The device identifier.</param>
        /// <param name="fileDescriptor">The file descriptor.</param>
        internal I2CDevice(int deviceId, System.Device.I2c.I2cDevice fileDescriptor)
        {
            DeviceId = deviceId;
            m_device = fileDescriptor;
        }

        /// <inheritdoc />
        public int DeviceId { get; }

        /// <inheritdoc />
        public int FileDescriptor
        {
            get
            {
                return m_device != null ? 1 : 0;
            }
        }

        /// <inheritdoc />
        public byte Read()
        {
            lock (_syncLock)
            {
                return m_device.ReadByte();
            }
        }

        /// <summary>
        /// Reads a byte from the specified file descriptor.
        /// </summary>
        /// <returns>The byte from device.</returns>
        public Task<byte> ReadAsync() => Task.Run(Read);

        /// <summary>
        /// Reads a buffer of the specified length, one byte at a time.
        /// </summary>
        /// <param name="length">The length.</param>
        /// <returns>The byte array from device.</returns>
        public byte[] Read(int length)
        {
            lock (_syncLock)
            {
                var buffer = new byte[length];
                m_device.Read(buffer);

                return buffer;
            }
        }

        /// <summary>
        /// Reads a buffer of the specified length, one byte at a time.
        /// </summary>
        /// <param name="length">The length.</param>
        /// <returns>The byte array from device.</returns>
        public Task<byte[]> ReadAsync(int length) => Task.Run(() => Read(length));

        /// <summary>
        /// Writes a byte of data the specified file descriptor.
        /// </summary>
        /// <param name="data">The data.</param>
        public void Write(byte data)
        {
            lock (_syncLock)
            {
                m_device.WriteByte(data);
            }
        }

        /// <summary>
        /// Writes a byte of data the specified file descriptor.
        /// </summary>
        /// <param name="data">The data.</param>
        /// <returns>The awaitable task.</returns>
        public Task WriteAsync(byte data) => Task.Run(() => { Write(data); });

        /// <summary>
        /// Writes a set of bytes to the specified file descriptor.
        /// </summary>
        /// <param name="data">The data.</param>
        public void Write(byte[] data)
        {
            lock (_syncLock)
            {
                m_device.Write(data);
            }
        }

        /// <summary>
        /// Writes a set of bytes to the specified file descriptor.
        /// </summary>
        /// <param name="data">The data.</param>
        /// <returns>The awaitable task.</returns>
        public Task WriteAsync(byte[] data) => Task.Run(() => { Write(data); });

        /// <summary>
        /// Write an 8 bit data value into the device register indicated.
        /// </summary>
        /// <param name="address">The register.</param>
        /// <param name="data">The data.</param>
        public void WriteAddressByte(int address, byte data)
        {
            lock (_syncLock)
            {
                byte[] raw = new byte[2];
                raw[0] = (byte)address;
                raw[1] = data;
                m_device.Write(raw);
            }
        }

        /// <summary>
        /// Write a 16-bit data value into the device register indicated.
        /// </summary>
        /// <param name="address">The register.</param>
        /// <param name="data">The data.</param>
        public void WriteAddressWord(int address, ushort data)
        {
            lock (_syncLock)
            {
                Span<byte> raw = stackalloc byte[3];
                raw[0] = (byte)address;
                raw[1] = (byte)(data & 0xFF);
                raw[2] = (byte)(data >> 8);
                m_device.Write(raw);
            }
        }

        /// <summary>
        /// These read an 8 or 16-bit value from the device register indicated.
        /// </summary>
        /// <param name="address">The register.</param>
        /// <returns>The address byte from device.</returns>
        public byte ReadAddressByte(int address)
        {
            lock (_syncLock)
            {
                Span<byte> ret = stackalloc byte[1];
                m_device.WriteRead(new byte[] { (byte)address }, ret);
                
                return ret[0];
            }
        }

        /// <summary>
        /// These read an 8 or 16-bit value from the device register indicated.
        /// </summary>
        /// <param name="address">The register.</param>
        /// <returns>The address word from the device, in Big-Endian form. </returns>
        public ushort ReadAddressWord(int address)
        {
            lock (_syncLock)
            {
                Span<byte> ret = stackalloc byte[2];
                m_device.WriteRead(new byte[] { (byte)address }, ret);

                int reti = ret[1] << 8 | ret[0];
                return (ushort)reti;
            }
        }

        public void Dispose()
        {
            m_device.Dispose();
        }
    }
}
