using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;

namespace Simplist2 {
	public partial class MainWindow : Window {

		#region Season + archive data

		private Grid MakeMainItem(bool isSeason, int ID = -1, string KeyName = "") {
			Grid gridBase = new Grid() { Height = 40, Background = Brushes.Transparent };
			int[] columns = new int[] { 10, 55, 1, 75, 10 };

			if (!isSeason) { columns[1] = 0; }

			foreach (int i in columns) {
				ColumnDefinition cd;
				if (i == 1) {
					cd = new ColumnDefinition() { Width = new GridLength(i, GridUnitType.Star) };
				} else {
					cd = new ColumnDefinition() { Width = new GridLength(i) };
				}
				gridBase.ColumnDefinitions.Add(cd);
			}

			// Command Button
			Button buttonBase = new Button() { Background = Brushes.Transparent };
			Grid.SetColumnSpan(buttonBase, 5);

			// Option
			Button buttonOption = new Button() { 
				Width = 100, 
				Margin = new Thickness(0, 0, 10, 0), 
				HorizontalAlignment = HorizontalAlignment.Right, 
				Background = FindResource("sColor") as SolidColorBrush };
			Grid.SetColumn(buttonOption, 0);

			// Text Time & Title

			TextBlock txtTime = new TextBlock() { 
				Text = "00:00", 
				FontSize = 16, 
				Margin = new Thickness(5, 0, 0, 0), 
				Foreground = Brushes.Gray, 
				HorizontalAlignment = HorizontalAlignment.Left };
			Grid.SetColumn(txtTime, 1);

			if (isSeason) {
				Binding bindTime = new Binding("TimeString");
				bindTime.Source = DictSeason[ID];
				txtTime.SetBinding(TextBlock.TextProperty, bindTime);
			}

			TextBlock txtTitle = new TextBlock() { 
				Text = "", 
				FontSize = 16, Width = 220, 
				Margin = new Thickness(5, 0, 5, 0), 
				HorizontalAlignment = HorizontalAlignment.Left };
			Grid.SetColumn(txtTitle, 2);

			Binding bindTitle = null, bindTitleForeground = new Binding("TextForeground");

			if (isSeason) {
				bindTitle = new Binding("Title");
				bindTitle.Source = DictSeason[ID];

				bindTitleForeground.Source = DictArchive[DictSeason[ID].KeyName];
			} else {
				bindTitle = new Binding("KeyName");
				bindTitle.Source = DictArchive[KeyName];

				bindTitleForeground.Source = DictArchive[KeyName];
			}
			txtTitle.SetBinding(TextBlock.ForegroundProperty, bindTitleForeground);
			txtTitle.SetBinding(TextBlock.TextProperty, bindTitle);

			if (IsTextTrimmed(txtTitle)) { gridBase.SetBinding(Grid.ToolTipProperty, bindTitle); }

			// Control Box
						
			Grid gridControl = new Grid() { Width = 75, Background = Brushes.Transparent };
			int[] controlColumns = new int[] { 20, 35, 20 };
			foreach (int i in controlColumns) {
				gridControl.ColumnDefinitions.Add(
					new ColumnDefinition() { Width = new GridLength(i) });
			}
			Grid.SetColumn(gridControl, 3);

			Binding bindControl = new Binding("ControlVisible");
			if (isSeason) {
				bindControl.Source = DictArchive[DictSeason[ID].KeyName];
			} else {
				bindControl.Source = DictArchive[KeyName];
			}
			gridControl.SetBinding(Grid.VisibilityProperty, bindControl);

			// Item Visibility
			if (!isSeason) {
				Binding bindItem = new Binding("ItemVisible");
				bindItem.Source = DictArchive[KeyName];
				gridBase.SetBinding(Grid.VisibilityProperty, bindItem);

				DictArchive[KeyName].IsVisible = ShowAll || DictArchive[KeyName].Episode >= 0 ? true : false;
			}

			// Button Left
			Button buttonLeft = new Button() { 
				Width = 20, Height = 40, 
				HorizontalAlignment = HorizontalAlignment.Center, 
				Background = Brushes.Transparent };

			buttonLeft.Content = new Image() { 
				Source = rtSource("iconMinus.png"), 
				Width = 12, Height = 12, };

			Grid.SetColumn(buttonLeft, 0);

			// Text Episode
			TextBlock txtEpisode = new TextBlock() { 
				FontFamily = new FontFamily("Century Gothic"), 
				FontSize = 20, 
				HorizontalAlignment = HorizontalAlignment.Center };
			Grid.SetColumn(txtEpisode, 1);

			Binding bindEpisode = new Binding("Episode");
			if (isSeason) {
				bindEpisode.Source = DictArchive[DictSeason[ID].KeyName];
			} else {
				bindEpisode.Source = DictArchive[KeyName];
			}

			bindEpisode.StringFormat = "{0:D2}";
			txtEpisode.SetBinding(TextBlock.TextProperty, bindEpisode);

			// Button Right
			Button buttonRight = new Button() { 
				Width = 20, Height = 40, 
				HorizontalAlignment = HorizontalAlignment.Center, 
				Background = Brushes.Transparent };

			buttonRight.Content = new Image() { 
				Source = rtSource("iconPlus.png"), 
				Width = 12, Height = 12, };

			Grid.SetColumn(buttonRight, 2);

			// Grid Splitter
			Grid gridSplitter = new Grid() { 
				Height = 1, 
				VerticalAlignment = VerticalAlignment.Bottom, 
				Margin = new Thickness(10, 0, 0, 0), 
				Background = FindResource("sColor") as SolidColorBrush };
			Grid.SetColumnSpan(gridSplitter, 4);

			// Control Tag

			KeyValuePair<int, string> kvp = new KeyValuePair<int, string>(ID, KeyName);
			buttonOption.Tag = buttonLeft.Tag = buttonRight.Tag = buttonBase.Tag = kvp;
			gridBase.Tag = kvp;

			// Event Handler

			gridBase.MouseEnter += gridBase_MouseEnter;
			gridBase.MouseLeave += gridBase_MouseLeave;

			buttonOption.Click += buttonOption_Click;

			buttonLeft.Click += buttonLeft_Click;
			buttonRight.Click += buttonRight_Click;

			buttonBase.Click += buttonBase_Click;
			buttonBase.MouseDown += buttonBase_MouseDown;

			// Add controls

			gridControl.Children.Add(buttonLeft);
			gridControl.Children.Add(txtEpisode);
			gridControl.Children.Add(buttonRight);

			gridBase.Children.Add(buttonBase);
			gridBase.Children.Add(buttonOption);
			gridBase.Children.Add(txtTime);
			gridBase.Children.Add(txtTitle);
			gridBase.Children.Add(gridControl);
			gridBase.Children.Add(gridSplitter);

			return gridBase;
		}

