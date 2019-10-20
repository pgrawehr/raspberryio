using System;
using System.Collections.Generic;
using System.Text;
using System.Device.Pwm.Drivers;
using Unosquare.RaspberryIO.Abstractions;

namespace Unosquare.RaspberryIO.LowLevel
{
    class SoftwarePwmChannel : IPwmDevice
    {
        private readonly IGpioPin m_pin;
        private System.Device.Pwm.Drivers.SoftwarePwmChannel m_device;
        private bool m_enabled;

        internal SoftwarePwmChannel(System.Device.Pwm.Drivers.SoftwarePwmChannel softPwmDevice, GpioPin pin)
        {
            m_pin = pin;
            m_device = softPwmDevice;
            m_device.Stop();
            m_enabled = false;
        }

        public int Pin => m_pin.BcmPinNumber;

        public bool IsHardware => false;

        public bool Enabled
        {
            get
            {
                return m_enabled;
            }
            set
            {
                if (value)
                {
                    m_device.Start();
                }
                else
                {
                    m_device.Stop();
                }

                m_enabled = value;
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (m_device != null)
                {
                    m_device.Stop();
                    m_enabled = false;
                    m_device.Dispose();
                }
            }
        }
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public void SetDutyCycle(double percent, int frequency = 400)
        {
            if (m_device == null)
            {
                throw new ObjectDisposedException(nameof(SoftwarePwmChannel));
            }
            if (percent < 0 || percent > 100)
            {
                throw new ArgumentOutOfRangeException(nameof(percent), "Percent must be between 0 and 100");
            }
            m_device.DutyCycle = percent / 100;
            m_device.Frequency = 1;
        }
    }
}
