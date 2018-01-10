using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace Euro_Transfer.Classes
{
    public class ResellerAPI
    {
        const string CreateCmd = "createcustomer";
        const string TransferEuroCmd = "settransaction";
        const string BlockCmd = "changeuserinfo";
        const string ChangePassCmd = "changepassword";
        const string CheckPassCmd = "validateuser";
        const string GetUserInfoCmd = "getuserinfo";
        const string CDRCmd = "calloverview";
        const string ResetPassCmd = "resetpassword";


        const string webUrl = "https://77.72.173.130/API/Request.ashx";

        static WebClient getUserInfoClient;
        static ResellerAPI()
        {
            getUserInfoClient = new WebClient();

            getUserInfoClient.QueryString.Add("command", GetUserInfoCmd);
            getUserInfoClient.QueryString.Add("username", "");
            getUserInfoClient.QueryString.Add("password", "");
            getUserInfoClient.QueryString.Add("customer", "");
        }

        async public static Task<Customer> CreateUser(Reseller reseller, string username, string password,int tarrifrate = -2)
        {
            Customer newCust = new Customer(username, password, reseller);

            WebClient client = new WebClient();
            client.QueryString.Add("command", CreateCmd);
            client.QueryString.Add("username", reseller.Name);
            client.QueryString.Add("password", reseller.Password);
            client.QueryString.Add("customer", username);
            client.QueryString.Add("customerpassword", password);
            client.QueryString.Add("tariffrate", tarrifrate.ToString());

            await commitCommand(client);

            return newCust;
        }

        async public static Task BlockUser(Customer cust,bool doBlock = true)
        {
            WebClient client = new WebClient();
            client.QueryString.Add("command", BlockCmd);
            client.QueryString.Add("username", cust.Reseller.Name);
            client.QueryString.Add("password", cust.Reseller.Password);
            client.QueryString.Add("customer", cust.Username);
            client.QueryString.Add("customerblocked", doBlock.ToString());

            await commitCommand(client);
        }

        async public static Task<bool> CheckPassword(Customer cust, string password)
        {
            WebClient client = new WebClient();
            client.QueryString.Add("command", CheckPassCmd);
            client.QueryString.Add("username", cust.Reseller.Name);
            client.QueryString.Add("password", cust.Reseller.Password);
            client.QueryString.Add("customer", cust.Username);
            client.QueryString.Add("customerpassword", password);

			try
			{
				await commitCommand(client);
			}
			catch
			{
				return false;
			}
            return true;
        }

        async public static Task ChangePassword(Customer cust, string newPassword)
        {
            WebClient client = new WebClient();
            client.QueryString.Add("command", ChangePassCmd);
            client.QueryString.Add("username", cust.Reseller.Name);
            client.QueryString.Add("password", cust.Reseller.Password);
            client.QueryString.Add("customer", cust.Username);
            client.QueryString.Add("oldcustomerpassword", cust.Password);
            client.QueryString.Add("newcustomerpassword", newPassword);

            await commitCommand(client);
        }

        async public static Task ResetPassword(Customer cust, string newPassword)
        {
            WebClient client = new WebClient();
            client.QueryString.Add("command", ResetPassCmd);
            client.QueryString.Add("username", cust.Reseller.Name);
            client.QueryString.Add("password", cust.Reseller.Password);
            client.QueryString.Add("customer", cust.Username);
            client.QueryString.Add("newcustomerpassword", newPassword);

            await commitCommand(client);

            //<ResetPassword>
            //    <Customer>a*ahkvoip0</Customer>
            //    <Result>Failed</Result>
            //    <Reason>The new password was denied because it is not secure enough</Reason>
            //</ResetPassword>
        }

        async public static Task<double> GetCustomerInfo(Customer cust)
        {
            double credits = 0;

            getUserInfoClient.QueryString["username"] = cust.Reseller.Name;
            getUserInfoClient.QueryString["password"] = cust.Reseller.Password;
            getUserInfoClient.QueryString["customer"] = cust.Username;

            string result = await commitCommand(getUserInfoClient);

            string d = Regex.Match(result, @"<Balance>(.*)</Balance>", RegexOptions.IgnoreCase).Groups[1].Value;
            credits = double.Parse(d);
            cust.IsBlocked = result.Contains("True");
            return credits;

            //<GetUserInfo>
            //    <Customer>walid*ahkvoip0</Customer>
            //    <Balance>4.37</Balance>
            //    <SpecificBalance>4.37507</SpecificBalance>
            //    <Blocked>False</Blocked>
            //    <EmailAddress/>
            //</GetUserInfo>
        }


        async public static Task TransferEuro(Customer cust, Transfer trans)
        {
            WebClient client = new WebClient();
            client.QueryString.Add("command", TransferEuroCmd);
            client.QueryString.Add("username", cust.Reseller.Name);
            client.QueryString.Add("password", cust.Reseller.Password);
            client.QueryString.Add("customer", cust.Username);
            client.QueryString.Add("amount", trans.Amount.ToString("0.00"));

            string result = await commitCommand(client);

            string d = Regex.Match(result, @"<DateTime>(.*) \(UTC\)</DateTime>", RegexOptions.IgnoreCase).Groups[1].Value;
            DateTime date = DateTime.Parse(d, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal);
            trans.TransferDate = date;

            //<Transaction>
            //	<Customer>aboyoussef</Customer>
            //	<Amount>5</Amount>
            //	<DateTime>2014-04-22 11:51:46 (UTC)</DateTime>
            //	<Result>Success</Result>
            //</Transaction>
        }

        async public static Task<bool> CheckReseller(string resellerName, string resellerPass)
        {
            WebClient client = new WebClient();
            client.QueryString.Add("command", GetUserInfoCmd);
            client.QueryString.Add("username", resellerName);
            client.QueryString.Add("password", resellerPass);
            client.QueryString.Add("customer", "aaa");

            try
            {
                string result = await commitCommand(client);
            }
            catch (InvalidOperationException ex)
            {
                if (ex.Message.Contains("Reseller/Password"))
                {
                    return false;
                }
            }
            return true;
        }

        async static Task<string> commitCommand(WebClient client)
        {
            string result;
            try
            {
                result = await client.DownloadStringTaskAsync(webUrl);
            }
            catch
            {
                throw new NoInternetException("No Internet Connectivity");
            }

            if (result.Contains("Failed"))
            {
                string reason = Regex.Match(result, "<reason>(.*)</reason>", RegexOptions.IgnoreCase).Groups[1].Value;
                throw new InvalidOperationException(reason);
            }
            else
                return result;
        }


    }
}
