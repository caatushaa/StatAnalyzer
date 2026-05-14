using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using StatAnalyzer.Models;
using StatAnalyzer.Services;

namespace StatAnalyzer.Forms
{
    public class RoadsForm : Form
    {
        private DataGridView dataGridView;
        private ScottPlot.WinForms.FormsPlot formsPlot;
        private Button btnOpenFile, btnForecast, btnExport;
        private NumericUpDown nudWindowSize, nudForecastSteps;
        private System.Windows.Forms.Label lblStats;

        private List<RoadsData> _data;
        private RoadsService _roadsService = new RoadsService();
        private MovingAverageService _movingAverageService = new MovingAverageService();

        public RoadsForm()
        {
            this.Text = "Доля плохих дорог по субъектам РФ";
            this.Size = new Size(1100, 700);

            // Кнопка открытия файла
            btnOpenFile = new Button { Text = "Открыть файл", Location = new Point(10, 10), Size = new Size(120, 30) };
            btnOpenFile.Click += BtnOpenFile_Click;

            // Поля для параметров скользящей средней
            new System.Windows.Forms.Label { Text = "Размер окна (n):", Location = new Point(150, 15), Size = new Size(110, 20), Parent = this };
            nudWindowSize = new NumericUpDown { Location = new Point(265, 13), Size = new Size(50, 25), Minimum = 2, Maximum = 10, Value = 3 };

            new System.Windows.Forms.Label { Text = "Шагов прогноза (N):", Location = new Point(330, 15), Size = new Size(130, 20), Parent = this };
            nudForecastSteps = new NumericUpDown { Location = new Point(465, 13), Size = new Size(50, 25), Minimum = 1, Maximum = 20, Value = 5 };

            // Кнопки прогноза и экспорта
            btnForecast = new Button { Text = "Прогноз", Location = new Point(530, 10), Size = new Size(100, 30), Enabled = false };
            btnForecast.Click += BtnForecast_Click;

            btnExport = new Button { Text = "Экспорт графика", Location = new Point(645, 10), Size = new Size(130, 30), Enabled = false };
            btnExport.Click += BtnExport_Click;

            // Таблица
            dataGridView = new DataGridView { Location = new Point(10, 55), Size = new Size(400, 580), ReadOnly = true, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, AllowUserToAddRows = false };

            // График
            formsPlot = new ScottPlot.WinForms.FormsPlot { Location = new Point(420, 55), Size = new Size(660, 520) };

            // Метка статистики
            lblStats = new System.Windows.Forms.Label { Location = new Point(420, 585), Size = new Size(660, 50), Text = "Загрузите файл для отображения статистики." };

            Controls.AddRange(new Control[] { btnOpenFile, nudWindowSize, nudForecastSteps, btnForecast, btnExport, dataGridView, formsPlot, lblStats });
        }

        // Открытие файла и отображение данных
        private void BtnOpenFile_Click(object sender, EventArgs e)
        {
            var dialog = new OpenFileDialog { Filter = "JSON файлы (*.json)|*.json" };
            if (dialog.ShowDialog() != DialogResult.OK) return;

            _data = _roadsService.LoadFromFile(dialog.FileName);
            FillTable();
            DrawChart();

            lblStats.Text = $"Максимальное снижение: {_roadsService.GetBestSubject(_data)}      " +
                            $"Минимальное снижение: {_roadsService.GetWorstSubject(_data)}";

            btnForecast.Enabled = true;
            btnExport.Enabled = true;
        }

        // Заполнение таблицы
        private void FillTable()
        {
            var table = new System.Data.DataTable();
            table.Columns.Add("Год", typeof(int));
            table.Columns.Add("Субъект", typeof(string));
            table.Columns.Add("% плохих дорог", typeof(double));

            foreach (var row in _data.OrderBy(d => d.Year).ThenBy(d => d.Subject))
                table.Rows.Add(row.Year, row.Subject, row.BadRoadsPercent);

            dataGridView.DataSource = table;
        }

        // Отрисовка графика
        private void DrawChart()
        {
            formsPlot.Plot.Clear();

            foreach (var subject in _data.Select(d => d.Subject).Distinct())
            {
                var points = _data.Where(d => d.Subject == subject).OrderBy(d => d.Year).ToList();
                var scatter = formsPlot.Plot.Add.Scatter(
                    points.Select(d => (double)d.Year).ToArray(),
                    points.Select(d => d.BadRoadsPercent).ToArray()
                );
                scatter.LegendText = subject;
            }

            formsPlot.Plot.Title("Доля плохих дорог по субъектам РФ");
            formsPlot.Plot.XLabel("Год");
            formsPlot.Plot.YLabel("% плохих дорог");
            formsPlot.Plot.ShowLegend();
            formsPlot.Plot.Axes.AutoScale();
            formsPlot.Refresh();
            formsPlot.Refresh();
        }

        // Построение прогноза
        private void BtnForecast_Click(object sender, EventArgs e)
        {
            if (_data == null) return;

            DrawChart();

            foreach (var subject in _data.Select(d => d.Subject).Distinct())
            {
                var points = _data.Where(d => d.Subject == subject).OrderBy(d => d.Year).ToList();
                int lastYear = points.Last().Year;

                var forecast = _movingAverageService.Forecast(
                    points.Select(d => d.BadRoadsPercent).ToList(),
                    (int)nudWindowSize.Value,
                    (int)nudForecastSteps.Value
                );

                var scatter = formsPlot.Plot.Add.Scatter(
                    Enumerable.Range(1, forecast.Count).Select(i => (double)(lastYear + i)).ToArray(),
                    forecast.ToArray()
                );
                scatter.LegendText = subject + " (прогноз)";
                scatter.LinePattern = ScottPlot.LinePattern.Dashed;
            }
            formsPlot.Plot.Axes.AutoScale();
            formsPlot.Refresh();
            formsPlot.Refresh();
        }

        // Экспорт графика в PNG или SVG
        private void BtnExport_Click(object sender, EventArgs e)
        {
            var dialog = new SaveFileDialog { Filter = "PNG (*.png)|*.png|SVG (*.svg)|*.svg" };
            if (dialog.ShowDialog() != DialogResult.OK) return;

            if (dialog.FileName.EndsWith(".svg"))
                formsPlot.Plot.SaveSvg(dialog.FileName, 800, 500);
            else
                formsPlot.Plot.SavePng(dialog.FileName, 800, 500);

            MessageBox.Show("График сохранён!", "Экспорт", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}