		string animePath = @"X:\Anime\";
		bool isMaster = false;

		private void buttonBase_MouseDown(object sender, MouseButtonEventArgs e) {
			KeyValuePair<int, string> kvpTag = (KeyValuePair<int, string>)(sender as Button).Tag;

			if (e.MiddleButton == MouseButtonState.Pressed) {
				CopyClipboard(kvpTag);
			} else if (e.RightButton == MouseButtonState.Pressed) {
				string animePath = @"X:\Anime\";
				string keyname = "";

				if (isMaster) {
					if (kvpTag.Key >= 0) {
						keyname = DictSeason[kvpTag.Key].KeyName;
					} else if (kvpTag.Value != "") {
						keyname = kvpTag.Value;
						BlinkText(DictControlArchive[kvpTag.Value].Children[3] as TextBlock);
					}

					animePath = Path.Combine(animePath, CleanFileName(DictArchive[keyname].KeyName));

					if (Directory.Exists(animePath)) {
						if (kvpTag.Key >= 0) {
							BlinkText(DictControlSeason[kvpTag.Key].Children[3] as TextBlock);
						} else if (kvpTag.Value != "") {
							BlinkText(DictControlArchive[kvpTag.Value].Children[3] as TextBlock);
						}

						var runningTask = Task.Factory.StartNew(() => OpenFolder(animePath));

					} else {
						ShowGlobalMessage("Folder is created", Brushes.Green);
						Directory.CreateDirectory(animePath);
						/*
						if (kvpTag.Key >= 0) {
							BlinkText(DictControlSeason[kvpTag.Key].Children[3] as TextBlock, 2);
						} else if (kvpTag.Value != "") {
							BlinkText(DictControlArchive[kvpTag.Value].Children[3] as TextBlock, 2);
						}
						 */
						var runningTask = Task.Factory.StartNew(() => OpenFolder(animePath));
					}
				} else {
					ShowOption(kvpTag);
				}
			}
		}

		private void OpenFolder(string strPath) {
			Process pro = new Process() { StartInfo = new ProcessStartInfo() { FileName = strPath } };
			pro.Start();
		}

