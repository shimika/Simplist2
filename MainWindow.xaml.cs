using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace Simplist2 {
	/// <summary>
	/// MainWindow.xaml에 대한 상호 작용 논리
	/// </summary>
	public partial class MainWindow : Window {
		public MainWindow() {
			InitializeComponent();

			this.Left = System.Windows.Forms.Screen.PrimaryScreen.WorkingArea.Right - 450;
			this.Top = System.Windows.Forms.Screen.PrimaryScreen.WorkingArea.Height / 2 - 300;

			gridAdd.Visibility = Visibility.Visible;

			this.Activated += (o, e) => {
				grideffectShadow.BeginAnimation(DropShadowEffect.OpacityProperty, new DoubleAnimation(0.4, TimeSpan.FromMilliseconds(100)));
				RefreshWeekHead();

				if (PageMode == "notice") {
					RefreshNoticeControl(ListNotice, false, false);
				}
			};
			this.Deactivated += (o, e) => grideffectShadow.BeginAnimation(DropShadowEffect.OpacityProperty, new DoubleAnimation(0.1, TimeSpan.FromMilliseconds(100)));

			SColor = FindResource("sColor") as SolidColorBrush;
			BColor = FindResource("bColor") as SolidColorBrush;

			/*
			this.Loaded += (o, e) => {
				Grid grid = MakeItem(0, true);
				stackSeason5.Children.Add(grid);
			};
			*/
		}

		string Ver = "1.2.2";

		SolidColorBrush SColor, BColor;
		public System.Windows.Forms.NotifyIcon ni = new System.Windows.Forms.NotifyIcon();

		private void Window_Loaded(object sender, RoutedEventArgs e) {
			if (Directory.Exists(animePath)) { isMaster = true; }

			Init();
			InitTray();

			GetArchiveData();
			GetSeasonData();
			RefreshcomboboxLink();
			PrepareSchedule();
			RefreshWeekData();

			isPreset = true;

			int nCount = 0;
			for (int i = 0; i < WeekDay; i++) {
				if (WeekCount[i] != 0) {
					nCount += WeekCount[i] + 1;
				}
			}

			if (WeekCount[WeekDay] != 0) { scrollSeason.ScrollToVerticalOffset(nCount * 40); }

			stackSeason.Focus();

			DispatcherTimer dtmCrawl = new DispatcherTimer() {
				Interval = TimeSpan.FromMinutes(10),
			};
			dtmCrawl.Tick += dtmCrawl_Tick;
			dtmCrawl.Start();

			RefreshNoticeList(true, false);

			textVersion.Text = string.Format("Ver. {0}", Ver);
		}

		int[] WeekCount = new int[7];
		private void RefreshWeekData() {
			for (int i = 0; i <= 6; i++) {
				WeekCount[i] = (FindName(string.Format("stackSeasonItem{0}", i)) as StackPanel).Children.Count;
				(FindName(string.Format("stackSeason{0}", i)) as StackPanel).Visibility =
					WeekCount[i] != 0 ? Visibility.Visible : Visibility.Collapsed;
			}
		}

		public bool ProcessCommandLineArgs(IList<string> args) {
			new AltTab().ShowAltTab(this);
			this.Opacity = 1;
			this.Activate();

			if (args == null || args.Count == 0) { return true; }

			return true;
		}

		private void textboxTitle_TextChanged(object sender, TextChangedEventArgs e) {
			if (AddMode != "season") { return; }
			if (textboxTitle.Text.Trim() == "") {
				comboboxLink.SelectedIndex = 0;
				return;
			}

			int focusIndex = 0, maxValue = 0, matchCount, textLength = textboxTitle.Text.Length;
			string existString;

			for (int i = 1; i < comboboxLink.Items.Count; i++) {
				existString = (comboboxLink.Items[i] as ComboBoxPairs).Key;
				int minLength = Math.Min(textboxTitle.Text.Length, existString.Length);

				matchCount = StringPrefixMatch(textboxTitle.Text.Substring(0, minLength), existString.Substring(0, minLength));

				if (matchCount > maxValue && matchCount * 2 >= textLength) {
					maxValue = matchCount;
					focusIndex = i;
				}
			}

			comboboxLink.SelectedIndex = focusIndex;
		}

		private void Window_KeyDown(object sender, KeyEventArgs e) {
			switch (e.Key) {
				case Key.Escape:
					if (AddMode != "close") {
						AnimateAddDialog("close");
					} else if (isDownOpened == 1) {
						isDownOpened = 0;
						ToggleDownloadDialog();
					} else if (Setting.IsTray) {
						buttonClose.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
					}
					break;
			}
		}

		private void buttonScreenshot_Click(object sender, RoutedEventArgs e) {
			SaveScreenShot(stackSeason, 0);
		}

		private void SaveScreenShot(Panel uie, int margin) {
			RenderTargetBitmap renderTarget = new RenderTargetBitmap(
				(int)uie.ActualWidth + margin, (int)uie.ActualHeight, 96, 96, PixelFormats.Pbgra32);

			DrawingVisual dv = new DrawingVisual();
			using (DrawingContext dc = dv.RenderOpen()) {
				VisualBrush vb = new VisualBrush(uie);

				dc.DrawRectangle(vb, null, new Rect(new Point(), new Size((int)uie.ActualWidth + margin, (int)uie.ActualHeight)));
			}
			renderTarget.Render(dv);


			PngBitmapEncoder encoder = new PngBitmapEncoder();
			BitmapFrame outputFrame = BitmapFrame.Create(renderTarget);
			encoder.Frames.Add(outputFrame);

			string desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
			string path = "";
			for (int i = 1; i <= 1000000; i++) {
				if (!File.Exists(System.IO.Path.Combine(desktop, string.Format("Simplist{0}.png", i)))) {
					path = System.IO.Path.Combine(desktop, string.Format("Simplist{0}.png", i));
					break;
				}
			}

			if (path == "") { return; }

			using (var file = File.OpenWrite(path)) {
				encoder.Save(file);
			}

			ShowGlobalMessage("Screenshot saved", Brushes.DarkMagenta);
		}
	}
}
