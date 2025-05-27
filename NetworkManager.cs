using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Поле_Чудес {
    public class NetworkManager {

        // Внутренний класс для хранения данных
        private class NetworkData {
            // Сокеты + IP
            public string LocalIp;  // Текущий IP
            public Socket UdpSocket;  // Сокет для UDP-рассылки/приёма
            public Socket TcpServerSocket;  // Сокет для приёма TCP-соединений
            // Комнаты
            public RoomInfo CurrentRoom;  // Текущая комната игрока
            public List<RoomInfo> AvailableRooms;  // Список доступных комнат
            // Флаги
            public bool IsLeader;  // Является ли игрок лидером
            public bool IsRunning;  // Работает ли NetworkManager
            // Порты
            public int UdpPort;  // Текущий UDP-порт (по умолчанию 9000)
            public int TcpPort;  // Текущий TCP-порт (по умолчанию 9001)
            // Потоки
            public Thread UdpReceiveThread;  // Поток для приёма UDP-пакетов
            public Thread TcpAcceptThread;  // Поток для принятия TCP-соединений
            public Thread UdpBroadcastThread;  // Поток для рассылки UDP (только для лидера)
        }

        // Статическое поле для данных (сокеты, списки, порты)
        private static readonly NetworkData Manager = new NetworkData {
            CurrentRoom = null, 
            AvailableRooms = new List<RoomInfo>(),
            IsRunning = false,  // флаг - активна ли сеть
            IsLeader = false,
            LocalIp = null
        };

        // Информция о комнате
        public class RoomInfo {
            public string Name { get; set; }
            public string Ip { get; set; }
            public int Port { get; set; }  // TCP-порт лидера
            public int PlayerCount { get; set; }
            public string Status { get; set; }
            public List<PlayerInfo> Players { get; set; } = new List<PlayerInfo>(); // Список игроков комнаты
            public Question Question { get; set; }  // Загаданный вопрос
        }

        // Информация об игроке
        public class PlayerInfo {
            public string Name { get; set; }
            public string Ip { get; set; }
            public int Port { get; set; }  // TCP-порт игрока
            public Socket Socket { get; set; }
            public DateTime JoinTime { get; set; }
        }

        // Публичный доступ к нашему адресу для RoomForm 
        public static string LocalAddress =>  $"{Manager.LocalIp}:{Manager.TcpPort}";

        // Событие для уведомления об обновлении списка комнат
        public static event Action RoomsUpdated;

        // Событие для уведомления UI о новом игроке
        public static event Action<PlayerInfo> PlayerJoined;

        // Событие для уведомления UI о получении сектора барабана
        public static event Action<string> SpinBarabanReceived;

        // Событие для уведомления UI о получении названной буквы
        public static event Action<string> SelectedLetterReceived;

        // Публичный метод для получения копии списка комнат
        public static List<RoomInfo> GetAvailableRooms() {
            lock (Manager.AvailableRooms) {
                // !!! ДОБАВИТЬ ПОТОМ ФИЛЬТРЫ -- Where(r => r.PlayerCount < 3 && r.Status == "Waiting")
                return Manager.AvailableRooms.ToList(); // Копия для безопасности
            }
        }

        // Публичный метод для получения текущей комнаты
        public static RoomInfo GetCurrentRoom() {
            return Manager.CurrentRoom;
        }

        // Запуск Manager
        public static void Start() {
            // Устанавливаем флаг, что сеть активна
            Manager.IsRunning = true;

            // Проверяем порты
            CheckUdpPort(9000);
            CheckTcpPort(9001);

            // Получаем и преобразуем IP
            Manager.LocalIp = GetLocalIp();
            IPAddress ipAddress;
            try {
                ipAddress = IPAddress.Parse(Manager.LocalIp);
            }
            catch (FormatException ex) {
                throw new Exception($"Неверный формат IP {Manager.LocalIp}: {ex.Message}");
            }

            // Привязываем UDP-сокет
            Manager.UdpPort = 9000;
            try {
                Manager.UdpSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                Manager.UdpSocket.EnableBroadcast = true;
                Manager.UdpSocket.Bind(new IPEndPoint(ipAddress, Manager.UdpPort));
            }
            catch (SocketException ex) {
                Manager.UdpSocket?.Close();
                throw new Exception($"IP {Manager.LocalIp} уже занят другим процессом: {ex.Message}");
            }

            // Привязываем TCP-сокет
            Manager.TcpPort = 9001;
            try {
                Manager.TcpServerSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                Manager.TcpServerSocket.Bind(new IPEndPoint(ipAddress, Manager.TcpPort));
                Manager.TcpServerSocket.Listen(10);
            }
            catch (SocketException ex) {
                Manager.UdpSocket?.Close();
                Manager.TcpServerSocket?.Close();
                throw new Exception($"IP {Manager.LocalIp} уже занят другим процессом: {ex.Message}");
            }

            // Запускаем поток для принятия UDP-пакетов (обнаружение комнат)
            Manager.UdpReceiveThread = new Thread(ReceiveUdpBroadcasts);
            Manager.UdpReceiveThread.Start();
        }

        // Проверка доступности UDP-порта
        private static void CheckUdpPort(int port) {
            Socket socket = null;  // создаем временный тестовый сокет
            try {
                socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                socket.Bind(new IPEndPoint(IPAddress.Any, port));
                socket.Close(); // Порт свободен, явно закрываем
            }
            catch (SocketException ex) {
                socket?.Close();
                throw new Exception($"Порт {port} для UDP занят: {ex.Message}");
            }
        }

        // Проверка доступности TCP-порта
        private static void CheckTcpPort(int port) {
            Socket socket = null;  // создаем временный тестовый сокет
            try {
                socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                socket.Bind(new IPEndPoint(IPAddress.Any, port));
                socket.Listen(10);
                socket.Close(); // Порт свободен, явно закрываем
            }
            catch (SocketException ex) {
                socket?.Close();
                throw new Exception($"Порт {port} для TCP занят: {ex.Message}");
            }
        }

        // Получение локального IP
        public static string GetLocalIp() {
            // Получаем аргументы командной строки
            string[] args = Environment.GetCommandLineArgs();
            string ipArg = null;

            // Ищем параметр "--ip=127.0.0.1"
            foreach (var arg in args) {
                if (arg.StartsWith("--ip="))
                    ipArg = arg.Substring(5);
            }
            // Возвращаем ip из параметра CMD - если он есть
            if (!string.IsNullOrEmpty(ipArg)) return ipArg;

            // Если аргумента нет, ищем IP в локальной сети
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList) {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                    return ip.ToString();
            }
            // Запасной вариант
            return "127.0.0.1";
        }

        // Создание комнаты (+ затирает старую комнату)
        public static void CreateRoom(string roomName) {

          

            // Устанавливаем игрока как лидера
            Manager.IsLeader = true;

            // Генерируем случайный вопрос
            var question = QuestionControl.GetRandomQuestion();

            // Инициализируем CurrentRoom
            Manager.CurrentRoom = new RoomInfo {
                Name = roomName,
                Ip = Manager.LocalIp,
                Port = Manager.TcpPort,
                PlayerCount = 1,
                Status = "Waiting",
                Players = new List<PlayerInfo> { 
                    // Добавляем текущего игрока в список Players
                    new PlayerInfo { 
                        Name = MenuForm.ActivePlayer.Name,
                        Ip = Manager.LocalIp,
                        Port = Manager.TcpPort,
                        Socket = null,
                        JoinTime = DateTime.Now
                    }
                },
                Question = question
            };

            // Завершаем предыдущий поток рассылки, если он существует
            if (Manager.UdpBroadcastThread != null && Manager.UdpBroadcastThread.IsAlive) {
                try {
                    Manager.UdpBroadcastThread.Interrupt();
                    Manager.UdpBroadcastThread.Join(100); // Ждём завершения до 100 мс
                }
                catch { }
            }

            // Завершаем предыдущий поток принятия TCP, если он существует
            if (Manager.TcpAcceptThread != null && Manager.TcpAcceptThread.IsAlive) {
                try {
                    Manager.TcpAcceptThread.Interrupt();
                    Manager.TcpAcceptThread.Join(100);
                }
                catch { }
            }

            // Запускаем поток для принятия TCP-соединений
            Manager.TcpAcceptThread = new Thread(AcceptTcpClients);
            Manager.TcpAcceptThread.Start();

            // Запускаем поток для отправки UDP-рассылки (только для лидера)
            Manager.UdpBroadcastThread = new Thread(SendUdpBroadcast);
            Manager.UdpBroadcastThread.Start();
        }


        // Отправка UDP-рассылки
        private static void SendUdpBroadcast() {

            while (Manager.IsRunning && Manager.IsLeader) {
                try {
                    // Формируем строку с данными игроков: Name|Ip|Port|JoinTime;...
                    var playersData = string.Join(";", Manager.CurrentRoom.Players.Select(p =>
                        $"{p.Name}|{p.Ip}|{p.Port}|{p.JoinTime.Ticks}"));

                    // Формируем строку с вопросом: Topic|Word
                    var questionData = Manager.CurrentRoom.Question != null
                        ? $"{Manager.CurrentRoom.Question.Topic}|{Manager.CurrentRoom.Question.Word}": "";

                    // Отправляем UDP-пакет на 255.255.255.255:9000 с информацией о комнате
                    string message = $"Room:{Manager.CurrentRoom.Name},IP:{Manager.LocalIp},Port:{Manager.TcpPort}," +
                        $"Players:{Manager.CurrentRoom.Players.Count}/3,Status:Waiting,PlayerData:{playersData},Question:{questionData}";
                    byte[] data = Encoding.UTF8.GetBytes(message);
                    Manager.UdpSocket.SendTo(data, new IPEndPoint(IPAddress.Broadcast, 9000));  // Рассылаем на стандартный порт 9000
                    Thread.Sleep(2000);  // Рассылка каждые 2 секунды
                }
                catch { }
            }
        }

        // Приём UDP-пакетов
        private static void ReceiveUdpBroadcasts() {
            byte[] buffer = new byte[4096];  // 1024 раньше
            EndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);

            while (Manager.IsRunning) {
                try {
                    // Принимаем UDP-пакеты
                    int bytesRead = Manager.UdpSocket.ReceiveFrom(buffer, ref remoteEndPoint);
                    string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);

                    // Парсим пакеты с информацией о комнатах
                    if (message.StartsWith("Room:")) {
                        var parts = message.Split(',');
                        if (parts.Length >= 7 && parts[0].StartsWith("Room:") && parts[1].StartsWith("IP:") &&
                            parts[2].StartsWith("Port:") && parts[3].StartsWith("Players:") && parts[4].StartsWith("Status:") && 
                            parts[5].StartsWith("PlayerData:") && parts[6].StartsWith("Question:")) {

                            string name = parts[0].Substring(5);
                            string ip = parts[1].Substring(3);
                            if (!int.TryParse(parts[2].Substring(5), out int port)) continue;
                            string playersPart = parts[3].Substring(8);
                            if (!int.TryParse(playersPart.Split('/')[0], out int playerCount)) continue;
                            string status = parts[4].Substring(7);
                            string playerData = parts[5].Substring(11);
                            string questionData = parts[6].Substring(9);

                            var players = new List<PlayerInfo>();
                            if (!string.IsNullOrEmpty(playerData)) {
                                var playerEntries = playerData.Split(';', (char)StringSplitOptions.RemoveEmptyEntries);
                                foreach (var entry in playerEntries) {
                                    var fields = entry.Split('|');
                                    if (fields.Length == 4 &&
                                        !string.IsNullOrEmpty(fields[0]) &&
                                        !string.IsNullOrEmpty(fields[1]) &&
                                        int.TryParse(fields[2], out int playerPort) &&
                                        long.TryParse(fields[3], out long ticks)) {
                                        players.Add(new PlayerInfo {
                                            Name = fields[0],
                                            Ip = fields[1],
                                            Port = playerPort,
                                            JoinTime = new DateTime(ticks),
                                            Socket = null
                                        });
                                    }
                                }
                            }

                            // Парсим вопрос
                            Question question = null;
                            if (!string.IsNullOrEmpty(questionData)) {
                                var questionParts = questionData.Split('|');
                                if (questionParts.Length == 2) {
                                    question = new Question {
                                        Topic = questionParts[0],
                                        Word = questionParts[1]
                                    };
                                }
                            }

                            // Обновляет или добавляет комнату в список доступных комнат AvailableRooms
                            lock (Manager.AvailableRooms) {
                                var room = Manager.AvailableRooms.FirstOrDefault(r => r.Name == name);
                                if (room == null) {
                                    Manager.AvailableRooms.Add(new RoomInfo {
                                        Name = name,
                                        Ip = ip,
                                        Port = port,
                                        PlayerCount = playerCount,
                                        Status = status,
                                        Players = players,
                                        Question = question
                                    });
                                }
                                else {
                                    // Обновляем IP/Port при смене лидера
                                    room.Ip = ip;
                                    room.Port = port;
                                    room.PlayerCount = playerCount;
                                    room.Status = status;
                                    room.Players = players;
                                    room.Question = question;
                                }
                            }

                            RoomsUpdated?.Invoke();  // Уведомляем об обновлении списка комнат
                        }
                    }
                }
                catch { }
            }
        }

        // Подключает игрока к существующей комнате
        public static void JoinRoom(RoomInfo room) {
            // Проверяем валидность комнаты
            if (room == null || room.Players.Count == 0)
                throw new Exception("Комната не выбрана или пуста");

            // Проверяем, не заполнена ли комната
            if (room.PlayerCount >= 3)
                throw new Exception("Комната заполнена");

            // Инициализируем текущую комнату на основе выбранной
            Manager.CurrentRoom = new RoomInfo {
                Name = room.Name,
                Ip = room.Ip,
                Port = room.Port,
                PlayerCount = room.PlayerCount + 1,
                Status = room.Status,
                Players = new List<PlayerInfo>(room.Players),
                Question = room.Question
            };
            
            // Добавляем себя в список игроков
            var self = new PlayerInfo {
                Name = MenuForm.ActivePlayer.Name,
                Ip = Manager.LocalIp,
                Port = Manager.TcpPort,
                Socket = null,
                JoinTime = DateTime.Now
            };
            Manager.CurrentRoom.Players.Add(self);

            // Подключаемся к каждому игроку в комнате, кроме самих себя
            foreach (var player in Manager.CurrentRoom.Players.Where(p => p.Ip != Manager.LocalIp)) {
                try {
                    // Создаём TCP-сокет для подключения и инициируем подключение к нему
                    Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    socket.Connect(player.Ip, player.Port);

                    // Сохраняем сокет для игрока из текущей комнаты
                    lock (Manager.CurrentRoom) { player.Socket = socket; }

                    // Отправляем сообщение Join с информацией о себе
                    SendMessage(socket, 1, $"{self.Name}:{self.Ip}:{self.Port}");

                    // Запускаем поток для приёма сообщений от этого игрока
                    Thread receiveThread = new Thread(() => ReceiveMessages(socket));
                    receiveThread.Start();
                }
                catch {

                }
            }

            // Запускаем поток для принятия TCP-соединений
            Manager.TcpAcceptThread = new Thread(AcceptTcpClients);
            Manager.TcpAcceptThread.Start();

        }

        // Отправляет результат вращения барабана другим игрокам
        public static void SendSpinResult(string sector) {
            lock (Manager.CurrentRoom) {
                // Проверка, не пустая ли комната
                if (Manager.CurrentRoom == null || Manager.CurrentRoom.Players == null) return;

                // Отправляем сообщение всем игрокам в комнате, кроме самих себя
                foreach (var player in Manager.CurrentRoom.Players.Where(p => p.Ip != Manager.LocalIp)) {
                    if (player.Socket != null)
                        SendMessage(player.Socket, 2, sector); // Тип 2: SpinBaraban                    
                }
            }
        }

        // Отправляет названную букву другим игрокам
        public static void SendSelectedLetter(string letter) {
            lock (Manager.CurrentRoom) {
                // Проверка, не пустая ли комната
                if (Manager.CurrentRoom == null || Manager.CurrentRoom.Players == null) return;

                // Отправляем сообщение всем игрокам в комнате, кроме самих себя
                foreach (var player in Manager.CurrentRoom.Players.Where(p => p.Ip != Manager.LocalIp)) {
                    if (player.Socket != null)
                        SendMessage(player.Socket, 3, letter); // Тип 3: SelectedLetter
                }
            }
        }

        // Принятие TCP-соединений от других игроков
        private static void AcceptTcpClients() {
            while (Manager.IsRunning) {
                try {
                    // Принимаем входящие TCP-соединения
                    Socket clientSocket = Manager.TcpServerSocket.Accept();

                    // Запускаем поток для обработки сообщений от клиента
                    Thread receiveThread = new Thread(() => ReceiveMessages(clientSocket));
                    receiveThread.Start();
                    // Каждый поток независим и привязан к своему сокету, старые потоки не закрываются
                }
                catch {
                    // Игнорируем ошибки принятия
                }
            }
        }

        // Обрабатывает входящие сообщения от других игроков
        private static void ReceiveMessages(Socket socket) {
            // Буфер для заголовка сообщения (1 байт типа + 4 байта длины)
            byte[] headerBuffer = new byte[5];
            try {
                // Продолжаем читать сообщения, пока сеть активна
                while (Manager.IsRunning) {
                    // Читаем заголовок и помещаем в буфер headerBuffer 
                    int bytesRead = socket.Receive(headerBuffer);
                    if (bytesRead < 5) break;

                    // Извлекаем тип сообщения и длину данных
                    byte type = headerBuffer[0];
                    int length = IPAddress.NetworkToHostOrder(BitConverter.ToInt32(headerBuffer, 1));

                    // Читаем данные сообщения и помещаем в буфер dataBuffer
                    byte[] dataBuffer = new byte[length];
                    int totalRead = 0;
                    while (totalRead < length) {
                        bytesRead = socket.Receive(dataBuffer, totalRead, length - totalRead, SocketFlags.None);
                        if (bytesRead == 0) throw new SocketException();
                        totalRead += bytesRead;
                    }

                    // Преобразуем данные в строку
                    string message = Encoding.UTF8.GetString(dataBuffer);

                    // Обрабатываем сообщение по типу
                    switch (type) {
                        case 1: // Join: новый игрок подключается         
                            if (message.Contains(':')) {
                                // Парсим сообщение
                                var parts = message.Split(':');
                                if (parts.Length == 3 && !string.IsNullOrEmpty(parts[0]) &&
                                    !string.IsNullOrEmpty(parts[1]) && int.TryParse(parts[2], out int port)) {

                                    // Синхронизируем доступ к CurrentRoom
                                    lock (Manager.CurrentRoom) {
                                        if (Manager.CurrentRoom != null && Manager.CurrentRoom.PlayerCount < 3) {
                                            // Создаём объект нового игрока
                                            var newPlayer = new PlayerInfo {
                                                Name = parts[0],
                                                Ip = parts[1],
                                                Port = port,
                                                Socket = socket,
                                                JoinTime = DateTime.Now
                                            };

                                            // Проверяем, нет ли уже этого игрока
                                            if (!Manager.CurrentRoom.Players.Any(p => p.Name == newPlayer.Name &&
                                                p.Ip == newPlayer.Ip && p.Port == newPlayer.Port)) {
                                                // Добавляем игрока в список
                                                Manager.CurrentRoom.Players.Add(newPlayer);
                                                Manager.CurrentRoom.PlayerCount = Manager.CurrentRoom.Players.Count;

                                                // Уведомляем локальный UI о новом игроке
                                                PlayerJoined?.Invoke(newPlayer);
                                            }
                                        }
                                    }
                                }
                            }
                            break;
                        case 2: // SpinBaraban: получен сектор барабана
                            SpinBarabanReceived?.Invoke(message); // Уведомляем локальный UI о вращении барабана до сектора
                            break;
                        case 3: // SelectedLetter: получена названная буква
                            SelectedLetterReceived?.Invoke(message); // Уведомляем локальный UI о назывании буквы
                            break;
                    }
                }
            }
            catch {
                
                // Закрываем сокет при ошибке или отключении
                socket.Close();
            }
        }

        // Отправляет байтовое сообщение через сокет
        private static void SendMessage(Socket socket, byte type, string message) {
            // Создаём сообщение
            byte[] data = CreateMessage(type, message);
            try {
                // Отправляем сообщение
                socket.Send(data);
            }
            catch {

            }
        }

        // Создаёт байтовое сообщение с заголовком [type:1][length:4][data]
        private static byte[] CreateMessage(byte type, string message) {
            // Преобразуем сообщение в байты
            byte[] data = Encoding.UTF8.GetBytes(message);

            // Создаём заголовок
            byte[] header = new byte[5];
            header[0] = type;
            Array.Copy(BitConverter.GetBytes(IPAddress.HostToNetworkOrder(data.Length)), 0, header, 1, 4);

            // Объединяем заголовок и данные
            byte[] result = new byte[5 + data.Length];
            Array.Copy(header, 0, result, 0, 5);
            Array.Copy(data, 0, result, 5, data.Length);
            return result;
        }

        // Остановка Manager
        public static void Stop() {
            // Отключаем флаг активности сети (останавливает циклы в потоках)
            Manager.IsRunning = false;

            // Закрываем все сокеты игроков
            if (Manager.CurrentRoom != null) {
                lock (Manager.CurrentRoom) {
                    foreach (var player in Manager.CurrentRoom.Players) 
                        player.Socket?.Close();   
                }
                // Очищаем текущую комнату после закрытия сокетов
                Manager.CurrentRoom = null;
            }

            // Останавливаем UDP-рассылку и закрываем UDP-сокет
            Manager.UdpBroadcastThread?.Interrupt();
            Manager.UdpSocket?.Close();

            // Останавливаем TCP-сервер и закрываем TCP-сокет
            Manager.TcpAcceptThread?.Interrupt();
            Manager.TcpServerSocket?.Close();

            // Останавливаем приём UDP
            Manager.UdpReceiveThread?.Interrupt();

            // Очищаем список AvailableRooms
            lock (Manager.AvailableRooms) {
                Manager.AvailableRooms.Clear();
            }
        }

     

        

    }
}
