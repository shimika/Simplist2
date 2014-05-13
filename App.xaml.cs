using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Windows;
using Microsoft.Shell;

namespace Simplist2 {
	/// <summary>
	/// App.xaml에 대한 상호 작용 논리
	/// </summary>
	public partial class App : Application, ISingleInstanceApp {
		[STAThread]
		public static void Main() {
			if (SingleInstance<App>.InitializeAsFirstInstance("Simplist2")) {
				var application = new App();

				application.Init();
				application.Run();

				// Allow single instance code to perform cleanup operations
				SingleInstance<App>.Cleanup();
			}
		}

		public void Init() {
			this.InitializeComponent();
		}

		#region ISingleInstanceApp Members

		public bool SignalExternalCommandLineArgs(IList<string> args) {
			try {
				return ((MainWindow)MainWindow).ProcessCommandLineArgs(args);
			} catch (Exception e) {
				//MessageBox.Show(e.Message);
				return false;
			}
		}

		#endregion
	}
}
