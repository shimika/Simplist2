using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;

namespace Simplist2 {
	public class SData : INotifyPropertyChanged {
		private int _id, _week = 0, _time = 0;
		private string _title, _keyword, _keyname, _timestring, _timetag;

		// Integer members
		public int ID {
			get { return _id; }
			set {
				_id = value;
				OnPropertyChanged("ID");
			}
		}

		public int Week {
			get { return _week; }
			set {
				_week = value;
				_timetag = string.Format("{0}:{1:D2}:{2:D2}", _week, _time / 100, _time % 100);
				OnPropertyChanged("Week");
			}
		}

		public int Time {
			get { return _time; }
			set {
				_time = value;
				TimeString = string.Format("{0:D2}:{1:D2}", _time / 100, _time % 100);
				TimeTag = string.Format("{0}:{1:D2}:{2:D2}", _week, _time / 100, _time % 100);
				OnPropertyChanged("Time");
			}
		}

		public string TimeTag {
			get { return _timetag; }
			set {
				_timetag = value;
				OnPropertyChanged("TimeTag");
			}
		}

		// String members

		public string Title {
			get { return _title; }
			set {
				_title = value;
				OnPropertyChanged("Title");
			}
		}

		public string Keyword {
			get { return _keyword; }
			set {
				_keyword = value;
				OnPropertyChanged("Keyword");
			}
		}

		public string KeyName {
			get { return _keyname; }
			set {
				_keyname = value;
				OnPropertyChanged("KeyName");
			}
		}

		public string TimeString {
			get { return _timestring; }
			set {
				_timestring = value;
				OnPropertyChanged("TimeString");
			}
		}


		public event PropertyChangedEventHandler PropertyChanged;
		private void OnPropertyChanged(string propertyName) {
			if (PropertyChanged != null) {
				PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
			}
		}
	}

	public class AData : INotifyPropertyChanged {
		private int _season, _episode, _sid;
		private string _keyname, _comicepisode;
		private bool _isvisible;

		private Visibility _controlvisible, _itemvisible;
		private SolidColorBrush _textforeground;

		public bool isLinked = false;

		// Integer members
		public int Season {
			get { return _season; }
			set {
				_season = value;
				OnPropertyChanged("Season");
			}
		}

		public int Episode {
			get { return _episode; }
			set {
				_episode = value;

				if (_episode >= 0) {
					ControlVisible = Visibility.Visible;
					TextForeground = Brushes.Black;
				} else {
					ControlVisible = Visibility.Collapsed;
					TextForeground = Brushes.LightGray;
				}

				OnPropertyChanged("Episode");
			}
		}

		public int SID {
			get { return _sid; }
			set {
				_sid = value;
				OnPropertyChanged("SID");
			}
		}

		// String members
		public string KeyName {
			get { return _keyname; }
			set {
				_keyname = value;
				OnPropertyChanged("KeyName");
			}
		}

		public string ComicEpisode {
			get { return _comicepisode; }
			set {
				_comicepisode = value;
				OnPropertyChanged("ComicEpisode");
			}
		}

		public Visibility ControlVisible {
			get { return _controlvisible; }
			set {
				_controlvisible = value;
				OnPropertyChanged("ControlVisible");
			}
		}

		public SolidColorBrush TextForeground {
			get { return _textforeground; }
			set {
				_textforeground = value;
				OnPropertyChanged("TextForeground");
			}
		}

		public bool IsVisible {
			get { return _isvisible; }
			set {
				_isvisible = value;
				ItemVisible = _isvisible ? Visibility.Visible : Visibility.Collapsed;
				OnPropertyChanged("IsVisible");
			}
		}

		public Visibility ItemVisible {
			get { return _itemvisible; }
			set {
				_itemvisible = value;
				OnPropertyChanged("ItemVisible");
			}
		}

		public event PropertyChangedEventHandler PropertyChanged;
		private void OnPropertyChanged(string propertyName) {
			if (PropertyChanged != null) {
				PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
			}
		}
	}

	public struct LData {
		public string Caption;
		public int Hour, Minute, Week;
	}
}
