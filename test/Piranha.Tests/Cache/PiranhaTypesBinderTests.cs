/*
 * Copyright (c) .NET Foundation and Contributors
 *
 * This software may be modified and distributed under the terms
 * of the MIT license. See the LICENSE file for details.
 *
 * https://github.com/piranhacms/piranha.core
 *
 */

using Newtonsoft.Json;
using Piranha.Cache;
using Xunit;

namespace Piranha.Tests.Security;

/// <summary>
/// Regression tests for issue #142: TypeNameHandling.All in DistributedCache
/// enabled RCE via cache poisoning. Verifies that PiranhaTypesBinder correctly
/// whitelists only Piranha assemblies during JSON deserialization.
/// </summary>
public class PiranhaTypesBinderTests
{
    private readonly PiranhaTypesBinder _binder = new();

    [Fact]
    public void BindToType_AllowsPiranhaAssembly()
    {
        // Piranha.Models.Site lives in the "Piranha" assembly — must be allowed.
        var type = _binder.BindToType("Piranha", "Piranha.Models.Site");

        Assert.Equal(typeof(Piranha.Models.Site), type);
    }

    [Theory]
    [InlineData("System", "System.Object")]
    [InlineData("mscorlib", "System.Object")]
    [InlineData("System.Windows.Forms", "System.Windows.Forms.Form")]
    [InlineData("Newtonsoft.Json", "Newtonsoft.Json.Linq.JObject")]
    public void BindToType_RejectsNonPiranhaAssemblies(string assemblyName, string typeName)
    {
        // Non-Piranha types must be rejected to prevent gadget-chain RCE.
        Assert.Throws<JsonSerializationException>(
            () => _binder.BindToType(assemblyName, typeName));
    }

    [Fact]
    public void BindToName_EmitsShortAssemblyName()
    {
        _binder.BindToName(typeof(Piranha.Models.Site), out var assemblyName, out var typeName);

        Assert.Equal("Piranha", assemblyName);
        Assert.Equal("Piranha.Models.Site", typeName);
    }

    [Fact]
    public void DistributedCache_SerializerSettings_UseAutoNotAll()
    {
        // Verify via reflection that DistributedCache no longer uses TypeNameHandling.All,
        // which was the root cause of the RCE vulnerability.
        var field = typeof(DistributedCache)
            .GetField("_jsonSettings", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        var cache = new DistributedCache(new FakeDistributedCache());
        var settings = (JsonSerializerSettings)field.GetValue(cache);

        Assert.Equal(TypeNameHandling.Auto, settings.TypeNameHandling);
        Assert.IsType<PiranhaTypesBinder>(settings.SerializationBinder);
    }

    [Fact]
    public async Task DistributedCache_RoundTrip_PolymorphicPiranhaType()
    {
        // Confirm that removing TypeNameHandling.All did not break round-trip
        // serialization of concrete Piranha types stored as a base-class reference.
        var inner = new FakeDistributedCache();
        var cache = new DistributedCache(inner);
        var site = new Piranha.Models.Site { Title = "Test", InternalId = "test" };

        await cache.SetAsync("s1", site);
        var roundTripped = await cache.GetAsync<Piranha.Models.Site>("s1");

        Assert.Equal(site.Title, roundTripped.Title);
        Assert.Equal(site.InternalId, roundTripped.InternalId);
    }

    // Minimal in-memory IDistributedCache so tests don't need a real cache.
    private sealed class FakeDistributedCache : Microsoft.Extensions.Caching.Distributed.IDistributedCache
    {
        private readonly Dictionary<string, byte[]> _store = new();

        public byte[] Get(string key) => _store.TryGetValue(key, out var v) ? v : null;
        public Task<byte[]> GetAsync(string key, CancellationToken token = default) => Task.FromResult(Get(key));
        public void Refresh(string key) { }
        public Task RefreshAsync(string key, CancellationToken token = default) => Task.CompletedTask;
        public void Remove(string key) => _store.Remove(key);
        public Task RemoveAsync(string key, CancellationToken token = default) { Remove(key); return Task.CompletedTask; }
        public void Set(string key, byte[] value, Microsoft.Extensions.Caching.Distributed.DistributedCacheEntryOptions options) => _store[key] = value;
        public Task SetAsync(string key, byte[] value, Microsoft.Extensions.Caching.Distributed.DistributedCacheEntryOptions options, CancellationToken token = default) { Set(key, value, options); return Task.CompletedTask; }
    }
}
