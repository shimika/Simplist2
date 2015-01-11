using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Simplist2 {
	public partial class MainWindow : Window {
		// Common Events
		private void buttonClose_Click(object sender, RoutedEventArgs e) { this.Close(); }
		private void gridTitlebar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) { DragMove(); }

		private void ShowGlobalMessage(string message, Brush brush, double duration = 2500) {
			this.Dispatcher.BeginInvoke(new Action(() => {
				textError.Text = message;
				gridError.Background = brush;

				Storyboard sbError = new Storyboard();

				DoubleAnimation daErrorOn = new DoubleAnimation(1, TimeSpan.FromMilliseconds(0));
				DoubleAnimation daErrorOff = new DoubleAnimation(0, TimeSpan.FromMilliseconds(250)) {
					BeginTime = TimeSpan.FromMilliseconds(duration)
				};

				Storyboard.SetTarget(daErrorOn, gridError);
				Storyboard.SetTarget(daErrorOff, gridError);
				Storyboard.SetTargetProperty(daErrorOn, new PropertyPath(Grid.OpacityProperty));
				Storyboard.SetTargetProperty(daErrorOff, new PropertyPath(Grid.OpacityProperty));

				sbError.Children.Add(daErrorOn);
				sbError.Children.Add(daErrorOff);

				sbError.Begin(this);
			}));
		}

		// Add Dialog Window Events

		string PageMode = "season", AddMode = "close";
		int isAddDialogOpened = 0;
		private void buttonAdd_Click(object sender, RoutedEventArgs e) {
			isAddDialogOpened = 1 - isAddDialogOpened;
			AnimateAddDialog(isAddDialogOpened == 1 ? PageMode : "close");
		}

		private void buttonRefresh_Click(object sender, RoutedEventArgs e) {
			RefreshNoticeList(false, false);
		}

		private void AnimateAddDialog(string AddWindowMode, int sid = -1, string keyname = "") {
			int isOpen = 1, isAdd = 1, isSeason = 0;

			AddMode = AddWindowMode;
			buttonAdd.IsEnabled = false;

			switch (AddMode) {
				case "season":
					stackSchedule.Children.Clear();
					ScheduleList(WeekDay, true);

					buttonAdd.IsEnabled = true;
					isSeason = 1;
					break;
				case "archive":
					buttonAdd.IsEnabled = true;
					break;
				case "seasonmodify":
					isAdd = 0;
					break;
				case "archivemodify":
					isAdd = 0;
					break;
				case "close":
					buttonAdd.IsEnabled = true;
					isAddDialogOpened = 0;
					isOpen = 0;
					break;
			}

			textboxTitle.IsEnabled = true;
			if (isOpen == 1) {
				if (isAdd == 1) {
					textLinkAlert.Visibility = Visibility.Collapsed;
					comboboxLink.Visibility = Visibility.Visible;
					textboxTitle.Text = textboxHour.Text = textboxMinute.Text = textboxSearchTag.Text = "";
					textMessage.Text = string.Format("Add to {0}", PageMode);

					if (PageMode == "season") {
						stackSeasonForm.Visibility = Visibility.Visible;
						textAddTitleType.Text = "제목";
					} else {
						stackSeasonForm.Visibility = Visibility.Collapsed;
						textAddTitleType.Text = "원제";
					}

					comboboxLink.SelectedIndex = 0;
				} else {
					textLinkAlert.Visibility = Visibility.Collapsed;
					comboboxLink.Visibility = Visibility.Collapsed;

					textMessage.Text = "Modify data";

					if (sid >= 0) {
						stackSeasonForm.Visibility = Visibility.Visible;

						textboxTitle.Text = DictSeason[sid].Title;
						textboxHour.Text = (DictSeason[sid].Time / 100).ToString("00");
						textboxMinute.Text = (DictSeason[sid].Time % 100).ToString("00");
						textboxSearchTag.Text = DictSeason[sid].Keyword;

						comboboxWeekday.SelectedIndex = DictSeason[sid].Week;

						textLinkedTitle.Text = DictSeason[sid].KeyName;

						keyname = DictSeason[sid].KeyName;
					} else {
						stackSeasonForm.Visibility = Visibility.Collapsed;
						textboxTitle.IsEnabled = false;

						textboxTitle.Text = DictArchive[keyname].KeyName;
						if (DictArchive[keyname].isLinked) {
							textLinkAlert.Visibility = Visibility.Visible;
							textLinkAlert.Text = string.Format("\'{0}\'라는 이름으로 시즌 데이터에 연결되어 있습니다\n\n이 항목을 지울 경우, 시즌 데이터도 지워집니다.", DictSeason[DictArchive[keyname].SID].Title);
						}

						sid = DictArchive[keyname].SID;
					}
					((buttonEnableDisable as Button).Content as TextBlock).Text = DictArchive[keyname].Episode >= 0 ? "Disable this item" : "Enable this item";
				}


				textboxTitle.Focus();

			} else {
				textMessage.Text = "";
				gridError.BeginAnimation(Grid.OpacityProperty, new DoubleAnimation(0, TimeSpan.FromMilliseconds(50)));
			}

			gridAdd.IsHitTestVisible = isOpen == 1 ? true : false;

			Storyboard sbAddDialog = new Storyboard();

			ThicknessAnimation taDialog = new ThicknessAnimation(new Thickness(0, 150 * (isOpen - 1), 0, 0), TimeSpan.FromMilliseconds(400)) {
				EasingFunction = new ExponentialEase() {
					Exponent = 7, EasingMode = EasingMode.EaseOut
				}, BeginTime = TimeSpan.FromMilliseconds(50 * isOpen),
			};
			ThicknessAnimation taSchedule = new ThicknessAnimation(new Thickness(0, 0, 0, (isSeason) * isOpen * 300 - 300), TimeSpan.FromMilliseconds(400)) {
				EasingFunction = new ExponentialEase() {
					Exponent = 7, EasingMode = EasingMode.EaseOut
				}, BeginTime = TimeSpan.FromMilliseconds(50 * isOpen),
			};
			ThicknessAnimation taRemove = new ThicknessAnimation(new Thickness(0, 0, 0, (1 - isAdd) * isOpen * 80 - 80), TimeSpan.FromMilliseconds(150)) {
				EasingFunction = new ExponentialEase() {
					Exponent = 7, EasingMode = EasingMode.EaseOut
				}, BeginTime = TimeSpan.FromMilliseconds(50 * isOpen),
			};
			DoubleAnimation daCover = new DoubleAnimation(1 * isOpen, TimeSpan.FromMilliseconds(300));

			Storyboard.SetTarget(taDialog, stackAddDialog);
			Storyboard.SetTargetProperty(taDialog, new PropertyPath(StackPanel.MarginProperty));

			Storyboard.SetTarget(daCover, gridAdd);
			Storyboard.SetTargetProperty(daCover, new PropertyPath(Grid.OpacityProperty));

			Storyboard.SetTarget(taSchedule, gridSchedule);
			Storyboard.SetTargetProperty(taSchedule, new PropertyPath(Grid.MarginProperty));

			Storyboard.SetTarget(taRemove, gridRemoveItem);
			Storyboard.SetTargetProperty(taRemove, new PropertyPath(Grid.MarginProperty));

			sbAddDialog.Children.Add(taDialog);
			sbAddDialog.Children.Add(daCover);
			sbAddDialog.Children.Add(taSchedule);
			sbAddDialog.Children.Add(taRemove);

			sbAddDialog.Begin(this);
		}

		private void gridAddDialogCover_MouseDown(object sender, MouseButtonEventArgs e) { AnimateAddDialog("close"); }
		private void buttonPopupCancel_Click(object sender, RoutedEventArgs e) { AnimateAddDialog("close"); }

		// Image Resource Function

		public BitmapImage rtSource(string uriSource) {
			uriSource = "pack://application:,,,/Simplist2;component/Resources/" + uriSource;
			BitmapImage source = new BitmapImage(new Uri(uriSource));
			return source;
		}

		// Download Dialog Window Events

		int isDownOpened = 0;
		private void ToggleDownloadDialog(bool isTorrentVisible = true) {
			gridDown.IsHitTestVisible = isDownOpened == 1 ? true : false;
			if (isDownOpened == 0) {
				LoadingID = -1;
			} else {
				buttonTabTorrent.Visibility = isTorrentVisible ? Visibility.Visible : Visibility.Collapsed;
			}

			Storyboard sbDown = new Storyboard();

			DoubleAnimation daBack = GetDoubleAnimation(buttonSubtitleBack, 0, 250);

			ThicknessAnimation taDown = new ThicknessAnimation(new Thickness(0, 0, 0, (isDownOpened - 1) * 100), TimeSpan.FromMilliseconds(200)) {
				EasingFunction = new ExponentialEase() {
					Exponent = 4, EasingMode = EasingMode.EaseOut
				}, BeginTime = TimeSpan.FromMilliseconds(50 * isDownOpened),
			};
			DoubleAnimation daCover = new DoubleAnimation(1 * isDownOpened, TimeSpan.FromMilliseconds(200));

			Storyboard.SetTarget(taDown, gridDownDialog);
			Storyboard.SetTargetProperty(taDown, new PropertyPath(Grid.MarginProperty));

			Storyboard.SetTarget(daCover, gridDown);
			Storyboard.SetTargetProperty(daCover, new PropertyPath(Grid.OpacityProperty));

			sbDown.Children.Add(daBack);
			sbDown.Children.Add(taDown);
			sbDown.Children.Add(daCover);
			sbDown.Begin(this);
		}

		private void gridDownCover_MouseDown(object sender, MouseButtonEventArgs e) {
			isDownOpened = 0;
			ToggleDownloadDialog();
		}

		// Tab Control Events

		private void buttonTabSeason_Click(object sender, RoutedEventArgs e) { ChangeTab("season"); }
		private void buttonTabArchive_Click(object sender, RoutedEventArgs e) { ChangeTab("archive"); }
		private void buttonTabSetting_Click(object sender, RoutedEventArgs e) { ChangeTab("setting"); }
		private void buttonTabNotify_Click(object sender, RoutedEventArgs e) {
			//buttonNoticeNo.Visibility = Visibility.Visible;
			//buttonNoticeYes.Visibility = Visibility.Collapsed;
			ChangeTab("notice");
		}

		private void buttonTabNotice_Click(object sender, RoutedEventArgs e) {
			RefreshNoticeControl(ListNotice, false, false);
			buttonInnerNotice.Text = "Notice";
			ChangeTab("notice");
		}

		private void ChangeTab(string tag) {
			PageMode = tag;

			buttonInnerSeason.Foreground = tag == "season" ? Brushes.White : BColor;
			buttonInnerArchive.Foreground = tag == "archive" ? Brushes.White : BColor;
			buttonInnerSetting.Foreground = tag == "setting" ? Brushes.White : BColor;
			buttonInnerNotice.Foreground = tag == "notice" ? Brushes.White : BColor;
			//buttonNoticeNo.Background = tag == "notice" ? Brushes.White : BColor;
			//buttonNoticeYes.Background = tag == "notice" ? Brushes.White : BColor;

			scrollSeason.Visibility = tag == "season" ? Visibility.Visible : Visibility.Collapsed;
			scrollArchive.Visibility = tag == "archive" ? Visibility.Visible : Visibility.Collapsed;
			stackSetting.Visibility = tag == "setting" ? Visibility.Visible : Visibility.Collapsed;
			gridNotice.Visibility = tag == "notice" ? Visibility.Visible : Visibility.Collapsed;

			buttonShowMode.Visibility = tag == "archive" ? Visibility.Visible : Visibility.Collapsed;
			buttonAdd.Visibility = (tag == "season" || tag == "archive") ? Visibility.Visible : Visibility.Collapsed;
			buttonRefresh.Visibility = tag == "notice" ? Visibility.Visible : Visibility.Collapsed;
			buttonScreenshot.Visibility = tag == "season" ? Visibility.Visible : Visibility.Collapsed;

			buttonTabSeason.IsEnabled = tag == "season" ? false : true;
			buttonTabArchive.IsEnabled = tag == "archive" ? false : true;
			buttonTabSetting.IsEnabled = tag == "setting" ? false : true;
			buttonTabNotice.IsEnabled = tag == "notice" ? false : true;
			//buttonNoticeNo.IsEnabled = tag == "notice" ? false : true;
			//buttonNoticeYes.IsEnabled = tag == "notice" ? false : true;

			switch (tag) {
				case "season": MoveTabIndicator(0); break;
				case "archive": MoveTabIndicator(90); break;
				case "setting": MoveTabIndicator(180); break;
				case "notice":
					scrollNotice.ScrollToTop();
					MoveTabIndicator(270);
					break;
			}
		}

		private void MoveTabIndicator(int leftMargin, double width = 90) {
			rectSelectedIndicator.BeginAnimation(Rectangle.MarginProperty,
				new ThicknessAnimation(new Thickness(leftMargin, 0, 0, 0), TimeSpan.FromMilliseconds(150)) {
					EasingFunction = new ExponentialEase() { EasingMode = EasingMode.EaseOut, Exponent = 6 },
				});
		}

		// Show All (Archive)

		bool ShowAll = false;
		private void buttonShowMode_Click(object sender, RoutedEventArgs e) {
			ShowAll = !ShowAll;
			ToggleArchiveViewMode(ShowAll);
		}

		private void ToggleArchiveViewMode(bool ShowMode) {
			buttonShowMode.Opacity = ShowMode ? 0.6 : 1;

			foreach (KeyValuePair<string, AData> kvp in DictArchive) {
				kvp.Value.IsVisible = ShowMode || kvp.Value.Episode >= 0 ? true : false;
			}
		}

		private void buttonTabTorrent_Click(object sender, RoutedEventArgs e) {
			stackTorrent.Visibility = Visibility.Visible;
			gridTorrentInfo.Visibility = Visibility.Visible;
			gridSubtitle.Visibility = Visibility.Collapsed;
			gridSubtitleInfo.Visibility = Visibility.Collapsed;

			scrollTorrent.IsHitTestVisible = true;
			gridSubtitle.IsHitTestVisible = false;

			(buttonTabTorrent.Content as TextBlock).Foreground = Brushes.White;
			(buttonTabSubtitle.Content as TextBlock).Foreground = BColor;

			buttonTabTorrent.IsHitTestVisible = false;
			buttonTabSubtitle.IsHitTestVisible = true;
		}

		private void buttonTabSubtitle_Click(object sender, RoutedEventArgs e) {
			stackTorrent.Visibility = Visibility.Collapsed;
			gridTorrentInfo.Visibility = Visibility.Collapsed;
			gridSubtitle.Visibility = Visibility.Visible;
			gridSubtitleInfo.Visibility = Visibility.Visible;

			scrollTorrent.IsHitTestVisible = false;
			gridSubtitle.IsHitTestVisible = true;

			(buttonTabTorrent.Content as TextBlock).Foreground = BColor;
			(buttonTabSubtitle.Content as TextBlock).Foreground = Brushes.White;

			buttonTabTorrent.IsHitTestVisible = true;
			buttonTabSubtitle.IsHitTestVisible = false;
		}

		bool isReallyClose = true, isFirstPopup = true;
		private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e) {
			if (isReallyClose) {
				ni.Dispose();
			} else {
				ShowGlobalMessage("", Brushes.Transparent, 0);

				if (isFirstPopup) {
					isFirstPopup = false;
					ni.ShowBalloonTip(3000, "Simplist2", "트레이로 이동되었습니다.\n더블클릭하면 다시 표시됩니다.\n\n종료하려면 트레이 아이콘의 메뉴에서 종료하세요.", System.Windows.Forms.ToolTipIcon.Info);
				}
				e.Cancel = true;
				this.Opacity = 0;
				new AltTab().HideAltTab(this);
			}
		}

		private void dtmCrawl_Tick(object sender, EventArgs e) {
			try {
				RefreshNoticeList(false, true);
			} catch (Exception ex) {
				MessageBox.Show(ex.Message);
			}
		}
	}
}
