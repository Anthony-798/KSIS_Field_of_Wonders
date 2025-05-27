using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Поле_Чудес {
    internal class QuestionControl {
        public static List<Question> QuestionsList { get; set; } = new List<Question>();  // список вопросов

        private static readonly string path = Path.Combine(Application.StartupPath, "questions.json");  // путь к файлу

        private static readonly Random random = new Random(); // Один статический Random

        public static void LoadQuestions() {  // Загрузка вопросов в список из файла
            // если файл не существует, то он создается и в него записывается базовый начальный список вопросов
            if (!File.Exists(path)) {
                QuestionsList.AddRange(new List<Question> {  // добавляем вопросы в список
                    new Question { Topic = "Писатели", Word = "Лермонтов" },
                    new Question { Topic = "Профессии", Word = "Программист" },
                    new Question { Topic = "Животные", Word = "Медведь" },
                    new Question { Topic = "Города", Word = "Москва" },
                    new Question { Topic = "Фрукты", Word = "Яблоко" },
                    new Question { Topic = "Фильмы", Word = "Аватар" },
                    new Question { Topic = "География", Word = "Экватор" },
                    new Question { Topic = "Напитки", Word = "Газировка" }
                }); 
                SaveQuestions();  // создаем файл, куда записываем вопросы
            }
            // если файл существует, то данные считываются из файла и помещаются в список
            if (File.Exists(path)) {
                string jsonText = File.ReadAllText(path);
                QuestionsList = JsonConvert.DeserializeObject<List<Question>>(jsonText);
            }
        }

        public static void SaveQuestions() {  // Сохранение вопросов из списка в файл (если файл не существует, он создается)
            string jsonText = JsonConvert.SerializeObject(QuestionsList, Formatting.Indented);
            File.WriteAllText(path, jsonText);
        }

        public static Question GetRandomQuestion() {  // Возвращает случайный вопрос                                                      
            return QuestionsList[random.Next(QuestionsList.Count)];
        }
    }
}
