using System;
using System.IO;
using Newtonsoft.Json;

namespace Server.Data
{
    [Serializable]
    public class ServerConfig
    {
        public string connectionString;
        public string dataPath;
    }

    public class ConfigManager
    {
        public static ServerConfig Config { get; private set; }

        public static void LoadConfig()
        {
            // 작업 디렉터리와 상관없이 실행 파일 옆의 config.json을 읽고, dataPath도 그 위치 기준으로 푼다.
            string baseDirectory = AppContext.BaseDirectory;
            string text = File.ReadAllText(Path.Combine(baseDirectory, "config.json"));
            Config = JsonConvert.DeserializeObject<ServerConfig>(text);
            Config.dataPath = Path.GetFullPath(Path.Combine(baseDirectory, Config.dataPath));
        }
    }
}