using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using Common.ExtensionMethods;
using Microsoft.Extensions.Logging;

namespace Common.Utility.Classes.Timers
{
    public class BlockTimer : IDisposable
    {
        public static readonly ConcurrentDictionary<(string Tag, string Message), ConcurrentBag<double>> AllExecutions = new();
        public static readonly ConcurrentDictionary<string, bool> IsTagEnabled = new();

        private readonly Stopwatch stopwatch = new Stopwatch();
        private readonly ILogger logger;
        private readonly string message;
        private readonly string tag;

        public BlockTimer(string message, string tag = null, ILogger logger = null)
        {
            this.message = message;
            this.tag = tag;
            this.logger = logger;
            this.stopwatch.Start();
        }

        public static void SetIsEnabled(bool isEnabled, string tag = null)
        {
            IsTagEnabled.AddOrUpdate(tag, isEnabled, (_, _) => isEnabled);
        }

        public static BlockTimer Time(string message, string tag = null, ILogger logger = null)
        {
            return new BlockTimer(message, tag, logger);
        }

        public record ExecutionSummary(string Tag, string Message, int Count, double TotalMs, double MinMs, double MaxMs, double MedianMs, double MeanMs)
        {
            public override string ToString()
            {
                return $"{this.Message} - Count: {this.Count}, Total: {this.TotalMs:f3}ms, Min: {this.MinMs:f3}ms, Max: {this.MaxMs:f3}ms, Median: {this.MedianMs:f3}ms, Mean: {this.MeanMs:f3}ms";
            }
        }

#pragma warning disable SA1201 // Elements should appear in the correct order
        public static List<ExecutionSummary> GetExecutionSummaries(string tag = null)
#pragma warning restore SA1201 // Elements should appear in the correct order
        {
            return AllExecutions
                .Where(x => x.Key.Tag == (tag ?? x.Key.Tag))
                .Select(x => new ExecutionSummary(
                    Tag: tag,
                    Message: x.Key.Message,
                    Count: x.Value.Count,
                    TotalMs: x.Value.Sum(),
                    MinMs: x.Value.Min(),
                    MaxMs: x.Value.Max(),
                    MedianMs: x.Value.OrderBy(x => x).ElementAt(x.Value.Count / 2),
                    MeanMs: x.Value.Average()))
            .OrderByDescending(x => x.TotalMs)
            .ToList();
        }

        public static void LogExecutionSummaries(ILogger logger, string tag = null)
        {
            if (!IsTagEnabled.TryGetValue(tag, out var isEnabled) || isEnabled)
            {
                logger.LogDebug($"BlockTimer Execution Summary{(tag == null ? string.Empty : $" (Tag - {tag})")}:\r\n{GetExecutionSummaries(tag).Select(x => x.ToString().PadLeft(4)).StringJoin("\r\n")}");
            }
        }

        public void AddTime(string message, TimeSpan elapsed, string tag = null)
        {
            if (!IsTagEnabled.TryGetValue(tag, out var isEnabled) || isEnabled)
            {
                this.logger?.LogDebug($"BlockTimer: {message} took {elapsed.TotalMilliseconds:f2}ms");
                var executions = AllExecutions.GetOrAdd((tag, message), _ => new ConcurrentBag<double>());
                executions.Add(elapsed.TotalMilliseconds);
            }
        }

        public void Dispose()
        {
            this.stopwatch.Stop();
            this.AddTime(this.message, this.stopwatch.Elapsed, this.tag);
        }
    }
}
