using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using static Поле_Чудес.NetworkManager;

namespace Поле_Чудес {
    public partial class GameForm : Form {

        private string currentSector = "300";  // Изначально 300

        // Индекс текущего игрока в списке игроков, отсортированном по JoinTime
        private int currentPlayerIndex = 0;

        private bool isSectorPlus = false;  // Флаг для сектора ПЛЮС

        public GameForm() {
            InitializeComponent();

            panelCloudYakub.DoubleBuffered(true); // Убирает мерцание с панели
            lblMessageYakub.DoubleBuffered(true); // Убирает мерцание с текста
            panelLetters.DoubleBuffered(true); // Убирает мерцание с панели

            // Добавляем обработчик Click для всех БУКВ (Label) на panelLetters
            foreach (Control control in panelLetters.Controls) {
                if (control is Panel panel) {
                    foreach (Control innerControl in panel.Controls) {
                        if (innerControl is Label label)  label.Click += LetterLabel_Click;                        
                    }
                }
            }

            NetworkManager.PlayerJoined += UpdateNewPlayers;  // (!!) Подписка на событие присоединения нового игрока к комнате
            NetworkManager.SpinBarabanReceived += SpinBaraban; // (!!) Подписка на событие получения сектора
            NetworkManager.SelectedLetterReceived += ProcessLetter; // (!!) Подписка на событие получения буквы
        } 

        public void JoinRoom() {

            // Получаем текущую комнату
            var room = NetworkManager.GetCurrentRoom();
            if (room == null) return;

            // Устанавливаем название комнаты
            lblRoomName.Text = $"Комната - \"{room.Name}\"";
            CentreLabelX(lblRoomName, panelTop, -74);
            lblRoomName.Show();

            // Устанавливаем количество игроков
            lblPlayerCount.Text = $"(Количество игроков - {room.PlayerCount}/3)";
            CentreLabelX(lblPlayerCount, panelTop, -74);
            lblPlayerCount.Show();

            // Слова Якубовича
            lblMessageYakub.Text = "Ожидание трех игроков...";
            CentreLabelXY(lblMessageYakub, panelCloudYakub);
            lblMessageYakub.Show();

            // Устанавливаем данные об игроках по умолчанию
            lblName1.Text = "Ожидание...";
            lblName1.Location = new Point(78, 29);
            lblAddress1.Hide();

            lblName2.Text = "Ожидание...";
            lblName2.Location = new Point(78, 29);
            lblAddress2.Hide();

            lblName3.Text = "Ожидание...";
            lblName3.Location = new Point(78, 29);
            lblAddress3.Hide();

            // Скрываем ненужные элементы
            lblTopic.Hide();
            panelWord.Hide();
            panelScore1.Hide();
            panelScore2.Hide();
            panelScore3.Hide();
            panelCloud1.Hide();
            panelCloud2.Hide();
            panelCloud3.Hide();
            btnSayWord.Hide();
            btnSpinBaraban.Hide();
            panelLetters.Hide();
            panelLetters.Location = new Point(137, 547);

            // Получаем список игроков, отсортированных по времени присоединения к комнате
            var sortedPlayers = room.Players.OrderBy(p => p.JoinTime).ToList();

            // Устанавливаем данные об игроках
            for (int i = 0; i < sortedPlayers.Count; i++) {
                var player = sortedPlayers[i];  // игрок из текущей итерации
                // Проверяем, является ли игрок текущим пользователем
                bool isLocalPlayer = $"{player.Ip}:{player.Port}" == NetworkManager.LocalAddress;

                // 1-й игрок
                if (i == 0) {
                    lblName1.Location = new Point(96, 11);
                    lblName1.Text = player.Name;
                    lblName1.ForeColor = isLocalPlayer ? Color.PaleGoldenrod : Color.LightGray;
                    lblAddress1.Text = $"({player.Ip}:{player.Port}) 👑";
                    lblAddress1.Show();
                    // Центрируем по X
                    CentreLabelX(lblName1, panelPlayer1, 0);
                    CentreLabelX(lblAddress1, panelPlayer1, 4);
                }
                // 2-й игрок
                else if (i == 1) {
                    lblName2.Location = new Point(96, 11);
                    lblName2.Text = player.Name;
                    lblName2.ForeColor = isLocalPlayer ? Color.PaleGoldenrod : Color.LightGray;
                    lblAddress2.Text = $"({player.Ip}:{player.Port})";
                    lblAddress2.Show();
                    // Центрируем по X
                    CentreLabelX(lblName2, panelPlayer2, 0);
                    CentreLabelX(lblAddress2, panelPlayer2, 4);
                }
                // 3-й игрок
                else if (i == 2) {
                    lblName3.Location = new Point(96, 11);
                    lblName3.Text = player.Name;
                    lblName3.ForeColor = isLocalPlayer ? Color.PaleGoldenrod : Color.LightGray;
                    lblAddress3.Text = $"({player.Ip}:{player.Port})";
                    lblAddress3.Show();
                    // Центрируем по X
                    CentreLabelX(lblName3, panelPlayer3, 0);
                    CentreLabelX(lblAddress3, panelPlayer3, 4);
                }
            }

            // Обратный отсчет до начала игры у Якубовича
            if (room.PlayerCount == 2) {
                Countdown();
            }

            // БАРАБАН
            btnSpinBaraban.Show();

        }

