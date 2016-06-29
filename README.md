﻿# Reactive Streams .NET#

The purpose of Reactive Streams is to provide a standard for asynchronous stream processing with non-blocking backpressure.

The latest release is [available on NuGet](https://www.nuget.org/packages/Reactive.Streams).

To install Reactive Streams, run the following command in the Package Manager Console

```
PM> Install-Package Reactive.Streams
```

## Goals, Design and Scope ##

Handling streams of data—especially “live” data whose volume is not predetermined—requires special care in an asynchronous system. The most prominent issue is that resource consumption needs to be carefully controlled such that a fast data source does not overwhelm the stream destination. Asynchrony is needed in order to enable the parallel use of computing resources, on collaborating network hosts or multiple CPU cores within a single machine.

The main goal of Reactive Streams is to govern the exchange of stream data across an asynchronous boundary – think passing elements on to another thread or thread-pool — while ensuring that the receiving side is not forced to buffer arbitrary amounts of data. In other words, backpressure is an integral part of this model in order to allow the queues which mediate between threads to be bounded. The benefits of asynchronous processing would be negated if the communication of backpressure were synchronous (see also the [Reactive Manifesto](http://reactivemanifesto.org/)), therefore care has been taken to mandate fully non-blocking and asynchronous behavior of all aspects of a Reactive Streams implementation.

It is the intention of this specification to allow the creation of many conforming implementations, which by virtue of abiding by the rules will be able to interoperate smoothly, preserving the aforementioned benefits and characteristics across the whole processing graph of a stream application.

It should be noted that the precise nature of stream manipulations (transformation, splitting, merging, etc.) is not covered by this specification. Reactive Streams are only concerned with mediating the stream of data between different [API Components](#api-components). In their development care has been taken to ensure that all basic ways of combining streams can be expressed.

In summary, Reactive Streams .NET is a standard and specification for Stream-oriented libraries for .NET that

 - process a potentially unbounded number of elements
 - in sequence,
 - asynchronously passing elements between components,
 - with mandatory non-blocking backpressure.

The Reactive Streams specification consists of the following parts:

***The API*** specifies the types to implement Reactive Streams and achieve interoperability between different implementations.

***The Technology Compatibility Kit (TCK)*** is a standard test suite for conformance testing of implementations.

Implementations are free to implement additional features not covered by the specification as long as they conform to the API requirements and pass the tests in the TCK.

### API Components ###

The API consists of the following components that are required to be provided by Reactive Stream implementations:

1. Publisher
2. Subscriber
3. Subscription
4. Processor

A *Publisher* is a provider of a potentially unbounded number of sequenced elements, publishing them according to the demand received from its Subscriber(s).

In response to a call to `Publisher.Subscribe(Subscriber)` the possible invocation sequences for methods on the `Subscriber` are given by the following protocol:

```
onSubscribe onNext* (onError | onComplete)?
```

This means that `onSubscribe` is always signalled,
followed by a possibly unbounded number of `onNext` signals (as requested by `Subscriber`) followed by an `onError` signal if there is a failure, or an `onComplete` signal when no more elements are available—all as long as the `Subscription` is not cancelled.

#### NOTES

- The specifications below use binding words in capital letters from https://www.ietf.org/rfc/rfc2119.txt
- The terms `emit`, `signal` or `send` are interchangeable. The specifications below will use `signal`.
- The terms `synchronously` or `synchronous` refer to executing in the calling `Thread`.
- The term "return normally" means "only throws exceptions that are explicitly allowed by the rule".

### SPECIFICATION

#### 1. Publisher ([Code](https://github.com/reactive-streams/reactive-streams-dotnet/blob/master/src/Reactive.Streams/IPublisher.cs))

```c#
public interface IPublisher<out T> {
    void Subscribe(ISubscriber<T> subscriber);
}
````

| ID                        | Rule                                                                                                   |
| ------------------------- | ------------------------------------------------------------------------------------------------------ |
| <a name="1.1">1</a>       | The total number of `onNext` signals sent by a `Publisher` to a `Subscriber` MUST be less than or equal to the total number of elements requested by that `Subscriber`´s `Subscription` at all times. |
| <a name="1.2">2</a>       | A `Publisher` MAY signal less `onNext` than requested and terminate the `Subscription` by calling `OnComplete` or `OnError`. |
| <a name="1.3">3</a>       | `onSubscribe`, `onNext`, `onError` and `onComplete` signaled to a `Subscriber` MUST be signaled sequentially (no concurrent notifications). |
| <a name="1.4">4</a>       | If a `Publisher` fails it MUST signal an `onError`. |
| <a name="1.5">5</a>       | If a `Publisher` terminates successfully (finite stream) it MUST signal an `onComplete`. |
| <a name="1.6">6</a>       | If a `Publisher` signals either `onError` or `onComplete` on a `Subscriber`, that `Subscriber`’s `Subscription` MUST be considered cancelled. |
| <a name="1.7">7</a>       | Once a terminal state has been signaled (`onError`, `onComplete`) it is REQUIRED that no further signals occur. |
| <a name="1.8">8</a>       | If a `Subscription` is cancelled its `Subscriber` MUST eventually stop being signaled. |
| <a name="1.9">9</a>       | `Publisher.Subscribe` MUST call `OnSubscribe` on the provided `Subscriber` prior to any other signals to that `Subscriber` and MUST return normally, except when the provided `Subscriber` is `null` in which case it MUST throw a `System.NullReferenceException` to the caller, for all other situations [[1]](#footnote-1-1) the only legal way to signal failure (or reject the `Subscriber`) is by calling `OnError` (after calling `OnSubscribe`). |
| <a name="1.10">10</a>     | `Publisher.Subscribe` MAY be called as many times as wanted but MUST be with a different `Subscriber` each time [see [2.12](#2.12)]. |
| <a name="1.11">11</a>     | A `Publisher` MAY support multiple `Subscriber`s and decides whether each `Subscription` is unicast or multicast. |

[<a name="footnote-1-1">1</a>] :  A stateful Publisher can be overwhelmed, bounded by a finite number of underlying resources, exhausted, shut-down or in a failed state.

#### 2. Subscriber ([Code](https://github.com/reactive-streams/reactive-streams-dotnet/blob/master/src/Reactive.Streams/ISubscriber.cs))

```c#
public interface ISubscriber<in T> {
    public void OnSubscribe(ISubscription subscription);
    public void OnNext(T element);
    public void OnError(Exception cause);
    public void OnComplete();
}
````

| ID                        | Rule                                                                                                   |
| ------------------------- | ------------------------------------------------------------------------------------------------------ |
| <a name="2.1">1</a>       | A `Subscriber` MUST signal demand via `Subscription.Request(long n)` to receive `onNext` signals. |
| <a name="2.2">2</a>       | If a `Subscriber` suspects that its processing of signals will negatively impact its `Publisher`'s responsivity, it is RECOMMENDED that it asynchronously dispatches its signals. |
| <a name="2.3">3</a>       | `Subscriber.OnComplete()` and `Subscriber.OnError(Exception cause)` MUST NOT call any methods on the `Subscription` or the `Publisher`. |
| <a name="2.4">4</a>       | `Subscriber.OnComplete()` and `Subscriber.OnError(Exception cause)` MUST consider the Subscription cancelled after having received the signal. |
| <a name="2.5">5</a>       | A `Subscriber` MUST call `Subscription.Cancel()` on the given `Subscription` after an `onSubscribe` signal if it already has an active `Subscription`. |
| <a name="2.6">6</a>       | A `Subscriber` MUST call `Subscription.Cancel()` if it is no longer valid to the `Publisher` without the `Publisher` having signaled `onError` or `onComplete`. |
| <a name="2.7">7</a>       | A `Subscriber` MUST ensure that all calls on its `Subscription` take place from the same thread or provide for respective external synchronization. |
| <a name="2.8">8</a>       | A `Subscriber` MUST be prepared to receive one or more `onNext` signals after having called `Subscription.Cancel()` if there are still requested elements pending [see [3.12](#3.12)]. `Subscription.Cancel()` does not guarantee to perform the underlying cleaning operations immediately. |
| <a name="2.9">9</a>       | A `Subscriber` MUST be prepared to receive an `onComplete` signal with or without a preceding `Subscription.Request(long n)` call. |
| <a name="2.10">10</a>     | A `Subscriber` MUST be prepared to receive an `onError` signal with or without a preceding `Subscription.Request(long n)` call. |
| <a name="2.11">11</a>     | A `Subscriber` MUST make sure that all calls on its `OnXXX` methods happen-before [[1]](#footnote-2-1) the processing of the respective signals. I.e. the Subscriber must take care of properly publishing the signal to its processing logic. |
| <a name="2.12">12</a>     | `Subscriber.OnSubscribe` MUST be called at most once for a given `Subscriber` (based on object equality). |
| <a name="2.13">13</a>     | Calling `OnSubscribe`, `OnNext`, `OnError` or `OnComplete` MUST return normally except when any provided parameter is `null` in which case it MUST throw a `System.NullReferenceException` to the caller, for all other situations the only legal way for a `Subscriber` to signal failure is by cancelling its `Subscription`. In the case that this rule is violated, any associated `Subscription` to the `Subscriber` MUST be considered as cancelled, and the caller MUST raise this error condition in a fashion that is adequate for the runtime environment. |

[<a name="footnote-2-1">1</a>] : See JMM definition of Happen-Before in section 17.4.5. on http://docs.oracle.com/javase/specs/jls/se7/html/jls-17.html

#### 3. Subscription ([Code](https://github.com/reactive-streams/reactive-streams-dotnet/blob/master/src/Reactive.Streams/ISubscription.cs))

```c#
public interface ISubscription {
    public void Request(long n);
    public void Cancel();
}
````

| ID                        | Rule                                                                                                   |
| ------------------------- | ------------------------------------------------------------------------------------------------------ |
| <a name="3.1">1</a>       | `Subscription.Request` and `Subscription.Cancel` MUST only be called inside of its `Subscriber` context. A `Subscription` represents the unique relationship between a `Subscriber` and a `Publisher` [see [2.12](#2.12)]. |
| <a name="3.2">2</a>       | The `Subscription` MUST allow the `Subscriber` to call `Subscription.Request` synchronously from within `OnNext` or `OnSubscribe`. |
| <a name="3.3">3</a>       | `Subscription.Request` MUST place an upper bound on possible synchronous recursion between `Publisher` and `Subscriber`[[1](#footnote-3-1)]. |
| <a name="3.4">4</a>       | `Subscription.Request` SHOULD respect the responsivity of its caller by returning in a timely manner[[2](#footnote-3-2)]. |
| <a name="3.5">5</a>       | `Subscription.Cancel` MUST respect the responsivity of its caller by returning in a timely manner[[2](#footnote-3-2)], MUST be idempotent and MUST be thread-safe. |
| <a name="3.6">6</a>       | After the `Subscription` is cancelled, additional `Subscription.Request(long n)` MUST be NOPs. |
| <a name="3.7">7</a>       | After the `Subscription` is cancelled, additional `Subscription.Cancel()` MUST be NOPs. |
| <a name="3.8">8</a>       | While the `Subscription` is not cancelled, `Subscription.Request(long n)` MUST register the given number of additional elements to be produced to the respective subscriber. |
| <a name="3.9">9</a>       | While the `Subscription` is not cancelled, `Subscription.Request(long n)` MUST signal `onError` with a `System.ArgumentException` if the argument is <= 0. The cause message MUST include a reference to this rule and/or quote the full rule. |
| <a name="3.10">10</a>     | While the `Subscription` is not cancelled, `Subscription.Request(long n)` MAY synchronously call `OnNext` on this (or other) subscriber(s). |
| <a name="3.11">11</a>     | While the `Subscription` is not cancelled, `Subscription.Request(long n)` MAY synchronously call `OnComplete` or `OnError` on this (or other) subscriber(s). |
| <a name="3.12">12</a>     | While the `Subscription` is not cancelled, `Subscription.Cancel()` MUST request the `Publisher` to eventually stop signaling its `Subscriber`. The operation is NOT REQUIRED to affect the `Subscription` immediately. |
| <a name="3.13">13</a>     | While the `Subscription` is not cancelled, `Subscription.Cancel()` MUST request the `Publisher` to eventually drop any references to the corresponding subscriber. Re-subscribing with the same `Subscriber` object is discouraged [see [2.12](#2.12)], but this specification does not mandate that it is disallowed since that would mean having to store previously cancelled subscriptions indefinitely. |
| <a name="3.14">14</a>     | While the `Subscription` is not cancelled, calling `Subscription.Cancel` MAY cause the `Publisher`, if stateful, to transition into the `shut-down` state if no other `Subscription` exists at this point [see [1.9](#1.9)].
| <a name="3.15">15</a>     | Calling `Subscription.Cancel` MUST return normally. The only legal way to signal failure to a `Subscriber` is via the `OnError` method. |
| <a name="3.16">16</a>     | Calling `Subscription.Request` MUST return normally. The only legal way to signal failure to a `Subscriber` is via the `OnError` method. |
| <a name="3.17">17</a>     | A `Subscription` MUST support an unbounded number of calls to Request and MUST support a demand (sum requested - sum delivered) up to 2^63-1 (`System.Int64.MaxValue`). A demand equal or greater than 2^63-1 (`System.Int64.MaxValue`) MAY be considered by the `Publisher` as “effectively unbounded”[[3](#footnote-3-3)]. |

[<a name="footnote-3-1">1</a>] : An example for undesirable synchronous, open recursion would be `Subscriber.OnNext` -> `Subscription.Request` -> `Subscriber.OnNext` -> …, as it very quickly would result in blowing the calling Thread´s stack.

[<a name="footnote-3-2">2</a>] : Avoid heavy computations and other things that would stall the caller´s thread of execution

[<a name="footnote-3-3">3</a>] : As it is not feasibly reachable with current or foreseen hardware within a reasonable amount of time (1 element per nanosecond would take 292 years) to fulfill a demand of 2^63-1, it is allowed for a `Publisher` to stop tracking demand beyond this point.

A `Subscription` is shared by exactly one `Publisher` and one `Subscriber` for the purpose of mediating the data exchange between this pair. This is the reason why the `Subscribe()` method does not return the created `Subscription`, but instead returns `void`; the `Subscription` is only passed to the `Subscriber` via the `OnSubscribe` callback.

#### 4.Processor ([Code](https://github.com/reactive-streams/reactive-streams-dotnet/blob/master/src/Reactive.Streams/IProcessor.cs))

```c#
public interface IProcessor<in T1, out T2> : ISubscriber<T1>, IPublisher<T2> {
}
````

| ID                       | Rule                                                                                                   |
| ------------------------ | ------------------------------------------------------------------------------------------------------ |
| <a name="4.1">1</a>      | A `Processor` represents a processing stage—which is both a `Subscriber` and a `Publisher` and MUST obey the contracts of both. |
| <a name="4.2">2</a>      | A `Processor` MAY choose to recover an `onError` signal. If it chooses to do so, it MUST consider the `Subscription` cancelled, otherwise it MUST propagate the `onError` signal to its Subscribers immediately. |

While not mandated, it can be a good idea to cancel a `Processors` upstream `Subscription` when/if its last `Subscriber` cancels their `Subscription`,
to let the cancellation signal propagate upstream.

### Asynchronous vs Synchronous Processing ###

The Reactive Streams API prescribes that all processing of elements (`onNext`) or termination signals (`onError`, `onComplete`) MUST NOT *block* the `Publisher`. However, each of the `On*` handlers can process the events synchronously or asynchronously.

Take this example:

```
nioSelectorThreadOrigin map(f) filter(p) consumeTo(toNioSelectorOutput)
```

It has an async origin and an async destination. Let's assume that both origin and destination are selector event loops. The `Subscription.Request(n)` must be chained from the destination to the origin. This is now where each implementation can choose how to do this.

The following uses the pipe `|` character to signal async boundaries (queue and schedule) and `R#` to represent resources (possibly threads).

```
nioSelectorThreadOrigin | map(f) | filter(p) | consumeTo(toNioSelectorOutput)
-------------- R1 ----  | - R2 - | -- R3 --- | ---------- R4 ----------------
```

In this example each of the 3 consumers, `map`, `filter` and `consumeTo` asynchronously schedule the work. It could be on the same event loop (trampoline), separate threads, whatever.

```
nioSelectorThreadOrigin map(f) filter(p) | consumeTo(toNioSelectorOutput)
------------------- R1 ----------------- | ---------- R2 ----------------
```

Here it is only the final step that asynchronously schedules, by adding work to the NioSelectorOutput event loop. The `map` and `filter` steps are synchronously performed on the origin thread.

Or another implementation could fuse the operations to the final consumer:

```
nioSelectorThreadOrigin | map(f) filter(p) consumeTo(toNioSelectorOutput)
--------- R1 ---------- | ------------------ R2 -------------------------
```

All of these variants are "asynchronous streams". They all have their place and each has different tradeoffs including performance and implementation complexity.

The Reactive Streams contract allows implementations the flexibility to manage resources and scheduling and mix asynchronous and synchronous processing within the bounds of a non-blocking, asynchronous, push-based stream.

In order to allow fully asynchronous implementations of all participating API elements—`IPublisher`/`ISubscription`/`ISubscriber`/`IProcessor`—all methods defined by these interfaces return `void`.

### Subscriber controlled queue bounds ###

One of the underlying design principles is that all buffer sizes are to be bounded and these bounds must be *known* and *controlled* by the subscribers. These bounds are expressed in terms of *element count* (which in turn translates to the invocation count of onNext). Any implementation that aims to support infinite streams (especially high output rate streams) needs to enforce bounds all along the way to avoid out-of-memory errors and constrain resource usage in general.

Since back-pressure is mandatory the use of unbounded buffers can be avoided. In general, the only time when a queue might grow without bounds is when the publisher side maintains a higher rate than the subscriber for an extended period of time, but this scenario is handled by backpressure instead.

Queue bounds can be controlled by a subscriber signaling demand for the appropriate number of elements. At any point in time the subscriber knows:

 - the total number of elements requested: `P`
 - the number of elements that have been processed: `N`

Then the maximum number of elements that may arrive—until more demand is signaled to the Publisher—is `P - N`. In the case that the subscriber also knows the number of elements B in its input buffer then this bound can be refined to `P - B - N`.

These bounds must be respected by a publisher independent of whether the source it represents can be backpressured or not. In the case of sources whose production rate cannot be influenced—for example clock ticks or mouse movement—the publisher must choose to either buffer or drop elements to obey the imposed bounds.

Subscribers signaling a demand for one element after the reception of an element effectively implement a Stop-and-Wait protocol where the demand signal is equivalent to acknowledgement. By providing demand for multiple elements the cost of acknowledgement is amortized. It is worth noting that the subscriber is allowed to signal demand at any point in time, allowing it to avoid unnecessary delays between the publisher and the subscriber (i.e. keeping its input buffer filled without having to wait for full round-trips).

## Legal

This project is a collaboration between engineers from Kaazing, Netflix, Pivotal, Red Hat, Twitter, Typesafe and many others. The code is offered to the Public Domain in order to allow free use by interested parties who want to create compatible implementations. For details see `COPYING`.

<p xmlns:dct="http://purl.org/dc/terms/" xmlns:vcard="http://www.w3.org/2001/vcard-rdf/3.0#">
  <a rel="license" href="http://creativecommons.org/publicdomain/zero/1.0/">
    <img src="http://i.creativecommons.org/p/zero/1.0/88x31.png" style="border-style: none;" alt="CC0" />
  </a>
  <br />
  To the extent possible under law,
  <a rel="dct:publisher" href="http://www.reactive-streams.org/">
    <span property="dct:title">Reactive Streams Special Interest Group</span></a>
  has waived all copyright and related or neighboring rights to
  <span property="dct:title">Reactive Streams .NET</span>.
  This work is published from:
  <span property="vcard:Country" datatype="dct:ISO3166" content="US" about="http://www.reactive-streams.org/">United States</span>.
</p>
