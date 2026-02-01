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

        public float Hard { get; set; }
        public float Soft { get; set; }
        public float Assault { get; set; }
        public float Defense { get; set; }
        public float Speed { get; set; }
        public int Strength { get; set; }

        public UnitQuality Quality { get; set; }

        public string Remark { get; set; }

        public bool IsLineUnit() => UnitType != UnitType.HeadQuarters && UnitType != UnitType.Artillery;

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

        public class UnitTypeCategory
        {
            public string name;
            public float strengthCoef; // 1 / 10 / 10
            public string strengthWordSingular; // man, gun, vehicle
            public string strengthWordPlural; // men / guns / vehicles

            public override string ToString()
            {
                return $"UnitTypeCategory({name}/{strengthCoef}/{strengthWordSingular}/{strengthWordPlural})";
            }
        }

        public static UnitTypeCategory personelCategory = new UnitTypeCategory()
        {
            name = "Personel",
            strengthCoef = 1.0f,
            strengthWordSingular = "man",
            strengthWordPlural = "men",
        };

        public static UnitTypeCategory gunCategory = new UnitTypeCategory()
        {
            name = "Gun",
            strengthCoef = 10.0f,
            strengthWordSingular = "gun",
            strengthWordPlural = "guns"
        };

        public static UnitTypeCategory vehicleCategory = new UnitTypeCategory()
        {
            name = "Vehicle",
            strengthCoef = 10.0f,
            strengthWordSingular = "vehicle",
            strengthWordPlural = "vehicles"
        };

        public static Dictionary<UnitType, UnitTypeCategory> unitTypeCategoryMap = new Dictionary<UnitType, UnitTypeCategory>()
        {
            { UnitType.Infantry, personelCategory },
            { UnitType.Tank, vehicleCategory },
            { UnitType.Artillery, gunCategory },
            { UnitType.AntiTank, gunCategory },
            { UnitType.AntiAir, gunCategory },
            { UnitType.Cavalry, personelCategory },
            { UnitType.HeadQuarters, personelCategory },
        };

        public float GetPower()
        {
            // var typeCoef = UnitType switch
            // {
            //     UnitType.Infantry => 1.0f,
            //     UnitType.HeadQuarters => 1.0f,
            //     _ => 10f
            // };
            var typeCoef = category.strengthCoef;
            return Strength * typeCoef * Assault;
        }

        public string GetStrengthWord(int strength) => strength >= 2 ? unitTypeCategoryMap[UnitType].strengthWordPlural : unitTypeCategoryMap[UnitType].strengthWordSingular;

        UnitTypeCategory categoryCached;

        public UnitTypeCategory category
        {
            get
            {
                if (categoryCached == null)
                {
                    categoryCached = unitTypeCategoryMap[UnitType];
                }
                return categoryCached;
            }
        }
    }

}