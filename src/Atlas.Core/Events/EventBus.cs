namespace Atlas.Core.Events;

public class EventBus : IEventBus
{
    public void Publish(IEvent @event)
    {
        // We'll implement subscribers later.
    }
}