namespace ProblemSourceModule.Models.Aggregates.ML
{
    public class ColumnTypeAttribute : Attribute
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

        public ColumnType Type { get; private set; }

        public ColumnTypeAttribute(ColumnType type)
        {
            Type = type;
        }
    }
}
