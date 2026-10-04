using Calorator.Database.Models;

namespace Calorator.Database.Contracts;

/// <summary>
/// Интерфейс доступа к данным (IDataAccess на диаграмме компонентов).
/// Предоставляет сервисам бизнес-логики (BLL) методы для работы с пользователями,
/// дневником питания, продуктами, приемами пищи, водой и рецептами.
/// </summary>
public interface IDataAccess
{
    /// <summary>
    /// Низкоуровневый интерфейс базы данных SQLite.
    /// </summary>
    IDataBase Database { get; }

    #region Пользователи (User) - Требование 1
    /// <summary>
    /// Регистрирует нового пользователя.
    /// </summary>
    Task<int> CreateUserAsync(User user, CancellationToken ct = default);

    /// <summary>
    /// Получает пользователя по идентификатору.
    /// </summary>
    Task<User?> GetUserByIdAsync(int id, CancellationToken ct = default);

    /// <summary>
    /// Получает пользователя по логину.
    /// </summary>
    Task<User?> GetUserByLoginAsync(string login, CancellationToken ct = default);

    /// <summary>
    /// Обновляет профильные данные пользователя (возраст, рост, вес, активность и т.д.).
    /// </summary>
    Task<bool> UpdateUserProfileAsync(User user, CancellationToken ct = default);

    /// <summary>
    /// Обновляет хэш пароля пользователя.
    /// </summary>
    Task<bool> UpdatePasswordAsync(int userId, string newPasswordHash, CancellationToken ct = default);

    /// <summary>
    /// Удаляет пользователя и все связанные с ним данные (каскадно).
    /// </summary>
    Task<bool> DeleteUserAsync(int userId, CancellationToken ct = default);

    /// <summary>
    /// Проверяет, занят ли логин.
    /// </summary>
    Task<bool> UserExistsAsync(string login, CancellationToken ct = default);
    #endregion

    #region Нормы КБЖУ (NutritionNorm) - Требование 2
    /// <summary>
    /// Сохраняет рассчитанную суточную норму КБЖУ пользователя.
    /// </summary>
    Task<int> SaveNutritionNormAsync(NutritionNorm norm, CancellationToken ct = default);

    /// <summary>
    /// Возвращает последнюю актуальную норму КБЖУ пользователя.
    /// </summary>
    Task<NutritionNorm?> GetLatestNutritionNormByUserIdAsync(int userId, CancellationToken ct = default);

    /// <summary>
    /// Возвращает историю рассчитанных норм КБЖУ пользователя.
    /// </summary>
    Task<IReadOnlyList<NutritionNorm>> GetNutritionNormHistoryByUserIdAsync(int userId, CancellationToken ct = default);
    #endregion

    #region Цели пользователя (Goal) - Требование 8
    /// <summary>
    /// Добавляет или обновляет цель пользователя (снижение веса, поддержание, набор).
    /// </summary>
    Task<int> SetGoalAsync(Goal goal, CancellationToken ct = default);

    /// <summary>
    /// Получает текущую активную цель пользователя.
    /// </summary>
    Task<Goal?> GetCurrentGoalByUserIdAsync(int userId, CancellationToken ct = default);

    /// <summary>
    /// Получает историю целей пользователя.
    /// </summary>
    Task<IReadOnlyList<Goal>> GetGoalHistoryByUserIdAsync(int userId, CancellationToken ct = default);

    /// <summary>
    /// Удаляет цель по идентификатору.
    /// </summary>
    Task<bool> DeleteGoalAsync(int goalId, CancellationToken ct = default);
    #endregion

    #region Продукты и штрихкоды (Product) - Требования 4, 7, 9
    /// <summary>
    /// Добавляет новый продукт в базу данных.
    /// </summary>
    Task<int> AddProductAsync(Product product, CancellationToken ct = default);

    /// <summary>
    /// Получает продукт по его идентификатору.
    /// </summary>
    Task<Product?> GetProductByIdAsync(int id, CancellationToken ct = default);

    /// <summary>
    /// Находит продукт по штрихкоду (для сканера/внешнего API).
    /// </summary>
    Task<Product?> GetProductByBarcodeAsync(string barcode, CancellationToken ct = default);

    /// <summary>
    /// Ищет продукты по названию (частичное совпадение).
    /// </summary>
    Task<IReadOnlyList<Product>> SearchProductsAsync(string query, int limit = 50, CancellationToken ct = default);

    /// <summary>
    /// Возвращает все продукты из базы данных.
    /// </summary>
    Task<IReadOnlyList<Product>> GetAllProductsAsync(CancellationToken ct = default);

    /// <summary>
    /// Обновляет параметры продукта.
    /// </summary>
    Task<bool> UpdateProductAsync(Product product, CancellationToken ct = default);

    /// <summary>
    /// Удаляет продукт из базы данных.
    /// </summary>
    Task<bool> DeleteProductAsync(int id, CancellationToken ct = default);
    #endregion

    #region Дневник питания и приемы пищи (DailyLog & Meal) - Требования 3, 4
    /// <summary>
    /// Получает существующую или создает новую запись дневника на указанную дату.
    /// </summary>
    Task<DailyLog> GetOrCreateDailyLogAsync(int userId, DateTime date, CancellationToken ct = default);

    /// <summary>
    /// Получает запись дневника по пользователю и дате.
    /// </summary>
    Task<DailyLog?> GetDailyLogAsync(int userId, DateTime date, bool includeDetails = true, CancellationToken ct = default);

    /// <summary>
    /// Получает запись дневника по первичному ключу.
    /// </summary>
    Task<DailyLog?> GetDailyLogByIdAsync(int id, bool includeDetails = true, CancellationToken ct = default);

