using YamlDotNet.Serialization;

namespace DotnetLensMcp.SerializerExtensions;

public static class SerializerHelper
{
    private static readonly ISerializer Serializer;


    static SerializerHelper()
    {
        Serializer = (new SerializerBuilder()).Build();
    }

    public static string ToSerialized(this object o)
    {
        return Serializer.Serialize(o);
    }
}