using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using ScottPlot;
using ScottPlot.WinForms;
using StatAnalyzer.Models;
using StatAnalyzer.Services;
using Label = System.Windows.Forms.Label;

namespace StatAnalyzer.Forms
{
    public class GdpForm : Form
    {
        private DataGridView grid;
        private FormsPlot plot;
        private NumericUpDown nudWindow;
        private NumericUpDown nudSteps;
        private Label lblStats;
        private List<GdpData> data;

        private readonly GdpService gdpService = new GdpService();
        private readonly MovingAverageService maService = new MovingAverageService();

        public GdpForm()
        {
            BuildUI();
        }

        private void BuildUI()
        {
            this.Text = "ВВП и ВНП России (2009–2023)";
            this.Size = new Size(1100, 700);

            grid = new DataGridView
            {
                Location = new Point(10, 10),
                Size = new Size(360, 400),
                ReadOnly = true,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };

            plot = new FormsPlot
            {
                Location = new Point(380, 10),
                Size = new Size(700, 400)
            };

            Label lblN = new Label { Text = "Окно (n):", Location = new Point(10, 420), AutoSize = true };
            nudWindow = new NumericUpDown { Location = new Point(90, 418), Minimum = 2, Maximum = 10, Value = 3, Width = 60 };

            Label lblSteps = new Label { Text = "Прогноз (N):", Location = new Point(160, 420), AutoSize = true };
            nudSteps = new NumericUpDown { Location = new Point(255, 418), Minimum = 1, Maximum = 10, Value = 3, Width = 60 };

            Button btnLoad = new Button { Text = "Открыть файл", Location = new Point(10, 455), Size = new Size(120, 30) };
            btnLoad.Click += BtnLoad_Click;

            Button btnForecast = new Button { Text = "Прогноз", Location = new Point(140, 455), Size = new Size(100, 30) };
            btnForecast.Click += BtnForecast_Click;

            Button btnExport = new Button { Text = "Экспорт графика", Location = new Point(250, 455), Size = new Size(130, 30) };
            btnExport.Click += BtnExport_Click;

            lblStats = new Label
            {
                Location = new Point(10, 495),
                Size = new Size(1060, 60),
                Font = new Font("Segoe UI", 9)
            };

            Controls.AddRange(new Control[] {
                grid, plot,
                lblN, nudWindow, lblSteps, nudSteps,
                btnLoad, btnForecast, btnExport, lblStats
            });
        }

        private void BtnLoad_Click(object sender, EventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.Filter = "JSON файлы|*.json";
            if (dlg.ShowDialog() != DialogResult.OK) return;

            data = gdpService.LoadData(dlg.FileName);
            FillGrid();
            DrawChart(null, null);
            ShowStats();
        }

        private void FillGrid()
        {
            var list = data.Select(d => new
            {
                Год = d.Year,
                ВВП_млрд_руб = d.GDP.ToString("N1"),
                ВНП_млрд_руб = d.GNP.ToString("N1")
            }).ToList();
            grid.DataSource = list;
        }

        private void DrawChart(List<double> forecastGdp, List<double> forecastGnp)
        {
            plot.Plot.Clear();

            double[] years = data.Select(d => (double)d.Year).ToArray();
            double[] gdp = data.Select(d => d.GDP).ToArray();
            double[] gnp = data.Select(d => d.GNP).ToArray();

            plot.Plot.Add.Scatter(years, gdp).LegendText = "ВВП";
            plot.Plot.Add.Scatter(years, gnp).LegendText = "ВНП";

            if (forecastGdp != null && forecastGdp.Count > 0)
            {
                double startYear = data.Last().Year;
                double[] fYears = Enumerable.Range(1, forecastGdp.Count)
                    .Select(i => startYear + i).Select(y => (double)y).ToArray();

                var scGdp = plot.Plot.Add.Scatter(fYears, forecastGdp.ToArray());
                scGdp.LegendText = "Прогноз ВВП";
                scGdp.Color = ScottPlot.Color.FromHex("#FF4500");
                scGdp.LinePattern = LinePattern.Dashed;

                var scGnp = plot.Plot.Add.Scatter(fYears, forecastGnp.ToArray());
                scGnp.LegendText = "Прогноз ВНП";
                scGnp.Color = ScottPlot.Color.FromHex("#9370DB");
                scGnp.LinePattern = LinePattern.Dashed;
            }

            plot.Plot.ShowLegend();
            plot.Plot.XLabel("Год");
            plot.Plot.YLabel("млрд руб.");
            plot.Plot.Title("ВВП и ВНП России");
            plot.Refresh();
        }

        private void BtnForecast_Click(object sender, EventArgs e)
        {
            if (data == null) { MessageBox.Show("Сначала загрузите файл"); return; }

            int n = (int)nudWindow.Value;
            int steps = (int)nudSteps.Value;

            List<double> gdpVals = data.Select(d => d.GDP).ToList();
            List<double> gnpVals = data.Select(d => d.GNP).ToList();

            List<double> fGdp = maService.Forecast(gdpVals, n, steps);
            List<double> fGnp = maService.Forecast(gnpVals, n, steps);

            DrawChart(fGdp, fGnp);

            string info = "Прогноз ВВП: " + string.Join(", ",
                fGdp.Select((v, i) => $"{data.Last().Year + i + 1}: {v:N1}"));
            lblStats.Text += "\n" + info;
        }

        private void BtnExport_Click(object sender, EventArgs e)
        {
            SaveFileDialog dlg = new SaveFileDialog();
            dlg.Filter = "PNG|*.png|SVG|*.svg";
            dlg.FileName = "gdp_chart";
            if (dlg.ShowDialog() != DialogResult.OK) return;

            if (dlg.FileName.EndsWith(".svg"))
                plot.Plot.SaveSvg(dlg.FileName, 900, 500);
            else
                plot.Plot.SavePng(dlg.FileName, 900, 500);

            MessageBox.Show("График сохранён!");
        }

        private void ShowStats()
        {
            var stats = gdpService.GetGdpStats(data);
            double maxG = stats.maxGrowth;
            int maxGY = stats.maxGrowthYear;
            double maxD = stats.maxDrop;
            int maxDY = stats.maxDropYear;
            lblStats.Text =
                $"Макс. рост ВВП: {maxG:+0.00;-0.00}% ({maxGY} г.)   " +
                $"Макс. падение ВВП: {maxD:+0.00;-0.00}% ({maxDY} г.)";
        }
    }
}