        // Обновляет UI при добавлении нового игрока
        private void UpdateNewPlayers(PlayerInfo newPlayer) {

            if (InvokeRequired) {
                Invoke(new Action<PlayerInfo>(UpdateNewPlayers), newPlayer);
                return;
            }

            // Получаем текущую комнату
            var room = NetworkManager.GetCurrentRoom();
            if (room == null) return;

            // Меняем количество игроков
            lblPlayerCount.Text = $"(Количество игроков - {room.PlayerCount}/3)";
            CentreLabelX(lblPlayerCount, panelTop, -74);

            // Получаем список игроков, отсортированных по времени присоединения к комнате
            var sortedPlayers = room.Players.OrderBy(p => p.JoinTime).ToList();
            int index = sortedPlayers.IndexOf(newPlayer);  // Получаем индекс нового игрока в данном списке

            // 2-й игрок
            if (index == 1) {
                lblName2.Location = new Point(96, 11);
                lblName2.Text = newPlayer.Name;
                lblName2.ForeColor = Color.LightGray;
                lblAddress2.Text = $"({newPlayer.Ip}:{newPlayer.Port})";
                lblAddress2.Show();
                // Центрируем по X
                CentreLabelX(lblName2, panelPlayer2, 0);
                CentreLabelX(lblAddress2, panelPlayer2, 4);
            }
            // 3-й игрок
            else if (index == 2) {
                lblName3.Location = new Point(96, 11);
                lblName3.Text = newPlayer.Name;
                lblName3.ForeColor = Color.LightGray;
                lblAddress3.Text = $"({newPlayer.Ip}:{newPlayer.Port})";
                lblAddress3.Show();
                // Центрируем по X
                CentreLabelX(lblName3, panelPlayer3, 0);
                CentreLabelX(lblAddress3, panelPlayer3, 4);
            }

            // Обратный отсчет до начала игры у Якубовича
            if (room.PlayerCount == 2) {
                Countdown();
            }
        }

