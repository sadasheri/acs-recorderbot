using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Sample.Common.Logging
{
    public class Logger : IObservable<LogEvent>
    {
        /// <summary>
        /// Underlying object containing all observers.
        /// To achieve Set semantics, use a dictionary.
        /// </summary>
        private readonly ConcurrentDictionary<IObserver<LogEvent>, bool> observers = new ConcurrentDictionary<IObserver<LogEvent>, bool>();

        public TraceLevel DiagnosticLevel { get; set; } = TraceLevel.Info;

        /// <summary>
        /// Initializes a new instance of the <see cref="Logger"/> class.
        /// </summary>
        /// <param name="component">The component in which log is createdThe component in which this logger is created.</param>
        /// <param name="properties">Common properties to be set on each event</param>
        /// <param name="redirectToTrace">if set to <c>true</c> [redirect to trace].</param>
        /// <param name="obfuscationConfiguration">The obfuscation configuration</param>
        public Logger(bool redirectToTrace = false)
        {
            if (redirectToTrace)
            {
                this.Subscribe(new TraceObserver());
            }
        }

        /// <summary>
        /// Create a subscription for logging events.
        /// </summary>
        /// <param name="observer">The object that is to receive notifications.</param>
        /// <returns>A reference to an interface that allows observers to stop receiving notifications before the provider has finished sending them.</returns>
        public IDisposable Subscribe(IObserver<LogEvent> observer)
        {
            if (!this.observers.TryAdd(observer, true))
            {
                throw new ArgumentException(nameof(observer));
            }

            return new Unsubscriber(observers, observer);
        }

        public LogEvent Log(
            TraceLevel level,
            string message,
            LogEventType eventType = LogEventType.Trace,
            [CallerMemberName] string memberName = null,
            [CallerFilePath] string filePath = null,
            [CallerLineNumber] int lineNumber = 0)
        {
            if (level > this.DiagnosticLevel)
            {
                return default(LogEvent);
            }

            var logEvent = new LogEvent
            {
                EventType = eventType,
                Level = level,
                Timestamp = DateTime.UtcNow,
                Message = message,
                MemberName = memberName,
                FilePath = filePath,
                LineNumber = lineNumber
            };

            this.OnNext(logEvent);
            return logEvent;
        }

        /// <summary>
        /// Notify observers.
        /// </summary>
        /// <param name="value">Value to observers.</param>
        public void OnNext(LogEvent value)
        {
            foreach (var o in this.observers.Keys)
            {
                o.OnNext(value);
            }
        }

        private class Unsubscriber : IDisposable
        {
            private ConcurrentDictionary<IObserver<LogEvent>, bool> _observers;
            private IObserver<LogEvent> _observer;

            public Unsubscriber(ConcurrentDictionary<IObserver<LogEvent>, bool> observers, IObserver<LogEvent> observer)
            {
                this._observers = observers;
                this._observer = observer;
            }

            public void Dispose()
            {
                if (_observer != null)
                {
                    _observers.TryRemove(_observer, out bool _);
                }
            }
        }

        private class TraceObserver : IObserver<LogEvent>
        {
            public void OnCompleted()
            {
                throw new NotImplementedException();
            }

            public void OnError(Exception error)
            {
                throw new NotImplementedException();
            }

            public void OnNext(LogEvent logEvent)
            {
                var text = $"{logEvent.Timestamp:O}: {logEvent.Message}";
                switch (logEvent.Level)
                {
                    case TraceLevel.Verbose:
                    case TraceLevel.Info:
                        Trace.TraceInformation(text);
                        break;
                    case TraceLevel.Warning:
                        Trace.TraceWarning(text);
                        break;
                    case TraceLevel.Error:
                        Trace.TraceError(text);
                        break;
                    case TraceLevel.Off:
                        break;
                    default:
                        Trace.TraceError($"Unsupported level: {logEvent.Level}: {text}");
                        break;
                }
            }
        }
    }
}
