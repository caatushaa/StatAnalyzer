using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using StatAnalyzer.Models;

namespace StatAnalyzer.Services
{
    public class GdpService
    {
        public List<GdpData> LoadData(string filePath)
        {
            string json = File.ReadAllText(filePath);
            return JsonConvert.DeserializeObject<List<GdpData>>(json);
        }

        public (double maxGrowth, int maxGrowthYear, double maxDrop, int maxDropYear)
            GetGdpStats(List<GdpData> data)
        {
            double maxGrowth = double.MinValue, maxDrop = double.MaxValue;
            int maxGrowthYear = 0, maxDropYear = 0;

            for (int i = 1; i < data.Count; i++)
            {
                double change = (data[i].GDP - data[i - 1].GDP) / data[i - 1].GDP * 100;
                if (change > maxGrowth) { maxGrowth = change; maxGrowthYear = data[i].Year; }
                if (change < maxDrop) { maxDrop = change; maxDropYear = data[i].Year; }
            }

            return (maxGrowth, maxGrowthYear, maxDrop, maxDropYear);
        }
    }
}
