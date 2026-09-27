using System.Threading.Tasks;

namespace NutritionApp.AI
{
    public class ProductRecognitionService
    {
        private readonly ProductApiClient _apiClient;

        // Получаем клиент внешнего API.
        public ProductRecognitionService(ProductApiClient apiClient)
        {
            _apiClient = apiClient;
        }

        // Проверка корректности штрихкода.
        public bool ValidateBarcode(string barcode)
        {
            throw new NotImplementedException();
        }

        // Основная функция распознавания продукта.
        // На вход получает штрихкод,
        // на выходе должна вернуть информацию о продукте.
        public Task<ProductInfo> RecognizeProductAsync(string barcode)
        {
            throw new NotImplementedException();
        }

        // Передача распознанных данных
        // в модуль сущностей и дневника.
        public void SendProductData(ProductInfo product)
        {
            throw new NotImplementedException();
        }
    }
}