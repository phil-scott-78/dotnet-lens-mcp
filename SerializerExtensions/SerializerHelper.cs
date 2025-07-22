using YamlDotNet.Serialization;

namespace RoslynMcp.Services.SerializerExtensions;

public static class SerializerHelper
{
    private static readonly ISerializer _serializer;


    static SerializerHelper()
    {
        _serializer = (new SerializerBuilder()).Build();
    }

    public static string ToSerialized(this object o)
    {
        return _serializer.Serialize(o);
    }
}