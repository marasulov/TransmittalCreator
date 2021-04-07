using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace TransmittalCreator.Services.Serializers
{
    public class TransmJsonSerializer:ISerializer
    {

        public T Deserialize<T>(Stream stream)
        {
            JsonSerializer serializer = new JsonSerializer();
            using (var sr = new StreamReader(stream))
            using (var jsonTextReader = new JsonTextReader(sr))
            {
                return (T)serializer.Deserialize(jsonTextReader, typeof(T));
            }
        }

        public string Filename { get; set; }

        

        public TransmJsonSerializer(string filename)
        {
            Filename = filename;
        }

        public string Serialize<T>(T item)
        {
            return JsonConvert.SerializeObject(item, Formatting.Indented);
        }
    }
}
