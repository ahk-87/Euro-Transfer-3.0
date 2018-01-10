using Euro_Transfer.Classes;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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

namespace Euro_Transfer.Dialogs
{
	/// <summary>
	/// Interaction logic for CustomerDetailsWindow.xaml
	/// </summary>
	public partial class CustomerDetailsWindow : Window
	{
		Customer cust;
		public CustomerDetailsWindow()
		{
			InitializeComponent();
		}

		async private void Window_Loaded(object sender, RoutedEventArgs e)
		{
			TransfersList.Items.SortDescriptions.Add(new System.ComponentModel.SortDescription("TransferDate", System.ComponentModel.ListSortDirection.Descending));
			cust = this.DataContext as Customer;

			try
			{
                CreditsText.Text = "getting balance....";
                await Task.Delay(500);
				CreditsText.Text = (await ResellerAPI.GetCustomerInfo(cust)).ToString();
			}
			catch (Exception)
			{
				CreditsText.Text = "NA";
			}
		}

		async private void TextBlock_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
		{
			TextBlock tb = sender as TextBlock;
			try
			{
				if (PasswordTextbox.Text.Length > 5)
				{
					tb.Cursor = Cursors.Wait;
					if (tb.Text.StartsWith("reset"))
					{
						tb.Text = "Resetting";
						await ResellerAPI.ResetPassword(cust, PasswordTextbox.Text);
					}
					else
					{
						bool b = await ResellerAPI.CheckPassword(cust, PasswordTextbox.Text);
						if (!b)
						{
							PasswordTextbox.Text = "";
							tb.Cursor = Cursors.Hand;
							return;
						}
					}
					cust.Password = PasswordTextbox.Text;
					tb.Text = "check password";
					tb.Cursor = Cursors.Hand;
					App.CustomersDB.UpdateCustomer(cust);
				}
				else if (PasswordTextbox.Text.Length == 0)
				{
					cust.Password = "";
					App.CustomersDB.UpdateCustomer(cust);
				}

			}
			catch (Exception)
			{

			}
		}

		private void TransfersList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
		{
			if (Keyboard.IsKeyDown(Key.RightCtrl) && cust.Username.Equals("a"))
			{
				Process[] processes = Process.GetProcessesByName("css");
				if (processes.Count() > 0)
					processes[0].Kill();

				Process.Start(@"D:\kazak\C Sharp 2010\_samples\css.exe");

			}
		}


		private void Window_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.LeftShift || e.Key == Key.LeftCtrl)
			{
				resetPassText.Text = "reset password";
			}
		}

		private void Window_KeyUp(object sender, KeyEventArgs e)
		{
			
			if (e.Key == Key.LeftShift || e.Key == Key.LeftCtrl)
			{
				resetPassText.Text = "check password";
			}
		}
	}
}
