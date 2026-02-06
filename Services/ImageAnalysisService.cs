using System.Globalization;
using OpenCvSharp;
using Carpet_Work_Progress.ViewModels;

namespace Carpet_Work_Progress.Services;

public interface IImageAnalysisService
{
    Task<AnalyzeResultVm> AnalyzeAsync(IFormFile file);
}

public class ImageAnalysisService : IImageAnalysisService
{
    public async Task<AnalyzeResultVm> AnalyzeAsync(IFormFile file)
    {
        using var ms = new MemoryStream();
        await file.CopyToAsync(ms);
        var bytes = ms.ToArray();

        using var src = Cv2.ImDecode(bytes, ImreadModes.Color);
        if (src.Empty())
            throw new InvalidOperationException("Failed to decode image.");

        var cropRatio = EnvDouble("IMAGE_BORDER_CROP_RATIO", 0.02);
        int cropX = (int)(src.Width * cropRatio);
        int cropY = (int)(src.Height * cropRatio);
        int roiW = src.Width - 2 * cropX;
        int roiH = src.Height - 2 * cropY;

        if (roiW <= 0 || roiH <= 0)
            throw new InvalidOperationException("Image too small after border crop.");

        using var cropped = new Mat(src, new Rect(cropX, cropY, roiW, roiH));

        using var hsv = new Mat();
        Cv2.CvtColor(cropped, hsv, ColorConversionCodes.BGR2HSV);

        int redHMin1 = EnvInt("RED_H_MIN1", 0);
        int redHMax1 = EnvInt("RED_H_MAX1", 10);
        int redHMin2 = EnvInt("RED_H_MIN2", 170);
        int redHMax2 = EnvInt("RED_H_MAX2", 180);
        int redSMin = EnvInt("RED_S_MIN", 80);
        int redVMin = EnvInt("RED_V_MIN", 50);
        int blackVMax = EnvInt("BLACK_V_MAX", 60);
        int morphKernel = EnvInt("MORPH_KERNEL", 5);
        int minBlobArea = EnvInt("MIN_BLOB_AREA", 200);

        using var redMask1 = new Mat();
        using var redMask2 = new Mat();
        using var redMask = new Mat();
        Cv2.InRange(hsv,
            new Scalar(redHMin1, redSMin, redVMin),
            new Scalar(redHMax1, 255, 255),
            redMask1);
        Cv2.InRange(hsv,
            new Scalar(redHMin2, redSMin, redVMin),
            new Scalar(redHMax2, 255, 255),
            redMask2);
        Cv2.BitwiseOr(redMask1, redMask2, redMask);

        using var blackMask = new Mat();
        Cv2.InRange(hsv,
            new Scalar(0, 0, 0),
            new Scalar(180, 255, blackVMax),
            blackMask);

        using var notRed = new Mat();
        Cv2.BitwiseNot(redMask, notRed);
        Cv2.BitwiseAnd(blackMask, notRed, blackMask);

        if (morphKernel % 2 == 0) morphKernel++;
        using var kernel = Cv2.GetStructuringElement(
            MorphShapes.Ellipse, new Size(morphKernel, morphKernel));

        Cv2.MorphologyEx(redMask, redMask, MorphTypes.Close, kernel);
        Cv2.MorphologyEx(redMask, redMask, MorphTypes.Open, kernel);
        Cv2.MorphologyEx(blackMask, blackMask, MorphTypes.Close, kernel);
        Cv2.MorphologyEx(blackMask, blackMask, MorphTypes.Open, kernel);

        RemoveSmallBlobs(redMask, minBlobArea);
        RemoveSmallBlobs(blackMask, minBlobArea);

        long totalPixels = (long)roiW * roiH;
        long redPixels = Cv2.CountNonZero(redMask);
        long blackPixels = Cv2.CountNonZero(blackMask);

        decimal normalPercent = Math.Clamp(
            Math.Round((decimal)blackPixels / totalPixels * 100, 2), 0m, 100m);
        decimal otPercent = Math.Clamp(
            Math.Round((decimal)redPixels / totalPixels * 100, 2), 0m, 100m);
        decimal totalPercent = Math.Round(normalPercent + otPercent, 2);

        return new AnalyzeResultVm
        {
            NormalPercent = normalPercent,
            OtPercent = otPercent,
            TotalPercent = totalPercent
        };
    }

    private static void RemoveSmallBlobs(Mat mask, int minArea)
    {
        using var labels = new Mat();
        using var stats = new Mat();
        using var centroids = new Mat();
        int numLabels = Cv2.ConnectedComponentsWithStats(mask, labels, stats, centroids);

        for (int i = 1; i < numLabels; i++)
        {
            int area = stats.At<int>(i, (int)ConnectedComponentsTypes.Area);
            if (area >= minArea) continue;

            using var scalarMat = new Mat(labels.Size(), labels.Type(), new Scalar(i));
            using var componentMask = new Mat();
            Cv2.Compare(labels, scalarMat, componentMask, CmpType.EQ);
            mask.SetTo(new Scalar(0), componentMask);
        }
    }

    private static int EnvInt(string key, int fallback)
    {
        var val = Environment.GetEnvironmentVariable(key);
        return int.TryParse(val, out var result) ? result : fallback;
    }

    private static double EnvDouble(string key, double fallback)
    {
        var val = Environment.GetEnvironmentVariable(key);
        return double.TryParse(val, NumberStyles.Float, CultureInfo.InvariantCulture, out var result)
            ? result : fallback;
    }
}
