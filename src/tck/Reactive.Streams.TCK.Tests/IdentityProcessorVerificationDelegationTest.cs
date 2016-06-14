using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using NUnit.Framework;

namespace Reactive.Streams.TCK.Tests
{
    /// <summary>
    /// The <see cref="IdentityProcessorVerification{T}"/> must also run all tests from
    /// <see cref="PublisherVerification{T}"/> and <see cref="SubscriberWhiteboxVerification{T}"/>.
    /// 
    /// Since in .Net this can be only achieved by delegating, we need to make sure we delegate to each of the tests,
    /// so that if in the future we add more tests to these verifications we're sure to not forget to add the delegating methods.
    /// </summary>
    public class IdentityProcessorVerificationDelegationTest
    {
        [Test]
        public void ShouldIncludeAllTestsFromPublisherVerification()
        {
            var processeroTests = GetTestNames(typeof(IdentityProcessorVerification<>)).ToList();
            var publisherTests = GetTestNames(typeof(PublisherVerification<>)).ToList();
            AssertSuiteDelegatedAllTests(typeof(IdentityProcessorVerification<>), processeroTests,
                typeof(PublisherVerification<>), publisherTests);
        }
        
        [Test]
        public void ShouldIncludeAllTestsFromSubscriberVerification()
        {
            var processeroTests = GetTestNames(typeof(IdentityProcessorVerification<>)).ToList();
            var subscriberTests = GetTestNames(typeof(SubscriberWhiteboxVerification<>)).ToList();
            AssertSuiteDelegatedAllTests(typeof(IdentityProcessorVerification<>), processeroTests,
                typeof(SubscriberWhiteboxVerification<>), subscriberTests);
        }

        private static void AssertSuiteDelegatedAllTests(Type delegatingFrom, IList<string> allTests, Type targetClass,
            IList<string> delegatedToTests)
        {
            foreach (var targetTest in delegatedToTests)
            {
                var message = new StringBuilder();
                message.AppendLine($"Test '{targetTest}' in '{targetClass}' has not been properly delegated to in aggregate '{delegatingFrom}'!");
                message.AppendLine($"You must delegate to this test from {delegatingFrom}, like this:");
                message.AppendLine("[Test]");
                message.AppendLine($"public void {targetTest} () => delegate{targetClass.Name}.{targetTest}();");

                Assert.True(TestsInclude(allTests, targetTest), message.ToString());
            }
        }

        private static bool TestsInclude(IList<string> processorTests, string targetTest)
            => processorTests.Contains(targetTest);

        private static IEnumerable<string> GetTestNames(Type type)
            => type.GetMethods()
                .Where(m => m.GetCustomAttribute<TestAttribute>() != null)
                .Select(m => m.Name);
    }
}
