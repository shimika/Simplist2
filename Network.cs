using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Json;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Xml;

namespace Simplist2 {
	public partial class MainWindow : Window {
		private string RssType = "nyaa";
		private async Task<List<ListData>> GetTorrentList(string strTag) {
			List<ListData> listTorrent = new List<ListData>();
			if (RssType == "nyaa") {
				// Nyaa torrent

				Task<string> httpTask = GetHTML(@"http://www.nyaa.eu/?page=rss&cats=1_0&term=" + strTag, "UTF-8");
				string strHTML = await httpTask;

				XmlDocument xmlDoc = new XmlDocument();
				XmlNodeList xmlnode;
				int nCount = 0;

				try {
					xmlDoc.LoadXml(strHTML);
					xmlnode = xmlDoc.SelectNodes("rss/channel/item");
				} catch { return listTorrent; }

				foreach (XmlNode node in xmlnode) {
					ListData tData = new ListData() {
						Caption = node["title"].InnerText, URL = node["link"].InnerText, IsTorrent = true,
					};
					if (node["category"].InnerText == "Raw Anime") { tData.IsRaw = true; }
					try {
						string strFileSize = node["description"].InnerText.Split(new string[] { " - ", "]]" }, StringSplitOptions.RemoveEmptyEntries)[1];
						tData.Memo += strFileSize;
					} catch { }

					listTorrent.Add(tData);
					nCount++;
					if (nCount >= 30) { break; }
				}
			} else if (RssType == "anirena") {
				// Anirena

				Task<string> httpTask = GetHTML(@"http://www.anirena.com/rss.php?s=" + strTag, "UTF-8");
				string strHTML = await httpTask;

				XmlDocument xmlDoc = new XmlDocument();
				XmlNodeList xmlnode;
				int nCount = 0;

				try {
					xmlDoc.LoadXml(strHTML);
					xmlnode = xmlDoc.SelectNodes("rss/channel/item");
				} catch { return listTorrent; }

				foreach (XmlNode node in xmlnode) {
					ListData tData = new ListData() {
						Caption = node["title"].InnerText, URL = node["link"].InnerText, IsTorrent = true,
					};
					if (node["category"].InnerText == "RAW") { tData.IsRaw = true; }
					try {
						string strFileSize = node["description"].InnerText.Split(new string[] { " - ", "," }, StringSplitOptions.RemoveEmptyEntries)[3].Trim();
						tData.Memo += strFileSize;
					} catch { }

					listTorrent.Add(tData);
					nCount++;
					if (nCount >= 30) { break; }
				}
			} else if (RssType == "tokyotosho") {
				// Tokyo Toshokan

				Task<string> httpTask = GetHTML(@"http://tokyotosho.info/search.php?terms=" + strTag, "UTF-8");
				string strHTML = await httpTask;

				int listStart = 0, listEnd = 0;

				listStart = strHTML.IndexOf("<table class=\"listing\">");
				if (listStart < 0) { return listTorrent; }
				listEnd = strHTML.IndexOf("</table>", listStart);

				int startIndex = 0, endIndex = 0;

				for (int i = 0; i < 50; i++) {
					startIndex = strHTML.IndexOf("<tr ", endIndex);
					endIndex = startIndex + 1;
					if (startIndex < 0) { break; }

					string title, link, size, tag, content;
					int tagcount = 0;
					bool isRaw = false;
					title = link = size = tag = content = "";

					// Check RAW File
					startIndex = strHTML.IndexOf("<a href=\"/?cat=", startIndex + 1);
					if (strHTML[startIndex + 15] == '7') { isRaw = true; }

					// Extract Title, Link
					startIndex = strHTML.IndexOf("<td class=\"desc-top\">", startIndex + 1);
					startIndex = strHTML.IndexOf("<a ", startIndex + 1);
					startIndex = strHTML.IndexOf("<a ", startIndex + 1);
					endIndex = strHTML.IndexOf('>', startIndex + 1);

					// Extract Link

					for (int j = startIndex + 1; j < endIndex; j++) {
						tag += strHTML[j];
					}
					foreach (string split in tag.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries)) {
						string[] split2 = split.Split(new string[] { "=" }, StringSplitOptions.RemoveEmptyEntries);
						if (split2[0] == "href") {
							link = split2[1].Replace("\"", "");
						}
					}

					// Extract Title

					startIndex = endIndex;
					endIndex = strHTML.IndexOf("</a>", startIndex + 1);

					for (int j = startIndex + 1; j < endIndex; j++) {
						content += strHTML[j];
					}
					title = content.Replace("<span class=\"s\"> </span>", "");



					// Extract Size

					startIndex = strHTML.IndexOf("<tr ", endIndex);
					endIndex = startIndex + 1;
					if (startIndex < 0) { break; }

					startIndex = strHTML.IndexOf("<td class=\"desc-bot\">", endIndex);
					if (startIndex < 0) { continue; }
					endIndex = strHTML.IndexOf("</td>", startIndex + 1);
					if (endIndex < 0) { continue; }
					content = "";

					for (int j = startIndex; j < endIndex; j++) {
						if (strHTML[j] == '<') {
							tagcount++;
						} else if (strHTML[j] == '>') {
							tagcount--;
						} else if (tagcount == 0) {
							content += strHTML[j];
						}
					}

					foreach (string split in content.Split(new string[] { "|" }, StringSplitOptions.RemoveEmptyEntries)) {
						string[] split2 = split.Trim().Split(new string[] { ":" }, StringSplitOptions.RemoveEmptyEntries);
						if (split2[0] == "Size") {
							size = split2[1].Trim();
							break;
						}
					}

					ListData tData = new ListData() {
						Caption = title,
						URL = link,
						Memo = size,
						IsRaw = isRaw,
						IsTorrent = true,
					};

					listTorrent.Add(tData);
				}
			}

