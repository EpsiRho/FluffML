using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.ML.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FluffML
{
    public class ModelInput
    {
        [ColumnName(@"Label")]
        public string Label { get; set; }

        [ColumnName(@"ImageSource")]
        public byte[] ImageSource { get; set; }

    }


    public class ImagePrediction : ImageData
    {
        public float[] Score;

        public string PredictedLabelValue;

    }

    public class ImageData
    {
        [LoadColumn(0)]
        public string ImagePath;

        [LoadColumn(1)]
        public string Label;
    }

    struct InceptionSettings
    {
        public const int ImageHeight = 224;
        public const int ImageWidth = 224;
        public const float Mean = 117;
        public const float Scale = 1;
        public const bool ChannelsLast = true;
    }
}