		private void buttonBase_Click(object sender, RoutedEventArgs e) {
			KeyValuePair<int, string> kvpTag = (KeyValuePair<int, string>)(sender as Button).Tag;

			if (kvpTag.Value != "") {
				if (DictArchive[kvpTag.Value].Episode >= 0) { return; }
				if (DictArchive[kvpTag.Value].Episode < 0) {
					DictArchive[kvpTag.Value].Episode = (DictArchive[kvpTag.Value].Episode + 1) * -1;
					RefreshcomboboxLink();
				}

			} else if (kvpTag.Key != 0) {
				Grid gridBase = (Grid)(sender as Button).Parent;

				if (DictArchive[DictSeason[kvpTag.Key].KeyName].Episode >= 0) {
					isDownOpened = 1;
					ToggleDownloadDialog();

					OptionAnimation(gridBase.Children[1] as Button, 10, 0);
					BlinkText(gridBase.Children[3] as TextBlock);

					InitDownloadDialog(kvpTag.Key);
				} else {
					DictArchive[DictSeason[kvpTag.Key].KeyName].Episode = (DictArchive[DictSeason[kvpTag.Key].KeyName].Episode + 1) * -1;
					RefreshcomboboxLink();
				}
			}
		}

		private void CopyClipboard(KeyValuePair<int, string> kvpTag) {
			string strCopy = "";
			if (kvpTag.Key >= 0) {
				strCopy = DictSeason[kvpTag.Key].Title;
				if (DictArchive[DictSeason[kvpTag.Key].KeyName].Episode > 0) {
					strCopy += " - " + DictArchive[DictSeason[kvpTag.Key].KeyName].Episode.ToString("00");
				}

				BlinkText(DictControlSeason[kvpTag.Key].Children[3] as TextBlock);
			} else if (kvpTag.Value != "") {
				strCopy = DictArchive[kvpTag.Value].KeyName;
				if (DictArchive[kvpTag.Value].Episode > 0) {
					strCopy += " - " + DictArchive[kvpTag.Value].Episode.ToString("00");
				}

				BlinkText(DictControlArchive[kvpTag.Value].Children[3] as TextBlock);
			}

			try {
				strCopy = CleanFileName(strCopy);
				Clipboard.SetText(strCopy);

				ShowGlobalMessage("Title copied to the clipboard", Brushes.Salmon, 1500);
			} catch { }
		}

		private void buttonOption_Click(object sender, RoutedEventArgs e) {
			KeyValuePair<int, string> kvpTag = (KeyValuePair<int, string>)(sender as Button).Tag;
			ShowOption(kvpTag);
		}

		private void ShowOption(KeyValuePair<int, string> kvpTag) {
			if (kvpTag.Key >= 0) {
				AnimateAddDialog(string.Format("{0}modify", PageMode), kvpTag.Key);
			} else {
				AnimateAddDialog(string.Format("{0}modify", PageMode), -1, kvpTag.Value);
			}
		}

		private void buttonLeft_Click(object sender, RoutedEventArgs e) {
			KeyValuePair<int, string> kvpTag = (KeyValuePair<int, string>)(sender as Button).Tag;

			string KeyName = kvpTag.Value;
			if (kvpTag.Key >= 0) { KeyName = DictSeason[kvpTag.Key].KeyName; }

			DictArchive[KeyName].Episode--;
			SaveData();
		}

		private void buttonRight_Click(object sender, RoutedEventArgs e) {
			KeyValuePair<int, string> kvpTag = (KeyValuePair<int, string>)(sender as Button).Tag;

			string KeyName = kvpTag.Value;
			if (kvpTag.Key >= 0) { KeyName = DictSeason[kvpTag.Key].KeyName; }

			DictArchive[KeyName].Episode++;
			SaveData();
		}

		private void gridBase_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e) {
			OptionAnimation((sender as Grid).Children[1] as Button, 10, 0);
		}

