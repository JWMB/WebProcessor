namespace ProblemSourceModule.Models.Aggregates.ML
{
    public enum ColumnType
    {
        Numeric,
        Text,
        Categorical,
        Ignored,
        ImagePath,
        Label,
        UserId
    }

    public class ColumnTypeAttribute : Attribute
    {
        public ColumnType Type { get; private set; }

        public ColumnTypeAttribute(ColumnType type)
        {
            Type = type;
        }
    }
}
