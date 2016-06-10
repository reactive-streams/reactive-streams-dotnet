using System.Linq;

namespace Reactive.Streams.Example.Unicast
{
    public class  NumberIterablePublisher : AsyncIterablePublisher<int?>
    {
        public NumberIterablePublisher(int start, int count) : base(Enumerable.Range(start, count).Cast<int?>())
        {
        }
    }
}
