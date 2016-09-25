using System;
using System.Diagnostics;
using System.Security.Principal;
using System.Threading;
using log4net.Appender;
using log4net.Config;
using log4net.Core;
using log4net.Layout;
using Xunit;
using Xunit.Abstractions;

namespace log4net.ext.boost
{
    public sealed class AccelerateForwardingAppenderTests
    {
        public abstract class TestBase
        {
            protected readonly AccelerateForwardingAppender AccelerateForwardingAppender;

            public TestBase()
            {
                AccelerateForwardingAppender = new AccelerateForwardingAppender();
            }
        }

        public sealed class Constructor : TestBase
        {
            [Fact]
            public void Should_enable_CacheUsername()
            {
                Assert.True(AccelerateForwardingAppender.CacheUsername);
            }

            [Fact]
            public void Should_enable_CacheIdentity()
            {
                Assert.True(AccelerateForwardingAppender.CacheIdentity);
            }
        }

        public sealed class Username : TestBase
        {
            [Fact]
            public void Should_return_current_windows_identity_name_if_not_defined()
            {
                // arrange
                var expected = WindowsIdentity.GetCurrent().Name;

                // act
                var actual = AccelerateForwardingAppender.Username;

                // assert
                Assert.Equal(expected, actual);
            }

            [Fact]
            public void Should_return_predefined_value_if_set()
            {
                // arrange
                const string expected = "USER_NAME";
                AccelerateForwardingAppender.Username = expected;

                // act
                var actual = AccelerateForwardingAppender.Username;

                // assert
                Assert.Equal(expected, actual);
            }
        }

        public sealed class Identity : TestBase
        {
            [Fact]
            public void Should_return_current_thread_identity_name_if_not_defined()
            {
                // arrange
                var expected = Thread.CurrentPrincipal.Identity.Name;

                // act
                var actual = AccelerateForwardingAppender.Identity;

                // assert
                Assert.Equal(expected, actual);
            }

            [Fact]
            public void Should_return_predefined_value_if_set()
            {
                // arrange
                const string expected = "IDENTITY_NAME";
                AccelerateForwardingAppender.Identity = expected;

                // act
                var actual = AccelerateForwardingAppender.Identity;

                // assert
                Assert.Equal(expected, actual);
            }
        }

        public sealed class DoAppend : TestBase
        {
            [Theory]
            [InlineData(true)]
            [InlineData(false)]
            public void Should_define_the_user_name_depends_on_CacheUsername(bool accelerate)
            {
                // arrange
                const string expected = "TEST_USER_NAME";

                AccelerateForwardingAppender.CacheUsername = accelerate;
                AccelerateForwardingAppender.Username = expected;

                var loggingEvent = new LoggingEvent(new LoggingEventData());

                // act
                AccelerateForwardingAppender.DoAppend(loggingEvent);
                var actual = loggingEvent.UserName;

                // assert
                var assert = accelerate ? (Action<string, string>)Assert.Equal : Assert.NotEqual;
                assert(expected, actual);
            }

            [Theory]
            [InlineData(true)]
            [InlineData(false)]
            public void Should_define_the_identity_depends_on_CacheIdentity(bool accelerate)
            {
                // arrange
                const string expected = "TEST_IDENTITY";

                AccelerateForwardingAppender.CacheIdentity = accelerate;
                AccelerateForwardingAppender.Identity = expected;

                var loggingEvent = new LoggingEvent(new LoggingEventData());

                // act
                AccelerateForwardingAppender.DoAppend(loggingEvent);
                var actual = loggingEvent.Identity;

                // assert
                var assert = accelerate ? (Action<string, string>)Assert.Equal : Assert.NotEqual;
                assert(expected, actual);
            }
        }

        public sealed class Performance : TestBase
        {
            private readonly ITestOutputHelper output;

            public Performance(ITestOutputHelper output)
            {
                this.output = output;
            }

            [Fact]
            public void Should_accelerate_the_logging_flow()
            {
                // arrange
                Trace.AutoFlush = Trace.UseGlobalLock = false;
                Trace.Listeners.Clear();

                var traceAppender = new TraceAppender { Layout = new PatternLayout("%timestamp [%thread] %ndc - %message%newline") };
                var accelerateForwardingAppender = AccelerateForwardingAppender;
                accelerateForwardingAppender.AddAppender(traceAppender);

                var slowElapsed = Perform(traceAppender);
                var fastElapsed = Perform(accelerateForwardingAppender);

                output.WriteLine($"{slowElapsed:g}/{fastElapsed:g} = x{(decimal)slowElapsed.Ticks/fastElapsed.Ticks:F}");

                // arrange
                Assert.True(slowElapsed.Ticks / 10 > fastElapsed.Ticks);
            }

            private static TimeSpan Perform(IAppender appender)
            {
                var stopwatch = Stopwatch.StartNew();
                for (var i = 0; i < 10000; i++)
                {
                    appender.DoAppend(new LoggingEvent(new LoggingEventData { TimeStamp  = DateTime.UtcNow, Message = "TEST" }));
                }
                stopwatch.Stop();
                return stopwatch.Elapsed;
            }
        }
    }
}