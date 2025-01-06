using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;

namespace Clothes.Data;

public class RedisCacheService
{
    private readonly IDistributedCache? DistributedCache;
    
    public RedisCacheService(IDistributedCache? distributedCache)
    {
        DistributedCache = distributedCache;
    }
    
    public async Task SetRecordAsync<T>(string recordId, T data)
    {
        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5),
            SlidingExpiration = TimeSpan.FromMinutes(1)
        };
        
        var jsonData = JsonSerializer.Serialize(data);
        await DistributedCache?.SetStringAsync(recordId, jsonData, options);
    }
    
    public async Task<T?> GetRecordAsync<T>(string recordId)
    {
        var jsonData = await DistributedCache?.GetStringAsync(recordId);
        
        if (jsonData is null)
        {
            return default;
        }
        
        return JsonSerializer.Deserialize<T>(jsonData);
    }
    
    public async Task RemoveRecordAsync(string recordId)
    {
        await DistributedCache?.RemoveAsync(recordId);
    }
}