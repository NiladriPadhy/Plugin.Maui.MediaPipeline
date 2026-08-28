namespace Plugin.Maui.MediaPipeline;

abstract record PipelineStep(MediaPipelineStage Stage);

record ResizeMaxStep(int MaxDimension) : PipelineStep(MediaPipelineStage.Resize);

record ResizeBoxStep(int Width, int Height, ResizeMode Mode) : PipelineStep(MediaPipelineStage.Resize);

record CompressStep(int Quality) : PipelineStep(MediaPipelineStage.Compress);

record FormatStep(MediaFormat Format) : PipelineStep(MediaPipelineStage.Compress);

record RemoveExifStep() : PipelineStep(MediaPipelineStage.RemoveExif);

record CorrectOrientationStep() : PipelineStep(MediaPipelineStage.Orientation);

record KeepOrientationStep() : PipelineStep(MediaPipelineStage.Orientation);

record TextWatermarkStep(string Text, WatermarkOptions Options) : PipelineStep(MediaPipelineStage.Watermark);

record ImageWatermarkStep(byte[] Image, WatermarkOptions Options) : PipelineStep(MediaPipelineStage.Watermark);

record BlurStep(MediaRegion Region, float Sigma) : PipelineStep(MediaPipelineStage.Blur);

record RedactStep(MediaRegion Region, MediaColor Color) : PipelineStep(MediaPipelineStage.Redact);

record MaxBytesStep(int MaxBytes) : PipelineStep(MediaPipelineStage.Compress);

record EncryptStep(byte[]? Key) : PipelineStep(MediaPipelineStage.Encrypt);
