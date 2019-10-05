namespace Unosquare.RaspberryIO.LowLevel
{
    using RaspberryIO.Abstractions;
    using Swan.DependencyInjection;

    /// <summary>
    /// Represents the Bootstrap class to extract resources.
    /// </summary>
    /// <seealso cref="Unosquare.RaspberryIO.Abstractions.IBootstrap" />
    public class BootstrapSystemGpio : IBootstrap
    {
        private static readonly object SyncLock = new object();

        /// <inheritdoc />
        public void Bootstrap()
        {
            lock (SyncLock)
            {
                DependencyContainer.Current.Register<IGpioController>(new GpioController()).AsSingleton();
                DependencyContainer.Current.Register<ISpiBus>(new SpiBus()).AsSingleton();
                DependencyContainer.Current.Register<II2CBus>(new I2CBus()).AsSingleton();
                DependencyContainer.Current.Register<ISystemInfo>(new SystemInfo());
                DependencyContainer.Current.Register<ITiming>(new Timing());
                DependencyContainer.Current.Register<IThreading>(new Threading());
            }
        }
    }
}
