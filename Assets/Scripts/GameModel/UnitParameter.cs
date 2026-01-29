using System.Collections.Generic;
using System;
using System.Globalization;
using System.IO;
using CsvHelper;
using CsvHelper.Configuration;
using System.Linq;

namespace GameModel
{
    public class UnitParameter // PZC-like parameters
    {
        // public string UnitType { get; set; }
        // public string Country { get; set; }
        public UnitType UnitType { get; set; }
        public Country Country { get; set; }

        public int Hard { get; set; }
        public int Soft { get; set; }
        public int Assault { get; set; }
        public int Defense { get; set; }
        public int Speed { get; set; }
        public int Strength { get; set; }

        public UnitQuality Quality { get; set; }

        public string Remark { get; set; }

        public override string ToString()
        {
            return $"UnitParameter({UnitType}/{Country}/{Hard}/{Soft}/{Assault}/{Defense}/{Speed}/{Strength}/{Quality})";
        }


        public static List<UnitParameter> ParseUnits(string csvText)
        {
            using var reader = new StringReader(csvText);

            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = true,
                IgnoreBlankLines = true,
                TrimOptions = TrimOptions.Trim,
                MissingFieldFound = null,
                BadDataFound = null
            };

            using var csv = new CsvReader(reader, config);

            return csv.GetRecords<UnitParameter>().ToList();
        }
    }

}