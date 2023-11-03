using System.IO;
using Newtonsoft.Json;

namespace TransmittalCreator.Services.Serializers;

//public class TransmJsonSerializer : ISerializer
//{
//	public TransmJsonSerializer(string filename)
//	{
//		Filename = filename;
//	}

//	public T Deserialize<T>(Stream stream)
//	{
//		var serializer = new JsonSerializer();
//		using (var sr = new StreamReader(stream))
//		using (var jsonTextReader = new JsonTextReader(sr))
//		{
//			return (T)serializer.Deserialize(jsonTextReader, typeof(T));
//		}
//	}

//	public string Filename { get; set; }

//	public string Serialize<T>(T item)
//	{
//		return JsonConvert.SerializeObject(item, Formatting.Indented);
//	}
//}