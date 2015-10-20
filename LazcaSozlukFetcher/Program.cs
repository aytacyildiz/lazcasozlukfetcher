using System;
using System.Net;
using System.IO;
using CsQuery;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace com.kodgulugum.lazcasozlukfetcher
{
	class MainClass
	{
		public static void Main (string[] args)
		{
			// test
			Fetcher f = new Fetcher();
			f.fetchAndSave(Fetcher.Language.Lazca);
			f.fetchAndSave(Fetcher.Language.Turkce);
		}
	}

	class Entry{
		public string Word { get; set; }
		public string Definition { get; set; }
		public Entry(string word, string def){
			this.Word = word;
			this.Definition = def;
		}
	}

	class Fetcher
	{
		// prop
		private CQ dom { get; set; }
		private List<Entry> words { get; set; }
		public enum Language { Turkce, Lazca }
		// methods
		public void fetchAndSave(Language lng){
			words = (lng == Language.Lazca) ? getLazcaWords ("A","B","C","C1","C2","D","E","F","G","Gy","G1","H","I","J","K","K1","Ky","Ky1","L","M","N","O","P","P1","R","S","S1","T","T1","U","V","X","X1","Y","Z","Z1","3","31") : getTurckeWords("A-C","D-J","K-R","S-Z");
			// null check
			if(words==null) {
				Console.WriteLine ("HATA: {0} hic bir kelime bulunamadi",lng.ToString());
				return;
			}
			StringBuilder wordlistHTML = new StringBuilder ("{\"wordlist\":["); 
			for (int i = 0; i < words.Count; i++) {
				if(words[i].Word==null || words[i].Definition==null) {
					Console.WriteLine ("HATA: Bir kelimede terslik var");
					break;
				};
				wordlistHTML.Append ("\""+  Regex.Replace(words[i].Word.Trim(), @"\t|\n|\r", "") +"\",");
				writeToDisk(lng.ToString() + i.ToString() + ".html" , words[i].Definition);
			}
			wordlistHTML.Append ("\"END\"]}");
			writeToDisk("datalist"+lng.ToString()+".json" , wordlistHTML.ToString());
		}
		private void writeToDisk(string name, string data){
			try {
				Directory.CreateDirectory("output");
				using(StreamWriter sw = new StreamWriter( Path.Combine("output" , name) , false , System.Text.Encoding.UTF8 )){
					sw.Write(data);
				}
			}
			catch (IOException e){
				Console.WriteLine ("HATA: Diske yazmada hata!");
				Console.WriteLine (e.Message);
			}
		}
		private List<Entry> getTurckeWords(params string[] letters){
			List<Entry> dict = new List<Entry> ();
			int counter = 1;
			foreach (var item in letters) {
				string url = "http://ayla7.free.fr/laz/Turkce-Lazca-"+item+".html";
				var p_elements = getElements(url,".western:not([lang='tr-TR'])");
				counter = 1;
				foreach (var p in p_elements) {
					if(counter >= 6){
						if(counter==6 && item=="A-C") continue; // need 7th
						dict.Add(new Entry(extractTurkceWord(p),"<p>"+ p.InnerHTML +"<em class=\"source\" style=\"font-size: 10pt\">Kaynak: http://ayla7.free.fr/laz</em></p>"));
					}
					counter++;
				}
			}
			if(dict!=null) mergeSameDefinitions(dict);
			return dict;
		}
		private List<Entry> getLazcaWords(params string[] letters){
			List<Entry> dict = new List<Entry> ();
			foreach (var letter in letters) {
				string url = "http://ayla7.free.fr/laz/Laz." + letter + ".html";
				var p_elements = getElements(url,".western[lang='tr-TR']");
				foreach (var p in p_elements) {
					// find the next element which will probably be a definition
					var nextElement = p.NextElementSibling;
					if (nextElement == null || nextElement.HasAttribute("lang") || nextElement.NodeName != "P") continue;
					// which word is definied
					string word = nextElement.FirstChild.TextContent;
					// get the definition
					StringBuilder definition = new StringBuilder ();
					// until reach another separator 
					while(!nextElement.HasAttribute("lang")){
						definition.AppendLine ("<p>"+ nextElement.InnerHTML +"<em class=\"source\" style=\"font-size: 10pt\">Kaynak: http://ayla7.free.fr/laz</em></p>");
						nextElement = nextElement.NextElementSibling;
						if (nextElement == null) break;
					}
					dict.Add (new Entry(word,definition.ToString()));
				}
			}
			if(dict!=null) mergeSameDefinitions(dict);
			return dict;
		} 
		private CQ getElements(string url,string selector){
			// create dom from html string
			dom = getHtml (url) ?? String.Empty;
			// find separators
			var p_elements = dom[selector];
			if(p_elements==null) Console.WriteLine ("HATA: {0} ile eleman bulunamadi",selector);
			return p_elements;
		}
		private void mergeSameDefinitions(List<Entry> dict){
			for (int i = 0; i < dict.Count; i++) {
				if(dict.Count <= i+1) break; // because removed items
				int j = i;
				while (dict[j].Word == dict[j+1].Word) {
					dict[j].Definition += dict[j+1].Definition;
					dict.RemoveAt(j+1);
				}
			}
		}
		private string getHtml(string url){
			HttpWebRequest hwreq = (HttpWebRequest)WebRequest.Create (url);
			// only accept html text
			hwreq.Accept = "text/html";
			// generic user agent
			hwreq.UserAgent = "Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/45.0.2454.101 Safari/537.36";
			HttpWebResponse hwres;
			try {
				hwres = (HttpWebResponse)hwreq.GetResponse ();
			} catch (WebException we) {
				// todo: handle other exceptions
				switch (we.Status) {
					case WebExceptionStatus.NameResolutionFailure:
						Console.WriteLine ("HATA: Girilen adresi tekrar kontrol edin veya internete bağlandığınıza emin olun!"); 
						break;
					default:
						Console.WriteLine ("HATA: {0}", we.Status);
						break;
				}
				return null;
			}
			// log the communication
			Console.WriteLine ("---------------------------------------------------------------------- HTTP");
			Console.WriteLine (hwres.ResponseUri);
			Console.WriteLine (hwres.StatusCode);
			Console.WriteLine (hwres.StatusDescription);
			Console.WriteLine (hwres.CharacterSet);
			Console.WriteLine ("------------------------------------------------------------ Request Header");
			for (int i = 0; i < hwreq.Headers.Count; i++) {
				Console.WriteLine ("{0}={1}",hwreq.Headers.AllKeys[i],hwreq.Headers[i]);
			}
			Console.WriteLine ("----------------------------------------------------------- Response Header");
			for (int i = 0; i < hwres.Headers.Count; i++) {
				Console.WriteLine ("{0}={1}",hwres.Headers.AllKeys[i],hwres.Headers[i]);
			}
			string result = null;
			using (Stream s = hwres.GetResponseStream ()) {
				using (StreamReader sr = new StreamReader (s, System.Text.Encoding.UTF8)) {
					result = sr.ReadToEnd ();
				}
			}
			hwres.Close (); // release the source
			return result;
		}
		private string extractTurkceWord(IDomObject de){
			// remove whitespaces and etc.
			string text = Regex.Replace(de.InnerText,@"\t|\n|\r", "");
			// remove Square Brackets and its content
			text = Regex.Replace(text,@"\[[^\]]*\]",""); // \[ [ ^ \] ]* \]
			// get string before before Colon
			Regex re = new Regex(@".*(?=\:)");
			text = re.Match(text).ToString().Trim();
			return text;
		}
	}
}
