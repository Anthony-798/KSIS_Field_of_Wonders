using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static Поле_Чудес.NetworkManager;

namespace Поле_Чудес {
    public partial class RoomForm : Form {

        private RoomInfo SelectedRoom;  // Подсвеченная комната в панели "Поиск комнат"

        private CustomButton btnCreate;

        private bool isSearchPanelActive = false; // Флаг активности панели "Поиск комнат"

        private GameForm gameForm;  // Дочерняя форма

        public RoomForm() {
            InitializeComponent();
            panelCreateRoom.DoubleBuffered(true); // Убирает мерцание с панели
            panelSearchRoom.DoubleBuffered(true); // Убирает мерцание с панели

            gameForm = new GameForm() { Dock = DockStyle.Fill, TopLevel = false };  // Дочерняя форма
            this.Controls.Add(gameForm);

            NetworkManager.RoomsUpdated += DisplayRooms;  // (!!) Подписка на событие обновления списка комнат

            // Добавление btnCreate программно
            btnCreate = new CustomButton {
                BackColor = Color.FromArgb(64, 64, 64),
                FlatAppearance = { BorderColor = Color.LightGray },
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Microsoft YaHei UI", 12F, FontStyle.Bold, GraphicsUnit.Point, 204),
                ForeColor = Color.LightGray,
                Location = new Point(86, 442),
                Size = new Size(174, 46),
                Text = "Создать",
                Enabled = false,
                UseVisualStyleBackColor = false
            };
            btnCreate.Click += btnCreate_Click;
            panelCreateRoom.Controls.Add(btnCreate);      
        }

        protected override CreateParams CreateParams {  // убирает мерцание 
            get {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= 0x02000000;  //  заставляет Windows использовать двойную буферизацию для перерисовки окна
                return cp;
            }
        }

        private void RoomForm_Load(object sender, EventArgs e) {
            // Устанавливаем текст адреса
            lblAddress.Text = $"Ваш адрес: {NetworkManager.LocalAddress}";

            // Центрирование по X
            Size textSize = TextRenderer.MeasureText(lblAddress.Text, lblAddress.Font);
            int x = (this.ClientSize.Width - textSize.Width) / 2;
            lblAddress.Location = new Point(x, lblAddress.Location.Y);
        }

        // Закрывает форму комнат
        private void btnBack_Click(object sender, EventArgs e) {
            this.ActiveControl = null; // Убираем фокус
            this.Hide();
        }

        private void btnCreateRoom_Click(object sender, EventArgs e) {
            btnBack.Enabled = false; // Блокируем кнопку Назад
            textBoxRoomName.Text = "";
            panelCreateRoom.Location = new Point(275, 161);
            btnCreate.Enabled = false; // Сбрасываем при открытии панели
            panelCreateRoom.BringToFront();
            panelCreateRoom.Show();
        }

        private void btnCancelCreation_Click(object sender, EventArgs e) {
            panelCreateRoom.Hide();
            btnBack.Enabled = true;  // Делаем доступной кнопку Назад
        }

        private void btnCreate_Click(object sender, EventArgs e) {
            string roomName = textBoxRoomName.Text.Trim();
            if (!string.IsNullOrWhiteSpace(roomName)) {
                NetworkManager.CreateRoom(roomName);  // (!!) Создаем комнату
                panelCreateRoom.Hide();
                btnBack.Enabled = true;  // Делаем доступной кнопку Назад

                // Добавляем начальную информацию на форму Игры (GameForm) и показываем ее
                gameForm.JoinRoom();
                gameForm.BringToFront(); // Поставить поверх всех элементов
                gameForm.Show();
            }
        }

        private void textBoxRoomName_TextChanged(object sender, EventArgs e) {
            // Если строка пустая или состоит из пробелов, то блокируем кнопку Создать
            if (string.IsNullOrWhiteSpace(textBoxRoomName.Text)) {
                btnCreate.Enabled = false;
                btnCreate.BackColor = Color.FromArgb(64, 64, 64);
            }
            else {
                btnCreate.Enabled = true;
                btnCreate.BackColor = Color.Indigo;
            }
        }