        private async void Countdown() {
            await Task.Delay(3000);  // Задержка 3 секунды

            //panelCloudYakub.SuspendLayout(); // Приостановить обновления (Устраняет мерцание)
            panelCloudYakub.Hide();
            lblMessageYakub.Text = "До начала игры осталось - 5";
            CentreLabelXY(lblMessageYakub, panelCloudYakub);
            panelCloudYakub.Show();
            await Task.Delay(1000); // Задержка 1 секунда

            panelCloudYakub.Hide();
            lblMessageYakub.Text = "До начала игры осталось - 4";
            panelCloudYakub.Show();
            await Task.Delay(1000); // Задержка 1 секунда

            panelCloudYakub.Hide();
            lblMessageYakub.Text = "До начала игры осталось - 3";
            panelCloudYakub.Show();
            await Task.Delay(1000); // Задержка 1 секунда

            panelCloudYakub.Hide();
            lblMessageYakub.Text = "До начала игры осталось - 2";
            panelCloudYakub.Show();
            await Task.Delay(1000); // Задержка 1 секунда

            panelCloudYakub.Hide();
            lblMessageYakub.Text = "До начала игры осталось - 1";
            panelCloudYakub.Show();
            await Task.Delay(1000);  // Задержка 1 секунда

            panelCloudYakub.Hide();
            lblMessageYakub.Text = "Добро пожаловать в игру - \nКапитал-шоу \"Поле Чудес\"";
            CentreLabelXY(lblMessageYakub, panelCloudYakub);
            panelCloudYakub.Show();
            //panelCloudYakub.ResumeLayout(); // Возобновить обновления

            // Обнуляем очки игроков и показываем их 
            lblScore1.Text = "0";
            lblScore2.Text = "0";
            lblScore3.Text = "0";
            panelScore1.Width = lblScore1.Width + 43;
            panelScore2.Width = lblScore2.Width + 43;
            panelScore3.Width = lblScore3.Width + 43;
            CentrePanelX(panelScore1, panelPlayer1,0);
            CentrePanelX(panelScore2, panelPlayer2,0);
            CentrePanelX(panelScore3, panelPlayer3, 0);
            panelScore1.Show();
            panelScore2.Show();
            panelScore3.Show();

            // Получаем информацию о текущей комнате
            var room = NetworkManager.GetCurrentRoom();
            if (room != null && room.Question != null) {
                // Речь Якубовича
                await Task.Delay(2500);  // Задержка 2.5 секунды
              
                // Тема и загаданное слово
                panelCloudYakub.Hide();
                lblMessageYakub.Text = $"          Вот задание на этот тур - \nЗагадано слово из {room.Question.Word.Length} " +
                    $"букв на тему - \n";
                CentreLabelXY(lblMessageYakub, panelCloudYakub);

                lblTopicYakub.Text = $"\"{room.Question.Topic}\"";
                CentreLabelX(lblTopicYakub, panelCloudYakub, -6);
                lblTopicYakub.Show();

                panelCloudYakub.Show();

                // Tема на табло
                lblTopic.Text =  $"Тема игры - {room.Question.Topic}";
                CentreLabelX(lblTopic, panelTop, -74);

                // Слово на табло
                panelWord.Controls.Clear();
                int letterCount = room.Question.Word.Length;
                // Вычисляем ширину panelWord: 2 + (n * 54) + ((n-1) * 6) + 2
                int panelWordWidth = 4 + (letterCount * 54) + ((letterCount - 1) * 6);
                panelWord.Size = new Size(panelWordWidth, panelWord.Height); 

                // Создаём панели для букв
                int x = 2; // Начальный отступ слева
                foreach (char letter in room.Question.Word) {
                    Panel letterPanel = GetLetterPanel(x, letter);
                    panelWord.Controls.Add(letterPanel);
                    x += 60; // Ширина панели (54) + отступ (6)
                }
                CentrePanelX(panelWord, panelTop, -74);

                lblRoomName.Hide();
                lblPlayerCount.Hide();
                panelWord.Show();
                lblTopic.Show();

                // Показываем текущий ход
                await Task.Delay(2500);  // Задержка 2,5 секунды
                lblTopicYakub.Hide();
                DisplayCurrentPlayerTurn();
            }
        }

