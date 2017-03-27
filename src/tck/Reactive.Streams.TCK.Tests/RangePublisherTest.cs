using NUnit.Framework;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Reactive.Streams.TCK.Tests
{
    [TestFixture]
    public class RangePublisherTest : PublisherVerification<int>
    {
        static readonly ConcurrentDictionary<int, string> stacks = new ConcurrentDictionary<int, string>();

        static readonly ConcurrentDictionary<int, bool> states = new ConcurrentDictionary<int, bool>();

        static int id;

        [TearDown]
        public void AfterTest()
        {
            bool fail = false;
            StringBuilder b = new StringBuilder();
            foreach (var t in states)
            {
                if (!t.Value)
                {
                    b.Append("\r\n-------------------------------");

                    b.Append("\r\nat ").Append(stacks[t.Key]);

                    fail = true;
                }
            }
            states.Clear();
            stacks.Clear();
            if (fail)
            {
                throw new InvalidOperationException("Cancellations were missing:" + b);
            }
        }

        public RangePublisherTest() : base(new TestEnvironment())
        {
        }

        public override IPublisher<int> CreatePublisher(long elements)
        {
            return new RangePublisher(1, elements);
        }

        public override IPublisher<int> CreateFailedPublisher()
        {
            return null;
        }

        internal sealed class RangePublisher : IPublisher<int>
        {

            readonly string stacktrace;

            readonly long start;

            readonly long count;

            internal RangePublisher(long start, long count)
            {
                this.stacktrace = Environment.StackTrace;
                this.start = start;
                this.count = count;
            }

            public void Subscribe(ISubscriber<int> s)
            {
                if (s == null)
                {
                    throw new ArgumentNullException();
                }

                int ids = Interlocked.Increment(ref id);

                RangeSubscription parent = new RangeSubscription(s, ids, start, start + count);
                stacks.AddOrUpdate(ids, (a) => stacktrace, (a, b) => stacktrace);
                states.AddOrUpdate(ids, (a) => false, (a, b) => false);
                s.OnSubscribe(parent);
            }

            sealed class RangeSubscription : ISubscription
            {

                readonly ISubscriber<int> actual;

                readonly int ids;

                readonly long end;

                long index;

                volatile bool cancelled;

                long requested;

                internal RangeSubscription(ISubscriber<int> actual, int ids, long start, long end)
                {
                    this.actual = actual;
                    this.ids = ids;
                    this.index = start;
                    this.end = end;
                }


                public void Request(long n)
                {
                    if (!cancelled)
                    {
                        if (n <= 0L)
                        {
                            cancelled = true;
                            states[ids] = true;
                            actual.OnError(new ArgumentException("§3.9 violated"));
                            return;
                        }

                        for (;;)
                        {
                            long r = Volatile.Read(ref requested);
                            long u = r + n;
                            if (u < 0L)
                            {
                                u = long.MaxValue;
                            }
                            if (Interlocked.CompareExchange(ref requested, u, r) == r)
                            {
                                if (r == 0)
                                {
                                    break;
                                }
                                return;
                            }
                        }

                        long idx = index;
                        long f = end;

                        for (;;)
                        {
                            long e = 0;
                            while (e != n && idx != f)
                            {
                                if (cancelled)
                                {
                                    return;
                                }

                                actual.OnNext((int)idx);

                                idx++;
                                e++;
                            }

                            if (idx == f)
                            {
                                if (!cancelled)
                                {
                                    states[ids] = true;
                                    actual.OnComplete();
                                }
                                return;
                            }

                            index = idx;
                            n = Interlocked.Add(ref requested, -n);
                            if (n == 0)
                            {
                                break;
                            }
                        }
                    }
                }

                public void Cancel()
                {
                    cancelled = true;
                    states[ids] = true;
                }
            }
        }
    }
}
