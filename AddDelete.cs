using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

namespace Simplist2 {
	public partial class MainWindow : Window {
		private void buttonPopupOK_Click(object sender, RoutedEventArgs e) {
			textboxTitle.Text =  textboxTitle.Text.Trim();
			if (textboxTitle.Text == "") {
				ShowGlobalMessage("Please enter title", Brushes.Crimson);
				textboxTitle.Focus();
				return;
			}

			// Time check
			if (AddMode.IndexOf("season") == 0) {
				try {
					int t = Convert.ToInt32(textboxHour.Text);
					t = Convert.ToInt32(textboxMinute.Text);
				} catch {
					ShowGlobalMessage("Please enter right number", Brushes.Crimson);
					return;
				}
			}

			if (AddMode == "season") {
				try {
					SeasonIDCount++;

					string archiveName = (comboboxLink.SelectedItem as ComboBoxPairs).Key;

					if (((int)comboboxLink.SelectedValue) == 0) {
						if (DictArchive.ContainsKey(textboxTitle.Text)) {
							ShowGlobalMessage("Title already exists in archive", Brushes.Crimson);
							return;
						}

						AData adataAdd = new AData() {
							KeyName = textboxTitle.Text, SID = SeasonIDCount,
							Episode = 0, Season = 1, isLinked = true,
						};
						archiveName = adataAdd.KeyName;
						AddArchive(adataAdd);
					} else {
					}

					SData sdata = new SData() {
						ID = SeasonIDCount, KeyName = archiveName, Title = textboxTitle.Text,
						Week = comboboxWeekday.SelectedIndex, Time = (Convert.ToInt32(textboxHour.Text) % 100) * 100 + (Convert.ToInt32(textboxMinute.Text) % 100),
						Keyword = textboxSearchTag.Text,
					};
					if (DictArchive[archiveName].Episode < 0) {
						DictArchive[archiveName].Episode = 0;
						ToggleArchiveViewMode(ShowAll);
					}
					DictArchive[archiveName].SID = SeasonIDCount;

					AddSeasonData(sdata);
					SaveData();

				} catch (Exception ex) {
					MessageBox.Show("season\n" + ex.Message);
				}

			} else if (AddMode == "archive") {
				try {
					if (DictArchive.ContainsKey(textboxTitle.Text)) {
						ShowGlobalMessage("Title already exists in archive", Brushes.Crimson);
						return;
					}

					AData adataAdd = new AData() {
						KeyName = textboxTitle.Text, SID = -1,
						Episode = 0, Season = 1, isLinked = false,
					};
					AddArchive(adataAdd);
					SaveData();
				} catch (Exception ex) {
					MessageBox.Show("archive\n" + ex.Message);
				}

			} else if (AddMode == "seasonmodify") {
				try {
					string KeyName = textLinkedTitle.Text.Trim();
					int sid = DictArchive[KeyName].SID;

					StackPanel stackItem = FindName(string.Format("stackSeasonItem{0}", DictSeason[sid].Week)) as StackPanel;
					BindingOperations.ClearAllBindings(DictControlSeason[sid]);
					stackItem.Children.Remove(DictControlSeason[sid]);
					DictControlSeason.Remove(sid);

					DictSeason[sid].Title = textboxTitle.Text;
					DictSeason[sid].Week = comboboxWeekday.SelectedIndex;
					DictSeason[sid].Time = (Convert.ToInt32(textboxHour.Text) % 100) * 100 + (Convert.ToInt32(textboxMinute.Text) % 100);
					DictSeason[sid].Keyword = textboxSearchTag.Text;

					AddSeasonData(DictSeason[sid]);
					SaveData();
				} catch (Exception ex) {
					MessageBox.Show("seasonmodify\n" + ex.Message);
				}
			}

			AnimateAddDialog("close");
		}

		private void textboxAddWindow_KeyDown(object sender, KeyEventArgs e) {
			if (e.Key == Key.Enter) {
				buttonPopupOK.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
			}
		}
		
		private void buttonRemoveItem_Click(object sender, RoutedEventArgs e) {
			if (AddMode == "seasonmodify") {
				int sid = DictArchive[textLinkedTitle.Text].SID;
				DeleteSeason(sid);

			} else if (AddMode == "archivemodify") {
				string keyname = textboxTitle.Text;
				DeleteArchive(keyname);
			}

			AnimateAddDialog("close");
			SaveData();
		}

