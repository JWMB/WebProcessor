namespace ProblemSource.Models.LogItems
{

    public class ErrorLogItem : LogItem
    {
        public static string _type = "ERROR";

        //timeStamp: Date;
        public string level { get; set; } = string.Empty; //error level (INFO, WARNING, ERROR, FATAL etc)
        public object[]? messages { get; set; }
        public object? exception { get; set; }
    }
}
