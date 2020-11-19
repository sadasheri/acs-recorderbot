using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Sample.Common.Logging
{
    public static class LoggingExtensions
    {
        /// <summary>
        /// Log exceptions.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="level">The trace level.</param>
        /// <param name="exception">Exception information.</param>
        /// <param name="message">The message.</param>
        /// <returns>
        /// Log Event object.
        /// </returns>
        /// TODO bodogado/Vinay simplify overloads.
        public static LogEvent Log(
            this Logger logger,
            TraceLevel level,
            Exception exception,
            string message = null,
            [CallerMemberName] string memberName = null,
            [CallerFilePath] string filePath = null,
            [CallerLineNumber] int lineNumber = 0)
        {
            var sb = new StringBuilder();
            
            if (!string.IsNullOrWhiteSpace(message))
            {
                sb.Append(message);
            }

            if (exception != null)
            {
                if (sb.Length > 0)
                {
                    sb.AppendLine();
                }

                sb.AppendLine($"exception: {exception.Message}");
                sb.AppendLine($"StackTrace: {exception.StackTrace}");
            }

            return logger.Log(
                level,
                sb.ToString(),
                eventType: LogEventType.Trace,
                memberName: memberName,
                filePath: filePath,
                lineNumber: lineNumber
                );
        }

        /// <summary>
        /// Log exceptions with error level.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="exception">Exception information.</param>
        /// <param name="message">The message.</param>
        /// <returns>
        /// Log Event object.
        /// </returns>
        public static LogEvent Error(
            this Logger logger,
            Exception exception,
            string message = "",
            [CallerMemberName] string memberName = null,
            [CallerFilePath] string filePath = null,
            [CallerLineNumber] int lineNumber = 0) =>
                Log(
                    logger,
                    TraceLevel.Error,
                    exception,
                    message: message,
                    memberName: memberName,
                    filePath: filePath,
                    lineNumber: lineNumber);

        /// <summary>
        /// Log messages with error level.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="message">The message.</param>
        /// <returns>
        /// Log Event object.
        /// </returns>
        public static LogEvent Error(
            this Logger logger,
            string message,
            [CallerMemberName] string memberName = null,
            [CallerFilePath] string filePath = null,
            [CallerLineNumber] int lineNumber = 0) =>
                logger.Log(
                    TraceLevel.Error,
                    message,
                    eventType: LogEventType.Trace,
                    memberName: memberName,
                    filePath: filePath,
                    lineNumber: lineNumber);

        /// <summary>
        /// Log exceptions with info level.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="message">The message.</param>
        /// <returns>
        /// Log Event object.
        /// </returns>
        public static LogEvent Info(
            this Logger logger,
            string message,
            [CallerMemberName] string memberName = null,
            [CallerFilePath] string filePath = null,
            [CallerLineNumber] int lineNumber = 0) =>
                logger.Log(
                    TraceLevel.Info,
                    message,
                    eventType: LogEventType.Trace,
                    memberName: memberName,
                    filePath: filePath,
                    lineNumber: lineNumber);

        /// <summary>
        /// Log exceptions with warning level.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="exception">Exception information.</param>
        /// <param name="message">The message.</param>
        /// <returns>
        /// Log Event object.
        /// </returns>
        public static LogEvent Warn(
            this Logger logger,
            Exception exception,
            string message = "",
            [CallerMemberName] string memberName = null,
            [CallerFilePath] string filePath = null,
            [CallerLineNumber] int lineNumber = 0) =>
                Log(
                    logger,
                    TraceLevel.Warning,
                    exception,
                    message: message,
                    memberName: memberName,
                    filePath: filePath,
                    lineNumber: lineNumber);

        /// <summary>
        /// Log exceptions with warning level.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="message">The message.</param>
        /// <returns>
        /// Log Event object.
        /// </returns>
        public static LogEvent Warn(
            this Logger logger,
            string message,
            [CallerMemberName] string memberName = null,
            [CallerFilePath] string filePath = null,
            [CallerLineNumber] int lineNumber = 0) =>
                logger.Log(
                    TraceLevel.Warning,
                    message,
                    eventType: LogEventType.Trace,
                    memberName: memberName,
                    filePath: filePath,
                    lineNumber: lineNumber);

        /// <summary>
        /// Log verbose level.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="message">The message.</param>
        /// <returns>
        /// Log Event object.
        /// </returns>
        public static LogEvent Verbose(
            this Logger logger,
            string message,
            [CallerMemberName] string memberName = null,
            [CallerFilePath] string filePath = null,
            [CallerLineNumber] int lineNumber = 0) =>
                logger.Log(
                    TraceLevel.Verbose,
                    message,
                    eventType: LogEventType.Trace,
                    memberName: memberName,
                    filePath: filePath,
                    lineNumber: lineNumber);

        /// <summary>
        /// Logs the http message.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="level">The trace level.</param>
        /// <param name="direction">The direction for request.</param>
        /// <param name="traceType">Type of the http trace.</param>
        /// <param name="url">The URL.</param>
        /// <param name="method">The method.</param>
        /// <param name="headers">The headers.</param>
        /// <param name="responseCode">The response code. (used only if [traceType == HttpResponse])</param>
        /// <param name="responseTime">The response time. (used only if [traceType == HttpResponse])</param>
        /// <returns>
        /// Log Event object
        /// </returns>
        public static LogEvent LogHttpMessage(
            this Logger logger,
            TraceLevel level,
            TransactionDirection direction,
            HttpTraceType traceType,
            string url,
            string method,
            IEnumerable<KeyValuePair<string, IEnumerable<string>>> headers,
            int responseCode = 200,
            long? responseTime = null,
            [CallerMemberName] string memberName = null,
            [CallerFilePath] string filePath = null,
            [CallerLineNumber] int lineNumber = 0)
        {
            var headerText = logger.GetHeaderText(headers);

            var httpLogData = new HttpLogData
            {
                TraceType = traceType,
                TransactionDirection = direction,
                Url = url,
                Method = method,
                Headers = headerText,
            };

            if (traceType == HttpTraceType.HttpResponse)
            {
                httpLogData.ResponseStatusCode = responseCode;
                httpLogData.ResponseTime = responseTime;
            }

            var httpLogJson = JsonConvert.SerializeObject(httpLogData, Formatting.Indented);

            return logger.Log(
                level,
                eventType: LogEventType.HttpTrace,
                message: httpLogJson,
                memberName: memberName,
                filePath: filePath,
                lineNumber: lineNumber);
        }

        /// <summary>
        /// Logs the headers text.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="headers">The headers.</param>
        /// <returns>
        /// Log Text
        /// </returns>
        private static IEnumerable<string> GetHeaderText(
            this Logger logger,
            IEnumerable<KeyValuePair<string, IEnumerable<string>>> headers)
        {
            return headers?.Select(pair => $"{pair.Key}: {string.Join(", ", pair.Value)}");
        }

    }

    public class LogEvent
    {
        /// <summary>
        /// Gets or sets the trace level of the event.
        /// </summary>
        public TraceLevel Level { get; set; }

        /// <summary>
        /// Gets or sets the Timestamp of the event.
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Gets or sets the type of the event.
        /// </summary>
        public LogEventType EventType { get; set; }

        /// <summary>
        /// Gets or sets the Description of the event.
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// Gets or sets the file path
        /// </summary>
        public string FilePath { get; set; }

        /// <summary>
        /// Gets or sets the line number
        /// </summary>
        public int LineNumber { get; set; }

        /// <summary>
        /// Gets or sets the member name
        /// </summary>
        public string MemberName { get; set; }
    }

    /// <summary>
    /// The log data for http trace.
    /// </summary>
    public class HttpLogData
    {
        /// <summary>
        /// Gets or sets the transaction direction.
        /// </summary>
        public TransactionDirection TransactionDirection { get; set; }

        /// <summary>
        /// Gets or sets the trace type.
        /// </summary>
        public HttpTraceType TraceType { get; set; }

        /// <summary>
        /// Gets or sets the url.
        /// </summary>
        public string Url { get; set; }

        /// <summary>
        /// Gets or sets the method.
        /// </summary>
        public string Method { get; set; }

        /// <summary>
        /// Gets or sets the headers.
        /// </summary>
        public IEnumerable<string> Headers { get; set; }

        /// <summary>
        /// Gets or sets the content.
        /// </summary>
        public string Content { get; set; }

        /// <summary>
        /// Gets or sets the response status code.
        /// </summary>
        public int? ResponseStatusCode { get; set; }

        /// <summary>
        /// Gets or sets the response time in milliseconds.
        /// </summary>
        public long? ResponseTime { get; set; }
    }

    /// <summary>
    /// Log event type that describes what type
    /// of <see cref="LogEvent"/> this is.
    /// </summary>
    public enum LogEventType
    {
        /// <summary>
        /// The event used to track Traces.
        /// </summary>
        Trace,

        /// <summary>
        /// The event used to track HTTP Calls.
        /// </summary>
        HttpTrace,

        /// <summary>
        /// The event used to track metrics
        /// </summary>
        Metric,
    }

    /// <summary>
    /// Direction for request message.
    /// </summary>
    public enum TransactionDirection
    {
        /// <summary>
        /// The incoming request message.
        /// </summary>
        Incoming,

        /// <summary>
        /// The outgoing request message.
        /// </summary>
        Outgoing,
    }

    /// <summary>
    /// Trace used for HTTP traces.
    /// </summary>
    public enum HttpTraceType
    {
        /// <summary>
        /// The HTTP request type
        /// </summary>
        HttpRequest,

        /// <summary>
        /// The HTTP response type.
        /// </summary>
        HttpResponse,
    }
}
