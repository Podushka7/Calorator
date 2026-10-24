using System.Net.Http.Headers;
using System.Net;

namespace NutritionApp.BarcodeRecognition;

/// <summary>
/// Выполняет HTTP GET к опубликованному сервису 1С. Не владеет переданным HttpClient;
/// адрес и авторизация задаются вызывающей стороной.
/// </summary>
public sealed class OneCProductClient : IOneCProductClient
{
    private const string BarcodePlaceholder = "{barcode}";

    private readonly HttpClient _httpClient;
    private readonly string _productByBarcodeUriTemplate;

    public OneCProductClient(HttpClient httpClient, OneCProductClientOptions options)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        ArgumentNullException.ThrowIfNull(options);

        if (string.IsNullOrWhiteSpace(options.ProductByBarcodeUriTemplate))
        {
            throw new ArgumentException(
                "Не задан URI-шаблон сервиса 1С.",
                nameof(options));
        }

        if (!options.ProductByBarcodeUriTemplate.Contains(BarcodePlaceholder, StringComparison.Ordinal))
        {
            throw new ArgumentException(
                $"URI-шаблон должен содержать маркер {BarcodePlaceholder}.",
                nameof(options));
        }

        _productByBarcodeUriTemplate = options.ProductByBarcodeUriTemplate;
    }

    /// <inheritdoc />
    public async Task<string?> GetProductDataAsync(
        string barcode,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(barcode);

        string requestUri = _productByBarcodeUriTemplate.Replace(
            BarcodePlaceholder,
            Uri.EscapeDataString(barcode.Trim()),
            StringComparison.Ordinal);

        using var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        using HttpResponseMessage response = await _httpClient.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken).ConfigureAwait(false);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
    }
}
