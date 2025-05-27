using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Поле_Чудес {
    public class Player {
        public int ID { get; set; }
        public string Name { get; set; }
        public bool IsActive { get; set; } // Активен ли в окне
        public DateTime LastPlayed { get; set; } // Время последнего использования
    }
}
