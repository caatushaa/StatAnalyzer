using System;

namespace StatAnalyzer.Models
{
    /// Модель одной записи о доле плохих дорог в субъекте РФ за конкретный год.
    public class RoadsData
    {
        /// Год наблюдения
        public int Year { get; set; }

        /// Название субъекта РФ
        public string Subject { get; set; }

        /// Доля плохих дорог в процентах
        public double BadRoadsPercent { get; set; }
    }
}