using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.IO;
using System.IO.Pipelines;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ByteSizeLib;
using Common.ExtensionMethods;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Items.Data.EFCore.ContextBaseClasses.Interceptors
{
    internal class QueryMetricsLoggingInterceptor : DbCommandInterceptor
    {
        private readonly ILogger<QueryMetricsLoggingInterceptor> logger;
        private readonly int minSizeKb;

        public QueryMetricsLoggingInterceptor(
            ILoggerFactory loggerFactory,
            int minSizeKb)
        {
            this.logger = loggerFactory.CreateLogger<QueryMetricsLoggingInterceptor>();
            this.minSizeKb = minSizeKb;
        }

        public override DbDataReader ReaderExecuted(DbCommand command, CommandExecutedEventData eventData, DbDataReader result)
        {
            result = this.LogQueryMetrics(command, eventData, result);
            return base.ReaderExecuted(command, eventData, result);
        }

        public override ValueTask<DbDataReader> ReaderExecutedAsync(DbCommand command, CommandExecutedEventData eventData, DbDataReader result, CancellationToken cancellationToken = default)
        {
            result = this.LogQueryMetrics(command, eventData, result);
            return base.ReaderExecutedAsync(command, eventData, result, cancellationToken);
        }

        private static int CalculateSizeOfData(DbDataReader reader)
        {
            int totalSizeInBytes = 0;

            while (reader.Read())
            {
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    object value = reader.GetValue(i);

                    if (value is DBNull)
                    {
                        continue;
                    }
                    else if (value is string str)
                    {
                        totalSizeInBytes += Encoding.UTF8.GetByteCount(str);
                    }
                    else if (value is byte[] byteArray)
                    {
                        totalSizeInBytes += byteArray.Length;
                    }
                    else
                    {
                        FieldInfo[] fields = value.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                        foreach (var field in fields)
                        {
                            totalSizeInBytes += Marshal.SizeOf(field.FieldType);
                        }
                    }
                }
            }

            return totalSizeInBytes;
        }

        private DbDataReader LogQueryMetrics(DbCommand command, CommandExecutedEventData eventData, DbDataReader result)
        {
            result.Close();

            var newReader = command.ExecuteReader();
            var dataSizeInBytes = CalculateSizeOfData(newReader);
            newReader.Close();

            var dataSizeInKb = ByteSize.FromBytes(dataSizeInBytes).KiloBytes;

            if (dataSizeInKb >= this.minSizeKb)
            {
                var sb = new StringBuilder("Query Metrics (Read)");
                sb.AppendLine();
                sb.AppendLine($"Duration: {eventData.Duration.TotalMilliseconds}ms");
                sb.AppendLine($"Estimated Data Size: {dataSizeInKb}Kb");
                sb.AppendLine("Command Text:");
                sb.AppendLine(command.CommandText);
                sb.AppendLine("Stack Trace:");

                var stackTraceLines = new StackTrace(true).ToString()
                    .Split("\r\n")
                    .Where(x =>
                    {
                        var trimmed = x.Trim();

                        return !string.IsNullOrWhiteSpace(x)
                            && !trimmed.StartsWith("at lambda")
                            && !trimmed.StartsWith("at System.")
                            && !trimmed.StartsWith("at Microsoft.")
                            && !trimmed.StartsWith("at Items.Data.EFCore.");
                    })
                    .Where(x => !string.IsNullOrWhiteSpace(x.Trim()))
                    .ToList();

                sb.AppendLine(stackTraceLines.StringJoin("\r\n"));

                this.logger.LogDebug(sb.ToString());
            }

            return command.ExecuteReader();
        }
    }
}
