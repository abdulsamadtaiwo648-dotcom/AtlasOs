namespace Atlas.Core.Events;

public interface IEventBus
{
    void Publish(IEvent @event);
}