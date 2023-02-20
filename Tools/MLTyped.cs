using Microsoft.ML.Data;
using Microsoft.ML;

namespace Tools
{
    internal class MLTyped
    {
        public void Test(string trainDataPath, string testDataPath)
        {
            var ctx = new MLContext(seed: 0);

            var model = Train(ctx, trainDataPath);
            var metrics = Evaluate(ctx, model, testDataPath);
            Console.WriteLine($"RSquared Score: {metrics.RSquared:0.##}, Root Mean Squared Error: {metrics.RootMeanSquaredError:#.##}");

            var prediction = TestSinglePrediction(ctx, model, new TaxiTrip()
            {
                VendorId = "VTS",
                RateCode = "1",
                PassengerCount = 1,
                TripTime = 1140,
                TripDistance = 3.75f,
                PaymentType = "CRD",
                FareAmount = 0 // To predict. Actual/Observed = 15.5
            });
            Console.WriteLine($"Predicted fare: {prediction:0.####}, actual fare: 15.5");
        }

        ITransformer Train(MLContext mlContext, string dataPath)
        {
            var pipeline = mlContext.Transforms.CopyColumns(outputColumnName: "Label", inputColumnName: "FareAmount")
                .Append(mlContext.Transforms.Categorical.OneHotEncoding(outputColumnName: "VendorIdEncoded", inputColumnName: "VendorId"))
                .Append(mlContext.Transforms.Categorical.OneHotEncoding(outputColumnName: "RateCodeEncoded", inputColumnName: "RateCode"))
                .Append(mlContext.Transforms.Categorical.OneHotEncoding(outputColumnName: "PaymentTypeEncoded", inputColumnName: "PaymentType"))
                .Append(mlContext.Transforms.Concatenate("Features", "VendorIdEncoded", "RateCodeEncoded", "PassengerCount", "TripDistance", "PaymentTypeEncoded"))
                .Append(mlContext.Regression.Trainers.FastTree());

            return pipeline.Fit(LoadFromCsv(mlContext, dataPath));
        }

        IDataView LoadFromCsv(MLContext mlContext, string filename)
        {
            //return mlContext.Data.LoadFromTextFile(dataPath, new TextLoader.Column[] { new TextLoader.Column() }, hasHeader: true, separatorChar: ',');
            return mlContext.Data.LoadFromTextFile<TaxiTrip>(filename, hasHeader: true, separatorChar: ',');
            //System.Globalization.CultureInfo.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;
            //return DataFrame.LoadCsv(filename, guessRows: 20);
        }

        RegressionMetrics Evaluate(MLContext mlContext, ITransformer model, string testDataPath)
        {
            var dataView = LoadFromCsv(mlContext, testDataPath); // mlContext.Data.LoadFromTextFile<TaxiTrip>(testDataPath, hasHeader: true, separatorChar: ',');
            var predictions = model.Transform(dataView);
            return mlContext.Regression.Evaluate(predictions, "Label", "Score");
        }

        float TestSinglePrediction(MLContext mlContext, ITransformer model, TaxiTrip item)
        {
            var predictionFunction = mlContext.Model.CreatePredictionEngine<TaxiTrip, TaxiTripFarePrediction>(model);
            var prediction = predictionFunction.Predict(item);
            return prediction.FareAmount;
        }

        public class TaxiTrip
        {
            [LoadColumn(0)]
            public string VendorId = "";

            [LoadColumn(1)]
            public string RateCode = "";

            [LoadColumn(2)]
            public float PassengerCount;

            [LoadColumn(3)]
            public float TripTime;

            [LoadColumn(4)]
            public float TripDistance;

            [LoadColumn(5)]
            public string PaymentType = "";

            [LoadColumn(6)]
            public float FareAmount;
        }

        public class TaxiTripFarePrediction
        {
            [ColumnName("Score")]
            public float FareAmount = 0f;
        }
    }
}
