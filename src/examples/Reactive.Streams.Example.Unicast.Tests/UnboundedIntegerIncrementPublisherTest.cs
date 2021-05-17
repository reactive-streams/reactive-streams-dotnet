﻿/***************************************************
 * Licensed under MIT No Attribution (SPDX: MIT-0) *
 ***************************************************/
using System;
using System.Collections;
using System.Collections.Generic;
using Xunit;
using Xunit.Abstractions;
using Reactive.Streams.TCK;

namespace Reactive.Streams.Example.Unicast.Tests
{
    public class UnboundedIntegerIncrementPublisherTest : PublisherVerification<int?>
    {
        public UnboundedIntegerIncrementPublisherTest(ITestOutputHelper output) : base(new TestEnvironment(output))
        {
        }

        public override IPublisher<int?> CreatePublisher(long elements) => new InfiniteIncrementNumberPublisher();


        public override IPublisher<int?> CreateFailedPublisher() => new FailedPublisher();

        private sealed class FailedPublisher : AsyncIterablePublisher<int?>
        {
            public FailedPublisher() : base(new FailedEnumerable())
            {
            }

            private sealed class FailedEnumerable : IEnumerable<int?>
            {
                public IEnumerator<int?> GetEnumerator()
                {
                    throw new Exception("Error state signal!");
                }

                IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            }
        }

        public override long MaxElementsFromPublisher => PublisherUnableToSignalOnComplete;
    }
}