		private void buttonEnableDisable_Click(object sender, RoutedEventArgs e) {
			string keyname = "";
			if (AddMode == "seasonmodify") {
				keyname = textLinkedTitle.Text;
			} else if (AddMode == "archivemodify") {
				keyname = textboxTitle.Text;
			}

			if (keyname == "") { return; }

			int sid = DictArchive[keyname].SID;
			DictArchive[keyname].Episode = (DictArchive[keyname].Episode + 1) * -1;
			DictArchive[keyname].IsVisible = ShowAll || DictArchive[keyname].Episode >= 0 ? true : false;

			AnimateAddDialog("close");
			SaveData();
			RefreshcomboboxLink();
		}

		private void AddSeasonData(SData sdata) {
			if (!DictSeason.ContainsKey(sdata.ID)) {
				DictSeason.Add(sdata.ID, sdata);
			}

			Grid grid = MakeMainItem(true, sdata.ID);
			DictControlSeason.Add(sdata.ID, grid);

			StackPanel stackItem = FindName(string.Format("stackSeasonItem{0}", sdata.Week)) as StackPanel;

			int i = 0;
			foreach (Grid gridItem in stackItem.Children) {
				KeyValuePair<int, string> kvpTag = (KeyValuePair<int, string>)(gridItem).Tag;

				if (sdata.Time < DictSeason[kvpTag.Key].Time) {
					break;
				}
				i++;
			}
			stackItem.Children.Insert(i, grid);
			RefreshcomboboxLink();
			RefreshWeekData();
		}

		private void AddArchive(AData adata) {
			if (!DictArchive.ContainsKey(adata.KeyName)) {
				DictArchive.Add(adata.KeyName, adata);
			}

			Grid grid = MakeMainItem(false, -1, adata.KeyName);
			DictControlArchive.Add(adata.KeyName, grid);

			List<string> list = new List<string>();
			foreach (KeyValuePair<string, AData> kvp in DictArchive) { list.Add(kvp.Value.KeyName); }
			list.Sort();

			for (int i = 0; i < list.Count; i++) {
				if (string.Compare(list[i], adata.KeyName) > 0) {
					stackArchive.Children.Insert(i, grid);
					RefreshcomboboxLink();
					return;
				}
			}
			stackArchive.Children.Add(grid);
			RefreshcomboboxLink();
			RefreshWeekData();
		}

		private void DeleteSeason(int sid) {
			DictArchive[DictSeason[sid].KeyName].isLinked = false;

			StackPanel stackItem = FindName(string.Format("stackSeasonItem{0}", DictSeason[sid].Week)) as StackPanel;
			BindingOperations.ClearAllBindings(DictControlSeason[sid]);
			stackItem.Children.Remove(DictControlSeason[sid]);
			DictControlSeason.Remove(sid);
			DictSeason.Remove(sid);

			RefreshcomboboxLink();
			RefreshWeekData();
		}

		private void DeleteArchive(string keyname) {
			if (DictArchive[keyname].isLinked) {
				DeleteSeason(DictArchive[keyname].SID);
			}

			BindingOperations.ClearAllBindings(DictControlArchive[keyname]);
			stackArchive.Children.Remove(DictControlArchive[keyname]);
			DictControlArchive.Remove(keyname);
			DictArchive.Remove(keyname);
		}

		private void RefreshcomboboxLink() {
			List<ComboBoxPairs> listContext = new List<ComboBoxPairs>();
			listContext.Add(new ComboBoxPairs("* 이 이름으로 목록에 새로 추가", 0));

			var listArchive = DictArchive.OrderBy(kvp => kvp.Value.KeyName);
			foreach (KeyValuePair<string,AData> kData in listArchive) {
				if (!kData.Value.isLinked) {
					listContext.Add(new ComboBoxPairs(kData.Value.KeyName, 1));
				}
			}

			comboboxLink.DisplayMemberPath = "Key";
			comboboxLink.SelectedValuePath = "Value";
			comboboxLink.ItemsSource = listContext;
		}
	}

	public class ComboBoxPairs {
		public string Key { get; set; }
		public int Value { get; set; }

		public ComboBoxPairs(string _key, int _value) {
			Key = _key;
			Value = _value;
		}
	}
}
