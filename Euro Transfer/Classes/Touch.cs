using System;
using System.Threading;
using System.ComponentModel;
using System.Windows.Forms;

namespace Euro_Transfer.Classes
{
	class TouchWeb
	{
		string step = "";
		int vouchersQuantity;
		bool loggedIn = true;
		BackgroundWorker worker;
		WebBrowser webBrowser;


		public string Username { get; set; }
		public string Password { get; set; }
		public string Number { get; set; }


		public TouchWeb(string user, string pass, string num)
		{
			Username = user;
			Password = pass;
			Number = num;
		}

		public void SendVouchers(int quant, MainWindow currentWindow)
		{
			vouchersQuantity = quant;
			worker = new BackgroundWorker();
			worker.DoWork += new DoWorkEventHandler(worker_DoWork);
			worker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(worker_RunWorkerCompleted);

			webBrowser = new System.Windows.Forms.WebBrowser();
			webBrowser.DocumentCompleted += new WebBrowserDocumentCompletedEventHandler(webBrowser_DocumentCompleted);
			webBrowser.Navigate("https://www.touch.com.lb");
			step = "step1";
		}

		void worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
		{
			App.Status.Status = "Messages Sent Successfully";
			webBrowser.Dispose();
			worker.Dispose();
			EventWaitHandle.OpenExisting("mainResetEvent").Set();
		}

		void worker_DoWork(object sender, DoWorkEventArgs e)
		{
			Thread.Sleep(5000);
			webBrowser.Invoke(new Action(() =>
			{
				App.Status.Messages = webBrowser.Document.GetElementById("smspointsId").InnerText;
			}
			));

			EventWaitHandle.OpenExisting("vouchersResetEvent").WaitOne();

			if (App.Status.IsError)
			{
				e.Cancel = true;
				return;
			}
			else if (!loggedIn)
			{
				App.Status.RiseError("Messages not sent,\nTry to send manually");
				e.Cancel = true;
				return;
			}

			App.Status.Brush = System.Windows.Media.Brushes.Turquoise;
			App.Status.Status = "Sending messages....";

			for (int i = 0; i < vouchersQuantity; i++)
			{
				webBrowser.Invoke(new Action(() =>
				{
					App.VoucherRecords[i].Number = Number;
					App.VoucherRecords[i].Date = DateTime.Now;
					HtmlElement elem = webBrowser.Document.GetElementById("number");
					elem.SetAttribute("value", Number);
					webBrowser.Document.InvokeScript("joinContacts");
					elem = webBrowser.Document.GetElementById("w2smessage");
					elem.SetAttribute("value", App.VoucherRecords[i].Voucher);
					elem = webBrowser.Document.GetElementById("send-button");
					elem.InvokeMember("click");
					App.VoucherRecords[i].Status = "Sent";
					VoucherRecord.UpdateXml(App.VoucherRecords[i]);
					string[] smsCount = App.Status.Messages.Split(new char[] { ' ' }, 2);
					App.Status.Messages = (int.Parse(smsCount[0]) - 1).ToString() + " " + smsCount[1];
				}
				));
				Thread.Sleep(4000);
			}
		}

		private void webBrowser_DocumentCompleted(object sender, WebBrowserDocumentCompletedEventArgs e)
		{
			HtmlElement elem;
			switch (step)
			{
				case "step1":
					if (webBrowser.DocumentText.Contains("Logout"))
					{
						goto step2;
					}
					elem = webBrowser.Document.GetElementById("user");
					elem.SetAttribute("value", Username);
					elem = webBrowser.Document.GetElementById("pass");
					elem.SetAttribute("value", Password);
					elem = webBrowser.Document.GetElementById("imageField");
					elem.InvokeMember("click");
					step = "step2";
					break;

				case "step2":
				step2:
					if (!webBrowser.DocumentText.Contains("Logout"))
					{
						loggedIn = false;
					}
					else
					{
						if (!worker.IsBusy)
						{
							loggedIn = true;
							step = "";
							App.Status.Brush = System.Windows.Media.Brushes.Turquoise;
							App.Status.Status = "Logged in touch.com.lb";
							worker.RunWorkerAsync();
						}
					}
					break;
			}
		}
	}
}