        // Метод для создания панели буквы
        private Panel GetLetterPanel(int x, char letter) {
            Panel panel = new Panel {
                BackColor = Color.Navy,   // AliceBlue
                BorderStyle = BorderStyle.FixedSingle,
                Location = new Point(x, 6), 
                Size = new Size(54, 54)
            };

            Label label = new Label {
                AutoSize = true, 
                BackColor = Color.Transparent,
                Font = new Font("Microsoft YaHei UI", 22.2F, FontStyle.Bold, GraphicsUnit.Point, 204),
                ForeColor = Color.Navy,
                Text = char.ToUpper(letter).ToString()
            };
            panel.Controls.Add(label);
            CentreLabelXY(label, panel);

            // Добавляем обработчик Click для сектора ПЛЮС
            label.Click += (s, e) => {            
                // Проверяем, что это локальный игрок и активен сектор ПЛЮС + буква еще НЕ ОТКРЫТА
                var currentPlayer = GetCurrentPlayer();
                bool isLocalPlayer = currentPlayer != null && $"{currentPlayer.Ip}:{currentPlayer.Port}" == NetworkManager.LocalAddress;

                if (isLocalPlayer && isSectorPlus && panel.BackColor != Color.AliceBlue) {
                    // Находим индекс ячейки в panelWord
                    int letterIndex = 0;
                    foreach (Control control in panelWord.Controls) {
                        if (control == panel) { break; }
                        letterIndex++;
                    }

                    // Формируем сообщение для сети (позиция:буква)
                    string message = $"{letterIndex + 1}:{label.Text}";
                    NetworkManager.SendSelectedLetter(message);

                    // Обрабатываем локально
                    ProcessLetter(message);

                    // Сбрасываем флаг ПЛЮС
                    isSectorPlus = false;
                }
            };

            return panel;
        }


        // Показывает, чей сейчас ход, и управляет видимостью кнопок
        private void DisplayCurrentPlayerTurn() {
            // Получаем текущую комнату
            var room = NetworkManager.GetCurrentRoom();
            if (room == null || room.Players == null || room.Players.Count == 0) return;

            // Получаем список игроков, отсортированный по времени присоединения
            var sortedPlayers = room.Players.OrderBy(p => p.JoinTime).ToList();

            // Если текущий игрок null (вышел), переключаем ход
            if (sortedPlayers[currentPlayerIndex] == null) SwitchPlayerTurn();
            
            // Берём текущего игрока
            var currentPlayer = sortedPlayers[currentPlayerIndex];
            // Проверяем, или это мы сами
            bool isLocalPlayer = $"{currentPlayer.Ip}:{currentPlayer.Port}" == NetworkManager.LocalAddress;

            // Показываем сообщение с именем игрока
            panelCloudYakub.Hide();
            lblMessageYakub.Text = $"{currentPlayer.Name}, вращайте барабан!";
            CentreLabelXY(lblMessageYakub, panelCloudYakub);
            panelCloudYakub.Show();

            // Показываем игровые кнопки только для локального игрока
            btnSpinBaraban.Visible = isLocalPlayer;
            btnSayWord.Visible = isLocalPlayer;
        }

        // Переключает ход на следующего игрока (Меняет currentPlayerIndex)
        private void SwitchPlayerTurn() {
            // Получаем текущую комнату
            var room = NetworkManager.GetCurrentRoom();
            if (room == null || room.Players == null || room.Players.Count == 0) return;

            // Получаем актуальный список игроков, отсортированный по времени присоединения
            var sortedPlayers = room.Players.OrderBy(p => p.JoinTime).ToList();

            // Сохраняем текущего игрока(может быть null)
            var oldPlayer = sortedPlayers[currentPlayerIndex];

            // Ищем следующего не-null игрока
            do {
                currentPlayerIndex = (currentPlayerIndex + 1) % sortedPlayers.Count;  // меняем индекс на следующего игрока
            } while (sortedPlayers[currentPlayerIndex] == null);   // если игрок null, то снова меняем индекс

            // Получаем нового игрока
            var newPlayer = sortedPlayers[currentPlayerIndex];

            // Если игрок сменился, показываем сообщение "Переход хода!"
            if (oldPlayer == null || $"{oldPlayer.Ip}:{oldPlayer.Port}" != $"{newPlayer.Ip}:{newPlayer.Port}" ) {
                panelCloudYakub.Hide();
                lblMessageYakub.Text = "Переход хода!";
                CentreLabelXY(lblMessageYakub, panelCloudYakub);
                panelCloudYakub.Show();
            }
        }

