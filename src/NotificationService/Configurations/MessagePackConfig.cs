using MessagePack;
using MessagePack.Resolvers;
using NotificationService.Models.Dashboard;

namespace NotificationService.Configurations;

/// <summary>
/// Configuration for MessagePack serialization
/// </summary>
public static class MessagePackConfig
{
    /// <summary>
    /// Configures MessagePack resolvers for optimal performance
    /// </summary>
    public static void ConfigureMessagePack()
    {
        // Use the default resolver with compression and string interning
        var resolver = MessagePackSerializerOptions.Standard
            .WithResolver(CompositeResolver.Create(
                // Use built-in resolver for basic types
                StandardResolver.Instance,
                // Use contractless resolver for our models
                ContractlessStandardResolver.Instance
            ))
            // Enable compression for smaller payloads
            .WithCompression(MessagePackCompression.Lz4Block);

        // Set as default options
        MessagePackSerializer.DefaultOptions = resolver;
    }

    /// <summary>
    /// Gets MessagePack options optimized for dashboard data
    /// </summary>
    public static MessagePackSerializerOptions GetDashboardOptions()
    {
        return MessagePackSerializerOptions.Standard
            .WithResolver(ContractlessStandardResolver.Instance)
            .WithCompression(MessagePackCompression.Lz4Block);
    }
}

/// <summary>
/// Extension methods for MessagePack serialization
/// </summary>
public static class MessagePackExtensions
{
    /// <summary>
    /// Serializes an object to MessagePack bytes
    /// </summary>
    public static byte[] ToMessagePackBytes<T>(this T obj)
    {
        return MessagePackSerializer.Serialize(obj, MessagePackConfig.GetDashboardOptions());
    }

    /// <summary>
    /// Deserializes MessagePack bytes to an object
    /// </summary>
    public static T FromMessagePackBytes<T>(this byte[] bytes)
    {
        return MessagePackSerializer.Deserialize<T>(bytes, MessagePackConfig.GetDashboardOptions());
    }
}


