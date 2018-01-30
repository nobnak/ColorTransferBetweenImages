using nobnak.Gist.Primitive;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ColorTransferBetweenImagesSystem {

    public static class ImageStats {
        public const int BIT_DENSITY = 255;
        public const float BLACK_POINT = 1f / (255 * 1000);
        public const float BLACK_THRESHOLD = 0.5f / BIT_DENSITY;

        public static readonly DefferedMatrix RGB_TO_LMS = new Matrix4x4(
            new Vector4(0.3811f, 0.1967f, 0.0241f, 0f),
            new Vector4(0.5783f, 0.7244f, 0.1288f, 0f),
            new Vector4(0.0402f, 0.0782f, 0.8444f, 0f),
            new Vector4(0f, 0f, 0f, 1f)
        );
        public static readonly DefferedMatrix LOG_LMS_TO_LAB =
            Matrix4x4.Scale(new Vector3(1f / Mathf.Sqrt(3), 1f / Mathf.Sqrt(6), 1f / Mathf.Sqrt(2)))
            * new Matrix4x4(
                new Vector4(1, 1, 1, 0),
                new Vector4(1, 1, -1, 0),
                new Vector4(1, -2, 0, 0),
                new Vector4(0, 0, 0, 1)
        );
        public static readonly float MIN_L = Mathf.Min(Mathf.Min(
            new Vector3(BLACK_THRESHOLD, 0, 0).RGBToLMS().LMSToLogLMS().LogLMSToLAB().x,
            new Vector3(0, BLACK_THRESHOLD, 0).RGBToLMS().LMSToLogLMS().LogLMSToLAB().x),
            new Vector3(0, 0, BLACK_THRESHOLD).RGBToLMS().LMSToLogLMS().LogLMSToLAB().x);

        #region Color <-> Clamped RGB
        public static Color RGBToColor(this Vector3 v) {
            return new Color(v.x, v.y, v.z);
        }
        public static Vector3 ColorToRGB(this Color c) {
            return new Vector3(
                    Mathf.Clamp(c.r, BLACK_POINT, 1f),
                    Mathf.Clamp(c.g, BLACK_POINT, 1f),
                    Mathf.Clamp(c.b, BLACK_POINT, 1f));
        }
        #endregion

        #region RGB <-> LMS
        public static Vector3 RGBToLMS(this Vector3 rgb) {
            return RGB_TO_LMS.TransformVector(rgb);
        }
        public static Vector3 LMSToRGB(this Vector3 lms) {
                return RGB_TO_LMS.InverseTransformVector(lms);
        }
        #endregion

        #region LSM <-> Log LMS
        public static Vector3 LMSToLogLMS(this Vector3 lms) {
            return new Vector3(Mathf.Log10(lms.x), Mathf.Log10(lms.y), Mathf.Log10(lms.z));
        }
        public static Vector3 LogLMSToLMS(this Vector3 lmsLog) {
                return new Vector3(
                    Mathf.Pow(10f, lmsLog.x),
                    Mathf.Pow(10f, lmsLog.y),
                    Mathf.Pow(10f, lmsLog.z));
        }
        #endregion

        #region Log LMS <-> LAB
        public static Vector3 LogLMSToLAB(this Vector3 logLms) {
                return LOG_LMS_TO_LAB.TransformVector(logLms);
        }
        public static Vector3 LABToLogLMS(this Vector3 lab) {
                return LOG_LMS_TO_LAB.InverseTransformVector(lab);
        }
        #endregion

        #region Color <-> LAB
        public static Vector3 ColorToLAB(this Color color) {
            return LogLMSToLAB(LMSToLogLMS(RGBToLMS(ColorToRGB(color))));
        }
        public static Color LABToColor(this Vector3 lab) {
            return RGBToColor(LMSToRGB(LogLMSToLMS(LABToLogLMS(lab))));
        }
        #endregion

        public static void CompileLABStats(this IEnumerable<Vector3> labIter, 
            out Vector3 average, out Vector3 sd) {

            average = Vector3.zero;
            var variance = Vector3.zero;
            var counter = 0;
            foreach (var lab in labIter) {
                if (lab.x < MIN_L)
                    continue;

                average += lab;
                variance += Vector3.Scale(lab, lab);
                counter++;
            }
            average /= counter;
            variance = variance / counter - Vector3.Scale(average, average);
            sd = new Vector3(Mathf.Sqrt(variance.x), Mathf.Sqrt(variance.y), Mathf.Sqrt(variance.z));
        }
        public static IEnumerable<Vector3> Convert(this IEnumerable<Vector3> inputLabIter,
            Vector3 inputAverage, Vector3 inputSD, Vector3 outputAverage, Vector3 outputSD) {

            var sdRatio = new Vector3(outputSD.x / inputSD.x, outputSD.y / inputSD.y, outputSD.z / inputSD.z);
            foreach (var lab in inputLabIter) {
                var outputLab = lab - inputAverage;
                outputLab = Vector3.Scale(sdRatio, outputLab);
                outputLab += outputAverage;
                yield return outputLab;
            }
        }
    }
}
