using System.IO;

namespace TransmittalCreator.Services.Serializers;

public interface ISerializer
{
	string Filename { get; }
	string Serialize<T>(T item);
	T Deserialize<T>(Stream stream);
}