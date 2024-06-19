using System;
using UnityEngine;

namespace TensorFlowLite
{
    /// <summary>
    /// EfficientDet
    /// Object Detection
    /// 
    /// License: Apache-2.0
    /// See https://www.kaggle.com/models/tensorflow/efficientdet/tfLite/lite0-detection-default
    /// </summary>
    public sealed class EfficientDet : BaseVisionTask
    {
        [Serializable]
        public class Options
        {
            [FilePopup("*.tflite")]
            public string modelPath = string.Empty;
            public AspectMode aspectMode = AspectMode.Fit;
            public TfLiteDelegateType delegateType = TfLiteDelegateType.GPU;
        }

        public readonly struct Result
        {
            public readonly int classID;
            public readonly float score;
            public readonly Rect rect;

            public Result(int classID, float score, Rect rect)
            {
                this.classID = classID;
                this.score = score;
                this.rect = rect;
            }
        }

        const int MAX_DETECTION = 25;
        private readonly float[] outputs0 = new float[MAX_DETECTION * 4]; // MAX_DETECTION * [top, left, bottom, right]
        private readonly float[] outputs1 = new float[MAX_DETECTION]; // Classes
        private readonly float[] outputs2 = new float[MAX_DETECTION]; // Scores
        private readonly Result[] results = new Result[MAX_DETECTION];

        public EfficientDet(Options options)
        {
            var interpreterOptions = new InterpreterOptions();
            interpreterOptions.AutoAddDelegate(options.delegateType, typeof(byte));
            Load(FileUtil.LoadFile(options.modelPath), interpreterOptions);
        }

        public EfficientDet(Options options, InterpreterOptions interpreterOptions)
        {
            Load(FileUtil.LoadFile(options.modelPath), interpreterOptions);
        }

        protected override void PostProcess()
        {
            interpreter.GetOutputTensorData(0, outputs0.AsSpan());
            interpreter.GetOutputTensorData(1, outputs1.AsSpan());
            interpreter.GetOutputTensorData(2, outputs2.AsSpan());
        }

        public ReadOnlySpan<Result> GetResults()
        {
            for (int i = 0; i < MAX_DETECTION; i++)
            {
                // Invert Y to adapt Unity UI space
                int current = i * 4;
                float top = 1f - outputs0[current];
                float left = outputs0[current + 1];
                float bottom = 1f - outputs0[current + 2];
                float right = outputs0[current + 3];

                results[i] = new Result(
                    classID: (int)outputs1[i],
                    score: outputs2[i],
                    rect: new Rect(left, top, right - left, top - bottom));
            }
            return results;
        }
    }
}
