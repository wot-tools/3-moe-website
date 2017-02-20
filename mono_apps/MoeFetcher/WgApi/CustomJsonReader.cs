using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WgApi
{
    class CustomJsonReader : JsonTextReader
    {
        public CustomJsonReader(TextReader reader) : base(reader) { }

        //relies on getElement returning null after the last element
        public IEnumerable<T> ReadArray<T>(Func<CustomJsonReader, T> getElement, Func<T, bool> includeElement)
        {
            T currentResult;
            LOOP:
            if ((currentResult = getElement(this)) == null)
                yield break;
            if (includeElement(currentResult))
                yield return currentResult;
            goto LOOP;
        }

        public T ReadValue<T>(string propertyName, Func<T> readValue, JsonToken endToken = JsonToken.None)
        {
            while (Read())
            {
                if (TokenType == endToken)
                    return default(T);
                if (TokenType == JsonToken.PropertyName && Value.ToString() == propertyName)
                    return readValue();
            }
            throw new KeyNotFoundException();
        }

        public void ReadToProperty(string propertyName, JsonToken endToken = JsonToken.None)
        {
            if (false == ReadToPropertyIfExisting(propertyName, endToken))
                throw new KeyNotFoundException();
        }

        public bool ReadToPropertyIfExisting(string propertyName, JsonToken endToken = JsonToken.None)
        {
            while (Read())
            {
                if (TokenType == JsonToken.PropertyName && Value.ToString() == propertyName)
                    return true;
                if (TokenType == endToken)
                    return false;
            }
            return false;
        }

        public string ReadNextPropertyNameAsData(JsonToken endToken = JsonToken.None)
        {
            while (Read())
            {
                if (TokenType == endToken)
                    return null;
                if (TokenType == JsonToken.PropertyName)
                    return Value.ToString();
            }
            throw new Exception();
        }

        public DateTime ReadAsEpoch()
        {
            return new DateTime(1970, 1, 1).AddSeconds(ReadAsInt32().Value);
        }
    }
}
