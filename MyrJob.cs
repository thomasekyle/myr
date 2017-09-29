using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Renci.SshNet;

namespace myr
{
    public class MyrJob
    {
        private string myrUser = string.Empty;
        private string myrPassword = string.Empty;
        private string myrServer = string.Empty;
        private string myrConnection = string.Empty;
        private string myrKey = string.Empty;
        private string myrPassphrase = string.Empty;
        private string scpFile = string.Empty;
        private PrivateKeyAuthenticationMethod pkey;

        //Constructor for passowrd authenticaton
        public MyrJob(string u, string s, string p)
        {
            myrUser = u;
            myrServer = s;
            myrPassword = p;
        }

        //Constructor for key authentication.
		public MyrJob(string u, string s, string k, string p)
		{
			myrUser = u;
			myrServer = s;
			myrKey = k;
			myrPassphrase = p;   
		}
			
		public string MyrUser
		{
			get {return myrUser;}
			set {myrUser = value;}
		}

		public string MyPassword
		{
			get {return myrPassword;}
			set {myrPassword = value;}
		}

		public string MyrServer
		{
			get {return myrServer;}
			set {myrServer = value;}
		}

		public string ScpFile
		{
			get {return scpFile;}
			set {scpFile = value;}
		}
			
        public void SetKey(string key_location)
        {
            pkey = new PrivateKeyAuthenticationMethod(myrUser, new PrivateKeyFile[]{
                    new PrivateKeyFile(@key_location)
            });
        }

        public void setKey(string key_location, string passphrase)
        {
            pkey = new PrivateKeyAuthenticationMethod(myrUser, new PrivateKeyFile[]{
                    new PrivateKeyFile(@key_location, passphrase)
            });
        }

		/* This method carries out a command on a single server */
        public void RunCommand(string myrCommand) 
        {
            string output = string.Empty;
			Console.WriteLine(myrServer);
			try
			{
                using (var client = new SshClient(myrServer, myrUser, myrPassword))
				{

					client.Connect();
					if (myrCommand != string.Empty)
					{
						Console.Write(client.RunCommand(myrCommand).Result);
					}
					client.Disconnect();
				}
			}
			catch (Exception e)
			{
				Console.WriteLine(e.Message);
			}

        }

		/* This method carries out all of the commands in a myr file on a single server. */
		public void RunFile(string myr_file)
		{
			
					Console.WriteLine(myrServer);
					StreamReader file = new StreamReader(myr_file);
					string line = string.Empty;
					try
					{
						using (var client = new SshClient(myrServer, myrUser, myrPassword))
						{
							client.Connect();
							while (!file.EndOfStream)
							{
								line = file.ReadLine();
								if (!string.IsNullOrEmpty(line) && !(line.StartsWith("#")))
								{
									Console.Write(client.RunCommand(line).Result);
								}
							}
							client.Disconnect();

						}
					}
					catch (Exception e)
					{
						Console.WriteLine(e.Message);
					}
		}

		//Upload file(s) to target servers
		static void RunScp(string server, string user, string password, string scp) {
			StreamReader file = new StreamReader(target);
			string line = String.Empty;
			//!sr.EndOfStream
			while (!file.EndOfStream)
			{
				line = file.ReadLine();
				Console.WriteLine(line);
				if (!string.IsNullOrEmpty(line) && !(line.StartsWith("#")))
				{
					try
					{
						using (var sftp = new SftpClient(startConnection(line, user, password))){
							string uploadfn = scp;
							Console.WriteLine("SCP to host: " + scp);
							sftp.Connect();
							sftp.ChangeDirectory("/tmp");
							Console.WriteLine("Sftp Client is connected: " + sftp.IsConnected);
							using (var uplfileStream = System.IO.File.OpenRead(uploadfn)) {
								sftp.UploadFile(uplfileStream, uploadfn, true);
							}
							sftp.Disconnect();
						}
					} catch (Exception e)
					{
						Console.WriteLine(e.Message);
					}

				}

			}

		}
    }
}
