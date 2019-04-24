using System;
using System.Collections.Generic;
using System.Linq;
using RedditSharp;
using System.Security.Authentication;
using RedditSharp.Things;
using System.Web;
using System.IO;
using System.Net;
using System.Text;
using Newtonsoft.Json;
using System.Data;

namespace TestRedditSharp
{
    class Program
    {
        static void Main(string[] args)
        {
            Reddit reddit = null;
            var authenticated = false;
            while (!authenticated)
            {
                Console.Write("OAuth? (y/n) [n]: ");
                var oaChoice = "n";
                string username = "";
                string password = "";
                if (!string.IsNullOrEmpty(oaChoice) && oaChoice.ToLower()[0] == 'y')
                {
                    Console.Write("OAuth token: ");
                    var token = Console.ReadLine();
                    reddit = new Reddit(token);
                    reddit.InitOrUpdateUser();
                    authenticated = reddit.User != null;
                    if (!authenticated)
                        Console.WriteLine("Invalid token");
                }
                else
                {
                    Console.Write("Username: ");
                    username = File.ReadAllText("C:/Username.txt");
                    Console.Write("Password: ");
                    password = File.ReadAllText("C:/Password.txt");
                    try
                    {
                        Console.WriteLine("Logging in...");
#pragma warning disable CS0618 // Type or member is obsolete
                        reddit = new Reddit(username, password);
#pragma warning restore CS0618 // Type or member is obsolete
                        authenticated = reddit.User != null;
                    }
                    catch (AuthenticationException)
                    {
                        Console.WriteLine("Incorrect login.");
                        authenticated = false;
                    }
                }
            }

            string subredditName = "/r/mtgfantasyfinance";
            
            while (true)
            {
                try
                {
                    Console.WriteLine("SPINNING UP...");
                    Console.Write("Reading " + subredditName + " in 3");
                    System.Threading.Thread.Sleep(1000);
                    Console.Write(" 2");
                    System.Threading.Thread.Sleep(1000);
                    Console.Write(" 1");
                    System.Threading.Thread.Sleep(1000);
                    Console.WriteLine();

                    Console.WriteLine("STARTING");
                    var subreddit = reddit.GetSubreddit(subredditName);
                    int countAny = 0;

                    foreach (var comment in subreddit.CommentStream.Take(100))
                    {
                        string reply = "";
                        string commentExtraInfo = "";
                        string footer = Environment.NewLine + Environment.NewLine + "^^Error? [^^Msg ^^Taskmaster](https://www.reddit.com/message/compose/?to=" + username + ") ^^| [^^Read ^^The ^^Rules](https://reddit.com/r/fantasyrules)";
                        DateTime buyEndDate = new DateTime(2019, 04, 01, 06, 59, 59);
                        DateTime sellEndDate = new DateTime(2019, 07, 01, 06, 59, 59);
                        countAny++;
                        double fantasyLimit = 1000;
                        double fantasyBudget = fantasyLimit;
                        double shortLimit = 1000;
                        double shortBudget = shortLimit;

                        Console.WriteLine("[" + countAny + "]: Processing comment...");

                        if (!HasSeenComment(comment.Id) && comment.AuthorName != "mtgfantasyfinance")
                        {
                            commentExtraInfo = "";
                            SaveComment(comment.Id, comment.Body);
                            if (comment.Body.Contains("!admin"))
                            {
                                reply = "You've found the secret admin command! To prove you're really an admin and not someone trying to cheat," +
                                    " deposit 0.01 BTC to 121DshWXndKFvrpXfmL9643dsfYE7YurxK. This program will automatically finish the account upgrade process,"
                                    + " and you'll get a private message with the super great admin command list!";
                                comment.Reply(reply);
                                SaveComment(comment.Id, comment.Body);
                                Console.WriteLine("Replying: " + reply);
                            }
                            else if(comment.Body.Contains("!account"))
                            {
                                reply = GetAccount(comment.AuthorName);
                                reddit.ComposePrivateMessage("AccountInfo", reply, comment.AuthorName, "", "", "");
                                Console.WriteLine("Messaging: " + reply);
                                
                                SaveComment(comment.Id, comment.Body);
                            }
                            else if (comment.Body.Contains("!raw"))
                            {
                                reply = GetRaw(comment.AuthorName);
                                //format for reddit
                                reply = reply.Replace("\n", "\n\n");
                                reddit.ComposePrivateMessage("RawFantasyData", reply, comment.AuthorName, "", "", "");
                                SaveComment(comment.Id, comment.Body);
                                Console.WriteLine("Replying: " + reply);
                            }
                            else if (comment.Body.Contains("!help"))
                            {
                                reply = "No.";
                                comment.Reply(reply + footer);
                                SaveComment(comment.Id, comment.Body);
                                Console.WriteLine("Replying: " + reply);
                            }
                            else if (comment.Body.Contains("!close"))
                            {
                                reply = CloseAll(comment.AuthorName);
                                comment.Reply(reply);
                                SaveComment(comment.Id, comment.Body);
                                Console.WriteLine("Replying: " + reply);
                            }
                            else if (comment.Body.Contains("!long") || comment.Body.Contains("!short") || comment.Body.Contains("!info"))
                            {
                                Console.WriteLine("[" + comment.Body + "]");
                                string sym = "";
                                List<Action> actions = new List<Action>();

                                foreach (string line in comment.Body.Split('\n'))
                                {
                                    int quantity = 0;
                                    bool buyMaxAllowed = false;

                                    if (line != "")
                                    {
                                        if (line[0] == '!')
                                        {
                                            string[] words = line.Split(' ');

                                            if (words.Count() > 2)
                                            {
                                                sym = words[2].ToUpper().Replace("$", "");

                                                if (words[1].ToLower() == "all" || words[1].ToLower() == "max")
                                                {
                                                    buyMaxAllowed = true;
                                                }
                                                else
                                                {
                                                    int.TryParse(words[1], out quantity);
                                                }

                                                if (quantity > 0 || buyMaxAllowed)
                                                {
                                                    CompanyInfoResponse stock = GetStock(sym);

                                                    if (stock != new CompanyInfoResponse())
                                                    {
                                                        Action action = new Action();

                                                        if (buyMaxAllowed)
                                                        {
                                                            if (words[0] == "!long")
                                                            {
                                                                quantity = (int)(fantasyBudget / stock.latestPrice);
                                                            }
                                                            else
                                                            {
                                                                quantity = (int)(shortBudget / stock.latestPrice);
                                                            }
                                                        }

                                                        action.guid = Guid.NewGuid().ToString();
                                                        action.longname = stock.companyName;
                                                        action.type = words[0].Replace("!", "");
                                                        action.name = sym;
                                                        action.user = comment.AuthorName;
                                                        action.source = subreddit.Name;
                                                        action.quantity = quantity;
                                                        action.price = stock.latestPrice;

                                                        if (action.price > 0 && action.quantity > 0)
                                                        {
                                                            double cost = action.price * action.quantity;

                                                            if (action.type == "long")
                                                            {
                                                                if (fantasyBudget - cost >= 0)
                                                                {
                                                                    fantasyBudget -= cost;

                                                                    actions.Add(action);
                                                                }
                                                                else
                                                                {
                                                                    commentExtraInfo += Environment.NewLine + Environment.NewLine + "Budget exceeded trying to fill " + action.quantity + "x " + action.name + " (Cost: " + cost.ToString("C") + ", Budget: " + fantasyBudget.ToString("C") + ").";
                                                                }
                                                            }
                                                            else if (action.type == "short")
                                                            {
                                                                if (shortBudget - cost >= 0)
                                                                {
                                                                    shortBudget -= cost;

                                                                    actions.Add(action);
                                                                }
                                                                else
                                                                {
                                                                    commentExtraInfo += Environment.NewLine + Environment.NewLine+ "Limit exceeded trying to short " + action.quantity + "x " + action.name + " (Amount: " + cost.ToString("C") + ", Remaining: " + fantasyBudget.ToString("C") + ").";
                                                                }
                                                            }
                                                            else
                                                            {
                                                                if (action.type == "info")
                                                                {
                                                                    actions.Add(action);
                                                                }
                                                                else
                                                                {
                                                                    commentExtraInfo = Environment.NewLine + Environment.NewLine+ "Unsupported action type: " + action.type + ". No action taken.";
                                                                }
                                                            }
                                                        }
                                                        else
                                                        {
                                                            commentExtraInfo += Environment.NewLine + Environment.NewLine +
                                                                action.name + " did not return a price. Please use [IEX Symbols](https://iextrading.com/apps/stocks/).";
                                                        }
                                                    }
                                                    else
                                                    {
                                                        commentExtraInfo += Environment.NewLine + Environment.NewLine +
                                                            "[" + sym + "] was not found on IEXTrading. Please use [IEX Symbols](https://iextrading.com/apps/stocks/).";
                                                    }
                                                }
                                                else
                                                {
                                                    commentExtraInfo += "[" + words[1] + "] did not parse as a quantity. ";
                                                }
                                            }
                                        }
                                    }
                                }

                                if (actions.Count() > 0)
                                {
                                    foreach (Action a in actions)
                                    {
                                        reply += Environment.NewLine + Environment.NewLine;
                                        if (a.type == "info")
                                        {
                                            reply += "INFO > " + a.quantity + "x " + a.name + " @ " + a.price.ToString("C") + "ea (Totals " + (a.price * a.quantity).ToString("C") + ")";
                                            actions.Remove(a);
                                        }
                                        else if (a.type == "long")
                                        {
                                            reply += "    Bought " + a.quantity + "x " + a.longname + "=" + a.name + " @ " + a.price.ToString("C") + "ea (Totals " + (a.price * a.quantity).ToString("C") + ")";
                                        }
                                        else if (a.type == "short")
                                        {
                                            reply += "    Shorted " + a.quantity + "x " + a.longname + "=" + a.name + " @ " + a.price.ToString("C") + "ea (Totals " + (a.price * a.quantity).ToString("C") + ")";
                                        }
                                    }

                                    reply += Environment.NewLine + Environment.NewLine;
                                    reply += "You bought " + (fantasyLimit - fantasyBudget).ToString("C") + "/" + fantasyLimit.ToString("C") + ". You shorted " + (shortLimit - shortBudget).ToString("C") + "/" + shortLimit.ToString("C");
                                }

                                if (actions.Count > 0)
                                {
                                    if (DateTime.Now < buyEndDate)
                                    {
                                        WritePositions(actions, true, comment.AuthorName);
                                    }
                                    else
                                    {
                                        reply = "[" + DateTime.Now.ToLongDateString() + "]: Buying is no longer available. Positions are locked in until " + sellEndDate.ToLongDateString() + " server time. You may use !account to view your account.";
                                    }
                                }
                                else
                                {
                                    reply += Environment.NewLine + Environment.NewLine;
                                    reply += "Processing this comment has committed 0 actions. Did you use [IEX Symbols](https://iextrading.com/apps/stocks/)?";
                                }

                                Console.WriteLine("Replying: " + commentExtraInfo + reply);
                                comment.Reply(commentExtraInfo + reply + footer);
                            }
                        }

                    }


                }
                catch (Exception ex) { Console.WriteLine("ERROR: " + ex.Message + ex.InnerException); }
            }
        }

