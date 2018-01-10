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
using System.Windows.Navigation;
using System.Windows.Shapes;

using System.Xml.Linq;
using System.ComponentModel;
using System.Net;
using System.Collections.ObjectModel;
using System.Threading;
using System.Text.RegularExpressions;
using System.Windows.Media.Animation;
using System.Windows.Forms.Integration;
using System.Globalization;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using Euro_Transfer.Classes;
using System.Threading.Tasks;

using Euro_Transfer.Dialogs;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Euro_Transfer
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		EventWaitHandle resetEvent = new EventWaitHandle(false, EventResetMode.AutoReset, "vouchersResetEvent");

		//double extraFreeRatio = 0;

		Storyboard numberErrorStory = new Storyboard();
		Storyboard pinErrorStory = new Storyboard();

		DateTime d;
		public MainWindow()
		{
			InitializeComponent();

			euroCostText.Text = "10 Euros = " + Configuration.VoucherPrice;

			VoucherRecord.GetVouchersFromXml();
			ColorAnimation ca = new ColorAnimation(Colors.OrangeRed, TimeSpan.FromMilliseconds(600));
			ca.AutoReverse = true;
			ca.RepeatBehavior = RepeatBehavior.Forever;
			Storyboard.SetTarget(ca, txtNumber);
			Storyboard.SetTargetProperty(ca, new PropertyPath("Background.Color"));
			numberErrorStory.Children.Add(ca);

			ICollectionView cv = CollectionViewSource.GetDefaultView(RecordsList.ItemsSource);
			cv.SortDescriptions.Add(new SortDescription("Status", ListSortDirection.Ascending));
			cv.SortDescriptions.Add(new SortDescription("Date", ListSortDirection.Descending));
			FocusManager.SetFocusedElement(this, textBox1);
		}

		public int LevenshteinDistance(string source, string target)
		{
			if (String.IsNullOrEmpty(source))
			{
				if (String.IsNullOrEmpty(target)) return 0;
				return target.Length;
			}
			if (String.IsNullOrEmpty(target)) return source.Length;

			if (source.Length > target.Length)
			{
				var temp = target;
				target = source;
				source = temp;
			}

			var m = target.Length;
			var n = source.Length;
			var distance = new int[2, m + 1];
			// Initialize the distance 'matrix'
			for (var j = 1; j <= m; j++) distance[0, j] = j;

			var currentRow = 0;
			for (var i = 1; i <= n; ++i)
			{
				currentRow = i & 1;
				distance[currentRow, 0] = i;
				var previousRow = currentRow ^ 1;
				for (var j = 1; j <= m; j++)
				{
					var cost = (target[j - 1] == source[i - 1] ? 0 : 1);
					distance[currentRow, j] = Math.Min(Math.Min(
								distance[previousRow, j] + 1,
								distance[currentRow, j - 1] + 1),
								distance[previousRow, j - 1] + cost);
				}
			}
			return distance[currentRow, m];
		}

		protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
		{
			base.OnMouseLeftButtonDown(e);
			if (!(e.Source is Label))
				this.Opacity = 0.7;

			try
			{
				this.DragMove();
			}
			catch (Exception)
			{

			}
		}

		protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
		{
			base.OnMouseLeftButtonUp(e);
			this.Opacity = 1;
		}

		CancellationTokenSource cts;
		async void textBox1_TextChanged(object sender, TextChangedEventArgs e)
		{
			if (cts != null)
				cts.Cancel();

			if (popup.IsOpen)
			{
				popup.IsOpen = false;
			}
			if (textBox1.Text.Length == 0)
			{
				CustomersListBox.SelectedItem = null;
				CustomersListBox.ScrollIntoView(CustomersListBox.Items[0]);
			}
			else if (textBox1.Text.StartsWith("70") || textBox1.Text.StartsWith("71")
				|| textBox1.Text.StartsWith("76") || textBox1.Text.StartsWith("78") || textBox1.Text.StartsWith("03")
				|| textBox1.Text.StartsWith("79") || textBox1.Text.StartsWith("81"))
			{
				CustomerCollection col = CustomersListBox.Items.SourceCollection as CustomerCollection;
				Customer ddd = col.FirstOrDefault(c => c.PhoneNumber.StartsWith(textBox1.Text));
				foreach (Customer user in CustomersListBox.Items)
				{
					if (user.PhoneNumber.StartsWith(textBox1.Text))
					{
						CustomersListBox.SelectedItem = user;
						CustomersListBox.ScrollIntoView(user);
						return;
					}
				}
				CustomersListBox.SelectedItem = null;
			}
			else
			{
				foreach (Customer user in CustomersListBox.Items)
				{
					if (user.Username.StartsWith(textBox1.Text))
					{
						CustomersListBox.SelectedItem = user;
						CustomersListBox.ScrollIntoView(user);
						return;
					}
				}
				CustomersListBox.SelectedItem = null;

				try
				{
					cts = new CancellationTokenSource();
					await Task.Delay(2000, cts.Token);
					cts = null;
					CustomerCollection equalList = new CustomerCollection();

					if (textBox1.Text.Contains("ismaelr") && (ResellersComboBox.SelectedItem as Reseller).Name.Equals("ahkvoip0", StringComparison.OrdinalIgnoreCase))
					{
						foreach (Customer c in CustomersListBox.Items)
						{
							if (c.Username.Equals("ismael"))
							{
								equalList.Add(c);

								didMeanText.Text = "You must select";
								possibleCustomers.ItemsSource = equalList;
								popup.IsOpen = true;
								CustomersListBox.IsEnabled = false;
								CustomersListBox.Opacity = 0.5;

								return;
							}
						}

					}

					foreach (Customer c in CustomersListBox.Items)
					{
						if (LevenshteinDistance(c.Username, textBox1.Text) < 3)
						{
							equalList.Add(c);
						}
					}

					if (equalList.Count > 0)
					{
						didMeanText.Text = "Did you mean:";
						possibleCustomers.ItemsSource = equalList;
						popup.IsOpen = true;
						CustomersListBox.IsEnabled = false;
						CustomersListBox.Opacity = 0.5;
					}
				}
				catch
				{
				}
			}
		}

		private void popup_Closed(object sender, EventArgs e)
		{
			CustomersListBox.Opacity = 1;
			CustomersListBox.IsEnabled = true;
		}
		
		private void Window_Loaded(object sender, RoutedEventArgs e)
		{
			

			ResellersComboBox.ItemsSource = App.Resellers;
			if (App.Resellers.Count > 0)
			{
				ResellersComboBox.SelectedIndex = 0;
			}

			CustomersListBox.Items.SortDescriptions.Add(new SortDescription("Username", ListSortDirection.Ascending));
		}

		private void CustomersListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			btnTransfer.IsEnabled = (CustomersListBox.SelectedItem != null);
			if (tblkResult.Text.Length > 0)
			{
				tblkResult.Text = string.Empty;
			}
		}

		async private void btnTransfer_Click(object sender, RoutedEventArgs e)
		{
			double amountToTransfer = 0;
			Transfer trans;
			Customer customer = CustomersListBox.SelectedItem as Customer;


			if (double.TryParse(cbxValues.Text, out amountToTransfer))
			{
				trans = Transfer.Create(customer, amountToTransfer);
			}
			else if (cbxValues.Text.Contains(':'))
			{
				string[] values = cbxValues.Text.Split(new char[] { ':' });
				int cost;
				try
				{
					amountToTransfer = double.Parse(values[0]);
					cost = int.Parse(values[1]);
					if ((cost % 1000) > 0)
						throw new ArgumentException();
					trans = new Transfer(amountToTransfer, cost, DateTime.Now);

				}
				catch (Exception)
				{
					tblkResult.Text = "Error in cost!!";
					tblkResult.Foreground = Brushes.Red;
					return;
				}
			}
			else
			{
				tblkResult.Text = "Wrong amount format entered!!";
				tblkResult.Foreground = Brushes.Red;
				return;
			}
			if (Math.Abs(trans.Amount) > 0 && Math.Abs(trans.Amount) < 200)
			{
				await transferEuro(customer, trans);
			}
			else
			{
				cbxValues.IsDropDownOpen = true;
			}
		}

		async private Task transferEuro(Customer cust, Transfer trans)
		{
			switch (MessageBox.Show(
				string.Format("Are you sure to send {0:0.00} euro (cost {1})\n     to the user : {2} ?", trans.Amount, trans.Cost, cust.Username),
				"Confirm Transfer", MessageBoxButton.OKCancel)
			)
			{
				case MessageBoxResult.Cancel:
					FocusManager.SetFocusedElement(this, textBox1);
					return;
			}

			try
			{
				btnTransfer.IsEnabled = false;
				closeApp.IsEnabled = false;
				await ResellerAPI.TransferEuro(cust, trans);
				tblkResult.Text = "Sent successfully";
				tblkResult.Foreground = Brushes.Green;
				App.CustomersDB.AddTransfer(cust, trans);
				closeApp.IsEnabled = true;
			}
			catch (InvalidOperationException)
			{
				tblkResult.Text = "User not found!";
				tblkResult.Foreground = Brushes.Red;
			}
			catch (NoInternetException)
			{
				tblkResult.Text = "No Internet!!";
				tblkResult.Foreground = Brushes.Red;
			}
		}

		private void tblkResult_MouseDown(object sender, MouseButtonEventArgs e)
		{
			tblkResult.Text = string.Empty;
			btnTransfer.IsEnabled = true;
			closeApp.IsEnabled = true;
		}

		private void Label_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
		{
			Label label = sender as Label;
			switch (label.Content as string)
			{
				case "-":
					this.WindowState = System.Windows.WindowState.Minimized;
					break;

				case "X":
					//App.Mutex.Close();
					this.Close();
					break;
			}
		}

		private void btnSendRynga_Click(object sender, RoutedEventArgs e)
		{
			numberErrorStory.Stop();
			txtPin.Background = Brushes.White;
			txtPin.ToolTip = null;
			XElement root = XElement.Load(App.xmlPath);

			XElement elem = root.Element("Rynga").Element("reseller");
			Rynga ryngaAccount = new Rynga(elem.Attribute("username").Value,
				elem.Attribute("password").Value,
				elem.Attribute("pincode").Value);

			if (txtPin.Password != ryngaAccount.Pincode)
			{
				txtPin.Background = Brushes.OrangeRed;
				txtPin.ToolTip = "Incorrect PIN";
				return;
			}
			else if (!Regex.IsMatch(txtNumber.Text, @"^(03|70|71|76|78|79)\d{6}$"))
			{
				numberErrorStory.Begin();
				return;
			}

			btnSendRynga.IsEnabled = false;
			elem = root.Element("Rynga").Element("touch");
			TouchWeb touchAccount = new TouchWeb(elem.Attribute("username").Value,
				elem.Attribute("password").Value,
				txtNumber.Text);
			ryngaAccount.GenerateVouchers(cmbxVoucherAmount.SelectedIndex, cmbxVoucherQuantity.SelectedIndex + 1);

			touchAccount.SendVouchers(cmbxVoucherQuantity.SelectedIndex + 1, this);

			Thread work = new Thread(new ParameterizedThreadStart(voucherGeneration));
			work.IsBackground = true;
			work.Start();
		}

		void voucherGeneration(object o)
		{
			EventWaitHandle reset = new EventWaitHandle(false, EventResetMode.AutoReset, "mainResetEvent");
			reset.WaitOne();

			//EventWaitHandle.OpenExisting("vouchersResetEvent").WaitOne();
			this.Dispatcher.BeginInvoke(new ThreadStart(() => btnSendRynga.IsEnabled = true));
		}

		private void textBox1_PreviewKeyDown(object sender, KeyEventArgs e)
		{
			//switch (e.Key)
			//{
			//	case Key.Down:
			//		if (CustomersListBox.SelectedIndex < CustomersListBox.Items.Count)
			//		{
			//			Customer c = (Customer)CustomersListBox.Items[CustomersListBox.SelectedIndex + 1];
			//			SelectCustomer(c);
			//		}
			//		e.Handled = true;
			//		break;

			//	case Key.Up:
			//		if (CustomersListBox.SelectedIndex > 0)
			//		{
			//			Customer c = (Customer)CustomersListBox.Items[CustomersListBox.SelectedIndex - 1];
			//			SelectCustomer(c);
			//		}
			//		e.Handled = true;
			//		break;

			//	default:
			//		e.Handled = false;
			//		break;
			//}
		}

		private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
		{
			if (((Keyboard.Modifiers & ModifierKeys.Control) > 0) &&
				((Keyboard.Modifiers & ModifierKeys.Shift) > 0))
			{
				switch (e.Key)
				{
					case Key.T:
						System.Diagnostics.Process.Start("Notepad.exe", App.xmlPath);
						break;
					case Key.A:
						if (new AddUserWindow() { Owner = this, Tag = ResellersComboBox.SelectedItem }.ShowDialog() ?? false)
						{
							ListBoxItem_MouseDoubleClick(null, null);
						}
						e.Handled = true;
						break;
				}
			}
		}

		private void Label_MouseDown(object sender, MouseButtonEventArgs e)
		{
			Label l = sender as Label;

			if (((string)l.Content) == "Manage Resellers")
				new ResellerWindow() { Owner = this }.ShowDialog();
			else
			{
				CustomersUpdaterWindow win1 = new CustomersUpdaterWindow();
				win1.DataContext = ResellersComboBox.SelectedItem;
				win1.ShowDialog();
			}
		}

		private void possibleCustomers_MouseDoubleClick(object sender, MouseButtonEventArgs e)
		{
			ListBox lb = sender as ListBox;
			Customer cust = lb.SelectedItem as Customer;
			popup.IsOpen = false;
			SelectCustomer(cust);
		}

		private void TransfersLabel_MouseDown(object sender, MouseEventArgs e)
		{
			new TransfersWindow() { Tag = ResellersComboBox.SelectedItem, Owner = this }.ShowDialog();
		}

		private void ListBoxItem_MouseDoubleClick(object sender, MouseEventArgs e)
		{
			new CustomerDetailsWindow() { DataContext = CustomersListBox.SelectedItem, Owner = this }.ShowDialog();
		}

		public void SelectCustomer(Customer cust)
		{
			CustomersListBox.SelectedItem = cust;
			CustomersListBox.ScrollIntoView(cust);
			textBox1.Text = cust.Username;
			textBox1.CaretIndex = textBox1.Text.Length;
			FocusManager.SetFocusedElement(this, textBox1);
		}
	}
}
