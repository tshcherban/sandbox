using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NoteKeeper.Model
{
    public interface IModelSaver
    {
        RootModel LoadOrNew();

        void Save(RootModel model);
    }

    internal sealed class FileSystemJsonModelSaver : IModelSaver
    {
        private readonly string _filePath;

        private readonly JsonSerializerSettings _settings;

        public FileSystemJsonModelSaver(string filePath)
        {
            _filePath = filePath;

            _settings = new JsonSerializerSettings
            {
                Formatting = Formatting.Indented,
            };
        }

        public RootModel LoadOrNew()
        {
            if (File.Exists(_filePath))
                return JsonConvert.DeserializeObject<RootModel>(File.ReadAllText(_filePath), _settings);

            return new RootModel();
        }

        public RootModel LoadOrNew1()
        {
            if (File.Exists(_filePath))
            {
                var s = File.ReadAllText(_filePath);
                s = Base64Decode(s);
                return JsonConvert.DeserializeObject<RootModel>(s, _settings);
            }
            return new RootModel();
        }

        public void Save(RootModel model)
        {
            var s = JsonConvert.SerializeObject(model, _settings);
            //s = Base64Encode(s);
            File.WriteAllText(_filePath, s);
        }

        private static string Base64Encode(string plainText)
        {
            var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
            return System.Convert.ToBase64String(plainTextBytes);
        }

        private static string Base64Decode(string base64EncodedData)
        {
            var base64EncodedBytes = System.Convert.FromBase64String(base64EncodedData);
            return System.Text.Encoding.UTF8.GetString(base64EncodedBytes);
        }
    }
}
