using ClosedXML.Excel;
using Newtonsoft.Json;
using Organization.Entities;
using Organization.Roles;
using Shouldly;
using System.Collections.Generic;

namespace Organization.Tests
{
    public class ReadExcel
    {
        [Fact]
        public void DeserializeWithInit()
        {
            var item = new ClassWithInit { Value = 10 };
            var json = JsonConvert.SerializeObject(item);
            var result = JsonConvert.DeserializeObject<ClassWithInit>(json);
            result!.Value.ShouldBe(10);
        }

        public class ClassWithInit
        {
            internal ClassWithInit() { }
            public int Value { get; init; }
        }

        [Fact]
        public void DeserializeWithConstructor()
        {
            var item = new ClassWithReadOnlyAndConstructor(10);
            var json = JsonConvert.SerializeObject(item);
            var result = JsonConvert.DeserializeObject<ClassWithReadOnlyAndConstructor>(json);
            result!.Value.ShouldBe(10);
        }

        public class ClassWithReadOnlyAndConstructor
        {
            [JsonConstructor]
            internal ClassWithReadOnlyAndConstructor(int value)
            {
                Value = value;
            }
            public int Value { get; }
        }

        [Fact(Skip = "missing local files")]
        public void ReadSchools()
        {
            List<SchoolBase> schools;
            var jsonSettings = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All };

            var jsonFile = @"C:\Users\uzk446\Downloads\Skolenhetsregistret_20220818.json";
            if (File.Exists(jsonFile))
            {
                var tmp = JsonConvert.DeserializeObject<List<SchoolBase>>(File.ReadAllText(jsonFile), jsonSettings);
                if (tmp == null) throw new NullReferenceException(jsonFile);
                schools = tmp;
            }
            else
            {
                var wb = new XLWorkbook(jsonFile.Replace(".json", ".xlsx"));

                var schoolSheets = new Dictionary<string, SchoolType>
            {
                { "Grundskola", SchoolType.School },
                { "Grundsärskola", SchoolType.Irregular },
                { "Sameskola", SchoolType.Sami },
                { "Specialskola", SchoolType.Special },
                { "Gymnasieskola", SchoolType.Highschool },
                { "Gymnasiesärskola", SchoolType.HighschoolIrregular },
            };

                schools = new List<SchoolBase>();
                foreach (var sheet in wb.Worksheets.OfType<IXLWorksheet>())
                {
                    if (schoolSheets.TryGetValue(sheet.Name, out var type))
                    {
                        var columnNames = sheet.Row(1).Cells().Select(o => o.Value.ToString() ?? "").ToList();

                        Func<IXLRow, SchoolBase> creator;
                        if (type == SchoolType.Highschool || type == SchoolType.HighschoolIrregular)
                        {
                            // Find columns for programs
                            var candidateColumns = new List<string>(columnNames);
                            for (int rowNum = 2; rowNum < 10; rowNum++)
                            {
                                var row = sheet.Row(rowNum);
                                foreach (var col in candidateColumns.ToArray())
                                {
                                    var val = GetValue<string>(row, columnNames, col);
                                    if (val != "N" && val != "J")
                                        candidateColumns.Remove(col);
                                }
                            }
                            creator = row =>
                            {
                                return new Highschool { Programs = candidateColumns.Where(col => GetValue<string>(row, columnNames, col) == "J").ToList() };
                            };
                        }
                        else
                        {
                            // Find columns for school years
                            var yearIndices = columnNames.Select((o, i) => new { Index = i, Value = o })
                                .Where(o => o.Value == "FKLASS" || o.Value.StartsWith("ÅR"))
                                .Select(o => new { Index = o.Index, Value = o.Value == "FKLASS" ? 0 : int.Parse(o.Value.Replace("ÅR", "")) })
                                .ToDictionary(o => o.Index, o => o.Value);

                            creator = row =>
                            {
                                var years = yearIndices.Where(kv => GetValue<string>(row, kv.Key) == "J").Select(kv => kv.Value).ToList();
                                return new BasicSchool { Years = years };
                            };
                        }

                        var lastRowIndex = sheet.LastRowUsed()?.RowNumber() ?? 0;
                        for (int rowNum = 2; rowNum <= lastRowIndex; rowNum++)
                        {
                            var row = sheet.Row(rowNum);
                            var school = creator(row);

                            school.Form = GetValue<string>(row, columnNames, "HUVUDMANNATYP");

                            school.Municipality = GetValue<string>(row, columnNames, "KOMMUNNAMN");
                            school.Region = GetValue<string>(row, columnNames, "LÄNSNAMN");
                            school.City = GetValue<string>(row, columnNames, "POSTORT");
                            school.ZipCode = GetValue<string>(row, columnNames, "POSTNR");

                            school.SchoolUnitId = GetValue<string>(row, columnNames, "SKOLENHETSKOD");
                            school.OrganisationId = GetValue<string>(row, columnNames, "ORGANISATIONSNR");
                            school.Name = GetValue<string>(row, columnNames, "SKOLENHETENS NAMN");

                            school.Uri = GetValue<string>(row, columnNames, "WEBB");
                            school.Email = GetValue<string>(row, columnNames, "EPOST");
                            school.Owner = GetValue<string>(row, columnNames, "HUVUDMANS NAMN");

                            var pedagogyTypeIndex = columnNames.IndexOf("INRIKTNING");
                            if (pedagogyTypeIndex >= 0)
                            {
                                school.PedagogyType = GetValue<string>(row, pedagogyTypeIndex);
                            }
                            schools.Add(school);

                            var headmasterName = GetValue<string>(row, columnNames, "REKTORS NAMN");
                            //var headmasterPerson = new Person(new Headmaster(school));
                        }
                    }
                }
                var json = JsonConvert.SerializeObject(schools, jsonSettings);
                File.WriteAllText(jsonFile, json);
            }
            //var json = System.Text.Json.JsonSerializer.Serialize(schools.Take(2), schools.GetType(), new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
            if (schools == null)
                throw new NullReferenceException(nameof(schools));

