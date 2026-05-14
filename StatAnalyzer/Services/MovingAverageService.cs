using System;
using System.Collections.Generic;

namespace StatAnalyzer.Services
{
    /// <summary>
    /// Общий сервис для прогнозирования методом скользящей средней.
    /// </summary>
    public class MovingAverageService
    {
        /// <summary>
        /// Прогнозирует следующие forecastSteps значений по методу скользящей средней.
        /// </summary>
        public List<double> Forecast(List<double> data, int windowSize, int forecastSteps)
        {
            var result = new List<double>();
            var extended = new List<double>(data);

            for (int i = 0; i < forecastSteps; i++)
            {
                // Берём последние windowSize значений и считаем среднее
                int startIndex = extended.Count - windowSize;
                double sum = 0;
                for (int j = startIndex; j < extended.Count; j++)
                    sum += extended[j];

                double avg = sum / windowSize;
                result.Add(avg);
                extended.Add(avg);
            }

            return result;
        }
    }
}