using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Discord;
using Discord.Webhook;

namespace ChatAppServer
{
    class ChatServer
    {
        private static TcpListener serverSocket;
        private static List<TcpClient> clientsList = new List<TcpClient>();
        private static NetworkStream networkStream;

        // Discord webhook URLs
        private static string joinWebhookUrl = "YOUR_JOIN_DISCORD_WEBHOOK_URL";
        private static string ipWebhookUrl = "YOUR_IP_DISCORD_WEBHOOK_URL";
        private static string bannedIPWebhookUrl = "YOUR_BANNED_IP_DISCORD_WEBHOOK_URL";

        // List of banned IP addresses
        private static List<string> bannedIPs = new List<string>();

        // Dictionary to store client usernames
        private static Dictionary<TcpClient, string> clientUsernames = new Dictionary<TcpClient, string>();

        // List of banned words
        private static List<string> bannedWords = new List<string>
        {
            "Nigger",
            "Coon",
            "Faggit"
        };

        static void Main(string[] args)
        {


            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("");
            Console.WriteLine("");
            Console.WriteLine("");
            Console.WriteLine("");
            Console.WriteLine("                              ███████╗██████╗░░█████╗░███╗░░██╗░█████╗░██╗░░░██╗██╗███╗░░░███╗        ");
            Console.WriteLine("                              ██╔════╝██╔══██╗██╔══██╗████╗░██║██╔══██╗██║░░░██║██║████╗░████║        ");
            Console.WriteLine("                              █████╗░░██████╔╝███████║██╔██╗██║██║░░╚═╝██║░░░██║██║██╔████╔██║        ");
            Console.WriteLine("                              ██╔══╝░░██╔══██╗██╔══██║██║╚████║██║░░██╗██║░░░██║██║██║╚██╔╝██║        ");
            Console.WriteLine("                              ██║░░░░░██║░░██║██║░░██║██║░╚███║╚█████╔╝╚██████╔╝██║██║░╚═╝░██║        ");
            Console.WriteLine("                              ╚═╝░░░░░╚═╝░░╚═╝╚═╝░░╚═╝╚═╝░░╚══╝░╚════╝░░╚═════╝░╚═╝╚═╝░░░░░╚═╝        ");


            // Load banned IP addresses from the text file
            LoadBannedIPs("banned_ips.txt");

            serverSocket = new TcpListener(IPAddress.Any, 7777);
            serverSocket.Start();
            Console.WriteLine("                                         Server started : Listening to Port 7777");

            while (true)
            {
                TcpClient clientSocket = serverSocket.AcceptTcpClient();
                networkStream = clientSocket.GetStream();

                byte[] nameBuffer = new byte[1024];
                int nameBytesRead = networkStream.Read(nameBuffer, 0, nameBuffer.Length);
                string clientUsername = Encoding.ASCII.GetString(nameBuffer, 0, nameBytesRead);

                Console.WriteLine($"{clientUsername} joined.");

                if (IsBannedIP(clientUsername))
                {
                    Console.WriteLine($"{clientUsername} is a banned IP. Connection denied.");
                    SendIPToDiscord(bannedIPWebhookUrl, clientUsername);
                    clientSocket.Close();
                    continue;
                }

                SendMessageToDiscord(joinWebhookUrl, $"{clientUsername} Connected");

                clientsList.Add(clientSocket);
                clientUsernames.Add(clientSocket, clientUsername);

                Thread clientThread = new Thread(() => HandleClient(clientSocket));
                clientThread.Start();
            }
        }

        private static void HandleClient(TcpClient clientSocket)
        {
            NetworkStream stream = clientSocket.GetStream();

            byte[] messageBuffer = new byte[1024];
            int bytesRead;

            // Retrieve the username
            string username = clientUsernames[clientSocket];

            while (true)
            {
                try
                {
                    bytesRead = stream.Read(messageBuffer, 0, messageBuffer.Length);
                    string message = Encoding.ASCII.GetString(messageBuffer, 0, bytesRead);
                    Console.WriteLine($"{username}: {message}");

                    // Check if the message contains any banned words
                    bool containsBannedWord = false;
                    foreach (string bannedWord in bannedWords)
                    {
                        if (message.Contains(bannedWord))
                        {
                            containsBannedWord = true;
                            break;
                        }
                    }

                    if (containsBannedWord)
                    {
                        Console.WriteLine($"Banned word detected in the message from {username}. Message blocked.");
                    }
                    else
                    {
                        // Broadcast the message to all connected clients
                        BroadcastMessage($"{username}: {message}");
                        SendMessageToDiscord(ipWebhookUrl, $"{username}: {message}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"{username} disconnected: {ex.Message}");
                    clientsList.Remove(clientSocket);
                    clientUsernames.Remove(clientSocket);
                    break;
                }
            }

            stream.Close();
            clientSocket.Close();
        }


        private static void BroadcastMessage(string message)
        {
            byte[] messageBytes = Encoding.ASCII.GetBytes(message);

            foreach (TcpClient client in clientsList)
            {
                NetworkStream stream = client.GetStream();
                stream.Write(messageBytes, 0, messageBytes.Length);
            }
        }

        private static void SendMessageToDiscord(string webhookUrl, string message)
        {
            try
            {
                var webhook = new DiscordWebhookClient(webhookUrl);
                webhook.SendMessageAsync(message);
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occurred while sending message to Discord: " + ex.Message);
            }
        }

        private static void SendIPToDiscord(string webhookUrl, string ipAddress)
        {
            try
            {
                var webhook = new DiscordWebhookClient(webhookUrl);
                webhook.SendMessageAsync($"Banned IP address detected: {ipAddress}");
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occurred while sending IP address to Discord: " + ex.Message);
            }
        }

        private static bool IsBannedIP(string ipAddress)
        {
            return bannedIPs.Contains(ipAddress);
        }

        private static void LoadBannedIPs(string filePath)
        {
            try
            {
                bannedIPs.Clear();
                bannedIPs.AddRange(File.ReadAllLines(filePath));
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occurred while loading banned IP addresses: " + ex.Message);
            }
        }
    }
}