        // Получает текущего ПО ХОДУ игрока
        private PlayerInfo GetCurrentPlayer() {
            // Получаем текущую комнату
            var room = NetworkManager.GetCurrentRoom();
            if (room == null || room.Players == null || room.Players.Count == 0)
                return null;

            // Получаем актуальный список игроков, отсортированный по времени присоединения
            var sortedPlayers = room.Players.OrderBy(p => p.JoinTime).ToList();
            if (currentPlayerIndex < 0 || currentPlayerIndex >= sortedPlayers.Count || sortedPlayers[currentPlayerIndex] == null)
                return null;

            return sortedPlayers[currentPlayerIndex];
        }

        // Метод для вращения барабана
        private void btnSpinBaraban_Click(object sender, EventArgs e) {

            // Список секторов и последовательность картинок
            var sectors = new string[] {
                "300", "200", "x3", "700", "Б", "1000", "100", "x2",
                "600", "800", "ПЛЮС", "400", "900", "0", "ПРИЗ", "500"
            };

            // Выбираем случайный сектор
            Random random = new Random();
            string targetSector = sectors[random.Next(sectors.Length)];

            // (!!) Отправляем сектор другим игрокам
            NetworkManager.SendSpinResult(targetSector);

            SpinBaraban(targetSector);  // Вызываем вращение барабана у себя
        }

        // Анимация вращения барабана до целевого сектора
        private void SpinBaraban(string targetSector) {

            if (InvokeRequired) {
                Invoke(new Action<string>(SpinBaraban), targetSector);
                return;
            }

            // Список секторов и последовательность картинок
            var sectors = new string[] {
                "300", "200", "x3", "700", "Б", "1000", "100", "x2",
                "600", "800", "ПЛЮС", "400", "900", "0", "ПРИЗ", "500"
            };

            // Загружаем изображения из ресурсов
            var images = new Dictionary<string, Image> {
                { "0", Properties.Resources.baraban_0 },
                { "100", Properties.Resources.baraban_100 },
                { "200", Properties.Resources.baraban_200 },
                { "300", Properties.Resources.baraban_300 },
                { "400", Properties.Resources.baraban_400 },
                { "500", Properties.Resources.baraban_500 },
                { "600", Properties.Resources.baraban_600 },
                { "700", Properties.Resources.baraban_700 },
                { "800", Properties.Resources.baraban_800 },
                { "900", Properties.Resources.baraban_900 },
                { "1000", Properties.Resources.baraban_1000 },
                { "ПЛЮС", Properties.Resources.baraban_ПЛЮС },
                { "x2", Properties.Resources.baraban_x2 },
                { "x3", Properties.Resources.baraban_x3 },
                { "ПРИЗ", Properties.Resources.baraban_ПРИЗ },
                { "Б", Properties.Resources.baraban_Б }
            };

            // Проверяем, загружены ли картинки
            if (!images.Values.Any(img => img != null)) {
                panelCloudYakub.Hide();
                lblMessageYakub.Text = "Ошибка: картинки не загружены";
                CentreLabelXY(lblMessageYakub, panelCloudYakub);
                panelCloudYakub.Show();
                return;
            }

            // Текущий сектор берём из поля
            if (!sectors.Contains(currentSector)) {
                currentSector = "300"; // На случай ошибки
                pictureBoxBaraban.Image = images[currentSector];
            }

            // Находим индексы текущего и целевого секторов
            int currentIndex = Array.IndexOf(sectors, currentSector);
            int targetIndex = Array.IndexOf(sectors, targetSector);

            // Рассчитываем количество смен картинок
            int fullRotations = 2; // 2 полных вращения
            int imagesPerRotation = sectors.Length; // 16 картинок
            int extraImages = (targetIndex - currentIndex + imagesPerRotation) % imagesPerRotation;
            if (extraImages == 0 && targetSector != currentSector)
                extraImages = imagesPerRotation; // Полный круг, если сектор тот же
            int totalImages = fullRotations * imagesPerRotation + extraImages;

            // Показываем начало вращения
            panelCloudYakub.Hide();
            lblMessageYakub.Text = "Вращаем барабан...";
            CentreLabelXY(lblMessageYakub, panelCloudYakub);
            panelCloudYakub.Show();

            // Анимация
            int imageCount = 0;
            float speed = 1f;
            int baseInterval = 50; // 50 мс между сменами
            System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer { Interval = baseInterval };
            timer.Tick += (s, args) => {
                // Замедление в последние 20% анимации
                if (imageCount > totalImages * 0.8) {
                    speed *= 0.95f;
                    timer.Interval = (int)(baseInterval / speed);
                }

                // Меняем картинку
                currentIndex = (currentIndex + 1) % imagesPerRotation;
                pictureBoxBaraban.Image = images[sectors[currentIndex]];
                pictureBoxBaraban.Refresh();
                imageCount++;

                // Остановка
                if (imageCount >= totalImages) {
                    timer.Stop();
                    pictureBoxBaraban.Image = images[targetSector];
                    pictureBoxBaraban.Refresh();

                    currentSector = targetSector; // Обновляем текущий сектор

                    // Обрабатываем сектор
                    HandleSector(targetSector);
                }
            };

            // Запускаем
            btnSpinBaraban.Hide();
            btnSayWord.Hide();
            timer.Start();
        }

