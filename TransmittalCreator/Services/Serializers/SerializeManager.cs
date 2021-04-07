using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TransmittalCreator.Services.Serializers
{
    public class SerializeManager
    {
        private readonly ISerializer _serializer;

        public string Filename => _serializer.Filename;
        
        public SerializeManager(ISerializer serializer)
        {
            _serializer = serializer;
        }

        public string Serialize<T>(T item)
        {
            return _serializer.Serialize(item);
        }
        public T Deserialize<T>(Stream stream)
        {
            return _serializer.Deserialize<T>(stream);
        }
    }
}
