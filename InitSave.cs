using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Simplist2 {
	public partial class MainWindow : Window {
		static string ffSeason = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\SimpListSeason.txt";
		static string ffList = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\SimpList.txt";
		static string ffSet = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\SimpListSet.txt";
		public static string ffFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\SimpList\";
		static string ffBackup = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\SimpListBackup\";

		int WeekDay = -1;
		string WeekString = "일월화수목금토";
		string SaveVerCode = "SaveForm ver.2";
		string Divider = @"\('o')/";

		public void Init() {
			RefreshWeekHead();

			if (!Directory.Exists(ffFolder)) { Directory.CreateDirectory(ffFolder); }
			if (!Directory.Exists(ffBackup)) { Directory.CreateDirectory(ffBackup); }
			if (!File.Exists(ffList)) { using (StreamWriter sw = new StreamWriter(ffList, true, Encoding.UTF8)) { sw.Write(""); } }
			if (!File.Exists(ffSeason)) { using (StreamWriter sw = new StreamWriter(ffSeason, true, Encoding.UTF8)) { sw.Write(""); } }

			string[] fileNames = Directory.GetFiles(ffFolder);
			foreach (string fileName in fileNames) {
				try {
					File.Delete(fileName);
				} catch { }
			}

			if (!File.Exists(ffSet)) {
				lastDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
				using (StreamWriter sw = new StreamWriter(ffSet, true, Encoding.UTF8)) {
					sw.WriteLine(SaveVerCode);
					sw.WriteLine(string.Format("DIR{0}{1}", Divider, lastDirectory));
				}
			} else {
				using (StreamReader sr = new StreamReader(ffSet)) {
					lastDirectory = sr.ReadLine();
					Setting.IsTray = false;
					Setting.IsNoti = false;

					if (lastDirectory == "SaveForm ver.2") {
						string[] split;

						for (int i = 0; ; i++) {
							try {
								split = sr.ReadLine().Split(new string[] { Divider }, StringSplitOptions.RemoveEmptyEntries);
							} catch {
								break;
							}
							switch (split[0]) {
								case "DIR": lastDirectory = split[1]; break;
								case "TRAY": Setting.IsTray = Convert.ToBoolean(split[1]); break;
								case "NOTI": Setting.IsNoti = Convert.ToBoolean(split[1]); break;
							}
						}
					}
				}
			}

			checkTray.IsChecked = Setting.IsTray;
			checkNoti.IsChecked = Setting.IsNoti;

			string strBackupSeason = ffBackup + DateTime.Now.ToString("yyyy-MM-dd") + "_Season.txt";
			string strBackupArchive = ffBackup + DateTime.Now.ToString("yyyy-MM-dd") + "_Archive.txt";
			if (!File.Exists(strBackupSeason)) {
				using (StreamReader sr = new StreamReader(ffSeason)) {
					using (StreamWriter sw = new StreamWriter(strBackupSeason, true, Encoding.UTF8)) {
						sw.Write(sr.ReadToEnd());
					}
				}
				using (StreamReader sr = new StreamReader(ffList)) {
					using (StreamWriter sw = new StreamWriter(strBackupArchive, true, Encoding.UTF8)) {
						sw.Write(sr.ReadToEnd());
					}
				}
			}
		}

		private void RefreshWeekHead() {
			if (WeekDay == (int)DateTime.Now.DayOfWeek) { return; }

			WeekDay = (int)DateTime.Now.DayOfWeek;

			for (int i = 0; i < 7; i++) {
				(((FindName(string.Format("stackSeason{0}", i)) as StackPanel).Children[0] as Button).Content as TextBlock).Text = string.Format("{0}요일", WeekString[i]);
				(((FindName(string.Format("stackSeason{0}", i)) as StackPanel).Children[0] as Button).Content as TextBlock).Foreground = BColor;
			}

			(((FindName(string.Format("stackSeason{0}", WeekDay)) as StackPanel).Children[0] as Button).Content as TextBlock).Text += " ★";
			(((FindName(string.Format("stackSeason{0}", WeekDay)) as StackPanel).Children[0] as Button).Content as TextBlock).Foreground = Brushes.Crimson;
		}

		SortedDictionary<string, AData> DictArchive = new SortedDictionary<string, AData>();
		Dictionary<string, Grid> DictControlArchive = new Dictionary<string, Grid>();

		public void GetArchiveData() {
			string[] strSplitList = null;
			using (StreamReader sr = new StreamReader(ffList)) {
				strSplitList = sr.ReadToEnd().Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
			}

			foreach (string str in strSplitList) {
				AData adata = new AData();

				string[] str2 = str.Split(new string[] { " : ", @" \/ " }, StringSplitOptions.RemoveEmptyEntries);
				if (str2.Length == 2) {
					adata.ComicEpisode = str2[1].Trim();
				} else { adata.ComicEpisode = ""; }

				str2 = str2[0].Split(new string[] { " - ", @" /\ " }, StringSplitOptions.RemoveEmptyEntries);
				if (str2.Length == 2) {
					string[] str3 = str2[1].Split(new string[] { "." }, StringSplitOptions.RemoveEmptyEntries);
					if (str3.Length == 1) {
						adata.Season = -1;
						adata.Episode = Convert.ToInt32(str3[0].Trim());
					} else {
						adata.Season = Convert.ToInt32(str3[0].Trim());
						adata.Episode = Convert.ToInt32(str3[1].Trim());
					}
				} else {
					adata.Season = adata.Episode = -1;
				}
				adata.KeyName = str2[0].Trim();

				//DictArchive.Add(adata.KeyName, adata);
				AddArchive(adata);
			}
		}

		int SeasonIDCount = 0;
		Dictionary<int, SData> DictSeason = new Dictionary<int, SData>();
		Dictionary<int, Grid> DictControlSeason = new Dictionary<int, Grid>();

		public void GetSeasonData() {
			string[] strSplitSeason = null;
			using (StreamReader sr = new StreamReader(ffSeason)) {
				strSplitSeason = sr.ReadToEnd().Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
			}

			for (int i = 0; i < strSplitSeason.Length; i++) {
				string[] strSplit = strSplitSeason[i].Split(new string[] { "	" }, StringSplitOptions.RemoveEmptyEntries);
				if (!DictArchive.ContainsKey(strSplit[2])) { continue; }

				DictArchive[strSplit[2]].isLinked = true;
				SeasonIDCount++;

				SData sdata = new SData() {
					ID = SeasonIDCount,
					Title = strSplit[1], Keyword = strSplit[3].Substring(1),
					KeyName = strSplit[2],
					Week = Convert.ToInt32(strSplit[0][0].ToString()),
					Time = Convert.ToInt32(strSplit[0].Substring(2).Replace(":", "")),
				};
				AddSeasonData(sdata);

				DictArchive[sdata.KeyName].SID = sdata.ID;
			}
		}

		public void SaveData() {
			SaveSettings();

			var listSeason = DictSeason.OrderBy(kvp => kvp.Value.TimeTag);

			using (StreamWriter sw = new StreamWriter(ffSeason, false, Encoding.UTF8)) {
				foreach (KeyValuePair<int, SData> kData in listSeason) {
					SData aData = kData.Value;
					sw.WriteLine(aData.TimeTag + "\t\t" + aData.Title + "\t\t" + aData.KeyName + "\t\t#" + aData.Keyword);
				}
			}

			// Write list data
			var listArchive = DictArchive.OrderBy(kvp => kvp.Value.KeyName);

			using (StreamWriter sw = new StreamWriter(ffList, false, Encoding.UTF8)) {
				string strAppend;
				foreach (KeyValuePair<string, AData> kData in listArchive) {
					AData aData = kData.Value;
					strAppend = "";

					if (aData.Episode < 0 && aData.ComicEpisode == "") { strAppend = "\t"; }
					strAppend += aData.KeyName;
					if (aData.Episode >= 0) { strAppend += @" /\ "; }
					if (aData.Season >= 0 && aData.Episode >= 0) { strAppend += aData.Season + "."; }
					if (aData.Episode >= 0) { strAppend += aData.Episode.ToString("00"); }
					if (aData.ComicEpisode != "") { strAppend += @" \/ " + aData.ComicEpisode; }

					sw.WriteLine(strAppend);
				}
			}
		}

		private void InitTray() {
			ni.Visible = Setting.IsTray;
			isReallyClose = !Setting.IsTray;

			ni.Icon = System.Drawing.Icon.FromHandle(Simplist2.Properties.Resources.BitTorrent.Handle);
			ni.Text = "Simplist2";

			System.Windows.Forms.ContextMenuStrip ctxt = new System.Windows.Forms.ContextMenuStrip();
			System.Windows.Forms.ToolStripMenuItem copen = new System.Windows.Forms.ToolStripMenuItem("열기");
			System.Windows.Forms.ToolStripMenuItem cshutdown = new System.Windows.Forms.ToolStripMenuItem("종료");

			ni.MouseDoubleClick += delegate(object sender, System.Windows.Forms.MouseEventArgs e) { ActivateMe(); };
			copen.Click += delegate(object sender, EventArgs e) { ActivateMe(); };
			cshutdown.Click += delegate(object sender, EventArgs e) { isReallyClose = true; Application.Current.Shutdown(); };

			ctxt.Items.Add(copen);
			ctxt.Items.Add(cshutdown);
			ni.ContextMenuStrip = ctxt;
		}

		private void ActivateMe() {
			new AltTab().ShowAltTab(this);
			this.Opacity = 1;
			this.Activate(); 

			RefreshNoticeControl(ListNotice, false, false);
		}

		private void SaveSettings(){ 
			using (StreamWriter sw = new StreamWriter(ffSet, false, Encoding.UTF8)) {
				sw.WriteLine(SaveVerCode);
				sw.WriteLine(string.Format("DIR{0}{1}", Divider, lastDirectory));
				sw.WriteLine(string.Format("TRAY{0}{1}", Divider, Setting.IsTray));
				sw.WriteLine(string.Format("NOTI{0}{1}", Divider, Setting.IsNoti));
			}
		}

		bool isPreset = false;
		private void Setting_Changed(object sender, RoutedEventArgs e) {
			if (!isPreset) { return; }
			Setting.IsTray = checkTray.IsChecked == true ? true : false;
			Setting.IsNoti = checkNoti.IsChecked == true ? true : false;

			ni.Visible = Setting.IsTray;
			isReallyClose = !Setting.IsTray;

			SaveSettings();
			ShowGlobalMessage("Setting saved", Brushes.SteelBlue, 1500);
		}
	}

	public class Setting {
		public static bool IsTray = false;
		public static bool IsNoti = false;
	}
}
