using System;

namespace Infrastructure.Configuration
{
    public interface IConfigurationProvider<T>
    {
        T Configuration { get; }
        event EventHandler<ConfigurationChanged> ConfigurationChanged;
    }
}
