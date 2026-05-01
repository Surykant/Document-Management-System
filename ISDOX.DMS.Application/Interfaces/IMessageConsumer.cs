namespace ISDOX.DMS.Application.Interfaces
{
    public interface IMessageConsumer<T> where T : class
    {
        Task HandleAsync(T message, CancellationToken ct);
    }
}
