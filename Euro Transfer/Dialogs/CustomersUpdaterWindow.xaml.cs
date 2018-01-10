using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

using Euro_Transfer.Classes;

namespace Euro_Transfer.Dialogs
{
	/// <summary>
	/// Interaction logic for CustomersUpdaterWindow.xaml
	/// </summary>
	public partial class CustomersUpdaterWindow : Window
	{

		Reseller res;
		CustomersExtractor extractor;
		public CustomersUpdaterWindow()
		{
			InitializeComponent();
		}

		private void UpdateButton_Click(object sender, RoutedEventArgs e)
		{
			UpdateButton.IsEnabled = false;
			Reseller res = this.DataContext as Reseller;
			bool getTransfers = (bool)TransfersCheckBox.IsChecked;
			extractor = new CustomersExtractor(res, StatusText, UpdateProgress, getTransfers);
			extractor.Finished += extractor_Finished;
			extractor.GetCustomers();
		}

		void extractor_Finished(object sender, EventArgs e)
		{
			StatusText.Text = "Finished";
			bool getTransfers = (bool)TransfersCheckBox.IsChecked;
			if (getTransfers)
			{
				res.LastUpdate = DateTime.Now;
				App.CustomersDB.AddReseller(res);
			}
			res.UpdateCustomers();
			App.CustomersDB.Save();
			((CustomersXmlDb)App.CustomersDB).EnableSaving();

			UpdateProgress.Visibility = System.Windows.Visibility.Collapsed;
			LastUpdateText.Text = res.LastUpdate.ToString();
		}

		private void Window_Loaded(object sender, RoutedEventArgs e)
		{
			res = this.DataContext as Reseller;
			if (!res.LastUpdate.Equals(DateTime.MinValue))
			{
				LastUpdateText.Text = res.LastUpdate.ToString();
			}
		}

		private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			if (extractor != null)
				extractor.EndWork();
		}
	}
}
