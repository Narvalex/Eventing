using Infrastructure.Utils;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Text;

namespace Infrastructure.Logging
{
    public class ConsoleLogger : ILogLite
    {
        private readonly LogMessageBuilder messageBuilder;
        private readonly bool showVerbose;

        private static JsonSerializer serializer = JsonSerializer.Create(new JsonSerializerSettings
        {
            Formatting = Formatting.Indented
        });

        private const string ERROR_level = "ERROR";
        private const string FATAL_level = "FATAL";
        private const string INFO_level = "info";
        private const string VERBOSE_level = "verbose";
        private const string SUCCESS_level = "success";
        private const string WARNING_level = "warning";

        private static object lockObject = new object();

        public bool VerboseEnabled => this.showVerbose;

        public ConsoleLogger(string componentName, bool showVerbose)
        {
            Ensure.NotEmpty(componentName, nameof(componentName));

            this.messageBuilder = new LogMessageBuilder(componentName, showVerbose);
            this.showVerbose = showVerbose;
        }

        public void Error(string message)
        {
            this.WriteWithLock(this.messageBuilder.BuildMessage(ERROR_level, message), ConsoleColor.Red);
        }

        public void Error(Exception ex, string message)
        {
            this.WriteWithLock(this.messageBuilder.BuildMessage(ex, ERROR_level, message), ConsoleColor.Red);
        }

        public void Error(object serializablePayload, Exception ex, string message)
        {
            message = PrintMessageWithPayload(serializablePayload, message);
            this.WriteWithLock(this.messageBuilder.BuildMessage(ex, ERROR_level, message), ConsoleColor.Red);
        }

        public void Fatal(string message)
        {
            this.WriteWithLock(this.messageBuilder.BuildMessage(FATAL_level, message), ConsoleColor.White, ConsoleColor.Red);
        }

        public void Fatal(Exception ex, string message)
        {
            this.WriteWithLock(this.messageBuilder.BuildMessage(ex, FATAL_level, message), ConsoleColor.White, ConsoleColor.Red);
        }

        public void Info(string message)
        {
            this.WriteWithLock(this.messageBuilder.BuildMessage(INFO_level, message));
        }

        public void Warning(string message)
        {
            this.WriteWithLock(this.messageBuilder.BuildMessage(WARNING_level, message), ConsoleColor.Yellow);
        }

        public void Warning(object serializablePayload, string message)
        {
            message = PrintMessageWithPayload(serializablePayload, message);
            this.WriteWithLock(this.messageBuilder.BuildMessage(WARNING_level, message), ConsoleColor.Yellow);
        }

        public void Verbose(string message)
        {
            if (this.showVerbose)
                this.WriteWithLock(this.messageBuilder.BuildMessage(VERBOSE_level, message), ConsoleColor.DarkGray);
        }

        public void Success(string message)
        {
            this.WriteWithLock(this.messageBuilder.BuildMessage(SUCCESS_level, message), ConsoleColor.Green);
        }

        private void WriteWithLock(string message)
        {
            lock (lockObject)
            {
                Console.WriteLine(message);
            }
        }

        private void WriteWithLock(string message, ConsoleColor foregroundColor)
        {
            lock (lockObject)
            {
                try
                {
                    Console.ForegroundColor = foregroundColor;
                    Console.WriteLine(message);
                }
                finally
                {
                    Console.ResetColor();
                }

            }
        }

        private void WriteWithLock(string message, ConsoleColor foregroundColor, ConsoleColor backgroundColor)
        {
            lock (lockObject)
            {
                try
                {
                    Console.ForegroundColor = foregroundColor;
                    Console.BackgroundColor = backgroundColor;
                    Console.WriteLine(message);
                }
                finally
                {
                    Console.ResetColor();
                }
            }
        }

        private static string PrintMessageWithPayload(object serializablePayload, string message)
        {
            var payload = Serialize(serializablePayload);
            var sb = new StringBuilder();
            sb.AppendLine(message);
            sb.AppendLine("Incoming payload:");
            sb.AppendLine(serializablePayload.GetType().Name);
            sb.AppendLine(payload);
            return sb.ToString();
        }

        private static string Serialize(object value)
        {
            using (var writer = new StringWriter())
            {
                var jsonWriter = new JsonTextWriter(writer);
                serializer.Serialize(jsonWriter, value);
                writer.Flush();
                return writer.ToString();
            }
        }
    }
}
