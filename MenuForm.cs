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

namespace Поле_Чудес {

    public partial class MenuForm : Form {

        public static Player ActivePlayer { get; private set; } // Текущий Активный игрок

        private Player SelectedPlayer; // Подсвеченный игрок в панели "Игроки"

        private RoomForm roomForm;  // Дочерняя форма

        private PrizesForm prizesForm;  // Дочерняя форма

        private CustomButton btnDelete;
        private CustomButton btnOkPlayers;
        private CustomButton btnOkNewPlayer;
        private CustomButton btnExit;

        public MenuForm() {
            InitializeComponent();
            panelPlayers.DoubleBuffered(true); // Убирает мерцание с панели
            panelNewPlayer.DoubleBuffered(true); // Убирает мерцание с панели
            panelError.DoubleBuffered(true); // Убирает мерцание с панели
            panelDelete.DoubleBuffered(true); // Убирает мерцание с панели

            roomForm = new RoomForm() { Dock = DockStyle.Fill, TopLevel = false };  // Дочерняя форма
            this.Controls.Add(roomForm);

            prizesForm = new PrizesForm() { Dock = DockStyle.Fill, TopLevel = false };  // Дочерняя форма
            this.Controls.Add(prizesForm);

            // Добавление btnDelete программно
            btnDelete = new CustomButton {
                BackColor = Color.FromArgb(64, 64, 64),
                FlatAppearance = { BorderColor = Color.LightGray },
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Microsoft YaHei UI", 12F, FontStyle.Bold, GraphicsUnit.Point, 204),
                ForeColor = Color.LightGray,
                Location = new Point(385, 370),
                Size = new Size(174, 46),
                Text = "Удалить",
                UseVisualStyleBackColor = false
            };
            btnDelete.Click += BtnDelete_Click;
            panelPlayers.Controls.Add(btnDelete);

            // Добавление btnOkPlayers программно
            btnOkPlayers = new CustomButton {
                BackColor = Color.FromArgb(64, 64, 64),
                FlatAppearance = { BorderColor = Color.LightGray },
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Microsoft YaHei UI", 12F, FontStyle.Bold, GraphicsUnit.Point, 204),
                ForeColor = Color.LightGray,
                Location = new Point(247, 459),
                Size = new Size(140, 46),
                Text = "ОК",  
                UseVisualStyleBackColor = false
            };
            btnOkPlayers.Click += btnOkPlayers_Click;
            panelPlayers.Controls.Add(btnOkPlayers);

            // Добавление btnOkNewPlayer программно
            btnOkNewPlayer = new CustomButton {
                BackColor = Color.FromArgb(64, 64, 64),
                FlatAppearance = { BorderColor = Color.LightGray },
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Microsoft YaHei UI", 12F, FontStyle.Bold, GraphicsUnit.Point, 204),
                ForeColor = Color.LightGray,
                Location = new Point(86, 442),
                Size = new Size(174, 46),
                Text = "ОК",
                Enabled = false,
                UseVisualStyleBackColor = false
            };
            btnOkNewPlayer.Click += btnOkNewPlayer_Click;
            panelNewPlayer.Controls.Add(btnOkNewPlayer);

            // Добавление btnExit программно
            btnExit = new CustomButton {
                Name = "btnExit",
                BackColor = Color.Indigo,
                FlatAppearance = { BorderColor = Color.LightGray },
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Microsoft YaHei UI", 13.8F, FontStyle.Bold, GraphicsUnit.Point,204),
                ForeColor = Color.LightGray,
                Location = new Point(435, 715),
                Size = new Size(321, 66),
                Text = "Выйти из игры",
                UseVisualStyleBackColor = false
            };
            btnExit.Click += btnExit_Click;
            this.Controls.Add(btnExit);

            // Удаляет все формы из Controls
            //this.Controls.OfType<Form>().ToList().ForEach(f => this.Controls.Remove(f));
            // Показываем дочернюю форму
            //PlayForm playForm = new PlayForm() { Dock = DockStyle.Fill, TopLevel = false };  // Дочерняя форма
            //this.Controls.Add(playForm);
        }

