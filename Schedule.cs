using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace Simplist2 {
	public partial class MainWindow : Window {

		int NowSelectedWeekDay = 0;
		private void PrepareSchedule() {
			for (int i = 0; i < 7; i++) {
				DictSchedule[i] = new List<LData>();
			}
		}

		Dictionary<int, List<LData>> DictSchedule = new Dictionary<int, List<LData>>();

		private async void ScheduleList(int weekCode, bool isInit = false) {
			gridWeekSchedule.IsHitTestVisible = false;
			stackSchedule.IsHitTestVisible = false;

			foreach (Button button in gridWeekSchedule.Children) {
				(button.Content as TextBlock).Foreground = BColor;
				button.IsEnabled = true;
			}
			((gridWeekSchedule.Children[weekCode] as Button).Content as TextBlock).Foreground = Brushes.White;
			(gridWeekSchedule.Children[weekCode] as Button).IsEnabled = false;

			scrollSchedule.BeginAnimation(ScrollViewer.OpacityProperty, new DoubleAnimation(0, TimeSpan.FromMilliseconds(50)));
			await WaitASecond(50);
			scrollSchedule.ScrollToTop();
			stackSchedule.Children.Clear();

			if (DictSchedule[weekCode].Count == 0) {
				Task<List<ListData>> httpTask;
				httpTask = GetWeekdayList(weekCode);
				List<ListData> listSchedule = await httpTask;

				foreach (ListData ld in listSchedule) {
					DictSchedule[weekCode].Add(new LData() {
						Caption = ld.Caption, Week = ld.Time / 10000,
						Hour = (ld.Time % 10000) / 100, Minute = ld.Time % 100,
					});
				}
			}

			foreach (LData ld in DictSchedule[weekCode]) {
				Button gridBase = MakeScheduleItem(ld);
				stackSchedule.Children.Add(gridBase);
			}

			scrollSchedule.BeginAnimation(ScrollViewer.OpacityProperty, new DoubleAnimation(1, TimeSpan.FromMilliseconds(50)));
			gridWeekSchedule.IsHitTestVisible = true;
			stackSchedule.IsHitTestVisible = true;

			NowSelectedWeekDay = WeekDay;
		}

		private void ButtonSchedule_Click(object sender, RoutedEventArgs e) {
			int weekCode = Convert.ToInt32((sender as Button).Tag.ToString());
			ScheduleList(weekCode);
		}

		private void buttonScheduleListItem_Click(object sender, RoutedEventArgs e) {
			BlinkText(sender as Button);
			LData ld = (LData)(sender as Button).Tag;

			textboxTitle.Text = ld.Caption;
			textboxHour.Text = ld.Hour.ToString("00");
			textboxMinute.Text = ld.Minute.ToString("00");
			comboboxWeekday.SelectedIndex = ld.Week;

			textboxSearchTag.Focus();
		}

		public Task<string> WaitASecond(double millisecond) {
			return Task.Run(() => {
				Thread.Sleep(300);
				return "";
			});
		}
	}
}
