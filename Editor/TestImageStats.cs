using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace ColorTransferBetweenImagesSystem {

    public class TestImageStats {

        [Test]
        public void TestImageStatsSimplePasses() {
            System.Func<Color,string> fcolor = c => string.Format("({0:f3},{1:f3},{2:f3})", c.r, c.g, c.b);
            System.Func<Vector3, string> fvec3 = v => string.Format("({0:f3},{1:f3},{2:f3})", v.x, v.y, v.z);


            var colors = new Color[] { Color.red };
            DebugOut(colors, "In RGB : {0}", fcolor);

            DebugOut(colors.Select(c => c.ColorToRGB().RGBToLMS()), "In LMS : {0}", fvec3);

            DebugOut(colors.Select(c => c.ColorToRGB().RGBToLMS().LMSToLogLMS()),
                "In Log LMS : {0}", fvec3);

            DebugOut(colors.Select(c => c.ColorToRGB().RGBToLMS().LMSToLogLMS().LogLMSToLAB()),
                "In LAB : {0}", fvec3);
        }

        private static void DebugOut<T>(IEnumerable<T> colors, string format, System.Func<T, string> parser) {
            var buf = new StringBuilder();
            foreach (var c in colors)
                buf.AppendFormat(parser(c));
            Debug.LogFormat(format, buf.ToString());
        }
    }
}
