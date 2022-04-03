using e6API;
using Microsoft.ML;
using Microsoft.ML.Data;
using Microsoft.ML.Vision;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using static Microsoft.ML.DataOperationsCatalog;

namespace FluffML
{
    public static class MLHandler
    {
        private static ITransformer trainedModel { get; set; }

        public static void CreateModel(int Epoch)
        {
            MLContext mlContext = new MLContext();
            var images = LoadImagesFromDirectory("Downloads");
            IDataView imageData = mlContext.Data.LoadFromEnumerable(images);
            IDataView shuffledData = mlContext.Data.ShuffleRows(imageData);
            var preprocessingPipeline = mlContext.Transforms.Conversion.MapValueToKey(
                inputColumnName: "Label",
                outputColumnName: "LabelAsKey")
                .Append(mlContext.Transforms.LoadRawImageBytes(
                outputColumnName: "Image",
                imageFolder: "Downloads",
                inputColumnName: "ImagePath"));
            IDataView preProcessedData = preprocessingPipeline
                    .Fit(shuffledData)
                    .Transform(shuffledData);
            TrainTestData trainSplit = mlContext.Data.TrainTestSplit(data: preProcessedData, testFraction: 0.3);
            TrainTestData validationTestSplit = mlContext.Data.TrainTestSplit(trainSplit.TestSet);

            IDataView trainSet = trainSplit.TrainSet;
            IDataView validationSet = validationTestSplit.TrainSet;
            IDataView testSet = validationTestSplit.TestSet;

            var classifierOptions = new ImageClassificationTrainer.Options()
            {
                FeatureColumnName = "Image",
                Epoch = Epoch,
                LabelColumnName = "LabelAsKey",
                ValidationSet = validationSet,
                Arch = ImageClassificationTrainer.Architecture.ResnetV2101,
                MetricsCallback = (metrics) => Console.WriteLine(metrics),
                TestOnTrainSet = false,
                ReuseTrainSetBottleneckCachedValues = true,
                ReuseValidationSetBottleneckCachedValues = true
            };

            var trainingPipeline = mlContext.MulticlassClassification.Trainers.ImageClassification(classifierOptions)
                .Append(mlContext.Transforms.Conversion.MapKeyToValue("PredictedLabel"));

            trainedModel = trainingPipeline.Fit(trainSet);

            mlContext.Model.Save(trainedModel, trainSet.Schema, "model.zip");
        
        }

        public static string ClassifySingleImage (string filePath)
        {
            MLContext mlContext = new MLContext();
            if(trainedModel == null)
            {
                trainedModel = mlContext.Model.Load("model.zip", out DataViewSchema schema);
            }
            PredictionEngine<ModelInput, ModelOutput> predictionEngine = mlContext.Model.CreatePredictionEngine<ModelInput, ModelOutput>(trainedModel);

            ImageData data= new ImageData();
            data.ImagePath = filePath.Replace("Downloads\\", "");
            data.Label = "Liked";
            List<ImageData> images = new List<ImageData>();
            images.Add(data);

            IDataView imageData = mlContext.Data.LoadFromEnumerable(images);
            var preprocessingPipeline = mlContext.Transforms.Conversion.MapValueToKey(
                inputColumnName: "Label",
                outputColumnName: "LabelAsKey")
                .Append(mlContext.Transforms.LoadRawImageBytes(
                outputColumnName: "Image",
                imageFolder: "Downloads",
                inputColumnName: "ImagePath"));
            IDataView preProcessedData = preprocessingPipeline
                    .Fit(imageData)
                    .Transform(imageData);

            ModelInput image = mlContext.Data.CreateEnumerable<ModelInput>(preProcessedData, reuseRowObject: true).First();

            ModelOutput prediction = predictionEngine.Predict(image);

            return prediction.PredictedLabel;
        }

        private static IEnumerable<ImageData> LoadImagesFromDirectory(string folder, bool useFolderNameAsLabel = true)
        {
            var files = Directory.GetFiles(folder, "*",
                searchOption: SearchOption.AllDirectories);

            foreach (var file in files)
            {
                if ((Path.GetExtension(file) != ".jpg") && (Path.GetExtension(file) != ".png"))
                    continue;

                var label = Path.GetFileName(file);

                if (useFolderNameAsLabel)
                    label = Directory.GetParent(file).Name;
                else
                {
                    for (int index = 0; index < label.Length; index++)
                    {
                        if (!char.IsLetter(label[index]))
                        {
                            label = label.Substring(0, index);
                            break;
                        }
                    }
                }

                yield return new ImageData()
                {
                    ImagePath = file.Replace("Downloads\\",""),
                    Label = label
                };
            }
        }
    }
}
