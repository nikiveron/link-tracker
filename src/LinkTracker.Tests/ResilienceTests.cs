using LinkTracker.Contracts;
using LinkTracker.Scrapper.Application.Interfaces;
using LinkTracker.Scrapper.Infrastructure.Clients;
using Moq;
using Polly;
using Polly.CircuitBreaker;
using Polly.Timeout;
using System.Diagnostics;
using System.Net;

namespace LinkTracker.Tests;

public class ResilienceTests
{
    private class FakeHttpMessageHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> handler) : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, Task<HttpResponseMessage>> _handler = handler;

        public int CallCount { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            CallCount++;
            return await _handler(request);
        }
    }

    [Fact]
    public async Task TC_1_1_Should_Timeout_When_Service_Is_Slow()
    {
        var handler = new FakeHttpMessageHandler(async _ =>
        {
            await Task.Delay(3000);
            return new HttpResponseMessage(HttpStatusCode.OK);
        });

        var timeoutPolicy = Policy.TimeoutAsync<HttpResponseMessage>(
            TimeSpan.FromSeconds(1),
            TimeoutStrategy.Pessimistic);

        var client = new HttpClient(handler);

        var sw = Stopwatch.StartNew();

        await Assert.ThrowsAsync<TimeoutRejectedException>(() =>
            timeoutPolicy.ExecuteAsync(() => client.GetAsync("http://test")));

        sw.Stop();

        Assert.True(sw.ElapsedMilliseconds < 3000);
    }

    [Fact]
    public async Task TC_2_1_Should_Retry_On_500_And_Succeed()
    {
        int attempt = 0;

        var handler = new FakeHttpMessageHandler(_ =>
        {
            attempt++;
            if (attempt < 3)
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError));
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        });

        var retryPolicy = Policy<HttpResponseMessage>
            .HandleResult(r => r.StatusCode == HttpStatusCode.InternalServerError)
            .WaitAndRetryAsync(2, _ => TimeSpan.Zero);

        var client = new HttpClient(handler);

        var response = await retryPolicy.ExecuteAsync(() => client.GetAsync("http://test"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(3, attempt);
    }

    [Fact]
    public async Task TC_2_2_Should_Not_Retry_On_400()
    {
        int attempt = 0;

        var handler = new FakeHttpMessageHandler(_ =>
        {
            attempt++;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadRequest));
        });

        var retryPolicy = Policy<HttpResponseMessage>
            .HandleResult(r => r.StatusCode == HttpStatusCode.InternalServerError)
            .WaitAndRetryAsync(3, _ => TimeSpan.Zero);

        var client = new HttpClient(handler);

        var response = await retryPolicy.ExecuteAsync(() => client.GetAsync("http://test"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(1, attempt);
    }

    [Fact]
    public async Task TC_2_3_Should_Respect_Constant_Backoff()
    {
        int attempt = 0;
        var delay = 500;

        var handler = new FakeHttpMessageHandler(_ =>
        {
            attempt++;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError));
        });

        var retryPolicy = Policy<HttpResponseMessage>
            .HandleResult(r => r.StatusCode == HttpStatusCode.InternalServerError)
            .WaitAndRetryAsync(2, _ => TimeSpan.FromMilliseconds(delay));

        var client = new HttpClient(handler);

        var sw = Stopwatch.StartNew();

        var response = await retryPolicy.ExecuteAsync(() => client.GetAsync("http://test"));

        Assert.Equal(3, attempt);
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.True(sw.ElapsedMilliseconds >= delay * 2);
    }

    [Fact]
    public async Task TC_4_1_Should_Open_Circuit_When_Failure_Threshold_Reached()
    {
        var breaker = Policy<HttpResponseMessage>
            .HandleResult(r => r.StatusCode == HttpStatusCode.InternalServerError)
            .AdvancedCircuitBreakerAsync(
                failureThreshold: 0.5,
                samplingDuration: TimeSpan.FromSeconds(5),
                minimumThroughput: 2,
                durationOfBreak: TimeSpan.FromSeconds(5));

        var handler = new FakeHttpMessageHandler(_ =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError)));

        var client = new HttpClient(handler);

        await breaker.ExecuteAsync(() => client.GetAsync("http://test"));
        await breaker.ExecuteAsync(() => client.GetAsync("http://test"));

        await Assert.ThrowsAsync<BrokenCircuitException<HttpResponseMessage>>(() =>
            breaker.ExecuteAsync(() => client.GetAsync("http://test")));
    }

    [Fact]
    public async Task TC_4_2_HalfOpen_Should_Close_On_Success()
    {
        int attempt = 0;

        var breaker = Policy<HttpResponseMessage>
            .HandleResult(r => r.StatusCode == HttpStatusCode.InternalServerError)
            .AdvancedCircuitBreakerAsync(
                0.5,
                TimeSpan.FromSeconds(2),
                2,
                TimeSpan.FromSeconds(2));

        var handler = new FakeHttpMessageHandler(_ =>
        {
            attempt++;
            if (attempt <= 2)
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError));
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        });

        var client = new HttpClient(handler);

        for (int i = 0; i < 2; i++)
        {
            try { await breaker.ExecuteAsync(() => client.GetAsync("http://test")); }
            catch { }
        }

        await Task.Delay(2500);

        var response = await breaker.ExecuteAsync(() => client.GetAsync("http://test"));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task TC_5_1_Should_Use_Kafka_When_Http_Fails()
    {
        var httpMock = new Mock<INotificationSender>();
        httpMock.Setup(x => x.SendAsync(It.IsAny<LinkUpdateDto>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("HTTP down"));

        var kafkaMock = new Mock<INotificationSender>();
        kafkaMock.Setup(x => x.SendAsync(It.IsAny<LinkUpdateDto>(), It.IsAny<CancellationToken>()))
                 .Returns(Task.CompletedTask);

        var fallback = new FallbackNotificationSender(
            httpMock.Object,
            kafkaMock.Object);

        await fallback.SendAsync(new LinkUpdateDto(), CancellationToken.None);

        kafkaMock.Verify(x => x.SendAsync(It.IsAny<LinkUpdateDto>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}