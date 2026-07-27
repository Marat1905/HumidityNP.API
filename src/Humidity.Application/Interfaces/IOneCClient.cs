using Humidity.Application.DTOs;

namespace Humidity.Application.Interfaces
{
    /// <summary>
    /// Клиент для взаимодействия с SOAP-сервисом 1С.
    /// </summary>
    public interface IOneCClient
    {
        /// <summary>
        /// Получить список машин из 1С за указанный период.
        /// </summary>
        /// <param name="from">Начало периода (включительно).</param>
        /// <param name="to">Конец периода (включительно).</param>
        /// <param name="cancellationToken">Токен отмены.</param>
        /// <returns>Список DTO машин.</returns>
        Task<IEnumerable<OneCVehicleDto>> GetVehiclesAsync(
            DateTimeOffset from,
            DateTimeOffset to,
            CancellationToken cancellationToken = default);
    }
}
