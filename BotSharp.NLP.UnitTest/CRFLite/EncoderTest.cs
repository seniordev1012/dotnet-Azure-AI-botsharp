using BotSharp.Models.CRFLite;
using BotSharp.Models.CRFLite.Decoder;
using BotSharp.Models.CRFLite.Encoder;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace BotSharp.NLP.UnitTest.CRFLite
{
    [TestClass]
    public class EncoderTest
    {
        /// <summary>
        /// 
        /// </summary>
        [TestMethod]
        public void TestEncode()
        {
            var encoder = new CRFEncoder();
            bool result = encoder.Learn(new EncoderOptions
            {
                /*
                 * traing corups format, split by tab, sentences is seperated by blank row
                 * 
                    ! PUN S
                    Tokyo NNP S_LOCATION
                    and	CC S
                    New	NNP	B_LOCATION
                    York NNP	E_LOCATION
                    are	VBP	S
                    major JJ S
                    financial JJ S
                    centers	NNS	S
                    . PUN S
                 */
                TrainingCorpusFileName = @"C:\Users\haipi\Documents\Projects\BotSharp\Data\CRF\eng.1k.training",
                TemplateFileName = @"C:\Users\haipi\Documents\Projects\BotSharp\Data\CRF\template.en",
                ModelFileName = @"C:\Users\haipi\Documents\Projects\BotSharp\Data\CRF\ner_model"
            });

            Assert.IsTrue(result);
        }

        object rdLocker = new object();

        [TestMethod]
        public void TestDecode()
        {
            var decoder = new CRFDecoder();
            var options = new DecoderOptions
            {
                ModelFileName = @"C:\Users\haipi\Documents\Projects\BotSharp\Data\CRF\ner_model"
            };

            //Load encoded model from file
            decoder.LoadModel(options.ModelFileName);

            //Create decoder tagger instance.
            var tagger = decoder.CreateTagger(options.NBest, options.MaxWord);
            tagger.set_vlevel(options.ProbLevel);

            //Initialize result
            var crf_out = new CRFSegOut[options.NBest];
            for (var i = 0; i < options.NBest; i++)
            {
                crf_out[i] = new CRFSegOut(options.MaxWord);
            }

            var dataset = GetTestData();

            //predict given string's tags
            decoder.Segment(crf_out, tagger, dataset);
        }

        private List<List<string>> GetTestData()
        {
            var dataset = new List<List<string>>();

            dataset.Add(new List<string> { "'", "PUN" });
            dataset.Add(new List<string> { "'", "POS" });
            dataset.Add(new List<string> { "Duchy", "NNP" });
            dataset.Add(new List<string> { "of", "IN" });
            dataset.Add(new List<string> { "Lithuania", "NNP" });

            return dataset;
        }
    }
}
