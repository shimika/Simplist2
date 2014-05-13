using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Animation;
using Microsoft.Win32;

namespace Simplist2 {
	public partial class MainWindow : Window {
		int LoadingID = -1;
		private async void InitDownloadDialog(int sid) {
			LoadingID = sid;
			textTorrentInfo.Text = string.Format("({0}화) {1}", DictArchive[DictSeason[sid].KeyName].Episode, DictSeason[sid].Title);

			imgLoadIndicator.Visibility = Visibility.Visible;
			ClearDownloadDialogControl();
			buttonTabTorrent.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));

			Task<List<ListData>> httpTask;
			httpTask = GetTorrentList(DictSeason[sid].Keyword);
			List<ListData> listTorrent = await httpTask;

			httpTask = GetWeekdayList(DictSeason[sid].Week);
			List<ListData> listSubtitle = await httpTask;

			if (LoadingID != sid || listTorrent.Count == 0 || listSubtitle.Count == 0) {
				imgLoadIndicator.Visibility = Visibility.Collapsed;
				return;
			}
			ClearDownloadDialogControl();

			// Torrent Download

			StackPanel stack = new StackPanel();
			for (int i = 0; i < listTorrent.Count; i++) {
				Button btn = MakeListItem(listTorrent[i]);
				btn.Tag = listTorrent[i];
				btn.Click += btnTorrent_Click;
				stack.Children.Add(btn);
			}

			if (LoadingID != sid) {
				stackTorrent.Children.Clear();
				gridSubtitle.Children.Clear();
				return;
			}

			stackTorrent.Children.Add(stack);
			scrollTorrent.ScrollToTop();

			// Subtitle Download

			int minValue = 9999, minIndex = 0, getValue, flag = 0;


			for (int i = 0; i < listSubtitle.Count; i++) {
				getValue = StringMatching(DictSeason[sid].Title, listSubtitle[i].Caption);

				if (getValue < minValue) {
					minValue = getValue; minIndex = i;
				}

				getValue = StringPrefixMatch(DictSeason[sid].Title, listSubtitle[i].Caption);
				if (getValue == DictSeason[sid].Title.Length) {
					flag = i;
					minIndex = i;
					break;
				}
			}

