using System;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Security.Principal;
using System.Threading;
using log4net.Appender;
using log4net.Core;

namespace log4net.ext.boost
{
    public sealed class AccelerateForwardingAppender : ForwardingAppender
    {
        private static readonly FieldAccessor<LoggingEvent, LoggingEventData> LoggingEventDataAccessor;

        static AccelerateForwardingAppender()
        {
            LoggingEventDataAccessor = new FieldAccessor<LoggingEvent, LoggingEventData>(@"m_data");
        }

        public AccelerateForwardingAppender()
        {
            CacheUsername = true;
            CacheIdentity = true;
            Username = WindowsIdentity.GetCurrent().Name ?? string.Empty;
            Identity = Thread.CurrentPrincipal.Identity?.Name ?? string.Empty;
        }

        public bool CacheUsername { get; set; }
        public bool CacheIdentity { get; set; }
        public string Username { get; set; }
        public string Identity { get; set; }

        protected override void Append(LoggingEvent loggingEvent)
        {
            Accelerate(loggingEvent);
            base.Append(loggingEvent);
        }

        protected override void Append(LoggingEvent[] loggingEvents)
        {
            for (var i = 0; i < loggingEvents.Length; i++)
            {
                Accelerate(loggingEvents[i]);
            }
            base.Append(loggingEvents);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Accelerate(LoggingEvent loggingEvent)
        {
            if (CacheUsername || CacheIdentity)
            {
                var loggingEventData = LoggingEventDataAccessor.Get(loggingEvent);
                if (CacheUsername)
                {
                    loggingEventData.UserName = Username;
                }
                if (CacheIdentity)
                {
                    loggingEventData.Identity = Identity;
                }
                LoggingEventDataAccessor.Set(loggingEvent, loggingEventData);
            }
        }

        private sealed class FieldAccessor<TSubject, TField>
        {
            public readonly Func<TSubject, TField> Get;
            public readonly Action<TSubject, TField> Set;

            public FieldAccessor(string fieldName)
            {
                Get = FieldReflection.CreateGetDelegate<TSubject, TField>(fieldName);
                Set = FieldReflection.CreateSetDelegate<TSubject, TField>(fieldName);
            }
        }

        private static class FieldReflection
        {
            public static Func<TSubject, TField> CreateGetDelegate<TSubject, TField>(string fieldName)
            {
                var owner = Expression.Parameter(typeof(TSubject), @"owner");
                var field = Expression.Field(owner, fieldName);
                var lambda = Expression.Lambda<Func<TSubject, TField>>(field, owner);
                return lambda.Compile();
            }

            public static Action<TS, TF> CreateSetDelegate<TS, TF>(string fieldName)
            {
                var owner = Expression.Parameter(typeof(TS), @"owner");
                var value = Expression.Parameter(typeof(TF), @"value");
                var field = Expression.Field(owner, fieldName);
                var assign = Expression.Assign(field, value);
                var lambda = Expression.Lambda<Action<TS, TF>>(assign, owner, value);
                return lambda.Compile();
            }
        }
    }
}
