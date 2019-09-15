using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Unosquare.RaspberryIO.Peripherals.AnalogDigitalConverter
{
    public sealed class AdcEventArgs : EventArgs
    {
        public AdcEventArgs(double a0, double a1, double a2, double a3)
        {
            A0 = a0;
            A1 = a1;
            A2 = a2;
            A3 = a3;
        }

        public double A0
        {
            get;
        }

        public double A1
        {
            get;
        }

        public double A2
        {
            get;
        }

        public double A3
        {
            get;
        }
    }
}
