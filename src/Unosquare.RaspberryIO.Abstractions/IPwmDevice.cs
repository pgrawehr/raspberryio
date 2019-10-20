using System;
using System.Collections.Generic;
using System.Text;

namespace Unosquare.RaspberryIO.Abstractions
{
    public interface IPwmDevice : IDisposable
    {
        int Pin
        {
            get;
        }

        bool IsHardware
        {
            get;
        }

        bool Enabled
        {
            get;
            set;
        }

        void SetDutyCycle(double percent, int frequency = 400);
    }
}
