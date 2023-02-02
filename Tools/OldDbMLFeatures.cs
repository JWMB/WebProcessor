using Microsoft.EntityFrameworkCore;
using Microsoft.ML;
using Microsoft.ML.Data;
using OldDb.Models;
using OldDbAdapter;
using ProblemSource.Models;
using ProblemSource.Models.Aggregates;
using ProblemSource.Services;

namespace Tools
{
    internal class OldDbMLFeatures
    {
        public async Task<string[]> CreateFeaturesForTraining(int trainingId)
        {
            var dbContext = new TrainingDbContext();
            var logItems = await RecreateLogFromOldDb.GetAsLogItems(dbContext, trainingId, Enumerable.Range(0, 6));

            var phases = LogEventsToPhases.Create(logItems);
            var features = MLFeaturesJulia.FromPhases(new TrainingSettings(), phases.PhasesCreated, 6, null, 5);
            return features.ToArray();
        }

        public async Task X()
        {
            // TODO: some criteria
            var trainingIds = new List<int>();

            var features = new List<string[]>();
            foreach (var id in trainingIds)
            {
                features.Add(await CreateFeaturesForTraining(id));
            }

        }

        public void TutorialTest()
        {
            var path = @"C:\Users\uzk446\Downloads\";
            var trainDataPath = Path.Join(path, "taxi-fare-train.csv");
            var testDataPath = Path.Join(path, "taxi-fare-test.csv");
            var mlContext = new MLContext(seed: 0);

            var model = Train(mlContext, trainDataPath);
            Evaluate(mlContext, model, testDataPath);

            TestSinglePrediction(mlContext, model, new TaxiTrip()
            {
                VendorId = "VTS",
                RateCode = "1",
                PassengerCount = 1,
                TripTime = 1140,
                TripDistance = 3.75f,
                PaymentType = "CRD",
                FareAmount = 0 // To predict. Actual/Observed = 15.5
            });
        }

        ITransformer Train(MLContext mlContext, string dataPath)
        {
            //var dataView = mlContext.Data.LoadFromTextFile<TaxiTrip>(dataPath, hasHeader: true, separatorChar: ',');
            var dataView = mlContext.Data.LoadFromTextFile(dataPath, new TextLoader.Column[] { new TextLoader.Column() }, hasHeader: true, separatorChar: ',');
            var pipeline = mlContext.Transforms.CopyColumns(outputColumnName: "Label", inputColumnName: "FareAmount")
                .Append(mlContext.Transforms.Categorical.OneHotEncoding(outputColumnName: "VendorIdEncoded", inputColumnName: "VendorId"))
                .Append(mlContext.Transforms.Categorical.OneHotEncoding(outputColumnName: "RateCodeEncoded", inputColumnName: "RateCode"))
                .Append(mlContext.Transforms.Categorical.OneHotEncoding(outputColumnName: "PaymentTypeEncoded", inputColumnName: "PaymentType"))
                .Append(mlContext.Transforms.Concatenate("Features", "VendorIdEncoded", "RateCodeEncoded", "PassengerCount", "TripDistance", "PaymentTypeEncoded"))
                .Append(mlContext.Regression.Trainers.FastTree());

            var model = pipeline.Fit(dataView);
            return model;
        }

        void Evaluate(MLContext mlContext, ITransformer model, string testDataPath)
        {
            var dataView = mlContext.Data.LoadFromTextFile<TaxiTrip>(testDataPath, hasHeader: true, separatorChar: ',');
            var predictions = model.Transform(dataView);
            var metrics = mlContext.Regression.Evaluate(predictions, "Label", "Score");

            Console.WriteLine($"RSquared Score: {metrics.RSquared:0.##}, Root Mean Squared Error: {metrics.RootMeanSquaredError:#.##}");
        }

        void TestSinglePrediction(MLContext mlContext, ITransformer model, TaxiTrip item)
        {
            var predictionFunction = mlContext.Model.CreatePredictionEngine<TaxiTrip, TaxiTripFarePrediction>(model);
            var prediction = predictionFunction.Predict(item);

            Console.WriteLine($"Predicted fare: {prediction.FareAmount:0.####}, actual fare: 15.5");
        }

        public class TaxiTrip
        {
            [LoadColumn(0)]
            public string VendorId;

            [LoadColumn(1)]
            public string RateCode;

            [LoadColumn(2)]
            public float PassengerCount;

            [LoadColumn(3)]
            public float TripTime;

            [LoadColumn(4)]
            public float TripDistance;

            [LoadColumn(5)]
            public string PaymentType;

            [LoadColumn(6)]
            public float FareAmount;
        }

        public class TaxiTripFarePrediction
        {
            [ColumnName("Score")]
            public float FareAmount;
        }
    }
}
