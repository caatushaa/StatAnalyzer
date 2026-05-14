using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Newtonsoft.Json;
using ScottPlot;
using StatAnalyzer.Models;
using StatAnalyzer.Services;

namespace StatAnalyzer.Forms
{
    /// <summary>
    /// Форма для отображения и анализа данных о доле плохих дорог по субъектам РФ.
    /// </summary>
    public class RoadsForm : Form
    {
        // Элементы управления
        private DataGridView dataGridView;
        private ScottPlot.WinForms.FormsPlot formsPlot;
        private Button btnOpenFile;
        private Button btnForecast;
        private Button btnExport;
        private NumericUpDown nudWindowSize;
        private NumericUpDown nudForecastSteps;
        private System.Windows.Forms.Label lblWindowSize;
        private System.Windows.Forms.Label lblForecastSteps;
        private System.Windows.Forms.Label lblStats;
        // Данные и сервисы
        private List<RoadsData> _data;
        private RoadsService _roadsService;
        private MovingAverageService _movingAverageService;

        /// <summary>
        /// Конструктор формы — инициализирует компоненты и сервисы.
        /// </summary>
        public RoadsForm()
        {
            _roadsService = new RoadsService();
            _movingAverageService = new MovingAverageService();
            InitializeComponents();
        }

        /// <summary>
        /// Создаёт и размещает все элементы управления на форме.
        /// </summary>
        private void InitializeComponents()
        {
            this.Text = "Доля плохих дорог по субъектам РФ";
            this.Size = new Size(1100, 700);
            this.MinimumSize = new Size(900, 600);

            // Кнопка открытия файла
            btnOpenFile = new Button
            {
                Text = "Открыть файл",
                Location = new Point(10, 10),
                Size = new Size(120, 30)
            };
            btnOpenFile.Click += BtnOpenFile_Click;

            // Подпись и поле для размера окна скользящей средней
            lblWindowSize = new System.Windows.Forms.Label
            {
                Text = "Размер окна (n):",
                Location = new Point(150, 15),
                Size = new Size(110, 20)
            };

            nudWindowSize = new NumericUpDown
            {
                Location = new Point(265, 13),
                Size = new Size(50, 25),
                Minimum = 2,
                Maximum = 10,
                Value = 3
            };

            // Подпись и поле для количества шагов прогноза
            lblForecastSteps = new System.Windows.Forms.Label
            {
                Text = "Шагов прогноза (N):",
                Location = new Point(330, 15),
                Size = new Size(130, 20)
            };

            nudForecastSteps = new NumericUpDown
            {
                Location = new Point(465, 13),
                Size = new Size(50, 25),
                Minimum = 1,
                Maximum = 20,
                Value = 5
            };

            // Кнопка прогноза
            btnForecast = new Button
            {
                Text = "Прогноз",
                Location = new Point(530, 10),
                Size = new Size(100, 30),
                Enabled = false
            };
            btnForecast.Click += BtnForecast_Click;

            // Кнопка экспорта графика
            btnExport = new Button
            {
                Text = "Экспорт графика",
                Location = new Point(645, 10),
                Size = new Size(130, 30),
                Enabled = false
            };
            btnExport.Click += BtnExport_Click;

            // Таблица с данными
            dataGridView = new DataGridView
            {
                Location = new Point(10, 55),
                Size = new Size(400, 580),
                ReadOnly = true,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                AllowUserToAddRows = false
            };

            // График
            formsPlot = new ScottPlot.WinForms.FormsPlot
            {
                Location = new Point(420, 55),
                Size = new Size(660, 520)
            };

            // Метка статистики внизу
            lblStats = new System.Windows.Forms.Label
            {
                Location = new Point(420, 585),
                Size = new Size(660, 50),
                Text = "Загрузите файл для отображения статистики.",
                Font = new Font("Arial", 9)
            };

            // Добавляем все элементы на форму
            this.Controls.Add(btnOpenFile);
            this.Controls.Add(lblWindowSize);
            this.Controls.Add(nudWindowSize);
            this.Controls.Add(lblForecastSteps);
            this.Controls.Add(nudForecastSteps);
            this.Controls.Add(btnForecast);
            this.Controls.Add(btnExport);
            this.Controls.Add(dataGridView);
            this.Controls.Add(formsPlot);
            this.Controls.Add(lblStats);
        }

        /// <summary>
        /// Обработчик кнопки "Открыть файл" — загружает JSON и отображает данные.
        /// </summary>
        private void BtnOpenFile_Click(object sender, EventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Filter = "JSON файлы (*.json)|*.json",
                Title = "Выберите файл с данными"
            };

