using System;
using System.Collections.Generic;
using System.Text;
using Unosquare.RaspberryIO.Abstractions;

namespace Unosquare.RaspberryIO.LowLevel
{
    class HardwarePwmChannel : IPwmDevice
    {
        private System.Device.Pwm.PwmChannel m_channel;
        private IGpioPin m_pin;
        private bool m_enabled;

        internal HardwarePwmChannel(System.Device.Pwm.PwmChannel channel, IGpioPin pin)
        {
            m_channel = channel;
            m_pin = pin;
            m_enabled = false;
        }

        public int Pin => m_pin.BcmPinNumber;

        public bool IsHardware => true;

        public bool Enabled
        {
            get
            {
                return m_enabled;
            }
            set
            {
                if (m_channel == null)
                {
                    throw new ObjectDisposedException(nameof(HardwarePwmChannel));
                }
                if (value)
                {
                    m_channel.Start();
                }
                else
                {
                    m_channel.Stop();
                }
                m_enabled = value;
            }
        }

        public void SetDutyCycle(double percent, int frequency)
        {
            if (m_channel == null)
            {
                throw new ObjectDisposedException(nameof(HardwarePwmChannel));
            }

            if (frequency != m_channel.Frequency)
            {
                // Otherwise, this may throw an exception (workaround until fixed in driver)
                m_channel.DutyCycle = 0;
                m_channel.Frequency = frequency;
            }
            m_channel.DutyCycle = percent;
        }

        public void SetDutyCycle(double percent)
        {
            if (m_channel == null)
            {
                throw new ObjectDisposedException(nameof(HardwarePwmChannel));
            }

            m_channel.DutyCycle = percent;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (m_channel != null)
                {
                    m_channel.Stop();
                    m_enabled = false;
                    m_channel.Dispose();
                    m_channel = null;
                }
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
