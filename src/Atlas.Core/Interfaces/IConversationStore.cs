namespace Atlas.Core.Interfaces;

using Atlas.Core.Models;
public interface IConversationStore
{
    Conversation Create();

    void Save(Conversation conversation);

    Conversation? Load(Guid id);

    void Delete(Guid id);

    List<Conversation> GetAll();
}