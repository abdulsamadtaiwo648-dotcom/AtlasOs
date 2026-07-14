namespace Atlas.Core.Events;

public class MessageReceivedEvent : IEvent
{
    public string Message { get; }

    public MessageReceivedEvent(string message)
    {
        Message = message;
    }
}