        private async void HandleSector(string targetSector) {
            // Получаем текущего игрока
            var currentPlayer = GetCurrentPlayer();
            string playerName = currentPlayer?.Name ?? "Игрок";
            bool isLocalPlayer = currentPlayer != null && $"{currentPlayer.Ip}:{currentPlayer.Port}" == NetworkManager.LocalAddress;

            // Обработка секторов
            panelCloudYakub.Hide();
            string message;

            if (new[] { "100", "200", "300", "400", "500", "600", "700", "800", "900", "1000" }.Contains(targetSector)) {
                message = $"{targetSector} очков на барабане! \n{playerName}, назовите букву!";
                // Показываем результат вращения
                lblMessageYakub.Text = message;
                CentreLabelXY(lblMessageYakub, panelCloudYakub);
                panelCloudYakub.Show();

                // Показываем панель букв только, если это наш ход (мы сами)
                await Task.Delay(1000);  // Задержка 1 секунда
                if (isLocalPlayer) {
                    panelLetters.BringToFront();
                    panelLetters.Show();
                }    
            }
            else if (targetSector == "x2" || targetSector == "x3") {
                message = $"Ваши очки умножаются на {targetSector}! \n{playerName}, назовите букву!";
                // Показываем результат вращения
                lblMessageYakub.Text = message;
                CentreLabelXY(lblMessageYakub, panelCloudYakub);
                panelCloudYakub.Show();

                // Показываем панель букв только, если это наш ход (мы сами)
                await Task.Delay(1000);  // Задержка 1 секунда
                if (isLocalPlayer) {
                    panelLetters.BringToFront();
                    panelLetters.Show();
                }
            }
            else if (targetSector == "0") {
                message = $"К сожалению, 0 очков на барабане!";
                // Показываем результат вращения
                lblMessageYakub.Text = message;
                CentreLabelXY(lblMessageYakub, panelCloudYakub);
                panelCloudYakub.Show();
                
                // Смена игрока
                await Task.Delay(1450);  // Задержка 1 секунда
                SwitchPlayerTurn();
                await Task.Delay(1450);  // Задержка 1 секунда
                DisplayCurrentPlayerTurn();
            }
            else if (targetSector == "Б") {
                message = $"К сожалению, вы банкрот! \nВсе ваши очки сгорели!";
                // Показываем результат вращения
                lblMessageYakub.Text = message;
                CentreLabelXY(lblMessageYakub, panelCloudYakub);
                panelCloudYakub.Show();

                // Обнуляем счёт игрока
                Label scoreLabel = currentPlayerIndex == 0 ? lblScore1 : currentPlayerIndex == 1 ? lblScore2 : lblScore3;
                scoreLabel.Text = "0";
                Panel scorePanel = currentPlayerIndex == 0 ? panelScore1 : currentPlayerIndex == 1 ? panelScore2 : panelScore3;
                scorePanel.Width = scoreLabel.Width + 43;
                CentrePanelX(scorePanel, currentPlayerIndex == 0 ? panelPlayer1 : currentPlayerIndex == 1 ? panelPlayer2 : panelPlayer3, 0);

                // Смена игрока
                await Task.Delay(1450);  // Задержка 1 секунда
                SwitchPlayerTurn();
                await Task.Delay(1450);  // Задержка 1 секунда
                DisplayCurrentPlayerTurn();
            }
            else if (targetSector == "ПРИЗ") {
                message = $"Сектор ПРИЗ на барабане! \nПРИЗ или играем?";
                // Показываем результат вращения
                lblMessageYakub.Text = message;
                CentreLabelXY(lblMessageYakub, panelCloudYakub);
                panelCloudYakub.Show();
                ////////////////////////////////
                btnSpinBaraban.Show();
            }
            else {  // ПЛЮС
                message = $"Сектор ПЛЮС на барабане! \n{playerName}, откройте любую букву!";
                // Показываем результат вращения
                lblMessageYakub.Text = message;
                CentreLabelXY(lblMessageYakub, panelCloudYakub);
                panelCloudYakub.Show();

                // Устанавливаем флаг для локального игрока (Позволяем выбрать буквы только если это мы сами)
                if (isLocalPlayer) isSectorPlus = true;     
            }
        }

