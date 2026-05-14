using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace StatAnalyzer.Forms
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            
            this.Text = "Статистический анализатор";
            this.Size = new Size(400, 300);

            var btnGdp = new Button
            {
                Text = "ВВП и ВНП России",
                Size = new Size(300, 60),
                Location = new Point(50, 60)
            };
            btnGdp.Click += (s, e) => new GdpForm().Show();

            var btnRoads = new Button
            {
                Text = "Качество дорог по субъектам",
                Size = new Size(300, 60),
                Location = new Point(50, 150)
            };
            btnRoads.Click += (s, e) => new RoadsForm().Show();
            Controls.Add(btnGdp);
            Controls.Add(btnRoads);
        }
    }
}