			if (flag > 0 || minValue <= Math.Min(DictSeason[sid].Title.Length, DictSeason[sid].KeyName.Length)) {
				MakeSubtitleActivity(string.Format("{0}요일", WeekString[DictSeason[sid].Week]), listSubtitle, listSubtitle[minIndex]);
			} else {
				MakeSubtitleActivity(string.Format("{0}요일", WeekString[DictSeason[sid].Week]), listSubtitle, new ListData() { Memo = "" });
			}
			imgLoadIndicator.Visibility = Visibility.Collapsed;
		}

		private void btnTorrent_Click(object sender, RoutedEventArgs e) {
			ListData ld = (ListData)(sender as Button).Tag;

			WebClient webDownload = new WebClient() { Proxy = null };
			webDownload.Headers.Add("user-agent", "Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/29.0.1547.76 Safari/537.36");
			Uri uri = new Uri(ld.URL);

			string downPath = string.Format("{0}{1}_{2}.torrent", ffFolder, DateTime.Now.ToString().Replace(':', '_'), GetMD5Hash(ld.Caption));

			webDownload.DownloadFileCompleted += webDownload_DownloadFileCompleted;
			KeyValuePair<string, string> kvp = new KeyValuePair<string, string>(ld.Caption, downPath);
			webDownload.DownloadFileAsync(uri, downPath, kvp);
		}

		private void MakeSubtitleActivity(string title, List<ListData> listCollection, ListData recallparam, int magnification = 1) {
			if (listCollection.Count == 0) { return; }

			Storyboard sb = new Storyboard();

			// Animate previous control if exists

			if (gridSubtitle.Children.Count != 0) {
				DoubleAnimation daBack = GetDoubleAnimation(buttonSubtitleBack, 1, 250 * magnification);
				sb.Children.Add(daBack);

				gridSubtitle.Children[gridSubtitle.Children.Count - 1].IsHitTestVisible = false;
				(gridSubtitle.Children[gridSubtitle.Children.Count - 1] as ScrollViewer).VerticalScrollBarVisibility = ScrollBarVisibility.Hidden;

				ThicknessAnimation taCaptionLast = GetThicknessAnimation(gridSubtitleCaptions.Children[gridSubtitleCaptions.Children.Count - 1], new Thickness(-150, 0, 30, 0), 250 * magnification, false);
				ThicknessAnimation taActivityLast = GetThicknessAnimation(gridSubtitle.Children[gridSubtitle.Children.Count - 1], new Thickness(-150, 0, 0, 0), 250 * magnification, true);

				DoubleAnimation daCaptionLast = GetDoubleAnimation(gridSubtitleCaptions.Children[gridSubtitleCaptions.Children.Count - 1], 0, 200 * magnification);
				DoubleAnimation daActivityLast = GetDoubleAnimation(gridSubtitle.Children[gridSubtitle.Children.Count - 1], 0, 200 * magnification);

				sb.Children.Add(taCaptionLast);
				sb.Children.Add(taActivityLast);

				sb.Children.Add(daCaptionLast);
				sb.Children.Add(daActivityLast);
			} else {
				magnification = 0;
			}

			// Create new control

			TextBlock text = new TextBlock() {
				Text = title, Foreground = Brushes.White,
				HorizontalAlignment = HorizontalAlignment.Center, FontSize = 16, Margin = new Thickness(120 * magnification + 30, 0, 30, 0), Opacity = 0
			};

			ScrollViewer sv = new ScrollViewer() { Margin = new Thickness(150 * magnification, 0, 0, 0), Opacity = 0 };
			StackPanel stack = new StackPanel();
			foreach (ListData ld in listCollection) {
				Button btn = MakeListItem(ld);
				stack.Children.Add(btn);

				btn.Tag = ld;
				btn.Click += btnSubtitleItem_Click;
			}
			sv.Content = stack;

			gridSubtitleCaptions.Children.Add(text);
			gridSubtitle.Children.Add(sv);

			// Animate control

			magnification = recallparam.Memo != "" ? 0 : 1;

			ThicknessAnimation taCaptionNew = GetThicknessAnimation(gridSubtitleCaptions.Children[gridSubtitleCaptions.Children.Count - 1], new Thickness(30, 0, 30, 0), 300 * magnification, false);
			ThicknessAnimation taActivityNew = GetThicknessAnimation(gridSubtitle.Children[gridSubtitle.Children.Count - 1], new Thickness(0, 0, 0, 0), 300 * magnification, true);

			DoubleAnimation daCaptionNew = GetDoubleAnimation(gridSubtitleCaptions.Children[gridSubtitleCaptions.Children.Count - 1], 1 * magnification, 250 * magnification);
			DoubleAnimation daActivityNew = GetDoubleAnimation(gridSubtitle.Children[gridSubtitle.Children.Count - 1], 1 * magnification, 250 * magnification);

			sb.Children.Add(taCaptionNew);
			sb.Children.Add(taActivityNew);

			sb.Children.Add(daCaptionNew);
			sb.Children.Add(daActivityNew);

			sb.Completed += sbMake_Completed;

			sb.Begin(this);

			if (recallparam.Memo != "") {
				RefreshSubtitleActivity(recallparam, 0);
			}
		}

		private void sbMake_Completed(object sender, EventArgs e) {
			if (gridSubtitle.Children.Count >= 2) {
				gridSubtitle.Children[gridSubtitle.Children.Count - 2].Visibility = Visibility.Collapsed;
			}
		}

		private void btnSubtitleItem_Click(object sender, RoutedEventArgs e) {
			ListData kvp = (ListData)(sender as Button).Tag;

			if (kvp.Memo == "anime" || kvp.Memo == "maker") {
				(((sender as Button).Parent as StackPanel).Parent as ScrollViewer).IsHitTestVisible = false;
			}

			RefreshSubtitleActivity(kvp);
		}

		string AnimeTitle = "";
		private async void RefreshSubtitleActivity(ListData ld, int magnification = 1) {
			imgLoadIndicator.Visibility = Visibility.Visible;
			Task<List<ListData>> httpTask;

			if (ld.Memo == "anime") {
				httpTask = GetMakerList(ld.URL);
				List<ListData> listMaker = await httpTask;

				AnimeTitle = ld.Caption;
				MakeSubtitleActivity(ld.Caption, listMaker, new ListData() { Memo = "" }, magnification);
			} else if (ld.Memo == "maker") {
				httpTask = GetFileList(ld.URL);
				List<ListData> listFile = await httpTask;

				listFile.Add(new ListData() { Caption = "블로그로 이동", URL = ld.URL, Memo = "blog" });

				MakeSubtitleActivity(ld.Caption, listFile, new ListData() { Memo = "" }, magnification);
			} else if (ld.Memo == "blog") {
				Process pro = new Process() {
					StartInfo = new ProcessStartInfo() {
						FileName = new UriBuilder(ld.URL).Uri.ToString(),
					}
				};
				pro.Start();
			} else if (ld.Memo == "file") {
				WebClient webDownload = new WebClient() { Proxy = null };
				webDownload.Headers.Add("user-agent", "Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/29.0.1547.76 Safari/537.36");
				Uri uri = new Uri(ld.URL);

				string downPath = string.Format("{0}{1}_{2}", ffFolder, DateTime.Now.ToString().Replace(':', '_'), ld.Caption);

				webDownload.DownloadFileCompleted += webDownload_DownloadFileCompleted;
				KeyValuePair<string, string> kvp = new KeyValuePair<string, string>(ld.Caption, downPath);
				webDownload.DownloadFileAsync(uri, downPath, kvp);
			} else if (ld.Memo == "innerzip") {
				using (ZipArchive archive = ZipFile.OpenRead(ld.URL)) {
					foreach (ZipArchiveEntry entry in archive.Entries) {
						if (entry.FullName == ld.Caption) {
							string str = string.Format("{0}.smi", GetMD5Hash(DateTime.Now + entry.FullName));
							entry.ExtractToFile(Path.Combine(ffFolder, str));

							try {
								int findNumber = FindNumberFromString(ld.Caption);
								string strFilename = "dummy";

								if (LoadingID >= 0) {
									strFilename = string.Format("{0} - {1:D2}.smi", DictSeason[LoadingID].Title, (findNumber >= 0/* && Math.Abs(DictArchive[DictSeason[LoadingID].KeyName].Episode - findNumber) <= 2*/) ? findNumber : DictArchive[DictSeason[LoadingID].KeyName].Episode);
								} else {
									if(findNumber>=0){
										strFilename = string.Format("{0} - {1:D2}.smi", AnimeTitle, findNumber);
									}else{
										strFilename = string.Format("{0}.smi", AnimeTitle);
									}
								}

								CopyFromFilesystem(Path.Combine(ffFolder, str), strFilename, ld.Caption);
							} catch (Exception ex) {
								CopyFromFilesystem(Path.Combine(ffFolder, str), ld.Caption, ld.Caption);
							}
						}
					}
				}
			}

			imgLoadIndicator.Visibility = Visibility.Collapsed;
		}

		private void webDownload_DownloadFileCompleted(object sender, System.ComponentModel.AsyncCompletedEventArgs e) {
			if (e.Error != null) {
				//MessageBox.Show(e.Error.Message);
				return;
			}

			KeyValuePair<string, string> kvp = (KeyValuePair<string, string>)e.UserState;

			string strPath = kvp.Value;
			string[] strExts = strPath.Split('.');
			string strExt = strExts[strExts.Length - 1].ToLower();
			string strFilename = "";

			if (strExt == "smi") {
				strExts = strPath.Split('\\');
				try {
					int findNumber = FindNumberFromString(kvp.Key);
					strFilename = string.Format("{0} - {1:D2}.smi", DictSeason[LoadingID].Title, (findNumber >= 0/* && Math.Abs(DictArchive[DictSeason[LoadingID].KeyName].Episode - findNumber) <= 100*/) ? findNumber : DictArchive[DictSeason[LoadingID].KeyName].Episode);
					CopyFromFilesystem(strPath, strFilename, kvp.Key);
				} catch {
					CopyFromFilesystem(strPath, strExts[strExts.Length - 1], kvp.Key);
				}
			} else if (strExt == "zip") {
				List<ListData> listInnerZip = new List<ListData>();

				using (ZipArchive archive = ZipFile.OpenRead(strPath)) {
					foreach (ZipArchiveEntry entry in archive.Entries) {
						listInnerZip.Add(new ListData() {
							Caption = entry.FullName, URL = strPath, Memo = "innerzip",
						});
					}
				}

				MakeSubtitleActivity(kvp.Key, listInnerZip, new ListData() { Memo = "" }, 1);
			} else {
				Process pro = new Process() {
					StartInfo = new ProcessStartInfo() {
						FileName = strPath
					}
				};
				pro.Start();
			}
		}

		string lastDirectory = "";
		private void CopyFromFilesystem(string fullpath, string filename, string beforename) {
			filename = CleanFileName(filename);

			SaveFileDialog saveDialog = new SaveFileDialog();
			saveDialog.InitialDirectory = lastDirectory;

			saveDialog.Title = string.Format("원제 : {0}", beforename);
			saveDialog.FileName = filename;

			if (saveDialog.ShowDialog() == true) {
				lastDirectory = Path.GetDirectoryName(saveDialog.FileName);
				File.Copy(fullpath, saveDialog.FileName, true);
				SaveData();
			}
		}

		private void SubtitleBack_Click(object sender, RoutedEventArgs e) {
			if (gridSubtitle.Children.Count <= 1) { return; }

			buttonSubtitleBack.IsEnabled = false;
			Storyboard sb = new Storyboard();

			// newest control remove

			if (gridSubtitle.Children.Count == 2) {
				DoubleAnimation daBack = GetDoubleAnimation(buttonSubtitleBack, 0, 150);
				sb.Children.Add(daBack);
			}

			gridSubtitle.Children[gridSubtitle.Children.Count - 1].IsHitTestVisible = false;

			ThicknessAnimation taCaptionLast = GetThicknessAnimation(gridSubtitleCaptions.Children[gridSubtitleCaptions.Children.Count - 1], new Thickness(150, 0, 30, 0), 250, false);
			ThicknessAnimation taActivityLast = GetThicknessAnimation(gridSubtitle.Children[gridSubtitle.Children.Count - 1], new Thickness(150, 0, 0, 0), 250, true);

			DoubleAnimation daCaptionLast = GetDoubleAnimation(gridSubtitleCaptions.Children[gridSubtitleCaptions.Children.Count - 1], 0, 200);
			DoubleAnimation daActivityLast = GetDoubleAnimation(gridSubtitle.Children[gridSubtitle.Children.Count - 1], 0, 200);

			sb.Children.Add(taCaptionLast);
			sb.Children.Add(taActivityLast);

			sb.Children.Add(daCaptionLast);
			sb.Children.Add(daActivityLast);

			// reappear old control

			(gridSubtitle.Children[gridSubtitle.Children.Count - 2] as ScrollViewer).VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
			gridSubtitle.Children[gridSubtitle.Children.Count - 2].Visibility = Visibility.Visible;

			ThicknessAnimation taCaptionNew = GetThicknessAnimation(gridSubtitleCaptions.Children[gridSubtitleCaptions.Children.Count - 2], new Thickness(30, 0, 30, 0), 300, false);
			ThicknessAnimation taActivityNew = GetThicknessAnimation(gridSubtitle.Children[gridSubtitle.Children.Count - 2], new Thickness(0, 0, 0, 0), 300, true);

			DoubleAnimation daCaptionNew = GetDoubleAnimation(gridSubtitleCaptions.Children[gridSubtitleCaptions.Children.Count - 2], 1, 250);
			DoubleAnimation daActivityNew = GetDoubleAnimation(gridSubtitle.Children[gridSubtitle.Children.Count - 2], 1, 250);

			sb.Children.Add(taCaptionNew);
			sb.Children.Add(taActivityNew);

			sb.Children.Add(daCaptionNew);
			sb.Children.Add(daActivityNew);

			sb.Completed += sbBack_Completed;

			sb.Begin(this);
		}

		private void sbBack_Completed(object sender, EventArgs e) {
			gridSubtitleCaptions.Children.RemoveAt(gridSubtitleCaptions.Children.Count - 1);
			gridSubtitle.Children.RemoveAt(gridSubtitle.Children.Count - 1);

			try {
				gridSubtitle.Children[gridSubtitle.Children.Count - 1].IsHitTestVisible = true;
				buttonSubtitleBack.IsEnabled = true;
			} catch { }
		}

		private DoubleAnimation GetDoubleAnimation(UIElement uie, double opacity, double time) {
			DoubleAnimation da = new DoubleAnimation(opacity, TimeSpan.FromMilliseconds(time));
			Storyboard.SetTarget(da, uie);
			Storyboard.SetTargetProperty(da, new PropertyPath(UIElement.OpacityProperty));
			return da;
		}

		private ThicknessAnimation GetThicknessAnimation(UIElement uie, Thickness margin, double time, bool isStack) {
			ThicknessAnimation ta = new ThicknessAnimation(margin, TimeSpan.FromMilliseconds(time)) {
				EasingFunction = new ExponentialEase() { Exponent = 5, EasingMode = EasingMode.EaseOut },
				BeginTime = TimeSpan.FromMilliseconds(50)
			};
			Storyboard.SetTarget(ta, uie);
			if (isStack) {
				Storyboard.SetTargetProperty(ta, new PropertyPath(ScrollViewer.MarginProperty));
			} else {
				Storyboard.SetTargetProperty(ta, new PropertyPath(TextBlock.MarginProperty));
			}
			return ta;
		}

		private void ClearDownloadDialogControl() {
			stackTorrent.Children.Clear();
			gridSubtitle.Children.Clear();
			gridSubtitleCaptions.Children.Clear();
			buttonSubtitleBack.Opacity = 0;
		}

		private int FindNumberFromString(string str) {
			str = str.Replace("1280x720", "").Replace("x264", "").Replace("1920x1080", "").Replace("720p", "").Replace("1080p", "").Replace("v2", "");

			int sIndex = 0, eIndex = -1, value = -1;
			for (int i = str.Length - 1; i >= 0; i--) {
				if (eIndex < 0 && isNumber(str[i])) { eIndex = i; }
				if (eIndex >= 0 && !isNumber(str[i])) {
					sIndex = i + 1;
					break;
				}
			}

			if (eIndex < 0) { return -1; }
			string sub = str.Substring(sIndex, eIndex - sIndex + 1);

			try {
				value = Convert.ToInt32(sub);
			} catch { }

			return value;
		}

		private bool isNumber(char c) {
			try {
				int v = Convert.ToInt32(c.ToString());
			} catch { return false; }
			return true;
		}

		public static string GetMD5Hash(string md5input) {
			md5input = md5input.ToLower();
			MD5CryptoServiceProvider md5x = new MD5CryptoServiceProvider();
			byte[] md5bs = Encoding.UTF8.GetBytes(md5input);
			md5bs = md5x.ComputeHash(md5bs);
			StringBuilder md5s = new StringBuilder();
			foreach (byte md5b in md5bs) { md5s.Append(md5b.ToString("x2").ToLower()); }
			return md5s.ToString();
		}

		private static string CleanFileName(string fileName) {
			return Path.GetInvalidFileNameChars().Aggregate(fileName, (current, c) => current.Replace(c.ToString(), string.Empty));
		}
	}
}
