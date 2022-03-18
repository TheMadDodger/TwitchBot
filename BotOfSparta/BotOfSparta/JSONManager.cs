using Newtonsoft.Json;
using System;
using System.IO;

namespace BotOfSparta
{
    public static class JSONManager
    {
        #region File Handling

        public static bool Convert<T>(string path, out T data) where T : class
        {
            data = null;

            try
            {
                string text = File.ReadAllText(path);
                data = (T)JsonConvert.DeserializeObject(text, typeof(T));
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return false;
            }

            return true;
        }

        public static void Write(string path, object data)
        {
            try
            {
                string text = JsonConvert.SerializeObject(data, Formatting.Indented);
                string[] lines = text.Split('\n');
                File.WriteAllLines(path, lines);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        #endregion
    }
}
