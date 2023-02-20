using System.Data;

namespace ProblemSourceModule.Models.Aggregates.ML
{
    public class ColumnInfo
    {
        public string Label { get; set; } = string.Empty;
        public string? UserId { get; set; }
        public IEnumerable<string>? Categorical { get; set; }
        public IEnumerable<string>? Ignore { get; set; }

        public static ColumnInfo Create(Type type)
        {
            var columnTypePerProperty = type
                .GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)
                .ToDictionary(o => o.Name, o => (Attribute.GetCustomAttribute(o, typeof(ColumnTypeAttribute)) as ColumnTypeAttribute)?.Type);

            return new ColumnInfo
            {
                Label = columnTypePerProperty.Single(o => o.Value == ColumnType.Label).Key,
                Categorical = columnTypePerProperty.Where(o => o.Value == ColumnType.Categorical).Select(o => o.Key),
                Ignore = columnTypePerProperty.Where(o => o.Value == ColumnType.Ignored).Select(o => o.Key),
                UserId = columnTypePerProperty.SingleOrDefault(o => o.Value == ColumnType.UserId).Key,
            };
        }
    }
}
