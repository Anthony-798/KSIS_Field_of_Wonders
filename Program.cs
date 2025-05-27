using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Поле_Чудес {
    internal static class Program {
        /// <summary>
        /// Главная точка входа для приложения.
        /// </summary>
        [STAThread]
        static void Main() {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            PlayerControl.LoadPlayers(); // Загрузка игроков из файла в список
            QuestionControl.LoadQuestions();  // Загрузка вопросов из файла в список

            Application.Run(new MenuForm());
        }
    }
}
