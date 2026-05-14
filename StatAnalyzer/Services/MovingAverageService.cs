using System;
using System.Collections.Generic;
using System.Linq;

namespace StatAnalyzer.Services
{
    public class MovingAverageService
    {
        public List<double> Forecast(List<double> data, int windowSize, int forecastSteps)
        {
            var result = new List<double>(data);
            for (int i = 0; i < forecastSteps; i++)
            {
                var window = result.Skip(result.Count - windowSize).Take(windowSize);
                double avg = window.Average();
                result.Add(avg);
            }
            return result.Skip(data.Count).ToList();
        }
    }
}