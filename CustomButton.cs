using static System.Net.Mime.MediaTypeNames;
using System.Drawing;
using System.Windows.Forms;
using System.Xml.Linq;

public class CustomButton : Button {
    protected override void OnPaint(PaintEventArgs pevent) {
        if (!Enabled) {
            // Рисуем фон
            using (var backBrush = new SolidBrush(BackColor)) {
                pevent.Graphics.FillRectangle(backBrush, ClientRectangle);
            }

            // Рисуем рамку (если есть)
            if (FlatAppearance.BorderSize > 0) {
                using (var pen = new Pen(FlatAppearance.BorderColor, FlatAppearance.BorderSize)) {
                    Rectangle borderRect = new Rectangle(ClientRectangle.X, ClientRectangle.Y,
                        ClientRectangle.Width - 1, ClientRectangle.Height - 1);
                    pevent.Graphics.DrawRectangle(pen, borderRect);
                }
            }

            // Рисуем текст с помощью TextRenderer
            using (var textBrush = new SolidBrush(ForeColor)) {
                Rectangle textRect = ClientRectangle;
                
                // Лёгкий сдвиг для точной подстройки 
                textRect = new Rectangle(ClientRectangle.X, ClientRectangle.Y - 1, 
                    ClientRectangle.Width, ClientRectangle.Height);

                // Используем TextRenderer для системного рендеринга
                TextRenderer.DrawText(pevent.Graphics, Text, Font, textRect, ForeColor,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoClipping);
            }
        }
        else {
            base.OnPaint(pevent);
        }
    }
}
