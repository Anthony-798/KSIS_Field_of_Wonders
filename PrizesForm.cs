using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Поле_Чудес {
    public partial class PrizesForm : Form {
        public PrizesForm() {
            InitializeComponent();
        }

        private void btnBack_Click(object sender, EventArgs e) {
            this.ActiveControl = null; // Убираем фокус
            this.Hide();
        }
    }
}