			return listTorrent;
		}

		public static async Task<List<ListData>> GetWeekdayList(int weekCode) {
			List<ListData> listWeekday = new List<ListData>();

			try {
				Task<string> httpTask = GetHTML(@"http://www.anissia.net/anitime/list?w=" + weekCode, "UTF-8");
				string strHTML = await httpTask;

				JsonTextParser parser = new JsonTextParser();
				JsonArrayCollection obj = (JsonArrayCollection)parser.Parse(strHTML);

				foreach (JsonObjectCollection item in obj) {
					ListData sItem = new ListData() {
						Caption = item["s"].GetValue().ToString(),
						URL = @"http://www.anissia.net/anitime/cap?i=" + item["i"].GetValue().ToString(),
						Memo = "anime", Time = weekCode * 10000 + Convert.ToInt32(item["t"].GetValue().ToString()),
						ID = item["i"].GetValue().ToString(),
					};
					listWeekday.Add(sItem);
				}
			} catch (Exception ex) {
				//MessageBox.Show("Weekday\n" + ex.Message);
				listWeekday.Clear();
			}

			return listWeekday;
		}

		public async Task<List<ListData>> GetMakerList(string strTag, bool ReturnOne = false) {
			List<ListData> listMaker = new List<ListData>();

			try {
				Task<string> httpTask = GetHTML(strTag, "UTF-8");
				string strHTML = await httpTask;


				JsonTextParser parser = new JsonTextParser();
				JsonArrayCollection obj = (JsonArrayCollection)parser.Parse(strHTML);

				int maxValue = -1, epValue;

				foreach (JsonObjectCollection item in obj) {
					ListData sItem;
					if (ReturnOne) {
						sItem = GetListDataNotice(item);
					} else {
						sItem = GetListData(item);
					}
					listMaker.Add(sItem);

					epValue = Convert.ToInt32(sItem.ID.Substring(14, 5));
					if (maxValue < epValue) { maxValue = epValue; }
				}

				listMaker.Sort(new mysortByValue());

				if (ReturnOne && listMaker.Count > 0) {
					for (int i = 0; i < listMaker.Count; i++) {
						epValue = Convert.ToInt32(listMaker[i].ID.Substring(14, 5));
						if (epValue == maxValue) {
							return listMaker.GetRange(i, 1);
						}
					}
				}
			} catch (Exception ex) {
				//MessageBox.Show("Maker\n" + ex.Message);
				listMaker.Clear();
			}

			return listMaker;
		}

