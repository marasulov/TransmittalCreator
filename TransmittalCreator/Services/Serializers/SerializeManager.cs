using System.IO;

namespace TransmittalCreator.Services.Serializers;

//public class SerializeManager
//{
//	private readonly ISerializer _serializer;

//	public SerializeManager(ISerializer serializer)
//	{
//		_serializer = serializer;
//	}

//	public string Filename => _serializer.Filename;

//	public string Serialize<T>(T item)
//	{
//		return _serializer.Serialize(item);
//	}

//	public T Deserialize<T>(Stream stream)
//	{
//		return _serializer.Deserialize<T>(stream);
//	}
//}