using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Web.Mvc;
using WebApplication2.Models;

namespace WebApplication2.Controllers
{
    public class HomeController : Controller
    {        
        protected static List<string> UrlList = new List<string>();
        protected static List<Tuple<string, double>> SubUrList;
        protected static List<Tuple<string, double>> UrlSetList;

        static private int count=0;

        //Find all sub links from given URL.
        protected List<string> FindAllLink(string url)
        {
            var doc = new HtmlWeb().Load(url);
            var linkTags = doc.DocumentNode.Descendants("link");
            var linkedPages = doc.DocumentNode.Descendants("a")
                                              .Select(a => a.GetAttributeValue("href", null))
                                              .Where(u => !String.IsNullOrEmpty(u));

            string[] blacklist = {"#"};
            linkedPages = linkedPages.Where(x => !blacklist.Contains(x)).ToList();

            linkedPages = linkedPages.Where(x => x.Contains("https")).ToList();

            return linkedPages.ToList();
        }

        //Calculate score between two URL.
        //Using Algorithm:::
        public double CalculateScoring(List<string> firstURL, List<string> secondURL){
            int i;
            int j;

            count = 0;

            for(i = 0; i < firstURL.Count; i++)
            {
                for(j = 0;j < secondURL.Count; j++)
                {
                    if(firstURL[i] == secondURL[j])
                    {
                        count++;
                    }
                }
            }

            double score = (double)count / (firstURL.Count + secondURL.Count -count);

            return score;
        }

        //Sort given url tags by score.  
        public void OrderGivenUrl(string url)
        {
            int count = 0;

            //File process!!
            string[] lines = System.IO.File.ReadAllLines(@"C:\Users\HakanK\source\repos\WebApplication2\WebApplication2\url.txt");

            double[] urlArray = new double[lines.Length];

            List<string> FirstUrlWord = new List<string>();
            List<string> SecondUrlWord = new List<string>();

            var FirstUrl = HTMLParse(url);
            
            foreach(var item in FirstUrl)
            {
                FirstUrlWord.Add(item.Key);
            }

            foreach (string line in lines)
            {
                HomeController.UrlList.Add(line);

                var SecondUrl = HTMLParse(line);

                foreach (var item in SecondUrl)
                {
                    SecondUrlWord.Add(item.Key);
                }

                urlArray[count] = CalculateScoring(FirstUrlWord, SecondUrlWord);

                count++;
            }

            var tuple_list = new List<Tuple<string, double>>();

            for (int i = 0; i < lines.Length; i++)
            {
                tuple_list.Add(new Tuple<string, double>(lines[i], urlArray[i]));
            }

            List<Tuple<string, double>> keylist;

            //Sorting Operation.
            keylist = tuple_list.OrderByDescending(x => x.Item2).ToList();
            SubUrList = tuple_list.OrderByDescending(x => x.Item2).ToList();

            ViewData["Searching_URL"] = url;
            ViewData["Tuple_List"] = keylist;
            ViewData["Url_Index"] = urlArray.OfType<double>().ToList();
        }

        //Get Synonyms word by Microsoft.Word Application
        public IEnumerable<string> GetSynonyms(string term)
        {
            var appWord = new Microsoft.Office.Interop.Word.Application();
            object objLanguage = Microsoft.Office.Interop.Word.WdLanguageID.wdEnglishUS;
            Microsoft.Office.Interop.Word.SynonymInfo si = appWord.get_SynonymInfo(term, ref (objLanguage));
            foreach (var meaning in (si.MeaningList as Array))
            {
                yield return meaning.ToString();
            }
            appWord.Quit(); //include this to ensure the related process (winword.exe) is correctly closed. 
            System.Runtime.InteropServices.Marshal.ReleaseComObject(appWord);
            objLanguage = null;
            appWord = null;
        }

        //Removes Html Tags from input provided
        public static string RemoveHtmlTags(string html)
        {
            string htmlRemoved = Regex.Replace(html, @"<script[^>]*>[\s\S]*?</script>|<[^>]+>| ", " ").Trim();
            string normalised = Regex.Replace(htmlRemoved, @"\s{2,}", " ");
            return normalised;
        }

        //Parsing HTML tag.Find words and their frequencies.
        public IOrderedEnumerable<IGrouping<string,string>> HTMLParse(string htmlURL)
        {
            using (WebClient client = new WebClient())
            {
                //get the page source
                string html = client.DownloadString(htmlURL).ToLower();

                //remove html elements
                html = RemoveHtmlTags(html);

                //split list into keywords by space characters
                List<string> list = html.Split(' ').ToList();

                //remove any non alphabet characters
                var onlyAlphabetRegEx = new Regex(@"^[A-z]+$");
                list = list.Where(f => onlyAlphabetRegEx.IsMatch(f)).ToList();

                //further blacklist words (greater than 2 characters, not important, etc..)
                string[] blacklist = { "a", "an", "on", "of", "or", "as", "i", "in", "is", "to", "the", "and", "for", "with", "not", "this", "that", "which", "what", "from", "so", "such", }; //add your own
                list = list.Where(x => x.Length > 2).Where(x => !blacklist.Contains(x)).ToList();

                //distict keywords by key and count, and then order by count.
                var keywords = list.GroupBy(x => x).OrderByDescending(x => x.Count());

                return keywords;                
            }
        }

        /// <summary>
        /// All Action Result Pages is given below!!!
        /// </summary>
        /// <returns></returns>

        public ActionResult Index()
        {
            return View();
        }