		private static ListData GetListData(JsonObjectCollection item) {
			ListData sItem = new ListData();
			string strEpisode = item["s"].GetValue().ToString();
			if (strEpisode.Length == 5) {
				int nEpisode = Convert.ToInt32(strEpisode);
				strEpisode = string.Format("{0}화 ", nEpisode / 10);
				if (nEpisode % 10 != 0) {
					strEpisode = string.Format("{0}.{1}화 ", nEpisode / 10, nEpisode % 10);
				}
			}

			sItem.Caption = strEpisode + item["n"].GetValue().ToString();
			sItem.URL = item["a"].GetValue().ToString();
			sItem.Memo = "maker";
			sItem.ID = string.Format("{0}{1}", item["s"].GetValue().ToString(), item["d"].GetValue().ToString());

			return sItem;
		}

		private static ListData GetListDataNotice(JsonObjectCollection item) {
			ListData sItem = new ListData();

			sItem.Memo = "anime";
			sItem.ID = string.Format("{0}{1}{2}", item["d"].GetValue().ToString(), item["s"].GetValue().ToString(), item["n"].GetValue().ToString());
			
			try {
				sItem.UpdateTime = DateTime.ParseExact(sItem.ID.Substring(0, 14), "yyyyMMddHHmmss", CultureInfo.InvariantCulture);
			} catch {
				sItem.UpdateTime = new DateTime(1900, 1, 1);
			}

			return sItem;
		}

		private class mysortByValue : IComparer<ListData> {
			public int Compare(ListData arg1, ListData arg2) {
				return string.Compare(arg2.ID, arg1.ID);
			}
		}

		enum SiteType { Naver, Other };
		public static async Task<List<ListData>> GetFileList(string strTag, bool isHakerano) {
			Task<string> httpTask = GetHTML(strTag, "UTF-8");
			string strHTML = await httpTask;


			if (strHTML == "") { return new List<ListData>(); }

			Dictionary<SiteType, int> DictCount = new Dictionary<SiteType, int>();
			DictCount[SiteType.Naver] = Regex.Matches(strHTML, "naver", RegexOptions.IgnoreCase).Cast<Match>().Count();
			DictCount[SiteType.Other] = Regex.Matches(strHTML, "tistory", RegexOptions.IgnoreCase).Cast<Match>().Count();
			DictCount[SiteType.Other] += Regex.Matches(strHTML, "egloos", RegexOptions.IgnoreCase).Cast<Match>().Count();

			SiteType sitetype = SiteType.Other;
			if (DictCount[SiteType.Naver] > DictCount[SiteType.Other]) { sitetype = SiteType.Naver; }

			if (sitetype == SiteType.Naver) {
				httpTask = GetHTML(strTag, "EUC-KR");
				strHTML = await httpTask;
				string nURL = "";

				if (strHTML.IndexOf("mainFrame") < 0 && strHTML.IndexOf("aPostFiles") < 0) {
					int nIndex = strHTML.IndexOf("screenFrame");

					if (nIndex >= 0) {
						for (int i = strHTML.IndexOf("http://blog.naver.com/", nIndex); ; i++) {
							if (strHTML[i] == '\"') { break; }
							nURL += strHTML[i];
						}
						if (nURL != "") {
							strTag = nURL;
							httpTask = GetHTML(strTag, "EUC-KR");
							strHTML = await httpTask;
						}
					}
				}

				if (strHTML.IndexOf("mainFrame") >= 0 && strHTML.IndexOf("aPostFiles") < 0) {
					int nIndex = strHTML.IndexOf("mainFrame");
					nIndex = strHTML.IndexOf("src", nIndex);
					bool flag = false;
					nURL = "";
					for (int i = nIndex; ; i++) {
						if (strHTML[i] == '\"') {
							if (flag) {
								break;
							} else {
								flag = true;
								continue;
							}
						}
						if (flag) { nURL += strHTML[i]; }
					}
					if (nURL[0] == '/') { nURL = "http://blog.naver.com" + nURL; }
					if (nURL != "") { strTag = nURL; }
				}

				httpTask = GetHTML(strTag, "utf-8");
				strHTML = await httpTask;
			}

			List<ListData> listSmi = null;

			if (isHakerano) {
				listSmi = Fc2Parse(strHTML);
			} else if (sitetype == SiteType.Naver) {
				httpTask = GetHTML(strTag, "EUC-KR");
				strHTML = await httpTask;

				listSmi = NaverParse(strHTML);
			} else {
				Task<List<ListData>> parseTask = TistoryParse(strHTML);
				listSmi = await parseTask;
			}

			return listSmi;
		}

