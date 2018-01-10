using System;
using System.Windows.Forms;
using System.Threading;

namespace Euro_Transfer.Classes
{
	public class Rynga
	{
		string step = "";
		System.Windows.Forms.WebBrowser webBrowser;
		int voucherCredit;
		int vouchersQuantity;

		public string Username { get; set; }
		public string Password { get; set; }
		public string Pincode { get; set; }


		public Rynga(string user, string pass, string pin)
		{
			Username = user;
			Password = pass;
			Pincode = pin;
		}

		public void GenerateVouchers(int voucherCredit, int vouchersQuant)
		{
			App.Status.Status = "-Initializing-";
			this.voucherCredit = voucherCredit;
			this.vouchersQuantity = vouchersQuant;
			if (readyVouchers())
			{
				App.Status.Status = "Vouchers Generated";
				EventWaitHandle.OpenExisting("vouchersResetEvent").Set();
				return;
			}
			webBrowser = new System.Windows.Forms.WebBrowser();
			webBrowser.DocumentCompleted += new WebBrowserDocumentCompletedEventHandler(webBrowser_DocumentCompleted);
			webBrowser.ScriptErrorsSuppressed = true;
			webBrowser.Navigate("https://www.rynga.com/login");
			step = "step1";
		}

		bool readyVouchers()
		{
			int availableVouchers = 0;
			int iterations = vouchersQuantity > App.VoucherRecords.Count ? App.VoucherRecords.Count : vouchersQuantity;
			for (int i = 0; i < iterations; i++)
			{
				if (App.VoucherRecords[i].Status == "Not Sent")
					availableVouchers++;
				else
					break;

			}
			vouchersQuantity -= availableVouchers;
			return vouchersQuantity == 0;
		}

		void webBrowser_DocumentCompleted(object sender, WebBrowserDocumentCompletedEventArgs e)
		{
			try
			{
				App.Status.IsError = false;
				switch (step)
				{
					case "step1":
						HtmlElement elem;
						HtmlDocument doc = webBrowser.Document;
						if (doc.Body.InnerText.Contains("Logout"))
						{
							App.Status.Status = "Already Logged in";
							step = "step2";
							return;
						}
						App.Status.Status = "Logging...";
						HtmlElement loginForm = doc.Forms[1];
						elem = loginForm.GetElementsByTagName("INPUT")[0];
						elem.SetAttribute("value", Username);
						elem = loginForm.GetElementsByTagName("INPUT")[1];
						elem.SetAttribute("value", Password);
						loginForm.InvokeMember("submit");
						step = "step2";
						break;

					case "step2":
						if (webBrowser.Document.Url.AbsolutePath.Contains("buy_credit"))
						{
							App.Status.Status = "Logged on.";
							webBrowser.Navigate("https://www.rynga.com/reseller_options/generate_vouchers");
							step = "step3";
						}
						else
							step = "step1";
						break;

					case "step3":
						if (webBrowser.Document.Url.AbsolutePath.Contains("generate_vouchers"))
						{
							step = "step4";
							goto step4;
						}
						else if (webBrowser.Document.Url.AbsolutePath.Contains("validate"))
						{
							App.Status.Status = "Generating Vouchers...";
							HtmlElement pincodeElem;
							pincodeElem = webBrowser.Document.GetElementById("pincode");
							pincodeElem.SetAttribute("value", Pincode);
							webBrowser.Document.Forms[0].InvokeMember("submit");
							step = "step4";
						}
						break;

					case "step4":
					step4:
						if (webBrowser.Document.Url.AbsolutePath.Contains("generate_vouchers"))
						{
							HtmlElement formVoucher = webBrowser.Document.Forms[voucherCredit];
							HtmlElement elemVoucher = formVoucher.GetElementsByTagName("INPUT")[0];
							elemVoucher.SetAttribute("value", vouchersQuantity.ToString());
							step = "step5";
							formVoucher.InvokeMember("submit");
						}
						break;

					case "step5":
						if (webBrowser.Document.Url.AbsolutePath.Contains("voucher_details"))
						{
							HtmlElement divVouchers = webBrowser.Document.GetElementById("page-local-agents-agent-details");
							HtmlElementCollection voucherElements = divVouchers.GetElementsByTagName("DIV")[1].GetElementsByTagName("H3");
							for (int i = 0; i < vouchersQuantity; i++)
							{
								VoucherRecord vouch = new VoucherRecord(DateTime.Now, voucherElements[i].InnerText, "", "Not Sent");
								App.VoucherRecords.Insert(0, vouch);
								VoucherRecord.SaveToXml(vouch);
							}
							App.Status.Status = "Vouchers Generated";
							webBrowser.Dispose();
							EventWaitHandle.OpenExisting("vouchersResetEvent").Set();
						}
						break;

					default:
						break;
				}
			}
			catch
			{
				App.Status.RiseError("Error generating vouchers!");
				webBrowser.Dispose();
				EventWaitHandle.OpenExisting("vouchersResetEvent").Set();
			}
		}
	}
}