        // Обработчик клика по букве
        private void LetterLabel_Click(object sender, EventArgs e) {
            // Выполняем только для НЕназванных букв
            if (sender is Label label && label.ForeColor != Color.DarkGray) {

                // Скрываем панель букв
                panelLetters.Hide();

                // Отправляем букву другим игрокам
                NetworkManager.SendSelectedLetter(label.Text);

                // Обрабатываем букву локально
                ProcessLetter(label.Text);
            }
        }

        // Общий метод обработки буквы
        private async void ProcessLetter(string selectedLetter) {

            if (InvokeRequired) {
                Invoke(new Action<string>(ProcessLetter), selectedLetter);
                return;
            }

            // Парсим входную строку
            int position = 0;  // номер открытой буквы через сектор ПЛЮС
            string letter = selectedLetter;
            bool isPlus = selectedLetter.Contains(":");
            if (isPlus) {
                var parts = selectedLetter.Split(':');
                if (parts.Length == 2 && int.TryParse(parts[0], out position) && !string.IsNullOrEmpty(parts[1])) 
                    letter = parts[1].ToUpper().Trim();  // буква
                else 
                    return; // Неверный формат 
            }

            // Ищем Label на panelLetters с соответствующей буквой
            Label targetLabel = null;
            foreach (Control control in panelLetters.Controls) {
                if (control is Panel panel) {
                    foreach (Control innerControl in panel.Controls) {
                        if (innerControl is Label label && label.Text.ToUpper() == letter.ToUpper()) {
                            targetLabel = label;
                            break;
                        }
                    }
                    if (targetLabel != null) break;
                }
            }

            // Если Label найден, отмечаем БУКВУ как названную
            if (targetLabel != null && targetLabel.ForeColor != Color.DarkGray) 
                targetLabel.ForeColor = Color.DarkGray;
            
            // Получаем текущую комнату
            var room = NetworkManager.GetCurrentRoom();
            if (room == null || room.Question == null) return;

            // Проверяем букву
            letter = letter.ToUpper().Trim();
            string word = room.Question.Word.ToUpper().Trim();
            int letterCount = word.Count(c => c.ToString() == letter);  // Число вхождений названной буквы

            // Выбираем панель и речь игрока по currentPlayerIndex
            Panel playerCloud = currentPlayerIndex == 0 ? panelCloud1 : currentPlayerIndex == 1 ? panelCloud2 : panelCloud3;
            Label playerMessage = currentPlayerIndex == 0 ? lblMessage1 : currentPlayerIndex == 1 ? lblMessage2 : lblMessage3;

            // Показываем сообщение игрока
            playerCloud.Hide();
            playerMessage.Text = isPlus ? $"{position}-я буква!" : $"Буква {letter}!";
            CentreLabelXY(playerMessage, playerCloud);
            playerCloud.Show();
            await Task.Delay(1200);

            // Буква верна (открываем буквы в panelWord, начисляем очки, сохраняем ход)
            if (letterCount > 0) {
                // Показываем сообщение
                panelCloudYakub.Hide();
                lblMessageYakub.Text = isPlus ? $"Откройте {position}-ю букву!" : $"Верно! Откройте букву '{letter}'!";
                CentreLabelXY(lblMessageYakub, panelCloudYakub);
                panelCloudYakub.Show();

                await Task.Delay(1200);

                // Открываем все вхождения буквы в panelWord
                int index = 0;
                foreach (Control control in panelWord.Controls) {
                    if (control is Panel letterPanel && letterPanel.Controls[0] is Label letterLabel) {
                        if (word[index].ToString().ToUpper() == letter) {
                            letterPanel.BackColor = Color.AliceBlue;  // Отображаем букву
                        }
                        index++;
                    }
                }

                // Начисляем очки только если не ПЛЮС
                if (!isPlus) {
                    // Начисляем очки
                    int scoreChange = 0;
                    Label scoreLabel = currentPlayerIndex == 0 ? lblScore1 : currentPlayerIndex == 1 ? lblScore2 : lblScore3;

                    // Меняем надписи очков
                    if (int.TryParse(currentSector, out int points)) {
                        // Числовой сектор (100–1000)
                        scoreChange = points * letterCount;  // Кол-во очков умножить на кол-во верных букв
                        if (int.TryParse(scoreLabel.Text, out int currentScore)) {
                            scoreLabel.Text = (currentScore + scoreChange).ToString();  // Добавляем очки
                        }
                    }
                    else if (currentSector == "x2" || currentSector == "x3") {
                        // Множитель x2 или x3
                        int multiplier = currentSector == "x2" ? 2 : 3;
                        if (int.TryParse(scoreLabel.Text, out int currentScore)) {
                            scoreLabel.Text = (currentScore * (int)Math.Pow(multiplier, letterCount)).ToString();   // Добавляем очки
                        }
                    }

                    // Обновляем ширину панели очков
                    Panel scorePanel = currentPlayerIndex == 0 ? panelScore1 : currentPlayerIndex == 1 ? panelScore2 : panelScore3;
                    scorePanel.Width = scoreLabel.Width + 43;
                    CentrePanelX(scorePanel, currentPlayerIndex == 0 ? panelPlayer1 : currentPlayerIndex == 1 ? panelPlayer2 : panelPlayer3, 0);
                }
                // Отображаем текущий ход (Ход сохраняется)
                await Task.Delay(1500);
                playerCloud.Hide();
                DisplayCurrentPlayerTurn();
            }
            // Буква неверна (передаём ход следующему игроку)
            else {
                // Показываем сообщение
                panelCloudYakub.Hide();
                lblMessageYakub.Text = $"Буква '{letter}'? Нет такой буквы!";
                CentreLabelXY(lblMessageYakub, panelCloudYakub);
                panelCloudYakub.Show();

                // Передаём ход следующему игроку
                await Task.Delay(1500);
                playerCloud.Hide();
                SwitchPlayerTurn();
                await Task.Delay(1500);
                DisplayCurrentPlayerTurn();
            }
        }

        private void CentrePanelX(Panel child, Panel parent, int add) {
            // Центрирование по X
            int x = ((parent.Width - child.Width) / 2) + add;
            child.Location = new Point(x, child.Location.Y);
        }


        private void CentreLabelX(Label lbl, Panel panel, int add) {
            // Центрирование по X
            Size textSize = TextRenderer.MeasureText(lbl.Text, lbl.Font);
            int x = ((panel.Width - textSize.Width) / 2) + add;
            lbl.Location = new Point(x, lbl.Location.Y);
        }

        private void CentreLabelXY(Label lbl, Panel panel) {
            // Центрирование по X и У
            Size textSize = TextRenderer.MeasureText(lbl.Text, lbl.Font);
            int x = ((panel.Width - textSize.Width) / 2);
            int y = ((panel.Height - textSize.Height) / 2);
            lbl.Location = new Point(x, y);
        }

        


      


    }
}
