using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Globalization;
using System.Diagnostics;
using System.Threading;
using System.Windows.Threading;

using WPF = System.Windows.Controls;
using System.Windows.Media;
using System.Security.Cryptography;

namespace Euro_Transfer.Classes
{
	class CustomersExtractor
	{
		const string loginPage = "www.voipinfocenter.com";

		const string loginNameTextBoxId = "tbUsername";
		const string loginPassTextBoxId = "tbPassword";
		const string loginSubmitId = "btnLogin";

		const string userLinkId = "ctl00_navCtrlV2_ctl07_ctl01_ctl01_LinkButton1";
		const string nextUserLinkId = "ctl00_MainContent_PrevNextCtrlTop_lbNext";
		const string usersTableId = "ctl00_MainContent_CustomerList1_GridView1";

		const string transactionsLinkId = "ctl00_navCtrlV2_ctl06_ctl02_LinkButton1";
		const string nextLinkId = "ctl00_MainContent_PrevNextCtrlSalesTop_lbNext";
		const string transactionsTableId = "ctl00_MainContent_SalesList_GridViewSales";

		const string creditsId = "ctl00_navCtrlV2_ctl02_lblCredits";

		WebBrowser browser;
		Reseller reseller;

		DispatcherTimer timer;

		WPF.TextBlock statusText;
		WPF.ProgressBar progressBar;

		bool getTransfers;

		public event EventHandler Finished;

		public CustomersExtractor(Reseller res, WPF.TextBlock st, WPF.ProgressBar pb, bool gt = false)
		{
			reseller = res;

			statusText = st;
			progressBar = pb;

			getTransfers = gt;

			browser = new WebBrowser();
			browser.ScriptErrorsSuppressed = true;
			browser.DocumentCompleted += performLogin;
		}

		public void GetCustomers()
		{
			//browser.DocumentCompleted += userPage;
			//using (StreamReader reader = new StreamReader("test.txt"))
			//{
			//    browser.DocumentText = reader.ReadToEnd();
			//}

			//browser.DocumentCompleted += transactionsPage;
			//using (StreamReader reader = new StreamReader("test1.txt"))
			//{
			//	browser.DocumentText = reader.ReadToEnd();
			//}

			browser.Navigate(loginPage);
			statusText.Text = "Initializing .....";
		}


		void performLogin(object sender, WebBrowserDocumentCompletedEventArgs e)
		{
			try
			{
				browser.Stop();
				browser.DocumentCompleted -= performLogin;
				if (browser.DocumentText.Contains("Remaining credit"))
				{
					loginCompleted(this, null);
					return;
				}
				else
					browser.DocumentCompleted += loginCompleted;
				HtmlDocument document = browser.Document;
				HtmlElement elem;
				elem = document.GetElementById(loginNameTextBoxId);
				elem.SetAttribute("value", reseller.Name);
				elem = document.GetElementById(loginPassTextBoxId);
				elem.SetAttribute("value", reseller.Password);
				elem = document.GetElementById(loginSubmitId);
				elem.InvokeMember("click");
			}
			catch (Exception ex)
			{
				handleException(ex);
			}
		}

		private void loginCompleted(object sender, WebBrowserDocumentCompletedEventArgs e)
		{
			try
			{
				statusText.Text = "Done intializing, preparing customers";
				browser.Stop();
				browser.DocumentCompleted -= loginCompleted;
				browser.DocumentCompleted += userPage;
				HtmlDocument document = browser.Document;
				HtmlElement elem;
				elem = document.GetElementById(userLinkId);
				elem.InvokeMember("click");
			}
			catch (Exception ex)
			{
				handleException(ex);
			}
		}

		int hash = 0;
		int repCount = 0;
		int userPages = 0;
		int currentPage = 1;
		bool checkUserPages = true;
		CustomerCollection updatedCustomers = new CustomerCollection();
		private void userPage(object sender, WebBrowserDocumentCompletedEventArgs e)
		{
			try
			{
				browser.Stop();

				HtmlDocument document = browser.Document;
				HtmlElement tableElem = document.GetElementById(usersTableId);

				if (hash == tableElem.InnerText.GetHashCode() && timer != null)
				{
					if (repCount++ == 10)
					{
						if (Finished != null) Finished(this, EventArgs.Empty);
						errorHandler("Taking long time. Check Internet. Updating halted.");
					}
					else
						timer.Start();
					return;
				}

				repCount = 0;
				hash = tableElem.InnerText.GetHashCode();

				HtmlElement nextElem;
				nextElem = document.GetElementById(nextUserLinkId);

				if (checkUserPages)
				{
					checkUserPages = false;
					repCount = 0;
					HtmlElement pagesContainer = nextElem.Parent;
					userPages = pagesContainer.Children.Count - 2;

					progressBar.Visibility = System.Windows.Visibility.Visible;
					progressBar.Maximum = userPages;

					timer = new DispatcherTimer();
					timer.Interval = TimeSpan.FromSeconds(3);
					timer.Tick += (s, args) =>
					{
						(s as DispatcherTimer).Stop();
						userPage(null, null);
					};

					((CustomersXmlDb)App.CustomersDB).DisableSaving();

				}

				statusText.Text = string.Format("Getting page {0} out of {1}", ++currentPage, userPages);

				getUsersFromHtmlTable(tableElem);

				if (nextElem.Enabled == true)
				{
					progressBar.Value = currentPage;
					nextElem.InvokeMember("click");
					timer.Start();
				}
				else
				{
					statusText.Text = "Done updating " + updatedCustomers.Count + " customers" +
						(getTransfers ? ", waiting to get transfers" : "");
					browser.DocumentCompleted -= userPage;
					if (getTransfers)
					{
						progressBar.IsIndeterminate = true;
						browser.DocumentCompleted += transactionsPage;
						timer = new DispatcherTimer();
						timer.Interval = TimeSpan.FromSeconds(5);
						timer.Tick += (s, args) =>
						{
							(s as DispatcherTimer).Stop();
							transactionsPage(null, null);
						};
						document.GetElementById(transactionsLinkId).InvokeMember("click");
					}
					else
					{
						if (Finished != null)
							Finished(this, EventArgs.Empty);
					}

				}
			}
			catch (Exception ex)
			{
				handleException(ex);
			}
		}

