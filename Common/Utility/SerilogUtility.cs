using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Serilog.Events;

namespace Common.Utility
{
    public enum LogFileSearchDateType
    {
        Today = 1,
        Yesterday = 2,
        ThisWeek = 3,
        LastWeek = 4
    }

    public static class SerilogUtility
    {
        private static readonly Dictionary<LogEventLevel, (string Full, string Char3, string Char1)> LogLevelToOutputFormatMap = new()
        {
            { LogEventLevel.Verbose, ("Verbose", "VRB", "V") },
            { LogEventLevel.Debug, ("Debug", "DBG", "D") },
            { LogEventLevel.Information, ("Information", "INF", "I") },
            { LogEventLevel.Warning, ("Warning", "WRN", "W") },
            { LogEventLevel.Error, ("Error", "ERR", "E") },
            { LogEventLevel.Fatal, ("Fatal", "FTL", "F") }
        };

        private static readonly Regex RgxLogLevel = new Regex(@"\[(?<Level>VRB|DBG|INF|WRN|ERR|FTL)\]");

        public static List<string> FindLogEntries(
            IConfiguration configuration,
            LogFileSearchDateType? logFileSearchDateType = null,
            DateTime? logFileDate = null,
            string lineFilter = null,
            IEnumerable<LogEventLevel> logLevels = null)
        {
            if (logFileSearchDateType == null && logFileDate == null)
            {
                throw new ArgumentException($"Must specify either {nameof(logFileSearchDateType)} or {nameof(logFileDate)}");
            }

            var (logFileFolder, logFilePrefix, logFileExtension) = ReadRollingFileSettingsFromConfiguration(configuration);

            if (logFileDate != null)
            {
                try
                {
                    return FindLogEntries(
                        logFileFolder: logFileFolder,
                        logFilePrefix: logFilePrefix,
                        logFileExtension: logFileExtension,
                        logFileDate: logFileDate.Value,
                        lineFilter: lineFilter,
                        logLevels: logLevels);
                }
                catch (FileNotFoundException fnfex)
                {
                    return new List<string> { fnfex.Message };
                }
                catch
                {
                    throw;
                }
            }

            var logFileDates = new List<DateTime>();

            switch (logFileSearchDateType)
            {
                case LogFileSearchDateType.Today:
                    logFileDates.Add(DateTime.Today);
                    break;

                case LogFileSearchDateType.Yesterday:
                    logFileDates.Add(DateTime.Today.AddDays(-1));
                    break;

                case LogFileSearchDateType.ThisWeek:
                    var thisWeekDate = DateTime.Today;

                    while (thisWeekDate.DayOfWeek != DayOfWeek.Sunday)
                    {
                        logFileDates.Add(thisWeekDate);
                        thisWeekDate = thisWeekDate.AddDays(-1);
                    }

                    break;

                case LogFileSearchDateType.LastWeek:
                    var lastWeekDate = DateTime.Today;

                    while (lastWeekDate.DayOfWeek != DayOfWeek.Sunday)
                    {
                        lastWeekDate = lastWeekDate.AddDays(-1);
                    }

                    logFileDates.Add(lastWeekDate);
                    lastWeekDate = lastWeekDate.AddDays(-1);

                    while (lastWeekDate.DayOfWeek != DayOfWeek.Sunday)
                    {
                        logFileDates.Add(lastWeekDate);
                        lastWeekDate = lastWeekDate.AddDays(-1);
                    }

                    break;

                default:
                    throw new NotSupportedException();
            }

            var allLines = new List<string>();

            foreach (var logFileIterationDate in logFileDates)
            {
                try
                {
                    allLines.AddRange(FindLogEntries(
                        logFileFolder: logFileFolder,
                        logFilePrefix: logFilePrefix,
                        logFileExtension: logFileExtension,
                        logFileDate: logFileIterationDate,
                        lineFilter: lineFilter,
                        logLevels: logLevels));
                }
                catch (FileNotFoundException fnfex)
                {
                    allLines.Add(fnfex.Message);
                }
                catch
                {
                    throw;
                }
            }

            return allLines;
        }

