using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Windows;
using System.IO;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Windows.Media;
using Euro_Transfer.Classes;
using System.Diagnostics;
using System.Threading;
using System.Runtime.InteropServices;

namespace Euro_Transfer
{
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App : Application
	{
		public static ObservableCollection<Reseller> Resellers = new ObservableCollection<Reseller>();

		public static CustomersDbBase CustomersDB = new CustomersXmlDb();

		public static string xmlPath = "transfers.xml";

		public static StatusProvider Status = new StatusProvider("     ready");

		public static ObservableCollection<VoucherRecord> VoucherRecords = new ObservableCollection<VoucherRecord>();

		//public static Mutex Mutex;

		[DllImport("user32.dll")]
		static extern bool SetForegroundWindow(IntPtr hWnd);
		private void Application_Startup(object sender, StartupEventArgs e)
		{
			//bool ownsMutex = false;
			//Mutex = new Mutex(true, "EuroTransferMutex", out ownsMutex);
			//GC.KeepAlive(Mutex);

			//// If the application owns the Mutex it can continue to execute;
			//// otherwise, the application should exit.
			//if (!ownsMutex)
			//{
			var currentProcess = Process.GetCurrentProcess();
			var processes = Process.GetProcessesByName(currentProcess.ProcessName);
			var process = processes.FirstOrDefault(p => p.Id != currentProcess.Id);
			if (process != null)
			{
				SetForegroundWindow(process.MainWindowHandle);
				this.Shutdown();
			}
			//}
			ResellerXml.InitializeFile();

			if (Resellers.Count == 0)
				this.StartupUri = new Uri("Dialogs/ResellerWindow.xaml", UriKind.Relative);
			else
			{
				foreach (Reseller res in Resellers)
				{
					CustomersDB.GetCustomers(res);
					res.UpdateCustomers();
				}
				this.StartupUri = new Uri("MainWindow.xaml", UriKind.Relative);
			}
		}
	}
}
