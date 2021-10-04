﻿using Infrastructure.Logging;
using Infrastructure.Utils;
using System;

namespace Infrastructure.Logging
{
    public class LazyLogger : ILogLite
    {
        private readonly Lazy<ILogLite> log;

        public bool VerboseEnabled => this.log.Value.VerboseEnabled;

        public LazyLogger(Func<ILogLite> factory)
        {
            Ensure.NotNull(factory, nameof(factory));

            this.log = new Lazy<ILogLite>(factory);
        }

        public void Error(string message)
        {
            this.log.Value.Error(message);
        }

        public void Error(Exception ex, string message)
        {
            this.log.Value.Error(ex, message);
        }

        public void Error(object serializablePayload, Exception ex, string message)
        {
            this.log.Value.Error(serializablePayload, ex, message);
        }

        public void Fatal(string message)
        {
            this.log.Value.Fatal(message);
        }

        public void Fatal(Exception ex, string message)
        {
            this.log.Value.Fatal(ex, message);
        }

        public void Info(string message)
        {
            this.log.Value.Info(message);
        }

        public void Warning(string message)
        {
            this.log.Value.Warning(message);
        }

        public void Warning(object serializablePayload, string message)
        {
            this.log.Value.Warning(serializablePayload, message);
        }

        public void Verbose(string message)
        {
            this.log.Value.Verbose(message);
        }

        public void Success(string message)
        {
            this.log.Value.Success(message);
        }
    }
}
