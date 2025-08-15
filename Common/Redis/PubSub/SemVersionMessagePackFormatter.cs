using MessagePack;
using MessagePack.Formatters;
using Semver;

namespace OpenShock.Common.Redis.PubSub;

public sealed class SemVersionMessagePackFormatter : IMessagePackFormatter<SemVersion?>
{
    public void Serialize(ref MessagePackWriter writer, SemVersion? value, MessagePackSerializerOptions options)
    {
        writer.Write(value?.ToString());
    }

    public SemVersion? Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
    {
        var str = reader.ReadString();
        return str is null ? null : SemVersion.Parse(str);
    }
}