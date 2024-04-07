using System;
using System.Collections.Generic;
using System.Text.Json;
using System.IO;
using UnityEngine;

using TownOfHost.Attributes;
using TownOfHost.Modules;

namespace TownOfHost
{

    //ﾅﾏｴﾃｷﾄｳﾀﾞｹﾄﾞﾕｽﾘﾃ((
    public static class CustomSpawnSaveandLoadManager
    {
        private static readonly string FILE_PATH = "./TOHK_DATA/CustomSpawns.txt";

        private readonly static LogHandler logger = Logger.Handler("CSSaLM");

        [PluginModuleInitializer]
        public static void Init()
        {
            CreateIfNotExists();
            Load();
        }

        public static void CreateIfNotExists()
        {
            if (!File.Exists(FILE_PATH))
            {
                try
                {
                    if (!Directory.Exists(@"TOHK_DATA")) Directory.CreateDirectory(@"TOHK_DATA");
                    if (File.Exists(@"./CustomSpawns.txt"))
                    {
                        File.Move(@"./CustomSpawns.txt", FILE_PATH);
                    }
                    else
                    {
                        logger.Info("Among Us.exeと同じフォルダにCustomSpawns.txtが見つかりませんでした。新規作成します。");
                        File.WriteAllText(FILE_PATH, "{}");
                    }
                }
                catch (Exception ex)
                {
                    logger.Exception(ex);
                }
            }
        }

        //ゴミコードでごめんな！自分こんなコードしか書けないんだわ！((
        public static void Save()
        {
            CreateIfNotExists();

            string hozon = "{";

            foreach (var Data in Main.CustomSpawnPosition)
            {
                hozon += $"\"{Data.Key}\" :[";
                foreach (var vec2 in Data.Value)
                {
                    hozon += $"{{\"x\":{vec2.x},\"y\":{vec2.y}}},";
                }
                hozon = hozon.Remove(hozon.Length - 1);
                hozon += "],";
            }
            hozon = hozon.Remove(hozon.Length - 1);
            hozon += "}";

            File.WriteAllText(FILE_PATH, hozon);
        }

        //ChatGPTさんが手伝ってくれました 感謝です
        public static void Load()
        {
            CreateIfNotExists();
            string data = File.ReadAllText(FILE_PATH);

            Dictionary<int, List<Vector2>> customSpawnPosition = new();

            try
            {
                using JsonDocument document = JsonDocument.Parse(data);
                JsonElement root = document.RootElement;
                foreach (JsonProperty property in root.EnumerateObject())
                {
                    int mapId = int.Parse(property.Name);
                    JsonElement positionArray = property.Value;
                    List<Vector2> positions = new();
                    foreach (JsonElement positionElement in positionArray.EnumerateArray())
                    {
                        float x = positionElement.GetProperty("x").GetSingle();
                        float y = positionElement.GetProperty("y").GetSingle();
                        Vector2 position = new(x, y);
                        positions.Add(position);
                    }
                    customSpawnPosition.Add(mapId, positions);
                }
            }
            catch (JsonException ex)
            {
                logger.Error($"{ex.Message}");
            }

            Main.CustomSpawnPosition = new(customSpawnPosition);
        }
    }
}