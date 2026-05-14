using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using StatAnalyzer.Models;

namespace StatAnalyzer.Services
{
    /// <summary>
    /// Сервис для загрузки и анализа данных о доле плохих дорог по субъектам РФ.
    /// </summary>
    public class RoadsService
    {
        /// <summary>
        /// Загружает данные из JSON файла по указанному пути.
        /// </summary>
        public List<RoadsData> LoadFromFile(string filePath)
        {
            string json = File.ReadAllText(filePath);
            return JsonConvert.DeserializeObject<List<RoadsData>>(json);
        }

        /// <summary>
        /// Возвращает субъект с максимальным снижением % плохих дорог за весь период.
        /// </summary>
        public string GetBestSubject(List<RoadsData> data)
        {
            return GetSubjectByDelta(data, best: true);
        }

        /// <summary>
        /// Возвращает субъект с минимальным снижением % плохих дорог за весь период.
        /// </summary>
        public string GetWorstSubject(List<RoadsData> data)
        {
            return GetSubjectByDelta(data, best: false);
        }

        /// <summary>
        /// Вспомогательный метод — ищет субъект с наибольшим или наименьшим снижением.
        /// </summary>
        private string GetSubjectByDelta(List<RoadsData> data, bool best)
        {
            // Получаем список всех субъектов
            var subjects = data.Select(d => d.Subject).Distinct();

            string resultSubject = null;
            double resultDelta = best ? double.MinValue : double.MaxValue;

            foreach (var subject in subjects)
            {
                // Данные по одному субъекту, отсортированные по году
                var subjectData = data
                    .Where(d => d.Subject == subject)
                    .OrderBy(d => d.Year)
                    .ToList();

                double first = subjectData.First().BadRoadsPercent;
                double last = subjectData.Last().BadRoadsPercent;

                // Снижение = первый год минус последний год (чем больше, тем лучше)
                double delta = first - last;

                if (best && delta > resultDelta)
                {
                    resultDelta = delta;
                    resultSubject = subject;
                }
                else if (!best && delta < resultDelta)
                {
                    resultDelta = delta;
                    resultSubject = subject;
                }
            }

            return resultSubject;
        }
    }
}