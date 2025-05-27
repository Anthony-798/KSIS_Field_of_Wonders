using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;

namespace Поле_Чудес {
    internal class PlayerControl {
        public static List<Player> PlayersList { get; set; } = new List<Player>();  // список пользователей

        private static readonly string path = Path.Combine(Application.StartupPath, "players.json");  // путь к файлу
        private static readonly object fileLock = new object();
        private static readonly Mutex mutex = new Mutex(false, "Поле_Чудес_PlayersJson");
       
        public static event Action PlayersListUpdated;  // Поле для события    
        private static bool isProcessingFileChange = false;  // Оптимизация FileSystemWatcher 

        private static readonly JsonSerializerSettings jsonSettings = new JsonSerializerSettings {
            Formatting = Newtonsoft.Json.Formatting.Indented, // Читаемый JSON
            DateFormatString = "yyyy-MM-dd HH:mm:ss" // Формат ГГГГ-ММ-ДД ЧЧ:ММ:СС
        };

        public static void InitializeFileWatcher() {
            FileSystemWatcher watcher = new FileSystemWatcher {
                Path = Path.GetDirectoryName(path),
                Filter = Path.GetFileName(path),
                NotifyFilter = NotifyFilters.LastWrite
            };
            watcher.Changed += (s, e) => {
                // Проверка для предотвращения множественных срабатываний
                if (!isProcessingFileChange) {
                    isProcessingFileChange = true;
                    try {                        
                        Thread.Sleep(100);  // Задержка для предотвращения гонки (влияет только на чтение из файла)
                        LoadPlayers();
                    }
                    finally {
                        isProcessingFileChange = false;
                    }
                }
            };
            watcher.EnableRaisingEvents = true;
        }

        public static void LoadPlayers() {  // Загрузка игроков в список из файла, если он существует
            lock (fileLock) {
                mutex.WaitOne();
                try {
                    if (File.Exists(path)) {
                        string jsonText = File.ReadAllText(path);
                        // Если результат null - присваиваем PlayersList новый пустой список (правый операнд)
                        PlayersList = JsonConvert.DeserializeObject<List<Player>>(jsonText, jsonSettings) ?? new List<Player>();
                        PlayersListUpdated?.Invoke(); // Уведомляем об обновлении
                    }
                }
                finally {
                    mutex.ReleaseMutex();
                }
            }
        }

        public static void SavePlayers() {  // Сохранение игроков из списка в файл (если файл не существует, он создается)
            lock (fileLock) {
                mutex.WaitOne();
                try {
                    string jsonText = JsonConvert.SerializeObject(PlayersList, jsonSettings);
                    File.WriteAllText(path, jsonText);
                }
                finally {
                    mutex.ReleaseMutex();
                }
            }
        }

        // Возвращает неактивного игрока с самым поздним LastPlayed, ЛИБО null - если список пуст/нет подходящих игроков 
        public static Player GetActivePlayer() {
            lock (fileLock) {
                return PlayersList.Where(p => !p.IsActive).OrderByDescending(p => p.LastPlayed).FirstOrDefault();
            }
        }

        public static void DeactivatePlayer(Player player) {
            lock (fileLock) {
                if (player != null) {
                    player.IsActive = false;
                    SavePlayers();
                }
            }
        }

        public static void ActivatePlayer(Player player) {
            lock (fileLock) {
                if (player != null) {
                    player.IsActive = true;  // Помечаем как активный
                    player.LastPlayed = DateTime.Now;  // Обновляем время
                    SavePlayers();
                }
            }
        }


    }
}