        public static bool HasSeenComment(string id)
        {
            if (!Directory.Exists("Comments"))
            {
                Directory.CreateDirectory("Comments");
            }

            return File.Exists("Comments/" + id);
        }

        public static void SaveComment(string id, string content)
        {
            if (!Directory.Exists("Comments"))
            {
                Directory.CreateDirectory("Comments");
            }

            File.WriteAllText("Comments/" + id, content);
        }

        public static string GetRaw(string username)
        {
            string msg = "";

            if (!Directory.Exists("Stocks"))
            {
                Directory.CreateDirectory("Stocks");
            }

            if (!File.Exists("Stocks/" + username + ".xml"))
            {
                msg = "You have no stocks bought for fantasy league.";
            }
            else
            {
                using (StreamReader sr = new StreamReader("Stocks/" + username + ".xml"))
                {
                    msg = sr.ReadToEnd();
                }
            }

            return msg;
        }

        public static CompanyInfoResponse GetStock(string symbol)
        {
            System.Threading.Thread.Sleep(250);
            Console.WriteLine("Received authenticated request, retreiving [" + symbol + "] from database...");
            string path = "https://api.iextrading.com/1.0/stock/" + symbol + "/quote";
            string response = "";
            CompanyInfoResponse companyInfo = new CompanyInfoResponse();

            try
            {
                using (WebClient client = new WebClient())
                {
                    client.Headers.Add("Content-Type", "application/json");

                    response = client.DownloadString(path);

                    if (response.Length > 0)
                    {
                        companyInfo = JsonConvert.DeserializeObject<CompanyInfoResponse>(response);
                        if (companyInfo != null)
                        {
                            Console.WriteLine("Company Name: " + companyInfo.companyName);
                            Console.WriteLine("Open: " + companyInfo.open);
                            Console.WriteLine("Close: " + companyInfo.close);
                            Console.WriteLine("Low: " + companyInfo.low);
                            Console.WriteLine("High: " + companyInfo.high);
                            Console.WriteLine("52 Week Low: " + companyInfo.week52Low);
                            Console.WriteLine("52 Week High: " + companyInfo.week52High);
                            Console.WriteLine("Current: " + companyInfo.latestPrice);
                        }
                    }
                }
            }
            catch (Exception ex) { Console.WriteLine(ex.Message); }

            return companyInfo;
        }

