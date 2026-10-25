using System.Net;
using Xunit;

namespace NutritionApp.BarcodeRecognition.Tests;

public sealed class OneCProductClientTests
{
    private const string Template = "http://1c.example.test/Base/hs/catalog/get_product/{barcode}";

    [Fact]
    public void Constructor_rejects_null_http_client()
    {
        var options = new OneCProductClientOptions { ProductByBarcodeUriTemplate = Template };

        Assert.Throws<ArgumentNullException>(() => new OneCProductClient(null!, options));
    }

    [Fact]
    public void Constructor_rejects_null_options()
    {
        using var httpClient = new HttpClient();

        Assert.Throws<ArgumentNullException>(() => new OneCProductClient(httpClient, null!));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Constructor_rejects_empty_uri_template(string template)
    {
        using var httpClient = new HttpClient();
        var options = new OneCProductClientOptions { ProductByBarcodeUriTemplate = template };

        Assert.Throws<ArgumentException>(() => new OneCProductClient(httpClient, options));
    }

    [Fact]
    public void Constructor_rejects_template_without_barcode_placeholder()
    {
        using var httpClient = new HttpClient();
        var options = new OneCProductClientOptions
        {
            ProductByBarcodeUriTemplate = "http://1c.example.test/products/"
        };

        Assert.Throws<ArgumentException>(() => new OneCProductClient(httpClient, options));
    }

    [Fact]
    public async Task GetProductDataAsync_sends_get_to_the_configured_gtin_uri()
    {
        var handler = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{\"Name\":\"Творог\"}")
        });
        using var httpClient = new HttpClient(handler);
        var client = CreateClient(httpClient);

        string? body = await client.GetProductDataAsync("04640009499755");

        Assert.Equal(HttpMethod.Get, handler.Method);
        Assert.Equal("http://1c.example.test/Base/hs/catalog/get_product/04640009499755", handler.RequestUri);
        Assert.Contains("application/json", handler.AcceptHeader, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("{\"Name\":\"Творог\"}", body);
    }

    [Fact]
    public async Task GetProductDataAsync_returns_null_for_http_404()
    {
        var handler = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.NotFound));
        using var httpClient = new HttpClient(handler);

        string? body = await CreateClient(httpClient).GetProductDataAsync("04640009499755");

        Assert.Null(body);
    }

    [Fact]
    public async Task GetProductDataAsync_does_not_hide_other_http_errors()
    {
        var handler = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.InternalServerError));
        using var httpClient = new HttpClient(handler);

        await Assert.ThrowsAsync<HttpRequestException>(
            () => CreateClient(httpClient).GetProductDataAsync("04640009499755"));
    }

    [Fact]
    public async Task GetProductDataAsync_honors_cancellation()
    {
        var handler = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
        using var httpClient = new HttpClient(handler);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => CreateClient(httpClient).GetProductDataAsync("04640009499755", cancellation.Token));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public async Task GetProductDataAsync_rejects_empty_barcode(string? barcode)
    {
        using var httpClient = new HttpClient(new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)));

        await Assert.ThrowsAnyAsync<ArgumentException>(
            () => CreateClient(httpClient).GetProductDataAsync(barcode!));
    }

    [Fact]
    public async Task GetProductDataAsync_trims_and_uri_escapes_barcode()
    {
        var handler = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
        using var httpClient = new HttpClient(handler);

        await CreateClient(httpClient).GetProductDataAsync("  ABC/123  ");

        Assert.Equal("http://1c.example.test/Base/hs/catalog/get_product/ABC%2F123", handler.RequestUri);
    }

    [Fact]
    public async Task GetProductDataAsync_resolves_relative_template_against_base_address()
    {
        var handler = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("ok")
        });
        using var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("http://1c.example.test")
        };
        var client = new OneCProductClient(httpClient, new OneCProductClientOptions
        {
            ProductByBarcodeUriTemplate = "/Base/hs/catalog/get_product/{barcode}"
        });

        string? response = await client.GetProductDataAsync("04640009499755");

        Assert.Equal("http://1c.example.test/Base/hs/catalog/get_product/04640009499755", handler.RequestUri);
        Assert.Equal("ok", response);
    }

    private static OneCProductClient CreateClient(HttpClient httpClient) =>
        new(httpClient, new OneCProductClientOptions { ProductByBarcodeUriTemplate = Template });

    private sealed class RecordingHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _createResponse;

        public RecordingHandler(Func<HttpRequestMessage, HttpResponseMessage> createResponse) =>
            _createResponse = createResponse;

        public HttpMethod? Method { get; private set; }
        public string? RequestUri { get; private set; }
        public string AcceptHeader { get; private set; } = string.Empty;

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Method = request.Method;
            RequestUri = request.RequestUri?.ToString();
            AcceptHeader = string.Join(",", request.Headers.Accept.Select(header => header.MediaType));
            return Task.FromResult(_createResponse(request));
        }
    }
}