        private void btnConnect_Click(object sender, EventArgs e) {
            isSearchPanelActive = false; // Панель неактивна
            panelSearchRoom.Hide();
            btnBack.Enabled = true;  // Делаем доступной кнопку Назад

            // Если комната не null, то подключаемся к ней и показываем ее
            if (SelectedRoom != null) {
                try {
                    NetworkManager.JoinRoom(SelectedRoom);  // (!!) Подключаемся к комнате
                    gameForm.JoinRoom();  
                    gameForm.BringToFront();  // Поставить поверх всех элементов
                    gameForm.Show();
                }
                catch (Exception ex) {
                    MessageBox.Show($"Ошибка подключения: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void btnSearchRoom_Click(object sender, EventArgs e) {
            btnBack.Enabled = false; // Блокируем кнопку Назад
            panelSearchRoom.Location = new Point(252, 142);

            isSearchPanelActive = true; // Панель активна
            DisplayRooms();  // Показываем комнаты

            panelSearchRoom.BringToFront();
            panelSearchRoom.Show();
        }

        private void btnCancelSearch_Click(object sender, EventArgs e) {
            isSearchPanelActive = false; // Панель неактивна
            panelSearchRoom.Hide();
            btnBack.Enabled = true;  // Делаем доступной кнопку Назад
        }

        private void DisplayRooms() {
            
            if (InvokeRequired) {
                Invoke(new Action(DisplayRooms));
                return;
            }

            List<RoomInfo> rooms = NetworkManager.GetAvailableRooms();  // Получаем список доступных комнат

            // Обновляем SelectedRoom по имени (Устанавливаем подсвеченную комнату)
            // Если комната с таким именем найдена - обновляем ссылку // Если нет - берём первую комнату или null
            SelectedRoom = rooms.FirstOrDefault(r => r.Name == SelectedRoom?.Name) ?? rooms.FirstOrDefault();

            // Обновляем карточки комнат если активна панель поиска комнат
            if (isSearchPanelActive) {

                // Сохраняем текущую позицию прокрутки
                Point scrollPosition = flowPanelRooms.AutoScrollPosition;
                // Нормализуем координаты (AutoScrollPosition возвращает отрицательные значения)
                scrollPosition = new Point(-scrollPosition.X, -scrollPosition.Y);

                flowPanelRooms.Controls.Clear();  // Очистка старых карточек комнат

                // Проходимся по всем комнатам
                foreach (RoomInfo room in rooms) {
                    Panel card = GetRoomCard(room);  // создаем карточку комнаты
                    flowPanelRooms.Controls.Add(card);  // помещаем  карточку комнаты на экран
                }
                
                // Восстанавливаем прокрутку + пользователь останется внизу списка, даже если карточек стало меньше
                // Прокрутка установится на максимально возможную позицию (вниз), если scrollPosition.Y больше новой MaxScrollY
                flowPanelRooms.AutoScrollPosition = scrollPosition;
            }
            
        }

        private Panel GetRoomCard(RoomInfo room) {

            int panelHeight = 117 + (room.PlayerCount - 1) * 24;

            Panel panel = new Panel() {
                BorderStyle = BorderStyle.FixedSingle,
                Margin = new Padding(7, 4, 7, 5),
                Size = new Size(510, panelHeight),
                BackColor = room == SelectedRoom ? Color.MediumPurple : Color.Indigo  // Подсветка активной комнаты
            };

            Label lblRoomName = new Label() {
                AutoEllipsis = true,
                BackColor = Color.Transparent,
                Font = new Font("Microsoft YaHei UI", 12F, FontStyle.Bold, GraphicsUnit.Point, 204),
                ForeColor = Color.LightGray,
                Location = new Point(13, 7),  
                AutoSize = false,
                Size = new Size(492, 27),
                Text = room.Name
            };
            panel.Controls.Add(lblRoomName);

            Panel panelBorder = new Panel() {
                BackColor = Color.BlueViolet,
                Location = new Point(15, 39),
                Size = new Size(480, 2)
            };
            panel.Controls.Add(panelBorder);

            Label lblPlayerCount = new Label() {
                AutoSize = true,
                BackColor = Color.Transparent,
                Font = new Font("Microsoft YaHei UI", 10.8F, FontStyle.Bold, GraphicsUnit.Point, 204),
                ForeColor = Color.LightGray,
                Location = new Point(13, 50),
                Text = $"Игроки ({room.PlayerCount}/3):"
            };
            panel.Controls.Add(lblPlayerCount);

            // Сортируем игроков по JoinTime
            var sortedPlayers = room.Players.OrderBy(p => p.JoinTime).ToList();
            int[] yPositions = { 79, 104, 129 };

            for (int i = 0; i < room.PlayerCount; i++) {
                var player = sortedPlayers[i];

                string playerText = $"{i + 1}) {player.Name} ({player.Ip}:{player.Port})";
                if (i == 0) playerText += " 👑";  // Лидер — первый игрок

                Label lblPlayer = new Label() {
                    AutoEllipsis = true,
                    AutoSize = false,
                    Size = new Size(492, 24),
                    BackColor = Color.Transparent,
                    Font = new Font("Microsoft YaHei UI", 10.8F, FontStyle.Regular, GraphicsUnit.Point, 204),
                    ForeColor = Color.LightGray,
                    Location = new Point(13, yPositions[i]),
                    Text = playerText
                };
                panel.Controls.Add(lblPlayer);

                // Обработчик нажатия для выбора комнаты
                lblPlayer.MouseDown += (s, e) => HandleRoomSelection(panel, room);
            }

            // Меняем цвет у выбранной панели комнаты нажатием на саму панель или на ее надписи
            panel.MouseDown += (s, e) => HandleRoomSelection(panel, room);
            lblRoomName.MouseDown += (s, e) => HandleRoomSelection(panel, room);
            lblPlayerCount.MouseDown += (s, e) => HandleRoomSelection(panel, room);

            return panel;
        }

        private void HandleRoomSelection(Panel panel, RoomInfo room) {
            if (panel.BackColor == Color.Indigo) {
                foreach (Panel p in flowPanelRooms.Controls.OfType<Panel>()) {
                    if (p.BackColor == Color.MediumPurple) p.BackColor = Color.Indigo;
                    if (p == panel) p.BackColor = Color.MediumPurple;
                }
                // Обновляем подсвеченную комнату
                SelectedRoom = room;
            }
        }

        private void pictureBoxLogo_Click(object sender, EventArgs e) {

        }


        /*
       // Показываем наш адрес
       private void RoomForm_VisibleChanged(object sender, EventArgs e) {
           if (this.Visible == true) {
               // Устанавливаем текст адреса
               lblAddress.Text = $"Ваш адрес: {NetworkManager.GetLocalIp()}:{NetworkManager.TcpPort}";

               // Центрирование по X
               Size textSize = TextRenderer.MeasureText(lblAddress.Text, lblAddress.Font);
               int x = (this.ClientSize.Width - textSize.Width) / 2;
               lblAddress.Location = new Point(x, lblAddress.Location.Y);
           }
       }*/
    }

}
