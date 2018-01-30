using nobnak.Gist;
using nobnak.Gist.Events;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Profiling;

namespace ColorTransferBetweenImagesSystem {

    public class ColorConverter : MonoBehaviour {
        [SerializeField] protected Texture2DEvent InputOnUpdate;
        [SerializeField] protected Texture2DEvent ReferenceOnUpdate;
        [SerializeField] protected Texture2DEvent OutputOnUpdate;

        [SerializeField] protected Texture2D inputTexture;
        [SerializeField] protected Texture2D refTexture;

        protected Texture2D outputTexture;

        #region Unity
        private void OnEnable() {
            Compile();
        }
        private void OnValidate() {
            Compile();
        }
        private void OnDisable() {
            Release();
        }
        #endregion

        private void Compile() {
            if (inputTexture == null)
                return;
            InputOnUpdate.Invoke(inputTexture);

            if (outputTexture == null
                || inputTexture.width != outputTexture.width
                || inputTexture.height != outputTexture.height) {
                Release();
                outputTexture = new Texture2D(inputTexture.width, inputTexture.height, TextureFormat.ARGB32, false);
            }


            IEnumerable<Color> outputColors;
            var inputColors = inputTexture.GetPixels();
            if (refTexture == null) {
                outputColors = inputColors;
            } else {
                ReferenceOnUpdate.Invoke(refTexture);
                var refColors = refTexture.GetPixels();
                Profiler.BeginSample("Color to LAB");
                var inputLabColors = inputColors.Select(c => c.ColorToLAB());
                Profiler.EndSample();
                var refLabColors = refColors.Select(c => c.ColorToLAB());
                Vector3 inputAverage, inputSD, outputAverage, outputSD;
                Profiler.BeginSample("Compile statistics");
                inputLabColors.CompileLABStats(out inputAverage, out inputSD);
                Profiler.EndSample();
                refLabColors.CompileLABStats(out outputAverage, out outputSD);

                Profiler.BeginSample("Convert");
                var outputLabColors = inputLabColors.Convert(inputAverage, inputSD, outputAverage, outputSD);
                Profiler.EndSample();
                outputColors = outputLabColors.Select(l => l.LABToColor());
            }

            outputTexture.SetPixels(outputColors.ToArray());
            outputTexture.Apply(false, false);

            OutputOnUpdate.Invoke(outputTexture);
        }


        private void Release() {
            ObjectDestructor.Destroy(outputTexture);
        }
    }
}