		private static List<ListData> NaverParse(string html) {
			List<ListData> listData = new List<ListData>();
			int sIndex = 0, eIndex = 0;
			string attachString;

			string msg = "";
			for (; ; ) {
				sIndex = html.IndexOf("aPostFiles[", sIndex);
				if (sIndex < 0) { break; }
				sIndex = html.IndexOf("[{", sIndex);
				if (sIndex < 0) { break; }
				eIndex = html.IndexOf("}];", sIndex);
				if (eIndex < 0) { break; }

				attachString = html.Substring(sIndex + 2, eIndex - sIndex).Replace("\"", "\'");

				int flag = 0;
				string sKey = "", sValue = "";
				string fileName = "", fileURL = "";

				for (int i = 0; i < attachString.Length; i++) {
					if (attachString[i] == '\\' && i + 1 != attachString.Length) {
						if (attachString[i + 1] == '\'') {
							if (flag == 1) {
								sKey += '\'';
							} else if (flag == 3) {
								sValue += '\'';
							}
							i++; continue;
						}
					}
					if (attachString[i] == '\'') {
						flag++; continue;
					}

					switch (flag) {
						case 1: sKey += attachString[i]; break;
						case 3: sValue += attachString[i]; break;
						case 4:
							sKey = sKey.Trim(); sValue = sValue.Trim();
							if (sKey == "encodedAttachFileName") { fileName = sValue; }
							if (sKey == "encodedAttachFileUrl") { fileURL = sValue; }

							msg += sKey + " = " + sValue + "\n";
							sKey = sValue = "";
							flag = 0;

							break;
					}
					if (attachString[i] == '}') {
						if (fileName != "" && fileURL != "") {
							listData.Add(new ListData() { Caption = fileName, URL = fileURL, Memo = "file" });
							fileName = fileURL = "";
						}
					}
				}

				msg += "\n";
			}
			return listData;
		}

		private static async Task<List<ListData>> TistoryParse(string html) {
			List<ListData> listData = new List<ListData>();
			int nIndex = 0, lastIndex = 0;
			string[] ext = new string[] { "zip", "rar", "7z", "egg", "smi" };
			List<int> lst = new List<int>();
			string fileName, fileURL;

			try {
				for (; ; ) {
					lst.Clear();
					foreach (string str in ext) {
						nIndex = html.IndexOf(string.Format(".{0}\"", str), lastIndex, StringComparison.OrdinalIgnoreCase);
						if (nIndex < 0) { nIndex = 999999999; }
						lst.Add(nIndex);
					}
					lst.Sort();
					if (lst[0] == 999999999) { break; }
					lastIndex = html.IndexOf("\"", lst[0]);
					fileURL = "";

					for (int i = lastIndex - 1; i >= 0; i--) {
						if (html[i] == '\"') { break; }
						fileURL = html[i] + fileURL;
					}

					Task<string> httpTask = GetFilenameFromURL(fileURL);
					fileName = await httpTask;

					if (fileName != "" && fileURL != "") {
						listData.Add(new ListData() { Caption = fileName, URL = fileURL, Memo = "file" });
					}
				}
			} catch (Exception ex) {
				MessageBox.Show(ex.Message);
			}
			return listData;
		}

		private static List<ListData> Fc2Parse(string html) {
			List<ListData> listData = new List<ListData>();

			int sIndex = 0;
			string str;

			for (; ; ) {
				try {
					sIndex = html.IndexOf("<img", sIndex + 1);
					if (sIndex < 0) { break; }
					int eIndex = html.IndexOf(">", sIndex + 1);
					str = html.Substring(sIndex, eIndex - sIndex + 1);
				} catch (Exception ex) {
					MessageBox.Show(ex.Message);
					continue;
				}

				if (str.IndexOf("/>") < 0) {
					str = string.Format("{0}</img>", str);
				}

				try {
					XmlDocument xmlDoc = new XmlDocument();
					xmlDoc.LoadXml(str);
					XmlElement root = xmlDoc.DocumentElement;

					string alt = root.Attributes["alt"].Value;
					string src = root.Attributes["src"].Value;

					if (alt.IndexOf("자막") < 0) { continue; }

					listData.Add(new ListData() {
						Caption = alt,
						URL = src,
						Memo = "file"
					});
				} catch(Exception ex) {
					MessageBox.Show(ex.Message + " : " + "\n" + str);
					continue;
				}
			}

			return listData;
		}