		private void gridBase_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e) {
			OptionAnimation((sender as Grid).Children[1] as Button, 0, 1);
		}

		private void OptionAnimation(Button buttonOption, double margin, double opacity) {
			buttonOption.BeginAnimation(Button.MarginProperty, new ThicknessAnimation(new Thickness(0, 0, margin, 0), TimeSpan.FromMilliseconds(50)) {
				EasingFunction = new ExponentialEase() {
					Exponent = 4, EasingMode = EasingMode.EaseOut
				}
			});

			buttonOption.BeginAnimation(Button.OpacityProperty, new DoubleAnimation(opacity, TimeSpan.FromMilliseconds(100)));
		}

		#endregion

		#region List item

		private Button MakeListItem(ListData listdata) {
			Button buttonBase = new Button() { Height = 40, Margin = new Thickness(10, 0, 10, 0), Background = Brushes.Transparent, HorizontalContentAlignment = HorizontalAlignment.Stretch, HorizontalAlignment = HorizontalAlignment.Stretch };

			Grid gridBase = new Grid() { Background = Brushes.Transparent, Height = 40 };
			TextBlock txtTitle = new TextBlock() { Text = listdata.Caption, FontSize = 14, HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(5, 0, 5, 0) };
			if (listdata.IsRaw) { txtTitle.Foreground = SColor; }
			Border border = new Border() { Height = 1, VerticalAlignment = VerticalAlignment.Bottom, Background = Brushes.LightGray };

			gridBase.Children.Add(txtTitle);
			gridBase.Children.Add(border);

			if (listdata.IsTorrent) { buttonBase.ToolTip = string.Format("({0}) {1}", listdata.Memo, listdata.Caption); }
			buttonBase.Content = gridBase;
			return buttonBase;
		}

		#endregion

		#region external functions

		public bool IsTextTrimmed(TextBlock textBlock) {
			Typeface typeface = new Typeface(textBlock.FontFamily,
				textBlock.FontStyle,
				textBlock.FontWeight,
				textBlock.FontStretch);

			// FormattedText is used to measure the whole width of the text held up by TextBlock container.
			FormattedText formmatedText = new FormattedText(
				textBlock.Text,
				System.Threading.Thread.CurrentThread.CurrentCulture,
				textBlock.FlowDirection,
			   typeface,
				textBlock.FontSize,
				textBlock.Foreground);
			return formmatedText.Width > textBlock.Width;
		}

		private void BlinkText(UIElement txt, int Count = 1) {
			txt.BeginAnimation(UIElement.OpacityProperty,
								new DoubleAnimation(0.1, 1, TimeSpan.FromMilliseconds(500 / Count)) {
									RepeatBehavior = new RepeatBehavior(Count)
								});
		}

		#endregion

		#region Schedule Item

		private Button MakeScheduleItem(LData ld) {
			Grid gridBase = new Grid() { Height = 40, Background = Brushes.Transparent };
			int[] columns = new int[] { 10, 50, 1, 10 };

			foreach (int i in columns) {
				ColumnDefinition cd;
				if (i == 1) {
					cd = new ColumnDefinition() { Width = new GridLength(i, GridUnitType.Star) };
				} else {
					cd = new ColumnDefinition() { Width = new GridLength(i) };
				}
				gridBase.ColumnDefinitions.Add(cd);
			}

			// Command Button
			Button buttonBase = new Button() { Background = Brushes.Transparent, HorizontalContentAlignment = HorizontalAlignment.Stretch };
			Grid.SetColumnSpan(buttonBase, 5);

			// Text Time & Title

			TextBlock txtTime = new TextBlock() { Text = string.Format("{0:D2}:{1:D2}", ld.Hour, ld.Minute), FontSize = 16, Margin = new Thickness(5, 0, 0, 0), Foreground = Brushes.Gray, HorizontalAlignment = HorizontalAlignment.Left };
			Grid.SetColumn(txtTime, 1);


			TextBlock txtTitle = new TextBlock() { Text = ld.Caption, FontSize = 16, Width = 300, Margin = new Thickness(5, 0, 5, 0), HorizontalAlignment = HorizontalAlignment.Left };
			Grid.SetColumn(txtTitle, 2);

			// Grid Splitter
			Grid gridSplitter = new Grid() { Height = 1, VerticalAlignment = VerticalAlignment.Bottom, Margin = new Thickness(10, 0, 0, 0), Background = FindResource("sColor") as SolidColorBrush };
			Grid.SetColumnSpan(gridSplitter, 3);

			// Event Handler

			buttonBase.Click += buttonScheduleListItem_Click;
			buttonBase.Tag = ld;

			// Add controls

			gridBase.Children.Add(txtTime);
			gridBase.Children.Add(txtTitle);
			gridBase.Children.Add(gridSplitter);

			buttonBase.Content = gridBase;

			return buttonBase;
		}

		#endregion

		private Button GetNoticeControl(ListData ld) {
			Button button = new Button() {
				HorizontalAlignment = HorizontalAlignment.Stretch,
				HorizontalContentAlignment = HorizontalAlignment.Stretch,
				Background = Brushes.Transparent,
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

			TextBlock txtTime = new TextBlock() {
				FontSize = 14, Foreground = Brushes.Gray,
				//Text = ld.ID.Substring(0, 14),
				Text = ld.Memo,
				Margin = new Thickness(0, 0, 15, 5),
				HorizontalAlignment = HorizontalAlignment.Right,
				VerticalAlignment = VerticalAlignment.Bottom,
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

			ld.Memo = "anime";
			button.Tag = ld;
			button.Content = gridBase;

			return button;
		}
	}
}