		private void getUsersFromHtmlTable(HtmlElement tableElem)
		{
			HtmlElement tableBody = tableElem.GetElementsByTagName("tbody")[0];

			for (int i = 1; i < tableBody.Children.Count; i++)
			{
				HtmlElement elem = tableBody.Children[i];
				string customerName = elem.Children[0].FirstChild.InnerText.Split(new char[] { '*' })[0];
				string bs = elem.Children[2].GetElementsByTagName("input")[0].GetAttribute("checked");
				bool blocked = (bs == "True");
				bool isReseller = elem.Children[4].InnerText == "Reseller";

				Customer cust = reseller.Customers.FirstOrDefault(c => c.Username == customerName);
				if (cust == null)
				{
					cust = new Customer(customerName, "", reseller) { IsBlocked = blocked, IsReseller = isReseller };

					updatedCustomers.Add(cust);
					App.CustomersDB.AddCustomer(cust);
				}
				else
				{
					if (cust.IsBlocked != blocked)
					{
						cust.IsBlocked = blocked;
						App.CustomersDB.UpdateCustomer(cust);
					}
				}
			}
		}

		private void transactionsPage(object sender, WebBrowserDocumentCompletedEventArgs e)
		{
			statusText.Text = "Getting Transfers, it may take several minutes";
			try
			{
				browser.Stop();
				HtmlDocument document = browser.Document;
				HtmlElement tableElem = document.GetElementById(transactionsTableId);

				if (hash == tableElem.InnerText.GetHashCode() && timer != null)
				{
					if (repCount++ == 10)
					{
						if (Finished != null) Finished(this, EventArgs.Empty);
						errorHandler("Taking long time. Check Internet. Updating halted.");
					}
					else
						timer.Start();
					return;
				}

				repCount = 0;
				hash = tableElem.InnerText.GetHashCode();

				HtmlElement tableBody = tableElem.GetElementsByTagName("tbody")[0];

				CultureInfo french = new CultureInfo("fr");
				french.NumberFormat.CurrencyDecimalSeparator = ".";
				french.NumberFormat.NumberDecimalSeparator = ".";

				for (int i = 1; i < tableBody.Children.Count; i++)
				{
					HtmlElement elem = tableBody.Children[i];
					string customerName = elem.Children[0].InnerText;
					string transferDateString = elem.Children[1].InnerText;
					DateTime transferDate = DateTime.Parse(transferDateString, CultureInfo.InvariantCulture);


					if (transferDate.CompareTo(reseller.LastUpdate) < 0)
					{
						browser.DocumentCompleted -= transactionsPage;
						if (Finished != null)
							Finished(this, EventArgs.Empty);
						return;
					}

					string amountStr = elem.Children[2].InnerText.Split(new char[] { ' ' })[0];

					double amount = 0.0;
					amount = double.Parse(amountStr, NumberStyles.Currency, french);

					Customer cust = reseller.Customers.FirstOrDefault(c => c.Username == customerName);
					//if (cust == null) continue;

					Transfer trans = cust.Transfers.FirstOrDefault(t =>
							t.TransferDate.Equals(transferDate)
						//TimeSpan comp = t.TranferDate.Subtract(trasnferDate);
						//return comp.TotalMinutes < 3 && comp.Negate().TotalMinutes < 3;
						);
					if (trans == null)
					{
						if (amount == Configuration.InitialTopup)
							trans = new Transfer(Configuration.InitialTopup, Configuration.InitialCost, transferDate);
						else
						{
							trans = Transfer.Create(cust, amount);
							trans.TransferDate = transferDate;
						}

						App.CustomersDB.AddTransfer(cust, trans);
					}
				}

				HtmlElement nextElem = document.GetElementById(nextLinkId);
				if (nextElem.Enabled == true)
				{
					nextElem.InvokeMember("click");
					timer.Start();
				}
				else
				{
					browser.DocumentCompleted -= transactionsPage;
					if (Finished != null)
						Finished(this, EventArgs.Empty);
				}
			}
			catch (Exception ex)
			{
				handleException(ex);
			}
		}

		private void handleException(Exception ex)
		{
			if (ex is NullReferenceException)
				errorHandler("Check Internet Connectivity");
			else
				handleException(ex);
		}

		private void errorHandler(string error)
		{
			EndWork();
			statusText.Text = "Error: " + error;
			statusText.Foreground = new SolidColorBrush(Colors.Red);
			progressBar.Visibility = System.Windows.Visibility.Collapsed;
			((CustomersXmlDb)App.CustomersDB).EnableSaving();
		}

		public void EndWork()
		{
			if (browser != null)
			{
				browser.Stop();
				browser.Dispose();
			}
		}
	}
}