            var basicSchools = schools.OfType<BasicSchool>();
            var byPedagogy = basicSchools.GroupBy(o => o.PedagogyType).ToList();
            var highschools = schools.OfType<Highschool>();
        }

        T GetValue<T>(IXLRow row, int columnIndex)
        {
            var val = row.Cell(columnIndex + 1).Value;
            return (T)Convert.ChangeType(val, typeof(T));
        }
        T GetValue<T>(IXLRow row, List<string> columnNames, string columnName)
        {
            var val = row.Cell(columnNames.IndexOf(columnName) + 1).Value;
            return (T)Convert.ChangeType(val, typeof(T));
        }

        class SchoolBase
        {
            public string Form { get; set; } = string.Empty;
            public string OrganisationId { get; set; } = string.Empty;
            public string Region { get; set; } = string.Empty;
            public string Municipality { get; set; } = string.Empty;
            public string ZipCode { get; set; } = string.Empty;
            public string City { get; set; } = string.Empty;

            public string SchoolUnitId { get; set; } = string.Empty;
            public string Name { get; set; } = string.Empty;
            public string Owner { get; set; } = string.Empty;

            public string Uri { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
            public string PedagogyType { get; set; } = string.Empty;

            public SchoolType Category { get; set; }
            // LÄN	KOMMUN	ADRESS	BESÖKSADRESS	BESÖKSPOSTNR	BESÖKSPOSTORT	TELENR	REKTORS NAMN

            public override string ToString()
            {
                return $"{Name} {Region} {Municipality}";
            }
        }
        class BasicSchool : SchoolBase
        {
            public List<int> Years { get; set; } = new();
        }

        class Highschool : SchoolBase
        {
            public List<string> Programs { get; set; } = new();
        }

        public enum SchoolType
        {
            School,
            Irregular,
            Sami,
            Special,
            Highschool,
            HighschoolIrregular,
        }

    }
}
