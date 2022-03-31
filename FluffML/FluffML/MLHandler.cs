using e6API;
using Microsoft.ML;
using Microsoft.ML.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace FluffML
{
    public static class MLHandler
    {
        public static void CreateModel()
        {
            MLContext mlContext = new MLContext();
            GenerateModel(mlContext);
        }

        private static void GenerateModel(MLContext mlContext)
        {
            IEstimator<ITransformer> pipeline = mlContext.Transforms.LoadImages(outputColumnName: "input", imageFolder: "Downloads", inputColumnName: nameof(ImageData.ImagePath))
                // The image transforms transform the images into the model's expected format.
                .Append(mlContext.Transforms.ResizeImages(outputColumnName: "input", imageWidth: InceptionSettings.ImageWidth, imageHeight: InceptionSettings.ImageHeight, inputColumnName: "input"))
                .Append(mlContext.Transforms.ExtractPixels(outputColumnName: "input", interleavePixelColors: InceptionSettings.ChannelsLast, offsetImage: InceptionSettings.Mean))
                .Append(mlContext.Model.LoadTensorFlowModel("tensorflow_inception_graph.pb")
                .ScoreTensorFlowModel(outputColumnNames: new[] { "softmax2_pre_activation" }, inputColumnNames: new[] { "input" }, addBatchDimensionInput: true))
                .Append(mlContext.Transforms.Conversion.MapValueToKey(outputColumnName: "LabelKey", inputColumnName: "Label"))
                .Append(mlContext.MulticlassClassification.Trainers.LbfgsMaximumEntropy(labelColumnName: "LabelKey", featureColumnName: "softmax2_pre_activation"))
                .Append(mlContext.Transforms.Conversion.MapKeyToValue("PredictedLabelValue", "PredictedLabel"))
                .AppendCacheCheckpoint(mlContext);



            IDataView trainingData = mlContext.Data.LoadFromTextFile<ImageData>(path: @"Downloads\tags.tsv", hasHeader: false);
            ITransformer model = pipeline.Fit(trainingData);

            IDataView testData = mlContext.Data.LoadFromTextFile<ImageData>(path: @"Downloads\test-tags.tsv", hasHeader: false);
            //IDataView predictions = model.Transform(testData);

            //// Create an IEnumerable for the predictions for displaying results
            //IEnumerable<ImagePrediction> imagePredictionData = mlContext.Data.CreateEnumerable<ImagePrediction>(predictions, true);
            //
            //MulticlassClassificationMetrics metrics =
            //mlContext.MulticlassClassification.Evaluate(predictions,
            //    labelColumnName: "LabelKey",
            //    predictedLabelColumnName: "PredictedLabel");
            //
            mlContext.Model.Save(model, trainingData.Schema, "model.zip");
        }

        //static void ClassifySingleImage(MLContext mlContext, ITransformer model)
        //{
        //    var imageData = new ImageData()
        //    {
        //        ImagePath = _predictSingleImage
        //    };
        //    var predictor = mlContext.Model.CreatePredictionEngine<ImageData, ImagePrediction>(model);
        //    var prediction = predictor.Predict(imageData);
        //}

        public static string MakePrediction(string post)
        {
            // Define path to training data
            //var image = File.ReadAllBytes(path);
            if (!Directory.Exists("Downloads\\Predict"))
            {
                Directory.CreateDirectory("Downloads\\Predict");
            }

            var input = new ImageData()
            {
                ImagePath = post,
            };

            var context = new MLContext();
            var model = context.Model.Load(@"model.zip", out var _);
            var predictEngine = context.Model.CreatePredictionEngine<ImageData, ImagePrediction>(model);
            var output = predictEngine.Predict(input);

            return output.PredictedLabelValue;

        }
    }
}
