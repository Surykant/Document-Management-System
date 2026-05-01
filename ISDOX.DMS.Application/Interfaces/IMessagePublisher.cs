namespace ISDOX.DMS.Application.Interfaces
{
    public interface IMessagePublisher
    {
        Task PublishAsync<T>(T message, CancellationToken ct = default);
    }
}