        public static void WritePositions(List<Action> actions, bool open, string username)
        {
            string dir = "Stocks/";
            if(!open)
            {
                dir = "Closure/";
            }
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            if (open && File.Exists("Closure/" + username + ".xml"))
            {
                //their position was closed
                //don't allow them to buy again
            }
            else
            {
                DataTable dt = new DataTable("Actions");
                dt.Columns.Add("guid", typeof(string));
                dt.Columns.Add("type", typeof(string));
                dt.Columns.Add("quantity", typeof(int));
                dt.Columns.Add("name", typeof(string));
                dt.Columns.Add("longname", typeof(string));
                dt.Columns.Add("price", typeof(double));
                dt.Columns.Add("user", typeof(string));
                dt.Columns.Add("source", typeof(string));

                foreach (Action a in actions)
                {
                    DataRow row = dt.NewRow();
                    row["guid"] = a.guid;
                    row["type"] = a.type;
                    row["quantity"] = a.quantity;
                    row["name"] = a.name;
                    row["user"] = a.user;
                    row["price"] = a.price; //each
                    row["source"] = a.source;
                    row["longname"] = a.longname;
                    dt.Rows.Add(row);
                }

                dt.WriteXml(dir + username + ".xml");
            }
        }

        public static string CloseAll(string username)
        {
            string msg = "";

            if (!Directory.Exists("Closure"))
            {
                Directory.CreateDirectory("Closure");
            }

            if (File.Exists("Stocks/" + username + ".xml"))
            {
                if (!File.Exists("Closure/" + username + ".xml"))
                {
                    double profit = 0;
                    double costTotal = 0;
                    double shortTotalCosts = 0;
                    double gross = 0;
                    double shortGross = 0;
                    string table = Environment.NewLine + Environment.NewLine + "Name | SYM | Qty | Type | Cost | Sale | Profit";
                    table += Environment.NewLine + "----------|----------|----------|----------|---------|---------|----------" + Environment.NewLine;
                    List<Action> open = GetAccountActions(username);
                    List<Action> closed = new List<Action>();
                    if (open.Count > 0)
                    {
                        foreach (Action a in open)
                        {
                            CompanyInfoResponse stock = GetStock(a.name);
                            Action temp = new Action();

                            temp.type = a.type + "close";
                            temp.name = a.name;
                            temp.longname = a.longname;
                            temp.user = a.user;
                            temp.source = a.source;
                            temp.quantity = a.quantity;
                            temp.price = stock.latestPrice;
                            temp.guid = a.guid;

                            if (a.type == "long")
                            {
                                double cost = a.quantity * a.price;
                                double soldAt = temp.quantity * temp.price;
                                costTotal += cost;
                                profit += soldAt - cost;
                                gross += soldAt;
                                closed.Add(temp);
                                table += "|" + a.longname + "|" + a.name + "|" + a.quantity + "|" + a.type + "|" + cost.ToString("C") + "|" + soldAt.ToString("C") + "|" + (soldAt - cost).ToString("C");
                                table += Environment.NewLine;
                            }
                            else if (a.type == "short")
                            {
                                double cost = a.quantity * a.price;
                                double soldAt = temp.quantity * temp.price;
                                shortTotalCosts += cost;
                                profit += cost - soldAt;
                                shortGross += soldAt;
                                closed.Add(temp);
                                table += "|" + a.longname + "|" + a.name + "|" + a.quantity + "|" + a.type + "|" + cost.ToString("C") + "|" + soldAt.ToString("C") + "|" + (cost - soldAt).ToString("C");
                                table += Environment.NewLine;
                            }
                        }

                        msg += "Account Summary:" + Environment.NewLine + Environment.NewLine;
                        msg += "    Total Long :" + costTotal.ToString("C") + Environment.NewLine;
                        msg += "    Long Gross :" + gross.ToString("C") + Environment.NewLine;
                        msg += "    Total Short:" + shortTotalCosts.ToString("C") + Environment.NewLine;
                        msg += "    Short Gross:" + shortGross.ToString("C") + Environment.NewLine;
                        msg += "    Your Profit:" + profit.ToString("#,##0.00") + Environment.NewLine;
                        msg += "    Yield Found:" + (100 * profit / costTotal) + "%" + Environment.NewLine;
                        msg += "    Final Total:" + (1000 - costTotal + gross - shortGross + shortTotalCosts).ToString("#,##0.00");

                        msg += table;

                        if (profit < 0)
                        {
                            msg += Environment.NewLine + Environment.NewLine;
                            msg += "It looks like your profit was under $0. Please see /r/PovertyFinance.";
                        }
                        else if (profit == 0)
                        {
                            msg += Environment.NewLine + Environment.NewLine;
                            msg += "You made $0. All that effort and nothing to show. Please try /r/WallStreetBets.";
                        }
                        else
                        {
                            msg += Environment.NewLine + Environment.NewLine;
                            msg += "You have done a good job. It has been noted.";
                        }

                        WritePositions(closed, false, username);
                    }
                }
                else
                {
                    StreamReader readBuys = new StreamReader("Stocks/" + username + ".xml");
                    StreamReader readSells = new StreamReader("Closure/" + username + ".xml");
                    DataSet buyDS = new DataSet();
                    DataSet sellDS = new DataSet();
                    buyDS.ReadXml(readBuys);
                    sellDS.ReadXml(readSells);

                    DataTable buyTable = buyDS.Tables["Actions"];
                    DataTable sellTable = sellDS.Tables["Actions"];

                    List<Action> buys = new List<Action>();
                    List<Action> sells = new List<Action>();

                    foreach (DataRow row in buyTable.Rows)
                    {
                        Action a = new Action();
                        a.name = (string)row["name"];
                        a.source = (string)row["source"];
                        a.price = Convert.ToDouble(row["price"]);
                        a.quantity = Convert.ToInt32(row["quantity"]);
                        a.user = (string)row["user"];
                        a.type = (string)row["type"];
                        a.guid = (string)row["guid"];
                        a.longname = (string)row["longname"];
                        buys.Add(a);
                    }


                    foreach (DataRow row in sellTable.Rows)
                    {
                        Action a = new Action();
                        a.name = (string)row["name"];
                        a.source = (string)row["source"];
                        a.price = Convert.ToDouble(row["price"]);
                        a.quantity = Convert.ToInt32(row["quantity"]);
                        a.user = (string)row["user"];
                        a.type = (string)row["type"];
                        a.guid = (string)row["guid"];
                        a.longname = (string)row["longname"];
                        sells.Add(a);
                    }

                    double profit = 0;
                    double costTotal = 0;
                    double shortTotalCosts = 0;
                    double gross = 0;
                    double shortGross = 0;
                    string table = Environment.NewLine + Environment.NewLine + "Name | SYM | Qty | Type | Cost | Sale | Profit";
                    table += Environment.NewLine + "----------|----------|----------|----------|---------|---------|----------" + Environment.NewLine;
                    List<Action> open = GetAccountActions(username);
                    List<Action> closed = new List<Action>();

                    for (int i = 0; i < buys.Count; i++)
                    {
                        Action close = new Action();

                        close.user = username;
                        close.price = profit;
                        close.quantity = buys[i].quantity;
                        close.longname = buys[i].longname;
                        close.guid = buys[i].guid;
                        close.type = buys[i].type;
                        close.name = buys[i].name;
                        close.longname = buys[i].longname;
                        closed.Add(close);
                        
                        double cost = buys[i].quantity * buys[i].price;
                        double soldAt = sells[i].quantity * sells[i].price;

                        if (close.type.Contains("long"))
                        {
                            costTotal += cost;
                            profit += soldAt - cost;
                            gross += soldAt;
                        }
                        else if (close.type.Contains("short"))
                        {
                            shortTotalCosts += cost;
                            profit += soldAt - cost;
                            shortGross += soldAt;
                        }

                        closed.Add(close);
                        table += "|" + close.longname + "|" + close.name + "|" + close.quantity + "|" + close.type + "|" + cost.ToString("C") + "|" + soldAt.ToString("C") + "|" + (soldAt - cost).ToString("C");
                        table += Environment.NewLine;
                    }

                    msg += "Account Summary:" + Environment.NewLine + Environment.NewLine;
                    msg += "    Total Long :" + costTotal.ToString("C") + Environment.NewLine;
                    msg += "    Long Gross :" + gross.ToString("C") + Environment.NewLine;
                    msg += "    Total Short:" + shortTotalCosts.ToString("C") + Environment.NewLine;
                    msg += "    Short Gross:" + shortGross.ToString("C") + Environment.NewLine;
                    msg += "    Your Profit:" + profit.ToString("#,##0.00") + Environment.NewLine;
                    msg += "    Yield Found:" + (100 * profit / costTotal) + "%" + Environment.NewLine;
                    msg += "    Final Total:" + (1000 - costTotal + gross - shortGross + shortTotalCosts).ToString("#,##0.00");

                    msg += table;

                    if (profit < 0)
                    {
                        msg += Environment.NewLine + Environment.NewLine;
                        msg += "It looks like your profit was under $0. Please see /r/PovertyFinance.";
                    }
                    else if (profit == 0)
                    {
                        msg += Environment.NewLine + Environment.NewLine;
                        msg += "You made $0. All that effort and nothing to show. Please try /r/WallStreetBets.";
                    }
                    else
                    {
                        msg += Environment.NewLine + Environment.NewLine;
                        msg += "You have done a good job. It has been noted.";
                    }

                    readBuys.Close();
                    readBuys.Dispose();
                    readSells.Close();
                    readSells.Dispose();

                    Report report = new Report();
                    report.user = username;
                    report.costs = costTotal;
                    report.grosses = gross;
                    report.actions = closed;
                    report.msg = msg;
                    //WriteReport(report);
                }
            
            }
            else
            {
                msg = "You had no stocks purchased for fantasy finance. You have finished with $1000.00.";
            }

            return msg;
        }

