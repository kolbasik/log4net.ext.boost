using System;
using System.Diagnostics;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using log4net.Appender;
using log4net.Core;
using log4net.Layout;

namespace log4net.ext.boost.benchmark
{
    public static class Program
    {
        private static void Main(string[] args)
        {
            var config = ManualConfig.Create(DefaultConfig.Instance)
                .With(StatisticColumn.AllStatistics)
                .With(Job.Default);

            BenchmarkRunner.Run<BoostBenchmark>(config);

            Console.WriteLine("Press any key to exit...");
            Console.ReadKey(true);
        }

        public class BoostBenchmark
        {
            public BoostBenchmark()
            {
                Trace.AutoFlush = Trace.UseGlobalLock = false;
                Trace.Listeners.Clear();

                TraceAppender = new TraceAppender { Layout = new PatternLayout("%timestamp [%thread] %ndc - %message%newline") };
                AccelerateForwardingAppender = new AccelerateForwardingAppender();
                AccelerateForwardingAppender.AddAppender(TraceAppender);
            }

            private TraceAppender TraceAppender { get; }
            private AccelerateForwardingAppender AccelerateForwardingAppender { get; }

            [Benchmark]
            public void TraceAppenderBenchmark()
            {
                Perform(TraceAppender);
            }

            [Benchmark]
            public void AccelerateForwardingAppenderBenchmark()
            {
                Perform(AccelerateForwardingAppender);
            }

            private static void Perform(IAppender appender)
            {
                for (var i = 0; i < 100; i++)
                {
                    appender.DoAppend(new LoggingEvent(new LoggingEventData { TimeStamp = DateTime.UtcNow, Message = "TEST" }));
                }
            }
        }
    }
}