    /// <summary>
    /// Получает список дневников пользователя за выбранный период дат.
    /// </summary>
    Task<IReadOnlyList<DailyLog>> GetDailyLogsByPeriodAsync(int userId, DateTime startDate, DateTime endDate, bool includeDetails = true, CancellationToken ct = default);

    /// <summary>
    /// Добавляет прием пищи (Завтрак, Обед, Ужин, Перекус) в дневник.
    /// </summary>
    Task<int> AddMealAsync(Meal meal, CancellationToken ct = default);

    /// <summary>
    /// Получает прием пищи со всеми продуктами по идентификатору.
    /// </summary>
    Task<Meal?> GetMealByIdAsync(int id, bool includeItems = true, CancellationToken ct = default);

    /// <summary>
    /// Получает все приемы пищи за день.
    /// </summary>
    Task<IReadOnlyList<Meal>> GetMealsByDailyLogIdAsync(int dailyLogId, bool includeItems = true, CancellationToken ct = default);

    /// <summary>
    /// Удаляет прием пищи и входящие в него продукты.
    /// </summary>
    Task<bool> DeleteMealAsync(int mealId, CancellationToken ct = default);
    #endregion

    #region Элементы приема пищи (MealItem) - Требование 4
    /// <summary>
    /// Добавляет продукт с указанием веса в прием пищи с автоматическим расчетом КБЖУ.
    /// </summary>
    Task<int> AddMealItemAsync(MealItem item, CancellationToken ct = default);

    /// <summary>
    /// Обновляет граммовку и КБЖУ продукта в приеме пищи.
    /// </summary>
    Task<bool> UpdateMealItemAsync(MealItem item, CancellationToken ct = default);

    /// <summary>
    /// Удаляет продукт из приема пищи.
    /// </summary>
    Task<bool> DeleteMealItemAsync(int itemId, CancellationToken ct = default);

    /// <summary>
    /// Получает элемент приема пищи по идентификатору.
    /// </summary>
    Task<MealItem?> GetMealItemByIdAsync(int id, CancellationToken ct = default);
    #endregion

    #region Учет воды (WaterIntake) - Требование 6
    /// <summary>
    /// Записывает количество выпитой воды.
    /// </summary>
    Task<int> AddWaterIntakeAsync(WaterIntake water, CancellationToken ct = default);

    /// <summary>
    /// Удаляет запись о выпитой воде.
    /// </summary>
    Task<bool> DeleteWaterIntakeAsync(int waterIntakeId, CancellationToken ct = default);

    /// <summary>
    /// Получает все записи о воде за день.
    /// </summary>
    Task<IReadOnlyList<WaterIntake>> GetWaterIntakesByDailyLogIdAsync(int dailyLogId, CancellationToken ct = default);

    /// <summary>
    /// Возвращает суммарный объем воды (мл) за указанную дату.
    /// </summary>
    Task<int> GetTotalWaterMlByDateAsync(int userId, DateTime date, CancellationToken ct = default);
    #endregion

    #region Рецепты (Recipe & RecipeIngredient) - Требование 7
    /// <summary>
    /// Создает новый рецепт пользователя.
    /// </summary>
    Task<int> CreateRecipeAsync(Recipe recipe, CancellationToken ct = default);

    /// <summary>
    /// Получает рецепт с ингредиентами по идентификатору.
    /// </summary>
    Task<Recipe?> GetRecipeByIdAsync(int id, bool includeIngredients = true, CancellationToken ct = default);

    /// <summary>
    /// Получает все рецепты пользователя.
    /// </summary>
    Task<IReadOnlyList<Recipe>> GetRecipesByUserIdAsync(int userId, bool includeIngredients = true, CancellationToken ct = default);

    /// <summary>
    /// Обновляет данные рецепта (название, порции).
    /// </summary>
    Task<bool> UpdateRecipeAsync(Recipe recipe, CancellationToken ct = default);

    /// <summary>
    /// Удаляет рецепт и его ингредиенты.
    /// </summary>
    Task<bool> DeleteRecipeAsync(int recipeId, CancellationToken ct = default);

    /// <summary>
    /// Добавляет ингредиент (продукт и вес) в рецепт.
    /// </summary>
    Task<int> AddRecipeIngredientAsync(RecipeIngredient ingredient, CancellationToken ct = default);

    /// <summary>
    /// Изменяет граммовку ингредиента в рецепте.
    /// </summary>
    Task<bool> UpdateRecipeIngredientAmountAsync(int ingredientId, double amountGrams, CancellationToken ct = default);

    /// <summary>
    /// Удаляет ингредиент из рецепта.
    /// </summary>
    Task<bool> RemoveRecipeIngredientAsync(int ingredientId, CancellationToken ct = default);

    /// <summary>
    /// Возвращает список ингредиентов рецепта с подгруженными данными о продуктах.
    /// </summary>
    Task<IReadOnlyList<RecipeIngredient>> GetRecipeIngredientsAsync(int recipeId, CancellationToken ct = default);
    #endregion

    #region Расчет КБЖУ за период и приемы пищи - Требование 5
    /// <summary>
    /// Рассчитывает суммарное КБЖУ и потребление воды за выбранный период дат.
    /// </summary>
    Task<NutritionSummary> GetNutritionSummaryForPeriodAsync(int userId, DateTime startDate, DateTime endDate, CancellationToken ct = default);

    /// <summary>
    /// Рассчитывает суммарное КБЖУ для конкретного приема пищи.
    /// </summary>
    Task<NutritionSummary> GetNutritionSummaryForMealAsync(int mealId, CancellationToken ct = default);
    #endregion
}
