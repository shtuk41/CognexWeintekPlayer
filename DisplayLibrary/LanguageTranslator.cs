// <copyright>
// Copyright © Carl Zeiss OIM GmbH 2008-2014 All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

using log4net;

namespace CognexPlayer
{
    internal class LanguageTranslator
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(LanguageTranslator));

        private string Location 
        { 
            get 
            { 
                return Path.GetDirectoryName(Assembly.GetEntryAssembly().Location); 
            } 
        }

        private string DictionaryFile 
        { 
            get 
            { 
                return Path.Combine(Location, "Dictionary.csv");
            }
        }

        private static readonly LanguageTranslator LanguageTranslatorInstance = new LanguageTranslator();

        private Dictionary<string, byte[]> byteDictionary;
   
        private LanguageTranslator()
        {
            FillDictionary();
        }

        public string Translate(string toTranslate)
        {
            byte[] translated = Encoding.Unicode.GetBytes("UTT");

            toTranslate = Regex.Replace(toTranslate, @"\t|\r|\n", string.Empty);

            if (new Regex(@"Scan a part\d+ remaining.").IsMatch(toTranslate))
            {
                Regex pattern = new Regex(@"Scan a part(?<partNumber>\d+) remaining.");
                Match match = pattern.Match(toTranslate);
                List<byte>  t = new List<byte>(Encoding.Unicode.GetBytes("扫描部件, 还有"));
                t.AddRange(Encoding.Unicode.GetBytes(match.Groups["partNumber"].Value));
                translated = t.ToArray();
            }
            else if (new Regex(@"Now scan the cartridge for slot\d+").IsMatch(toTranslate))
            {
                Regex pattern = new Regex(@"Now scan the cartridge for slot(?<slot>\d+)");
                Match match = pattern.Match(toTranslate);
                List<byte> t = new List<byte>(Encoding.Unicode.GetBytes("现在扫描卡盒插槽"));
                t.AddRange(Encoding.Unicode.GetBytes(match.Groups["slot"].Value));
                translated = t.ToArray();
            }
            else if (new Regex(@"Now scan the cartridge for slot \d+").IsMatch(toTranslate))
            {
                Regex pattern = new Regex(@"Now scan the cartridge for slot (?<slot>\d+)");
                Match match = pattern.Match(toTranslate);
                List<byte> t = new List<byte>(Encoding.Unicode.GetBytes("现在扫描卡盒插槽"));
                t.AddRange(Encoding.Unicode.GetBytes(match.Groups["slot"].Value));
                translated = t.ToArray();
            }
            else if (new Regex(@"Part \d+ passedScan part \d+ to unload it.").IsMatch(toTranslate))
            {
                Regex pattern = new Regex(@"Part (?<first>\d+) passedScan part (?<second>\d+) to unload it.");
                Match match = pattern.Match(toTranslate);
                List<byte> t = new List<byte>(Encoding.Unicode.GetBytes("部件"));
                t.AddRange(Encoding.Unicode.GetBytes(match.Groups["first"].Value));
                t.AddRange(Encoding.Unicode.GetBytes("通过,扫描部件"));
                t.AddRange(Encoding.Unicode.GetBytes(match.Groups["second"].Value));
                translated = t.ToArray();
            }
            else if (new Regex(@"Part \d+ failedScan part \d+ to unload it.").IsMatch(toTranslate))
            {
                Regex pattern = new Regex(@"Part (?<first>\d+) failedScan part (?<second>\d+) to unload it.");
                Match match = pattern.Match(toTranslate);
                List<byte> t = new List<byte>(Encoding.Unicode.GetBytes("部件"));
                t.AddRange(Encoding.Unicode.GetBytes(match.Groups["first"].Value));
                t.AddRange(Encoding.Unicode.GetBytes("通过,扫描部件"));
                t.AddRange(Encoding.Unicode.GetBytes(match.Groups["second"].Value));
                translated = t.ToArray();
            }
            else if (new Regex(@"Scan a fixture prior to evaluationWorking on:w*").IsMatch(toTranslate))
            {
                translated = Encoding.Unicode.GetBytes("先扫描夹具");
            }
            else if (new Regex(@"Scan part \d+").IsMatch(toTranslate))
            {
                Regex pattern = new Regex(@"Scan part (?<part>\d+)");
                Match match = pattern.Match(toTranslate);
                List<byte> t = new List<byte>(Encoding.Unicode.GetBytes("扫描部件"));
                t.AddRange(Encoding.Unicode.GetBytes(match.Groups["part"].Value));
                translated = t.ToArray();
            }
            else if (new Regex(@"Part \d+ passedScan the cartridge again to see summary").IsMatch(toTranslate))
            {
                translated = Encoding.Unicode.GetBytes("扫描卡盒看总结");
            }
            else if (new Regex(@"\d+ Fails.Yes\? Scan next cart to unload.No\? Press 'abort'").IsMatch(toTranslate))
            {
                Regex pattern = new Regex(@"(?<part>\d+) Fails.Yes\? Scan next cart to unload.No\? Press 'abort'");
                Match match = pattern.Match(toTranslate);
                List<byte> t = new List<byte>(Encoding.Unicode.GetBytes(match.Groups["part"].Value));
                t.AddRange(Encoding.Unicode.GetBytes(@"失败.扫描下一卡盒继续"));
                translated = t.ToArray();
                ////33 Fails.Yes? Scan next cart to unload.No? Press 'abort'
            }
            else if  (new Regex(@"\d+ fails. Put parts in bins. Scan next cartridge to unload or press ok if no next cartridge.").IsMatch(toTranslate))
            {
                Regex pattern = new Regex(@"(?<part>\d+) fails. Put parts in bins. Scan next cartridge to unload or press ok if no next cartridge.");
                Match match = pattern.Match(toTranslate);
                List<byte> t = new List<byte>(Encoding.Unicode.GetBytes(match.Groups["part"].Value));
                t.AddRange(Encoding.Unicode.GetBytes(@"失败.扫描下一卡盒继续"));
                translated = t.ToArray();
            }
            else if (byteDictionary.ContainsKey(toTranslate) == true)
            {
                translated = byteDictionary[toTranslate];
            }
            else
            {
                if (!string.IsNullOrEmpty(toTranslate))
                {
                    log.Debug("Missing Message:" + toTranslate + "Missing Message End");
                }
            }

            return System.Text.Encoding.Unicode.GetString(translated);
        }

        public static LanguageTranslator Instance
        {
            get 
            {
                return LanguageTranslatorInstance; 
            }
        }

        private void FillDictionary()
        {
            try
            {
                string line;
                using (StreamReader file = new StreamReader(DictionaryFile))
                {
                    byteDictionary = new Dictionary<string, byte[]>();
                    while ((line = file.ReadLine()) != null)
                    {
                        string[] text = line.Split('|');

                        if (text.Length < 2)
                        {
                            throw new DisplayException("Dictionary file has invalid format");
                        }

                        string key = text[0];
                        byte[] val = Encoding.Unicode.GetBytes(text[1]);

                        byteDictionary.Add(key, val);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Unable to read file {0} located in {1}; Error is {2}", DictionaryFile, Path.GetFullPath(DictionaryFile), e.Message);
            }
        }

        #region Fill Dictionary using characters hex values

        private void FillDictionaryHex()
        {
            try
            {
                string line;
                using (StreamReader file = new StreamReader(DictionaryFile))
                {
                    byteDictionary = new Dictionary<string, byte[]>();
                    while ((line = file.ReadLine()) != null)
                    {
                        string[] text = line.Split(',');

                        string key = text[0];

                        if (text.Length >= 2)
                        {
                            byte[] val = new byte[(text.Length - 1) * 2];

                            for (int ii = 1; ii < text.Length; ii++)
                            {
                                val[(ii * 2) - 2] = byte.Parse(text[ii].Substring(0, 2), System.Globalization.NumberStyles.HexNumber);
                                val[(ii * 2) - 1] = byte.Parse(text[ii].Substring(2, 2), System.Globalization.NumberStyles.HexNumber);
                            }

                            byteDictionary.Add(key, val);
                        }
                        else
                        {
                            byteDictionary.Add(key, new byte[2] { 0x00, 0x32 });
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Unable to read file {0} located in {1}; Error is {2}", DictionaryFile, Path.GetFullPath(DictionaryFile), e.Message);
            }
        }

        #endregion
    }
}
