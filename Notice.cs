using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace Simplist2 {
	public partial class MainWindow : Window {

		Dictionary<string, Button> DictNoticeControl = new Dictionary<string, Button>();
		Dictionary<string, string> DictNoticeTag = new Dictionary<string, string>();
		Dictionary<string, bool> DictUnread = new Dictionary<string, bool>();

		bool isUpdating = false;
		private async void RefreshNoticeList(bool isInit = false, bool isAuto = true) {
			if (!isAuto) { ShowGlobalMessage("Updating...", Brushes.BlueViolet); }

			if (isUpdating) { return; }
			isUpdating = true;

			RefreshWeekHead();
			Task<List<ListData>> httpTask;
			Dictionary<string, ListData> dictCrawl = new Dictionary<string, ListData>();
			List<ListData> listAnime, listMaker;

			string changeTitle = "", changeMaxValue = "";

			for (int i = -2; i <= 0; i++) {
				httpTask = GetWeekdayList((WeekDay + i + 7) % 7);
				listAnime = await httpTask;

				if (listAnime.Count == 0) {
					if (!isAuto) { ShowGlobalMessage("Update failed", Brushes.Crimson); }
					isUpdating = false;
					return;
				}

				foreach (ListData ldTitle in listAnime) {
					httpTask = GetMakerList(ldTitle.URL, true);
					listMaker = await httpTask;

					if (listMaker.Count == 0) { continue; }

					ListData ld = listMaker[0];
					ld.Caption = ldTitle.Caption;
					ld.URL = ldTitle.URL;

					dictCrawl.Add(ldTitle.ID, ld);
					listMaker.Clear();
				}

				listAnime.Clear();
			}

			// Remove dummy data

			List<KeyValuePair<string, string>> listNoticeClone = DictNoticeTag.ToList();

			foreach (KeyValuePair<string, string> kvp in listNoticeClone) {
				try {
					if (!dictCrawl.ContainsKey(kvp.Key) || dictCrawl[kvp.Key].ID != kvp.Value) {
						stackNotice.Children.Remove(DictNoticeControl[kvp.Key]);
						DictNoticeControl.Remove(kvp.Key);
						DictNoticeTag.Remove(kvp.Key);

						if (dictCrawl.ContainsKey(kvp.Key)) {
							LogN(string.Format("removed : {0}", dictCrawl[kvp.Key].Caption));
						} else {
							DictUnread.Remove(kvp.Key);
						}
					}
				} catch (Exception ex) { }
			}

			listNoticeClone.Clear();


			// Insert new data

			List<string> listPosition = DictNoticeTag.Values.ToList().OrderByDescending(x => x).ToList();
			int lBound;

			foreach (KeyValuePair<string, ListData> ld in dictCrawl) {
				if (!DictNoticeTag.ContainsKey(ld.Key)) {
					if (!DictUnread.ContainsKey(ld.Key)) {
						DictUnread.Add(ld.Key, true);
					}

					if (string.Compare(ld.Value.ID, changeMaxValue) >= 0) {
						changeMaxValue = ld.Value.ID;
						changeTitle = ld.Value.Caption;
					}

					lBound = DictNoticeTag.Count;

					for (int i = 0; i < listPosition.Count; i++) {
						if (string.Compare(ld.Value.ID, listPosition[i]) >= 0) {
							lBound = i;
							break;
						}
					}

					Button button = GetNoticeControl(ld.Value);
					button.Click += buttonNoticeItem_Click;

					listPosition.Insert(lBound, ld.Value.ID);
					DictNoticeTag.Add(ld.Key, ld.Value.ID);
					DictNoticeControl.Add(ld.Key, button);

					stackNotice.Children.Insert(lBound, button);
				}
			}

			// Rewrite time info

			DateTime dtNow = DateTime.Now;

			foreach (Button button in stackNotice.Children) {
				TextBlock txtTime = (button.Content as Grid).Children[2] as TextBlock;
				DateTime dtSaved = (DateTime)txtTime.Tag;
				TimeSpan tsTime = dtNow - dtSaved;

				if (dtSaved.Year == 1900) {
					txtTime.Text = "";
					continue;
				}

				if (tsTime.TotalSeconds < 60) {
					txtTime.Text = string.Format("{0}초 전", (int)tsTime.TotalSeconds);
				} else if (tsTime.TotalMinutes < 60) {
					txtTime.Text = string.Format("{0}분 전", (int)tsTime.TotalMinutes);
				} else if (tsTime.TotalHours < 24) {
					txtTime.Text = string.Format("{0}시간 전", (int)tsTime.TotalHours);
				} else {
					if (dtSaved.Year != dtNow.Year) {
						txtTime.Text = string.Format("{0}/{1}/{2} {3}:{4:D2}", dtSaved.Year, dtSaved.Month, dtSaved.Day, dtSaved.Hour, dtSaved.Minute);
					} else {
						txtTime.Text = string.Format("{0}/{1} {2}:{3:D2}", dtSaved.Month, dtSaved.Day, dtSaved.Hour, dtSaved.Minute);
					}
				}
			}


			if (DictUnread.Count > 0) {
				if (PageMode != "notice") {
					if (changeMaxValue != "") {
						DateTime dt = DateTime.ParseExact(changeMaxValue.Substring(0, 14), "yyyyMMddHHmmss", CultureInfo.InvariantCulture);
						LogN(changeTitle + " : " + DictUnread.Count + " : " + dt);
					}

					if (isAuto && !isInit) {
						NoticeCount = DictUnread.Count;

						if (NoticeCount >= 10 && PageMode != "notice") {
							buttonInnerNotice.Text = "Notice 9+";
						} else if (NoticeCount > 0 && PageMode != "notice") {
							buttonInnerNotice.Text = string.Format("Notice {0}", NoticeCount);
						}
					}
				} else {
					DictUnread.Clear();
				}
			} else {
				LogN("no refresh");
			}

			isUpdating = false;
			if (!isAuto) { ShowGlobalMessage("Update complete", Brushes.BlueViolet); }
		}

		private void LogN(string str) {
			using (StreamWriter sw = new StreamWriter(@"C:\Simplist2log.txt", true)) {
				sw.WriteLine(string.Format("{0} : {1}", DateTime.Now, str));
			}
		}

		private void buttonNoticeItem_Click(object sender, RoutedEventArgs e) {
			ClearDownloadDialogControl();
			buttonTabSubtitle.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));

			isDownOpened = 1;
			ToggleDownloadDialog(false);

			RefreshSubtitleActivity((ListData)(sender as Button).Tag);
		}

		private Button GetNoticeControl(ListData ld) {
			Button button = new Button() {
				HorizontalAlignment = HorizontalAlignment.Stretch, 
				HorizontalContentAlignment = HorizontalAlignment.Stretch,
				Tag = ld, Background = Brushes.Transparent,
			};

			// Based grid

			Grid gridBase = new Grid() {
				Height = 55,
			};

			// Title text

			TextBlock txtTitle = new TextBlock() {
				FontSize = 16, Text = ld.Caption,
				Margin = new Thickness(15, 5, 0, 0), Width = 300,
				HorizontalAlignment = HorizontalAlignment.Left,
				VerticalAlignment = VerticalAlignment.Top, 
				TextTrimming = TextTrimming.CharacterEllipsis,
			};
			gridBase.Children.Add(txtTitle);

			// Title area

			StackPanel stackTitle = new StackPanel() {
				Orientation = Orientation.Horizontal,
				VerticalAlignment = VerticalAlignment.Bottom,
				Margin = new Thickness(20, 0, 0, 5)
			};

			// Episode text

			TextBlock txtEpisode = new TextBlock() {
				FontSize = 14,
				Foreground = SColor,
				Margin = new Thickness(0, 0, 5, 0),
			};
			txtEpisode.Text = Convert.ToInt32(ld.ID.Substring(14, 4)).ToString("00");
			if (ld.ID.Substring(18, 1) != "0") { txtEpisode.Text += "." + ld.ID.Substring(18, 1); }

			// Maker text

			TextBlock txtMaker = new TextBlock() {
				FontSize = 14, Foreground = Brushes.DimGray,
				Text = ld.ID.Substring(19)
			};

			stackTitle.Children.Add(txtEpisode);
			stackTitle.Children.Add(txtMaker);

			gridBase.Children.Add(stackTitle);

			// Time text
			DateTime dt = new DateTime();

			try {
				dt = DateTime.ParseExact(ld.ID.Substring(0, 14), "yyyyMMddHHmmss", CultureInfo.InvariantCulture);
			} catch {
				dt = new DateTime(1900, 1, 1);
			}

			TextBlock txtTime = new TextBlock() {
				FontSize = 14, Foreground = Brushes.Gray,
				//Text = ld.ID.Substring(0, 14),
				Margin = new Thickness(0, 0, 15, 5),
				HorizontalAlignment = HorizontalAlignment.Right,
				Tag = dt, VerticalAlignment = VerticalAlignment.Bottom,
			};

			gridBase.Children.Add(txtTime);

			// Underbar

			Grid gridSplitter = new Grid() {
				Width = 360, Height = 1, Margin = new Thickness(10, 0, 10, 0),
				VerticalAlignment = VerticalAlignment.Bottom,
				Background = SColor
			};
			gridBase.Children.Add(gridSplitter);

			// set content

			button.Content = gridBase;

			return button;
		}
	}

	class DescComparer<T> : IComparer<T> {
		public int Compare(T x, T y) {
			return Comparer<T>.Default.Compare(y, x);
		}
	}
}