            if (dialog.ShowDialog() != DialogResult.OK) return;

            _data = _roadsService.LoadFromFile(dialog.FileName);
            FillTable(_data);
            DrawChart(_data);

            // Определяем лучший и худший субъект
            string best = _roadsService.GetBestSubject(_data);
            string worst = _roadsService.GetWorstSubject(_data);
            lblStats.Text = $"Максимальное снижение: {best}      Минимальное снижение: {worst}";

            btnForecast.Enabled = true;
            btnExport.Enabled = true;
        }

        /// <summary>
        /// Заполняет таблицу данными.
        /// </summary>
        private void FillTable(List<RoadsData> data)
        {
            dataGridView.DataSource = null;
            dataGridView.Columns.Clear();

            var table = new System.Data.DataTable();
            table.Columns.Add("Год", typeof(int));
            table.Columns.Add("Субъект", typeof(string));
            table.Columns.Add("% плохих дорог", typeof(double));

            foreach (var row in data.OrderBy(d => d.Year).ThenBy(d => d.Subject))
                table.Rows.Add(row.Year, row.Subject, row.BadRoadsPercent);

            dataGridView.DataSource = table;
        }

        /// <summary>
        /// Рисует график — отдельная линия для каждого субъекта.
        /// </summary>
        private void DrawChart(List<RoadsData> data)
        {
            formsPlot.Plot.Clear();

            var subjects = data.Select(d => d.Subject).Distinct().ToList();

            foreach (var subject in subjects)
            {
                var subjectData = data
                    .Where(d => d.Subject == subject)
                    .OrderBy(d => d.Year)
                    .ToList();

                double[] xs = subjectData.Select(d => (double)d.Year).ToArray();
                double[] ys = subjectData.Select(d => d.BadRoadsPercent).ToArray();

                var scatter = formsPlot.Plot.Add.Scatter(xs, ys);
                scatter.LegendText = subject;
            }

            formsPlot.Plot.Title("Доля плохих дорог по субъектам РФ");
            formsPlot.Plot.XLabel("Год");
            formsPlot.Plot.YLabel("% плохих дорог");
            formsPlot.Plot.ShowLegend();
            formsPlot.Refresh();
        }

        /// <summary>
        /// Обработчик кнопки "Прогноз" — строит прогноз методом скользящей средней.
        /// </summary>
        private void BtnForecast_Click(object sender, EventArgs e)
        {
            if (_data == null) return;

            int windowSize = (int)nudWindowSize.Value;
            int forecastSteps = (int)nudForecastSteps.Value;

            DrawChart(_data);

            var subjects = _data.Select(d => d.Subject).Distinct().ToList();

            foreach (var subject in subjects)
            {
                var subjectData = _data
                    .Where(d => d.Subject == subject)
                    .OrderBy(d => d.Year)
                    .ToList();

                double[] ys = subjectData.Select(d => d.BadRoadsPercent).ToList().ToArray();
                int lastYear = subjectData.Last().Year;

                // Вызываем метод прогноза из общего сервиса коллеги
                List<double> forecast = _movingAverageService.Forecast(
                    subjectData.Select(d => d.BadRoadsPercent).ToList(),
                    windowSize,
                    forecastSteps
                );

                // Строим прогнозные точки
                double[] forecastXs = Enumerable.Range(1, forecastSteps)
                    .Select(i => (double)(lastYear + i)).ToArray();
                double[] forecastYs = forecast.ToArray();

                var forecastScatter = formsPlot.Plot.Add.Scatter(forecastXs, forecastYs);
                forecastScatter.LegendText = subject + " (прогноз)";
                forecastScatter.LinePattern = ScottPlot.LinePattern.Dashed;
            }

            formsPlot.Refresh();
        }

        /// <summary>
        /// Обработчик кнопки "Экспорт графика" — сохраняет график в PNG или SVG.
        /// </summary>
        private void BtnExport_Click(object sender, EventArgs e)
        {
            var dialog = new SaveFileDialog
            {
                Filter = "PNG изображение (*.png)|*.png|SVG файл (*.svg)|*.svg",
                Title = "Сохранить график"
            };

            if (dialog.ShowDialog() != DialogResult.OK) return;

            string path = dialog.FileName;

            if (path.EndsWith(".svg"))
                formsPlot.Plot.SaveSvg(path, 800, 500);
            else
                formsPlot.Plot.SavePng(path, 800, 500);

            MessageBox.Show("График сохранён!", "Экспорт", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}