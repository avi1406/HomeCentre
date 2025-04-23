using HomeCentre.PIM.Common.Interfaces;
using Newtonsoft.Json;

namespace HomeCentre.PIM.Common
{
    public class NewtonSoftJsonSerializer : IJsonSerializer
    {
        private readonly Formatting _formatting;
        private readonly JsonSerializerSettings _settings;

        public NewtonSoftJsonSerializer(Formatting formatting, JsonSerializerSettings settings)
        {
            _formatting = formatting;
            _settings = settings;
        }

        public string Serialize<T>(T t)
        {
            return JsonConvert.SerializeObject(t, _formatting, _settings);
        }

        public T Deserialize<T>(string s)
        {
            return JsonConvert.DeserializeObject<T>(s);
        }
    }
}