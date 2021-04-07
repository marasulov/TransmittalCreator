using System.IO;

namespace TransmittalCreator.Services.Serializers
{
    public interface ISerializer
    {
        string Serialize<T>(T item);
        T Deserialize<T>(Stream stream);
        string Filename { get; }
    }
}