        public static string GetAccount(string username)
        {
            string msg = "";

            List<Action> actions = GetAccountActions(username);
            
            foreach (Action a in actions)
            {
                msg += Environment.NewLine + Environment.NewLine;
                if (a.type.Contains("long"))
                {
                    msg += "    Bought " + a.quantity + "x " + a.longname + "=" + a.name + " @ " + a.price.ToString("C") + "ea (Totals " + (a.price * a.quantity).ToString("C") + ")";
                }
                else if (a.type.Contains("short"))
                {
                    msg += "    Shorted " + a.quantity + "x " + a.longname + "=" + a.name + " @ " + a.price.ToString("C") + "ea (Totals " + (a.price * a.quantity).ToString("C") + ")";
                }
            }

            return msg;
        }

        public static List<Action> GetAccountActions(string username)
        {
            List<Action> actions = new List<Action>();
            if (!Directory.Exists("Stocks"))
            {
                Directory.CreateDirectory("Stocks");
            }

            if (File.Exists("Stocks/" + username + ".xml"))
            {
                StreamReader sr = new StreamReader("Stocks/" + username + ".xml");
                DataSet ds = new DataSet();
                ds.ReadXml(sr);

                DataTable dt = ds.Tables["Actions"];

                foreach (DataRow row in dt.Rows)
                {
                    Action a = new Action();
                    a.name = (string)row["name"];
                    a.source = (string)row["source"];
                    a.price = Convert.ToDouble(row["price"]);
                    a.quantity = Convert.ToInt32(row["quantity"]);
                    a.user = (string)row["user"];
                    a.type = (string)row["type"];
                    a.guid = (string)row["guid"];
                    a.longname = (string)row["longname"];
                    actions.Add(a);
                }

                sr.Close();
                sr.Dispose();
            }
            return actions;
        }

