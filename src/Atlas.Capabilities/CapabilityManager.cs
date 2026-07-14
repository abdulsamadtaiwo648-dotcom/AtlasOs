using Atlas.Capabilities.Interfaces;

namespace Atlas.Capabilities;

public class CapabilityManager
{
    private readonly IEnumerable<ICapability> _capabilities;

    public CapabilityManager(IEnumerable<ICapability> capabilities)
    {
        _capabilities = capabilities;
    }

    public async Task<string?> TryExecuteAsync(string input)
    {
        foreach (ICapability capability in _capabilities)
        {
            if (capability.CanHandle(input))
            {
                return await capability.ExecuteAsync(input);
            }
        }

        return null;
    }
}