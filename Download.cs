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

			// Torrent Download

			Task<List<ListData>> httpTask;
			httpTask = GetTorrentList(DictSeason[sid].Keyword);
			List<ListData> listTorrent = await httpTask;

			if (LoadingID != sid || listTorrent.Count == 0) {
				imgLoadIndicator.Visibility = Visibility.Collapsed;
				return;
			}

			imgLoadIndicator.Visibility = Visibility.Collapsed;
			ClearDownloadDialogControl();

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

			httpTask = GetWeekdayList(DictSeason[sid].Week);
			List<ListData> listSubtitle = await httpTask;

			if (LoadingID != sid || listSubtitle.Count == 0) {
				imgLoadIndicator.Visibility = Visibility.Collapsed;
				return;
			}

			imgLoadIndicator.Visibility = Visibility.Collapsed;
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
			string downPath = string.Format("{0}{1:MM-dd HH_mm_ss}_{2}.torrent", ffFolder, DateTime.Now, GetMD5Hash(ld.Caption));

			DownloadProcess(ld.URL, downPath, ld.Caption);
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
				bool isHakerano = false;
				if (ld.Caption.IndexOf("하케라노") >= 0) {
					isHakerano = true;
				}

				httpTask = GetFileList(ld.URL, isHakerano);
				List<ListData> listFile = await httpTask;

				listFile.Add(new ListData() { Caption = "블로그로 이동", URL = ld.URL, Memo = "blog" });

				MakeSubtitleActivity(ld.Caption, listFile, new ListData() { Memo = "" }, magnification);
			} else if (ld.Memo == "blog") {
				if (ld.URL == "") {
					ShowGlobalMessage("Can't verify url", Brushes.Crimson);
				} else {
					Process pro = new Process() {
						StartInfo = new ProcessStartInfo() {
							FileName = new UriBuilder(ld.URL).Uri.ToString(),
						}
					};
					pro.Start();
				}
			} else if (ld.Memo == "file") {
				if (Path.GetExtension(ld.Caption) == "") {
					ld.Caption += ".zip";
				}

				string downPath = string.Format("{0}{1:MM-dd HH_mm_ss}_{2}", ffFolder, DateTime.Now, ld.Caption);
				DownloadProcess(ld.URL, downPath, ld.Caption);
			} else if (ld.Memo == "innerzip") {
				using (ZipArchive archive = ZipFile.OpenRead(ld.URL)) {
					foreach (ZipArchiveEntry entry in archive.Entries) {
						if (entry.FullName == ld.Caption) {
							string[] name = entry.FullName.Split(new char[] { '.' }, StringSplitOptions.RemoveEmptyEntries);
							string ext = name[name.Length - 1];

							string str = string.Format("{0}.{1}", GetMD5Hash(DateTime.Now + entry.FullName), ext);
							entry.ExtractToFile(Path.Combine(ffFolder, str));

							try {
								int findNumber = FindNumberFromString(ld.Caption);
								string strFilename = "dummy";

								if (LoadingID >= 0) {
									strFilename = string.Format("{0} - {1:D2}.{2}"
										, DictSeason[LoadingID].Title
										, (findNumber >= 0/* && Math.Abs(DictArchive[DictSeason[LoadingID].KeyName].Episode - findNumber) <= 2*/) ? findNumber : DictArchive[DictSeason[LoadingID].KeyName].Episode
										, ext);
								} else {
									if (findNumber >= 0) {
										strFilename = string.Format("{0} - {1:D2}.{2}", AnimeTitle, findNumber, ext);
									} else {
										strFilename = string.Format("{0}.{1}", AnimeTitle, ext);
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

		private async void DownloadProcess(string url, string path, string caption) {
			Task<bool> httpTask;
			httpTask = DownloadFile(url, path, caption);
			bool isOK = await httpTask;

			if (!isOK) { return; }

			string[] strExts = path.Split('.');
			string strExt = strExts[strExts.Length - 1].ToLower();
			string strFilename = "";

			if (strExt == "smi") {
				strExts = path.Split('\\');

				try {
					int findNumber = FindNumberFromString(caption);

					if (LoadingID >= 0) {
						strFilename = string.Format("{0} - {1:D2}.smi", 
							DictSeason[LoadingID].Title, 
							findNumber >= 0 ? findNumber : DictArchive[DictSeason[LoadingID].KeyName].Episode);
					} else {
						if (findNumber >= 0) {
							strFilename = string.Format("{0} - {1:D2}.smi", AnimeTitle, findNumber);
						} else {
							strFilename = string.Format("{0}.smi", AnimeTitle);
						}
					}
					CopyFromFilesystem(path, strFilename, caption);
				} catch {
					CopyFromFilesystem(path, strExts[strExts.Length - 1], caption);
				}
			} else if (strExt == "zip" || strExt == "jpg") {
				List<ListData> listInnerZip = new List<ListData>();

				using (ZipArchive archive = ZipFile.OpenRead(path)) {
					foreach (ZipArchiveEntry entry in archive.Entries) {
						listInnerZip.Add(new ListData() {
							Caption = entry.FullName, URL = path, Memo = "innerzip",
						});
					}
				}

				MakeSubtitleActivity(caption, listInnerZip, new ListData() { Memo = "" }, 1);
			} else {
				Process pro = new Process() {
					StartInfo = new ProcessStartInfo() {
						FileName = path
					}
				};
				pro.Start();
			}
		}

		private Task<bool> DownloadFile(string url, string path, string caption) {
			return Task.Run(() => {
				HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create(new UriBuilder(url).Uri);

				httpWebRequest.ContentType = "application/x-www-form-urlencoded; charset=utf-8";
				httpWebRequest.Method = "GET";
				httpWebRequest.Referer = "www.google.com";
				httpWebRequest.UserAgent =
					"Mozilla/4.0 (compatible; MSIE 8.0; Windows NT 6.0; WOW64; " +
					"Trident/4.0; SLCC1; .NET CLR 2.0.50727; Media Center PC 5.0; " +
					".NET CLR 3.5.21022; .NET CLR 3.5.30729; .NET CLR 3.0.30618; " +
					"InfoPath.2; OfficeLiveConnector.1.3; OfficeLivePatch.0.0)";
				httpWebRequest.ContentLength = 0;
				httpWebRequest.Credentials = CredentialCache.DefaultCredentials;

				try {
					httpWebRequest.Proxy = null;

					HttpWebResponse httpWebResponse = (HttpWebResponse)httpWebRequest.GetResponse();
					using (var stream = httpWebResponse.GetResponseStream()) {
						using (FileStream fstream = new FileStream(path, FileMode.Create)) {
							var buffer = new byte[8192];
							var maxCount = buffer.Length;
							int count;
							while ((count = stream.Read(buffer, 0, maxCount)) > 0)
								fstream.Write(buffer, 0, count);
						}
					}
				} catch(Exception ex) {
					MessageBox.Show(ex.Message);
					return false;
				}

				return true;
			});
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
