using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace Simplist2 {
	public partial class MainWindow : Window {

		Dictionary<string, Button> DictNoticeControl = new Dictionary<string, Button>();
		Dictionary<string, string> DictNoticeTag = new Dictionary<string, string>();
		string log = "";

		bool isUpdating = false;
		DateTime LastCheck = new DateTime(1970, 1, 1);
		private async void RefreshNoticeList(bool Init = false, bool isAuto = true) {
			int area = 0;

			try {
				if (!isAuto && !Init) { ShowGlobalMessage("Updating...", Brushes.BlueViolet); }

				if (isUpdating) { return; }
				isUpdating = true;

				area = 1;

				Task<List<ListData>> httpTask;
				Dictionary<string, ListData> dictCrawl = new Dictionary<string, ListData>();
				List<ListData> listAnime, listMaker;

				for (int i = -2; i <= 0; i++) {
					try {
						httpTask = GetWeekdayList((WeekDay + i + 7) % 7);
						listAnime = await httpTask;
					} catch {
						return;
					}

					if (listAnime.Count == 0) {
						dictCrawl.Clear();
						break;
					}

					foreach (ListData ldTitle in listAnime) {
						httpTask = GetMakerList(ldTitle.URL, true);
						listMaker = await httpTask;

						if (listMaker.Count == 0) { continue; }

						ListData ld = listMaker[0];
						ld.Caption = ldTitle.Caption;
						ld.URL = ldTitle.URL;

						log += string.Format("{0} : {1}\n", ldTitle.ID, ldTitle.Caption);

						dictCrawl.Add(ldTitle.ID, ld);
						listMaker.Clear();
					}

					listAnime.Clear();
				}

				area = 2;

				if (dictCrawl.Count == 0) {
					if (!isAuto) { ShowGlobalMessage("Update failed", Brushes.Crimson); }
					isUpdating = false;
					return;
				}

				await this.Dispatcher.BeginInvoke(new Action(() => {
					RefreshNoticeControl(dictCrawl.Values
						.OrderByDescending(x => x.UpdateTime)
						.ToList()
						, Init, isAuto);
				}));

				isUpdating = false;
				if (!isAuto && !Init) { ShowGlobalMessage("Update complete", Brushes.MediumOrchid); }

			} catch (Exception ex) {
				if (!isMaster) { return; }

				//MessageBox.Show(ex.Message + "\n" + "Area" + area);
			}
		}

		List<ListData> ListNotice = new List<ListData>();
		private void RefreshNoticeControl(List<ListData> list, bool isInit, bool isAuto) {
			try {
				if (list.Count == 0) { return; }

				ListNotice = list;

				if (this.Visibility == System.Windows.Visibility.Visible) {
					stackNotice.Children.Clear();
				}

				int NewCount = 0;
				string NewTitle = "";

				if (!isAuto) { LastCheck = DateTime.Now; }

				for (int i = 0; i < list.Count; i++) {
					ListData ld = list[i];

					TimeSpan tsTime = DateTime.Now - ld.UpdateTime;
					TimeSpan svTime = DateTime.Now - LastCheck;

					// Add new 

					if (ld.UpdateTime.Year == 1900) {
						ld.Memo = "";
					} else if (tsTime.TotalSeconds < 60) {
						ld.Memo = string.Format("{0}초 전", (int)tsTime.TotalSeconds);
					} else if (tsTime.TotalMinutes < 60) {
						ld.Memo = string.Format("{0}분 전", (int)tsTime.TotalMinutes);
					} else if (tsTime.TotalHours < 24) {
						ld.Memo = string.Format("{0}시간 전", (int)tsTime.TotalHours);
					} else {
						if (ld.UpdateTime.Year != DateTime.Now.Year) {
							ld.Memo = string.Format("{0}/{1}/{2} {3}:{4:D2}", ld.UpdateTime.Year, ld.UpdateTime.Month, ld.UpdateTime.Day, ld.UpdateTime.Hour, ld.UpdateTime.Minute);
						} else {
							ld.Memo = string.Format("{0}/{1} {2}:{3:D2}", ld.UpdateTime.Month, ld.UpdateTime.Day, ld.UpdateTime.Hour, ld.UpdateTime.Minute);
						}
					}

					if (this.Visibility == System.Windows.Visibility.Visible) {
						Button button = GetNoticeControl(ld);
						button.Click += buttonNoticeItem_Click;

						stackNotice.Children.Add(button);
					}

					// Check new

					if (svTime > tsTime) {
						NewCount++;
						if (NewTitle == "") {
							NewTitle = ld.Caption;
						}
					}
				}

				if (NewCount > 0 && isAuto && !isInit) {
					if (PageMode != "notice" && this.Visibility == System.Windows.Visibility.Visible) {
						if (NewCount >= 10) {
							buttonInnerNotice.Text = "Notice 9+";
						} else {
							buttonInnerNotice.Text = string.Format("Notice {0}", NewCount);
						}
					}

					if (Setting.IsNoti) {
						ni.ShowBalloonTip(3000,
							"자막 알림",
							NewCount == 1
								? string.Format("{0}의 자막이 등록되었습니다.", NewTitle)
								: string.Format("{0}외 {1}개의 애니메이션 자막이 새로 등록되었습니다.", NewTitle, NewCount - 1),
							System.Windows.Forms.ToolTipIcon.Info);
					}
				}
			} catch (Exception ex) {
				MessageBox.Show(ex.Message + "\n" + "RefreshNoticeControl : Notice.cs");
			}
		}

		private void RefreshNoticeTime(List<ListData> list) {
			stackNotice.Children.Clear();
		}

		private void LogN(string str) {
			if (!isMaster) { return; }
			using (StreamWriter sw = new StreamWriter(@"C:\Simplist2log.txt", true)) {
				sw.WriteLine(string.Format("{0}", str));
			}
		}

		private void buttonNoticeItem_Click(object sender, RoutedEventArgs e) {
			ClearDownloadDialogControl();
			buttonTabSubtitle.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));

			isDownOpened = 1;
			ToggleDownloadDialog(false);

			RefreshSubtitleActivity((ListData)(sender as Button).Tag);
		}
	}

	class DescComparer<T> : IComparer<T> {
		public int Compare(T x, T y) {
			return Comparer<T>.Default.Compare(y, x);
		}
	}
}
