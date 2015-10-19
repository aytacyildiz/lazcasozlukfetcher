using System;
using System.Net;
using System.IO;
using CsQuery;
using System.Collections.Generic;
using System.Text;

namespace com.kodgulugum.lazcasozlukfetcher
{
	class MainClass
	{
		public static void Main (string[] args)
		{
			// test
			Fetcher f = new Fetcher();
			f.fetchAndSave(Fetcher.Language.Lazca);
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
			words = (lng == Language.Lazca) ? getLazcaWords ("A","B","C","C1","C2","D","E","F","G","Gy","G1","H","I","J","K","K1","Ky","Ky1","L","M","N","O","P","P1","R","S","S1","T","T1","U","V","X","X1","Y","Z","Z1","3","31") : null;
			StringBuilder wordlistHTML = new StringBuilder ("<datalist id=\""+ Language.Lazca.ToString() +"Words\">"); 
			for (int i = 0; i < words.Count; i++) {
				if(words[i].Word==null || words[i].Definition==null) {
					Console.WriteLine ("HATA: Bir kelimede terslik var");
					break;
				};
				// https://developer.mozilla.org/en-US/docs/Web/HTML/Global_attributes/data-*
				wordlistHTML.Append ("<option data-index=\""+ i.ToString() +"\" value=\""+  words[i].Word.Trim() +"\">");
				writeToDisk(lng.ToString() + i.ToString() , words[i].Definition);
			}
			wordlistHTML.Append ("</datalist>");
			writeToDisk("datalist"+lng.ToString() , wordlistHTML.ToString());
		}
		private void writeToDisk(string name, string data){
			try {
				Directory.CreateDirectory("output");
				using(StreamWriter sw = new StreamWriter( Path.Combine("output" , name+".html") , false , System.Text.Encoding.UTF8 )){
					sw.Write(data);
				}
			}
			catch (System.IO.IOException e){
				Console.WriteLine ("HATA: Diske yazmada hata!");
				Console.WriteLine (e.Message);
			}
		}
		private List<Entry> getLazcaWords(params string[] letters){
			List<Entry> dict = new List<Entry> ();
			foreach (var letter in letters) {
				string url = "http://ayla7.free.fr/laz/Laz." + letter + ".html";
				// create dom from html string
				dom = getHtml (url) ?? String.Empty;
				// find separators
				var p_elements = dom[".western[lang='tr-TR']"];
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
						definition.AppendLine ("<p>"+ nextElement.InnerHTML +"</p>");
						nextElement = nextElement.NextElementSibling;
						if (nextElement == null) break;
					}
					dict.Add (new Entry(word,definition.ToString()));
				}
			}
			mergeSameDefinitions(dict);
			return dict;
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
	}
}