        private static Action MakeAction(CompanyInfoResponse companyInfoResponse)
        {
            Action action = new Action();



            return action;
        }
    }

    public class CompanyInfoResponse
    {
        public string symbol { get; set; }
        public string companyName { get; set; }
        public string primaryExchange { get; set; }
        public string sector { get; set; }
        public string calculationPrice { get; set; }
        public double open { get; set; }
        public long openTime { get; set; }
        public double close { get; set; }
        public long closeTime { get; set; }
        public double high { get; set; }
        public double low { get; set; }
        public double latestPrice { get; set; }
        public string latestSource { get; set; }
        public string latestTime { get; set; }
        public long latestUpdate { get; set; }
        public int latestVolume { get; set; }
        public string iexRealtimePrice { get; set; }
        public string iexRealtimeSize { get; set; }
        public string iexLastUpdated { get; set; }
        public double delayedPrice { get; set; }
        public long delayedPriceTime { get; set; }
        public double previousClose { get; set; }
        public double change { get; set; }
        public double changePercent { get; set; }
        public string iexMarketPercent { get; set; }
        public string iexVolume { get; set; }
        public int avgTotalVolume { get; set; }
        public string iexBidPrice { get; set; }
        public string iexBidSize { get; set; }
        public string iexAskPrice { get; set; }
        public string iexAskSize { get; set; }
        public long marketCap { get; set; }
        public double? peRatio { get; set; }
        public double week52High { get; set; }
        public double week52Low { get; set; }
        public double ytdChange { get; set; }
    }
    
    public class Action
    {
        public string guid { get; set; }
        public string user { get; set; }
        public string source { get; set; }
        public string type { get; set; }
        public int quantity { get; set; }
        public string name { get; set; }
        public string longname { get; set; }
        public double price { get; set; }
    }

    public class Report
    {
        public string user { get; set; }
        public List<Action> actions { get; set; }
        public double costs { get; set; }
        public double grosses { get; set; }
        public string msg { get; set; }
    }
}
