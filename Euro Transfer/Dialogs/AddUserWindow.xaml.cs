using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;


using System.IO;
using System.Xml.Linq;

using Euro_Transfer.Classes;
using System.Printing;
using System.Windows.Xps.Packaging;
using System.Windows.Xps;
using System.Windows.Markup;
using System.Threading.Tasks;

namespace Euro_Transfer
{
	/// <summary>
	/// Interaction logic for AddUserWindow.xaml
	/// </summary>
	public partial class AddUserWindow : Window
	{
		CustomerCollection col;

		public AddUserWindow()
		{
			InitializeComponent();
			UsernameTextbox.Focus();
		}

		async private void Button_Click(object sender, RoutedEventArgs e)
		{
			AddButton.IsEnabled = false;
			ErrorEllipse.Visibility = System.Windows.Visibility.Collapsed;

			ErrorText.Text = "";
			ErrorText.Foreground = Brushes.Green;

			if (string.IsNullOrWhiteSpace(PasswordTextbox.Text))
			{
				ErrorEllipse.Visibility = System.Windows.Visibility.Visible;
				return;
			}

			try
			{
				Customer c = await ResellerAPI.CreateUser(Tag as Reseller, UsernameTextbox.Text, PasswordTextbox.Text, 4);
				c.PhoneNumber = PhoneTextbox.Text;
				App.CustomersDB.AddCustomer(c);

				ErrorText.Text = "Customer created. Waiting to send 4.5 = 11000";

				await Task.Delay(2000);

				Transfer t = new Transfer(Configuration.InitialTopup, Configuration.InitialCost, DateTime.Now);
				await ResellerAPI.TransferEuro(c, t);

				App.CustomersDB.AddTransfer(c, t);

				ErrorText.Text = "Credits sent. Closing after a moment";

				await Task.Delay(3000);
				this.DialogResult = true;

				(this.Owner as MainWindow).SelectCustomer(c);

				this.Close();
			}
			catch (NoInternetException)
			{
				ErrorText.Text = "No internet connection";
				ErrorText.Foreground = Brushes.Red;
			}
			catch (InvalidOperationException ex)
			{
				ErrorText.Text = ex.Message;
				ErrorText.Foreground = Brushes.Red;
			}
			catch (Exception)
			{
				ErrorText.Text = "Unknown error..";
				ErrorText.Foreground = Brushes.Red;
			}
			finally
			{
				AddButton.IsEnabled = true;
			}

		}

		private void UsernameTextbox_TextChanged(object sender, TextChangedEventArgs e)
		{
			var matchColl = from c in col
							where c.Username.StartsWith(UsernameTextbox.Text)
							select c;

			ExistingUsersList.ItemsSource = matchColl;

			Customer cust = matchColl.FirstOrDefault(c => c.Username.Equals(UsernameTextbox.Text));

			bool hasLetters = !string.IsNullOrWhiteSpace(UsernameTextbox.Text);

			popup.IsOpen = hasLetters && ExistingUsersList.HasItems;
			AddButton.IsEnabled = hasLetters && (cust == null);

		}

		private void Window_Loaded(object sender, RoutedEventArgs e)
		{
			Reseller r = Tag as Reseller;
			col = r.Customers;
			this.Title = "Create user by " + r.Name;
		}

	}
}