        public ActionResult Frequency()
        {
            return View();
        }

        public ActionResult FindFrequency(string firstURL)
        {
            ViewBag.Message = "Your page is finding frequency of the url.";

            var FirstUrl = HTMLParse(firstURL);

            List<string> FirstUrlWord = new List<string>();
            List<int> FirstUrlWordFreq = new List<int>();

            foreach (var item in FirstUrl)
            {
                FirstUrlWord.Add(item.Key);
                FirstUrlWordFreq.Add(item.Count());
            }

            ViewData["Word"] = FirstUrlWord;
            ViewData["Frequency"] = FirstUrlWordFreq;

            return View();
        }

        public ActionResult Scoring()
        {
            ViewBag.Message = "Your Scoring page.";

            return View();
        }

        public ActionResult FindScoring(string firstURL, string secondURL)
        {
            URLData urlData = new URLData() { firstURL = firstURL, secondURL = secondURL };

            var FirstUrl = HTMLParse(urlData.firstURL);
            var SecondUrl = HTMLParse(urlData.secondURL);

            List<string> FirstUrlWord = new List<string>();
            List<int> FirstUrlWordFreq = new List<int>();
            List<string> SecondUrlWord = new List<string>();
            List<int> SecondUrlWordFreq = new List<int>();

            int count_1 = 0;
            int count_2 = 0;

            foreach (var item in FirstUrl)
            {
                if (count_1 < 5)
                {
                    FirstUrlWord.Add(item.Key);
                    FirstUrlWordFreq.Add(item.Count());
                }
                count_1++;
            }

            foreach (var item in SecondUrl)
            {
                if (count_2 < 5)
                {
                    SecondUrlWord.Add(item.Key);
                    SecondUrlWordFreq.Add(item.Count());
                }
                count_2++;
            }

            double score = CalculateScoring(FirstUrlWord, SecondUrlWord);

            ViewData["FirstUrl_Keyword"] = FirstUrlWord;
            ViewData["FirstUrl_Keyword_Freq"] = FirstUrlWordFreq;

            ViewData["SecondUrl_Keyword"] = SecondUrlWord;
            ViewData["SecondUrl_Keyword_Freq"] = SecondUrlWordFreq;

            ViewData["Similarity Score"] = score;

            return View(urlData);
        }

        public ActionResult Indexing()
        {
            return View();
        }

        public ActionResult WebIndexing(string firstURL)
        {
            OrderGivenUrl(firstURL);

            return View();
        }

        public ActionResult Analys()
        {
            return View();
        }

        public ActionResult SemanticAnalys(string firstURL)
        {
            string[] lines = System.IO.File.ReadAllLines(@"C:\Users\HakanK\source\repos\WebApplication2\WebApplication2\url.txt");

            OrderGivenUrl(firstURL);

            List<string>[] SecondUrlWord = new List<string>[lines.Length];
            for (int i = 0; i < lines.Length; i++)
            {
                SecondUrlWord[i] = new List<string>();
            }

            var KeywordOfUrl = new List<List<string>>();

            List<IEnumerable<string>> Synonyms = new List<IEnumerable<string>>();

            int count = 0;

            for (int i = 0; i < SubUrList.Count(); i++)
            {
                var SecondUrl = HTMLParse(SubUrList[i].Item1);

                for (int k = 0; k < 5; k++)
                {
                    SecondUrlWord[count].Add(SecondUrl.ElementAt(k).Key);
                }

                KeywordOfUrl.Add(SecondUrlWord[count]);
                count++;
            }

            for (int j = 0; j < KeywordOfUrl.Count(); j++)
            {
                for (int k = 0; k < 5; k++)
                {
                    IEnumerable<string> vs = GetSynonyms(KeywordOfUrl[j].ElementAt(k));
                    Synonyms.Add(vs);
                }
            }

            int len = Synonyms.Count();
            List<string>[] SynList = new List<string>[len];

            for (int i = 0; i < len; i++)
            {
                SynList[i] = Synonyms[i].ToList();
            }

            var FirstUrl = HTMLParse(firstURL);

            List<string> FirstUrlWord = new List<string>();
            List<int> FirstUrlWordFreq = new List<int>();

            foreach (var item in FirstUrl)
            {
                FirstUrlWord.Add(item.Key);
                FirstUrlWordFreq.Add(item.Count());
            }

            double[] urlArray = new double[lines.Length];
            double[] SemanticScore = new double[lines.Length];
            double[] TotalScore = new double[lines.Length];

            for (int j = 0; j < lines.Length; j++)
            {
                urlArray[j] = CalculateScoring(FirstUrlWord, SynList[j]);
                TotalScore[j] = (double)(urlArray[j] + SubUrList[j].Item2);
                SemanticScore[j] = urlArray[j];
            }

            ViewData["KeywordOfUrl"] = KeywordOfUrl;
            ViewData["Synonyms"] = SynList;
            ViewData["SemanticScore"] = SemanticScore;
            ViewData["TotalScore"] = TotalScore;

            return View();
        }

        public ActionResult SubUrlnfo()
        {
            string[] lines = System.IO.File.ReadAllLines(@"C:\Users\HakanK\source\repos\WebApplication2\WebApplication2\url.txt");

            List<string>[] find_link = new List<string>[lines.Length];

            for (int i = 0; i < lines.Length; i++)
            {
                find_link[i] = FindAllLink(SubUrList[i].Item1);
            }

            ViewData["Sub_Url"] = find_link;
            ViewData["Sub_Url_List"] = SubUrList;

            return View();
        }
    }
}