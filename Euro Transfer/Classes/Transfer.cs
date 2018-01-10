using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;

namespace Euro_Transfer.Classes
{
    public class Transfer
    {
        private double amount;
        private int cost;
        private DateTime transferDate;

        public double Amount
        {
            get { return amount; }
            set
            {
                amount = value;
                cost = (int)(amount * Configuration.Rate);
                roundCost();
            }
        }

        public int Cost
        {
            get { return cost; }
            set
            {
                cost = value;
                double a = ((double)cost) / Configuration.Rate;
                amount = double.Parse(a.ToString("0.00"));
            }
        }

        public DateTime TransferDate
        {
            get { return transferDate; }
            set { transferDate = value; }
        }

        public Transfer()
        {

        }

        public Transfer(double amount, DateTime transferDate)
        {
            Amount = amount;
            this.transferDate = transferDate;
        }

        public Transfer(double amount, int cost, DateTime transferDate)
        {
            this.amount = amount;
            this.cost = cost;
            this.transferDate = transferDate;
        }

        public static Transfer Create(Customer cust, double amountOrCost)
        {
            Transfer trans = new Transfer();
            string username = cust.Username;
            if ((amountOrCost % 500) == 0)
            {
                trans.Cost = (int)amountOrCost;
                if (username.ToLower() == "wissam2" && amountOrCost == 15000)
                    trans.amount = 7;
                else if (cust.IsReseller)
                {
                    trans.amount = amountOrCost / 2000;
                    //int extraFree = (int)amountToTransfer / 10;
                    //amountToTransfer = amountToTransfer + (extraFree * extraFreeRatio);
                }
            }
            else if (username.Contains("ma7al"))
            {
                trans.cost = 0;
                trans.amount = amountOrCost;
            }
            else
            {
                trans.Amount = amountOrCost;

                //int extraFree = (int)amountToTransfer / 10;
                //amountToTransfer = amountToTransfer + (extraFree * extraFreeRatio);

                if (username.ToLower() == "wissam2" && amountOrCost == 7)
                    trans.cost = 15000;
                else if (cust.IsReseller)
                {
                    trans.cost = (int)(amountOrCost * 2000);

                    trans.cost = (int)Math.Ceiling(trans.cost / 500.0);
                    trans.cost = trans.cost * 500;
                }
            }
            addExtra(trans, cust);
            return trans;
        }

        static void addExtra(Transfer t, Customer c)
        {
            double extra = Configuration.ExtraCust;
            if (c.IsReseller)
                extra = Configuration.ExtraReseller;

            int tens = (int)(t.amount / 10.0);
            t.amount += tens * extra;
        }

        void roundCost()
        {
            cost -= 10;
            cost = (int)Math.Ceiling(cost / 500.0);
            cost = cost * 500;
        }
    }
}