        protected override CreateParams CreateParams {  // убирает мерцание 
            get {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= 0x02000000;  //  заставляет Windows использовать двойную буферизацию для перерисовки окна
                return cp;
            }
        }

        private void MenuForm_Load(object sender, EventArgs e) {
            // (!!) Старт сети
            try {
                NetworkManager.Start();  // (!!) Старт сети

                PlayerControl.InitializeFileWatcher();
                PlayerControl.PlayersListUpdated += UpdatePlayerReferences; // Подписка на событие обновления списка

                // Получаем неактивного на данный момент игрока, за которого играли последним 
                Player playerToActivate = PlayerControl.GetActivePlayer();
                // Если null (список игроков пуст/все игроки активны), показываем панель создания нового игрока без кнопки отмены
                if (playerToActivate == null) ShowWelcomePanel();
                else SetNewActivePlayer(playerToActivate);  // активируем найденного игрока
            }
            catch (Exception ex) {
                MessageBox.Show(ex.Message, "Ошибка сети", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Close();
            }
        }

        private void btnExit_Click(object sender, EventArgs e) {
            //Application.Exit(); 
            Close();  // Закрыть программу + вызывает FormClosing
        }

        private void MenuForm_FormClosing(object sender, FormClosingEventArgs e) {
            PlayerControl.DeactivatePlayer(ActivePlayer); // Сбрасываем IsActive вышедшего игрока
            NetworkManager.Stop(); // (!!) Останавливаем сеть
        }

        private void UpdatePlayerReferences() {  // обновляет ссылки при обновлении списка игроков
            if (ActivePlayer != null) {
                ActivePlayer = PlayerControl.PlayersList.FirstOrDefault(p => p.Name == ActivePlayer.Name);
                UpdateWelcomeLabel();
                // Скрываем все панели, чтобы обновить данные 
                panelPlayers.Hide();
                panelDelete.Hide();
                panelNewPlayer.Hide();
                panelError.Hide();
                btnExit.Enabled = true;  // Делаем кнопку выхода доступной 
            }                 
        }

        private void BtnDelete_Click(object sender, EventArgs e) {
            panelDelete.Location = new Point(283, 164);
            lblNameToDelete.Text = SelectedPlayer.Name;
            // Центрирование по X
            Size textSize = TextRenderer.MeasureText(lblNameToDelete.Text, lblNameToDelete.Font);
            int x = (panelDelete.Width - textSize.Width) / 2;
            lblNameToDelete.Location = new Point(x, lblNameToDelete.Location.Y);
            // Показываем панель
            panelDelete.BringToFront();
            panelDelete.Show();
        }

        private void btnNo_Click(object sender, EventArgs e) {
            panelDelete.Hide();
        }

        private void btnYes_Click(object sender, EventArgs e) {
            bool isCurrentPlayer = SelectedPlayer == ActivePlayer;
            PlayerControl.PlayersList.Remove(SelectedPlayer);  // Удаляем игрока из списка
            // Если удаляем текущего активного игрока, то активируем неактивного на данный момент игрока, за которого играли последним
            if (isCurrentPlayer) {
                Player newActivePlayer = PlayerControl.GetActivePlayer();
                // Если null (список игроков пуст/все игроки активны), показываем панель создания нового игрока без кнопки отмены
                if (newActivePlayer == null) ShowWelcomePanel();
                else SetNewActivePlayer(newActivePlayer);  // активируем найденного игрока    
            }
            PlayerControl.SavePlayers();
            DisplayUsers();
            panelDelete.Hide();
        }

        private void btnPlay_Click(object sender, EventArgs e) {
            roomForm.BringToFront(); // Поставить поверх всех элементов
            roomForm.Show();        
        }

        private void btnPlayers_Click(object sender, EventArgs e) {
            btnExit.Enabled = false; // Заблокировать кнопку выхода
            DisplayUsers();
            panelPlayers.Location = new Point(283, 164);
            panelPlayers.Show();
        }

        private void DisplayUsers() {
            flowPanelPlayers.Controls.Clear();  // Очистка старых карточек игроков                
            List<Player> sortedPlayers = PlayerControl.PlayersList.OrderBy(p => p.ID).ToList();  // Сортировка по ID
            SelectedPlayer = ActivePlayer; // Устанавливаем подсвеченного игрока

            // проходимся по всем игрокам
            foreach (Player player in sortedPlayers) {
                Panel card = GetPlayerCard(player);  // создаем карточку игрока
                flowPanelPlayers.Controls.Add(card);  // помещаем  карточку игрока на экран
            }

            // Обновляем состояние кнопок в зависимости от SelectedPlayer
            UpdateButtonStates();
        }

        private Panel GetPlayerCard(Player player) {
            Panel panel = new Panel() {
                Location = new Point(3, 3),
                Size = new Size(320, 44),
                BackColor = player == SelectedPlayer ? Color.MediumPurple : Color.Indigo  // Подсветка активного игрока
            };
            Label label = new Label() {
                AutoSize = true,
                Font = new Font("Microsoft YaHei UI", 12F, FontStyle.Bold, GraphicsUnit.Point, 204),
                ForeColor = Color.LightGray,
                Location = new Point(13, 9),
                Text = player.Name,
                BackColor = Color.Transparent
            };
            panel.Controls.Add(label);
            // Меняем цвет у выбранной панели игрока
            panel.MouseDown += (s, e) => {
                HandlePanelSelection(panel, player);
            };
            // Меняем цвет у выбранной панели игрока нажатием на label
            label.MouseDown += (s, e) => {
                HandlePanelSelection(panel, player);
            };
            return panel;
        }

        private void HandlePanelSelection(Panel panel, Player player) {
            // Если текущая панель не активна, то меняем цвет 
            if (panel.BackColor == Color.Indigo) {
                // Проходим по всем панелям в flowPanelPlayers
                foreach (Panel p in flowPanelPlayers.Controls.OfType<Panel>()) {
                    // Прошлую активную панель делаем неактивной
                    if (p.BackColor == Color.MediumPurple) p.BackColor = Color.Indigo;
                    // Если это переданная панель
                    if (p == panel) p.BackColor = Color.MediumPurple;
                }
                // Обновляем подсвеченного игрока
                SelectedPlayer = player;
                // Обновляем состояние кнопок в зависимости от SelectedPlayer
                UpdateButtonStates();
            }
        }

        private void UpdateButtonStates() {
            bool hasInactivePlayers = PlayerControl.PlayersList.Any(p => !p.IsActive);
            // Кнопка удаления доступна только для неактивных игроков и для текущего игрока, если есть неактивные в списке (чтобы было на кого сменить себя после удаления)
            if (SelectedPlayer != null &&
                (!SelectedPlayer.IsActive || (SelectedPlayer == ActivePlayer && hasInactivePlayers))) {
                btnDelete.Enabled = true;
                btnDelete.BackColor = Color.Indigo;
            } else {
                btnDelete.Enabled = false;
                btnDelete.BackColor = Color.FromArgb(64, 64, 64);
            }

            // Кнопка выбора игрока (ОК) доступна только для неактивных игроков и для текущего игрока
            if (SelectedPlayer != null && (!SelectedPlayer.IsActive || SelectedPlayer == ActivePlayer)) {
                btnOkPlayers.Enabled = true;
                btnOkPlayers.BackColor = Color.Indigo;
            } else {
                btnOkPlayers.Enabled = false;
                btnOkPlayers.BackColor = Color.FromArgb(64, 64, 64);
            }
        }

        private void btnOkPlayers_Click(object sender, EventArgs e) {
            // если подсвеченный игрок отличается от текущего активного, то делаем его активным
            if (SelectedPlayer != null && ActivePlayer != SelectedPlayer) SetNewActivePlayer(SelectedPlayer);

            btnExit.Enabled = true;  // Разблокировать кнопку выхода
            panelPlayers.Hide();  // Скрываем панель игроков
        }


        private void btnNew_Click(object sender, EventArgs e) {
            textBoxName.Text = "";
            panelNewPlayer.Location = new Point(283, 164);
            btnOkNewPlayer.Location = new Point(86, 442);
            lblNewPlayer.Text = "Новый игрок";
            lblNewPlayer.Location = new Point(198, 13);
            btnCancel.Location = new Point(362, 442);
            btnCancel.Show();
            btnOkNewPlayer.Enabled = false; // Сбрасываем при открытии панели
            panelNewPlayer.BringToFront();  
            panelNewPlayer.Show();
        }

        private void ShowWelcomePanel() {
            textBoxName.Text = "";
            panelNewPlayer.Location = new Point(283, 164);
            btnOkNewPlayer.Location = new Point(231, 419);
            lblNewPlayer.Text = "Добро пожаловать в игру!";
            lblNewPlayer.Location = new Point(89, 13);
            btnCancel.Hide();
            btnOkNewPlayer.Enabled = false; // Сбрасываем при открытии панели
            panelNewPlayer.BringToFront();
            panelNewPlayer.Show();
        }

        private void btnCancel_Click(object sender, EventArgs e) {
            panelNewPlayer.Hide();
        }

        private void btnOkNewPlayer_Click(object sender, EventArgs e) {
            // если нет пробелов или пустых строк, то создаем нового игрока и активируем его
            if (!string.IsNullOrWhiteSpace(textBoxName.Text)) {
                string newName = textBoxName.Text.Trim();
                // если игрок с таким именем уже существует - то показываем ошибку
                if (PlayerControl.PlayersList.Any(p => p.Name == newName)) {
                    ShowPanelError();
                    textBoxName.Text = "";
                    btnOkNewPlayer.Enabled = false; // Сбрасываем после ошибки
                    return;
                }
                Player newPlayer = new Player() {
                    ID = PlayerControl.PlayersList.Any() ? PlayerControl.PlayersList.Max(p => p.ID) + 1 : 1,
                    Name = newName,
                    IsActive = true,
                    LastPlayed = DateTime.Now
                };
                PlayerControl.PlayersList.Add(newPlayer);
                SetNewActivePlayer(newPlayer);
                PlayerControl.SavePlayers();
                DisplayUsers();
                panelNewPlayer.Hide();
            }          
        }

        private void SetNewActivePlayer(Player player) {
            // Деактивируем текущего активного игрока
            PlayerControl.DeactivatePlayer(ActivePlayer);
            // Устанавливаем нового активного игрока
            ActivePlayer = player;
            PlayerControl.ActivatePlayer(ActivePlayer);
            // Обновляем надпись приветствия
            UpdateWelcomeLabel();
        }

        private void UpdateWelcomeLabel() {
            if (ActivePlayer != null) lblWelcome.Text = $"Добро пожаловать, {ActivePlayer.Name}!";
            else lblWelcome.Text = "Добро пожаловать!";

            // Центрирование по X  
            if (this.ClientSize.Width > 0) {   // this.IsHandleCreated && 
                Size textSize = TextRenderer.MeasureText(lblWelcome.Text, lblWelcome.Font);  // вычисляет ширину текста с учётом шрифта               
                int x = (this.ClientSize.Width - textSize.Width) / 2;
                lblWelcome.Location = new Point(x, lblWelcome.Location.Y);
            }
        }

        private void ShowPanelError() {
            panelError.Location = new Point(283, 164);
            panelError.BringToFront();
            panelError.Show();
        }

        private void btnOkError_Click(object sender, EventArgs e) {
            panelError.Hide();
        }

        private void textBoxName_TextChanged(object sender, EventArgs e) {
            // Если строка пустая или состоит из пробелов, то блокируем кнопку ОК
            if (string.IsNullOrWhiteSpace(textBoxName.Text)) {
                btnOkNewPlayer.Enabled = false;
                btnOkNewPlayer.BackColor = Color.FromArgb(64, 64, 64);
            } else {
                btnOkNewPlayer.Enabled = true;
                btnOkNewPlayer.BackColor = Color.Indigo;
            }
        }

        private void btnPrizes_Click(object sender, EventArgs e) {
            prizesForm.BringToFront(); // Поставить поверх всех элементов
            prizesForm.Show();
        }
    }

    public static class ControlExtensions {
        public static void DoubleBuffered(this Control control, bool enable) {
            var prop = control.GetType().GetProperty("DoubleBuffered", BindingFlags.Instance | BindingFlags.NonPublic);
            prop.SetValue(control, enable, null);
        }
    }
}
