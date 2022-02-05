﻿using SimpleServer.ClassLib;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Windows.Forms;

namespace SimpleServer
{
	public delegate void ThrDlg();

	public partial class Form1 : Form
	{
		TcpListener server;
		Socket connection;
		NetworkStream networkStream;
		int port;
		IPAddress localAddr;
		BinaryWriter bWriter;
		BinaryReader bReader;
		bool quit;	// Flag to indicate if the server stil running or not.

		// Threads
		Thread acceptSocketThread;
		ThrDlg acceptSocketDlg;

		public Form1()
		{
			InitializeComponent();
			port = 13000;
			localAddr = IPAddress.Parse("127.0.0.1");
			server = new TcpListener(localAddr, port);
			DataLayer.SeedUsers();
			quit = false;

			// Threads
			acceptSocketDlg += AcceptSocket;
			acceptSocketThread = new Thread(new ThreadStart(acceptSocketDlg));
		}

		private void EstablishServer_Click(object sender, EventArgs e)
		{
			server.Start();
			acceptSocketThread.Start();
			ClosingForm clsFrm = new ClosingForm();
			clsFrm.Show();
			this.Hide();
		}

		/// <summary>
		/// AcceptSocketThread thread method to prevent the system
		/// from being freeze while waiting for clients to connect
		/// to the server
		/// </summary>
		private void AcceptSocket()
		{
			while (!quit)
			{
				TcpClient client = server.AcceptTcpClient();
				Thread clientThread = new Thread(
					new ParameterizedThreadStart(AcceptClient)
					);
				clientThread.Start(client);
			}
		}

		private void AcceptClient(object clientObj)
		{
			while (!quit)
			{
				TcpClient client = clientObj as TcpClient;
				networkStream = client.GetStream();
				if (networkStream.DataAvailable)
				{
					bReader = new BinaryReader(networkStream);
					string reqRes = bReader.ReadString();
					int status = int.Parse(reqRes.Split(',')[0]);
					switch (status)
					{
						// Login Status
						case 0:
							LogInUser(reqRes.Split(',')[2]);
							break;
						case 2:
							RedirectToWaitingRoom();
							break;
					}
				}
			}
		}

		private void LogInUser(string userName)
		{
			var matches =	DataLayer.Users.Where(u => u.UserName == userName).ToList();
			bWriter = new BinaryWriter(networkStream);
			if (matches.Count > 0)
			{
				DataLayer.ConnectedUsers.AddRange(matches);
				bWriter.Write($"1,{Views.RoomsList}");
			}
			else
			{
				bWriter.Write("-1,Can't find username.");
			}
		}

		private void RedirectToWaitingRoom()
		{
			bWriter = new BinaryWriter(networkStream);
			bWriter.Write($"1,{Views.WaitingRoom}");
		}
	}
}