        private static (string LogFileFolder, string LogFilePrefix, string LogFileExtension) ReadRollingFileSettingsFromConfiguration(
            IConfiguration configuration)
        {
            var serilogWriteToSection = configuration.GetSection("Serilog:WriteTo")?.GetChildren().ToArray();

            if (serilogWriteToSection != null)
            {
                var fileSection = serilogWriteToSection.FirstOrDefault(x => x.GetValue<string>("Name") == "File");

                if (fileSection != null)
                {
                    var rollingType = fileSection.GetValue<string>("Args:rollingInterval");

                    if (rollingType != "Day")
                    {
                        throw new NotSupportedException("Currently only supports daily rolling log file");
                    }

                    var rollingLogFilePath = fileSection.GetValue<string>("Args:path");

                    if (!string.IsNullOrWhiteSpace(rollingLogFilePath))
                    {
                        var filenameFormat = rollingLogFilePath.Split(Path.DirectorySeparatorChar).Last();
                        var prefix = filenameFormat.Split("..")[0];
                        var extension = filenameFormat.Split("..").ElementAtOrDefault(1);

                        if (extension == null)
                        {
                            throw new NotSupportedException("Currently only supports filename format {Prefix}..{Extension}");
                        }

                        rollingLogFilePath = Directory
                            .GetParent(rollingLogFilePath.Replace("%BASE_DIR%", EnvironmentUtility.ApplicationBaseDirectory))
                            .FullName;

                        return (rollingLogFilePath, prefix, extension);
                    }
                }
            }

            throw new InvalidOperationException("Could not find Serilog settings in configuration in expected format");
        }

        private static List<string> FindLogEntries(
            string logFileFolder,
            string logFilePrefix,
            string logFileExtension,
            DateTime logFileDate,
            string lineFilter = null,
            IEnumerable<LogEventLevel> logLevels = null)
        {
            HashSet<string> logLevelsToString = null;

            if (logLevels != null)
            {
                logLevelsToString = logLevels.Select(x => LogLevelToOutputFormatMap[x].Char3).ToHashSet();
            }

            var fullPath = FileUtility.CombinePath(logFileFolder, $"{logFilePrefix}.{logFileDate:yyyyMMdd}.{logFileExtension}");

            if (!File.Exists(fullPath))
            {
                throw new FileNotFoundException($"Log file for {logFileDate} not found at path {fullPath}");
            }

            var linesFound = new List<string>();

            using (var fileStream = new FileStream(
                path: fullPath,
                mode: FileMode.Open,
                access: FileAccess.Read,
                share: FileShare.ReadWrite))
            {
                using (var reader = new StreamReader(fileStream))
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        HandleLine(
                            line,
                            logFileDate,
                            lineFilter,
                            logLevelsToString,
                            linesFound,
                            reader);
                    }
                }
            }

            return linesFound;
        }

        private static void HandleLine(
                string line,
                DateTime logFileDate,
                string lineFilter,
                HashSet<string> logLevelsToString,
                List<string> linesFound,
                StreamReader reader)
        {
            if (lineFilter == null || line.Contains(lineFilter, StringComparison.OrdinalIgnoreCase))
            {
                if (logLevelsToString == null)
                {
                    linesFound.Add(line);
                }
                else
                {
                    var levelMatch = RgxLogLevel.Match(line);

                    if (levelMatch.Success)
                    {
                        var level = levelMatch.Groups["Level"].Value;

                        if (logLevelsToString.Contains(level))
                        {
                            linesFound.Add(line);

                            // lines following Error or Fatal might contain exception output
                            // we need to add them to the current line if so, moving past them in the reader
                            if (level == LogLevelToOutputFormatMap[LogEventLevel.Error].Char3
                                || level == LogLevelToOutputFormatMap[LogEventLevel.Fatal].Char3)
                            {
                                string nextLine;
                                while ((nextLine = reader.ReadLine()) != null)
                                {
                                    // regular log line, handle it, break the loop, return to caller
                                    if (nextLine.StartsWith(logFileDate.ToString("yyyy")))
                                    {
                                        HandleLine(
                                            nextLine,
                                            logFileDate,
                                            lineFilter,
                                            logLevelsToString,
                                            linesFound,
                                            reader);

                                        break;
                                    }

                                    // line containing exception detail/stack trace, append it to current line and continue
                                    else
                                    {
                                        linesFound[^1] += Environment.NewLine + nextLine;
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        throw new InvalidOperationException($"Could not parse log level from log entry '{line}'");
                    }
                }
            }
        }
    }
}