		private static Task<string> GetFilenameFromURL(string url) {
			return Task.Run(() => {
				using (WebClient client = new WebClient() { Proxy = null }) {
					using (Stream rawStream = client.OpenRead(url)) {
						string fileName = string.Empty;
						string contentDisposition = client.ResponseHeaders["content-disposition"];
						string realName = "";
						if (!string.IsNullOrEmpty(contentDisposition)) {
							string lookFor = "filename=";
							int index = contentDisposition.IndexOf(lookFor, StringComparison.CurrentCultureIgnoreCase);
							if (index >= 0) {
								fileName = contentDisposition.Substring(index + lookFor.Length);
								realName = HttpUtility.UrlDecode(fileName, Encoding.GetEncoding("UTF-8"));
							}
						} else {
							string[] strSplit = url.Split('/');
							realName = strSplit[strSplit.Length - 1];
						}
						rawStream.Close();
						if (realName[realName.Length - 1] == '\"') {
							realName = realName.Substring(0, realName.Length - 1);
							if (realName[0] == '\"') {
								realName = realName.Substring(1);
							}
						}
						return realName;
					}
				}
			});
		}

		// common api
		public static Task<string> GetHTML(string url, string encoding) {
			return Task.Run(() => {
				try {
					HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create(new UriBuilder(url).Uri);
					httpWebRequest.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8";
					httpWebRequest.ContentType = "application/x-www-form-urlencoded; charset=utf-8";
					httpWebRequest.Method = "GET";
					httpWebRequest.Referer = "google.com";
					httpWebRequest.UserAgent =
						"Mozilla/4.0 (compatible; MSIE 8.0; Windows NT 6.0; WOW64; " +
						"Trident/4.0; SLCC1; .NET CLR 2.0.50727; Media Center PC 5.0; " +
						".NET CLR 3.5.21022; .NET CLR 3.5.30729; .NET CLR 3.0.30618; " +
						"InfoPath.2; OfficeLiveConnector.1.3; OfficeLivePatch.0.0)";
					httpWebRequest.ContentLength = 0;

					string rtHTML = "";

					httpWebRequest.Proxy = null;

					HttpWebResponse httpWebResponse = (HttpWebResponse)httpWebRequest.GetResponse();
					StreamReader streamReader = new StreamReader(httpWebResponse.GetResponseStream(), Encoding.GetEncoding(encoding));
					rtHTML = streamReader.ReadToEnd();

					return rtHTML;
				} catch (Exception ex) {
					//MessageBox.Show(ex.Message);
					return "";
				}
			});
		}

		private int StringMatching(string s1, string s2) {
			int[,] a = new int[s1.Length + 1, s2.Length + 1];

			for (int i = 0; i <= s1.Length; i++) {
				for (int j = 0; j <= s2.Length; j++) {
					if (i == 0 || j == 0) {
						if (i != 0) {
							a[i, j] = a[i - 1, j] + 1;
						} else if (j != 0) {
							a[i, j] = a[i, j - 1] + 1;
						}
					} else {
						if (s1[i - 1] == s2[j - 1]) {
							a[i, j] = a[i - 1, j - 1];
						} else {
							a[i, j] = Math.Min(a[i - 1, j], a[i, j - 1]) + 1;
						}
					}
				}
			}

			return a[s1.Length, s2.Length];
		}

		private int StringPrefixMatch(string s1, string s2) {
			int i, n = Math.Min(s1.Length, s2.Length), m;
			m = n;

			for (i = 0; i < n; i++) {
				if (s1[i] != s2[i]) {
					m = i;
					break;
				}
			}
			return m;
		}
	}

	public struct ListData {
		public string Caption, URL, Memo, ID;
		public int Time;
		public bool IsRaw, IsTorrent;
		public DateTime UpdateTime;
	}
}
