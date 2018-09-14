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
        [TestMethod]
        public void TestEncode()
        {
            var encoder = new CRFEncoder();
            bool result = encoder.Learn(new EncoderOptions
            {
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
                InputFileName = @"C:\Users\haipi\Documents\Projects\BotSharp\Data\CRF\test.txt",
                OutputSegFileName = @"C:\Users\haipi\Documents\Projects\BotSharp\Data\CRF\test.seg.txt",
                OutputFileName = @"C:\Users\haipi\Documents\Projects\BotSharp\Data\CRF\test.output.txt",
                ModelFileName = @"C:\Users\haipi\Documents\Projects\BotSharp\Data\CRF\ner_model"
            };

            var sr = new StreamReader(options.InputFileName);
            StreamWriter sw = null, swSeg = null;

            if (options.OutputFileName != null && options.OutputFileName.Length > 0)
            {
                sw = new StreamWriter(options.OutputFileName);
            }
            if (options.OutputSegFileName != null && options.OutputSegFileName.Length > 0)
            {
                swSeg = new StreamWriter(options.OutputSegFileName);
            }

            //Load encoded model from file
            decoder.LoadModel(options.ModelFileName);

            var queueRecords = new ConcurrentQueue<List<List<string>>>();
            var queueSegRecords = new ConcurrentQueue<List<List<string>>>();

            var parallelOption = new ParallelOptions();
            parallelOption.MaxDegreeOfParallelism = options.Thread;
            Parallel.For(0, options.Thread, parallelOption, t =>
            {

                //Create decoder tagger instance. If the running environment is multi-threads, each thread needs a separated instance
                var tagger = decoder.CreateTagger(options.NBest, options.MaxWord);
                tagger.set_vlevel(options.ProbLevel);

                //Initialize result
                var crf_out = new crf_seg_out[options.NBest];
                for (var i = 0; i < options.NBest; i++)
                {
                    crf_out[i] = new crf_seg_out(tagger.crf_max_word_num);
                }

                var inbuf = new List<List<string>>();
                while (true)
                {
                    lock (rdLocker)
                    {
                        if (ReadRecord(inbuf, sr) == false)
                        {
                            break;
                        }

                        queueRecords.Enqueue(inbuf);
                        queueSegRecords.Enqueue(inbuf);
                    }

                    //Call CRFSharp wrapper to predict given string's tags
                    if (swSeg != null)
                    {
                        decoder.Segment(crf_out, tagger, inbuf);
                    }
                    else
                    {
                        decoder.Segment((CRFTermOut[])crf_out, (DecoderTagger)tagger, inbuf);
                    }

                    List<List<string>> peek = null;
                    //Save segmented tagged result into file
                    if (swSeg != null)
                    {
                        var rstList = ConvertCRFTermOutToStringList(inbuf, crf_out);
                        while (peek != inbuf)
                        {
                            queueSegRecords.TryPeek(out peek);
                        }
                        for (int index = 0; index < rstList.Count; index++)
                        {
                            var item = rstList[index];
                            swSeg.WriteLine(item);
                        }
                        queueSegRecords.TryDequeue(out peek);
                        peek = null;
                    }

                    //Save raw tagged result (with probability) into file
                    if (sw != null)
                    {
                        while (peek != inbuf)
                        {
                            queueRecords.TryPeek(out peek);
                        }
                        OutputRawResultToFile(inbuf, crf_out, tagger, sw);
                        queueRecords.TryDequeue(out peek);

                    }
                }
            });


            sr.Close();

            if (sw != null)
            {
                sw.Close();
            }
            if (swSeg != null)
            {
                swSeg.Close();
            }
        }

        private bool ReadRecord(List<List<string>> inbuf, StreamReader sr)
        {
            inbuf.Clear();

            while (true)
            {
                var strLine = sr.ReadLine();
                if (strLine == null)
                {
                    //At the end of current file
                    if (inbuf.Count == 0)
                    {
                        return false;
                    }
                    else
                    {
                        return true;
                    }
                }
                strLine = strLine.Trim();
                if (strLine.Length == 0)
                {
                    return true;
                }

                //Read feature set for each record
                var items = strLine.Split(new char[] { '\t' });
                inbuf.Add(new List<string>());
                for (int index = 0; index < items.Length; index++)
                {
                    var item = items[index];
                    inbuf[inbuf.Count - 1].Add(item);
                }
            }
        }

        private void OutputRawResultToFile(List<List<string>> inbuf, CRFTermOut[] crf_out, SegDecoderTagger tagger, StreamWriter sw)
        {
            for (var k = 0; k < crf_out.Length; k++)
            {
                if (crf_out[k] == null)
                {
                    //No more result
                    break;
                }

                var sb = new StringBuilder();

                var crf_seg_out = crf_out[k];
                //Show the entire sequence probability
                //For each token
                for (var i = 0; i < inbuf.Count; i++)
                {
                    //Show all features
                    for (var j = 0; j < inbuf[i].Count; j++)
                    {
                        sb.Append(inbuf[i][j]);
                        sb.Append("\t");
                    }

                    //Show the best result and its probability
                    sb.Append(crf_seg_out.result_[i]);

                    if (tagger.vlevel_ > 1)
                    {
                        sb.Append("\t");
                        sb.Append(crf_seg_out.weight_[i]);

                        //Show the probability of all tags
                        sb.Append("\t");
                        for (var j = 0; j < tagger.ysize_; j++)
                        {
                            sb.Append(tagger.yname(j));
                            sb.Append("/");
                            sb.Append(tagger.prob(i, j));

                            if (j < tagger.ysize_ - 1)
                            {
                                sb.Append("\t");
                            }
                        }
                    }
                    sb.AppendLine();
                }
                if (tagger.vlevel_ > 0)
                {
                    sw.WriteLine("#{0}", crf_seg_out.prob);
                }
                sw.WriteLine(sb.ToString().Trim());
                sw.WriteLine();
            }
        }

        private List<string> ConvertCRFTermOutToStringList(List<List<string>> inbuf, crf_seg_out[] crf_out)
        {
            var sb = new StringBuilder();
            for (var i = 0; i < inbuf.Count; i++)
            {
                sb.Append(inbuf[i][0]);
            }

            var strText = sb.ToString();
            var rstList = new List<string>();
            for (var i = 0; i < crf_out.Length; i++)
            {
                if (crf_out[i] == null)
                {
                    //No more result
                    break;
                }

                sb.Clear();
                var crf_term_out = crf_out[i];
                for (var j = 0; j < crf_term_out.Count; j++)
                {
                    var str = strText.Substring(crf_term_out.tokenList[j].offset, crf_term_out.tokenList[j].length);
                    var strNE = crf_term_out.tokenList[j].strTag;

                    sb.Append(str);
                    if (strNE.Length > 0)
                    {
                        sb.Append("[" + strNE + "]");
                    }
                    sb.Append(" ");
                }
                rstList.Add(sb.ToString().Trim());
            }

            return rstList;
        }
    }
}
