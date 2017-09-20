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
        private string myr_user = String.Empty;
        private string myr_password = String.Empty;
        private string myr_server = String.Empty;
        private string myr_connection = String.Empty;
        private string myr_key = String.Empty;
        private string myr_passphrase = String.Empty;
        private PrivateKeyAuthenticationMethod pkey;

        public MyrJob(string u, string s, string p)
        {
            myr_user = u;
            myr_server = s;
            myr_password = p;
        }

        public void setPassword(string p) 
        {
            myr_password = p;
        }

        public void setUser(string u) {
            myr_user = u;
        }

        public void setSever(string s) 
        {
            myr_server = s;
        }

        public void setKey(string key_location)
        {
            pkey = new PrivateKeyAuthenticationMethod("username", new PrivateKeyFile[]{
                    new PrivateKeyFile(@key_location)
            });
        }

        public void setKey(string key_location, string passphrase)
        {
            pkey = new PrivateKeyAuthenticationMethod("username", new PrivateKeyFile[]{
                    new PrivateKeyFile(key_location, passphrase)
            });
        }

        public void runCommand(string myr_command) 
        {
            string output = String.Empty;
			Console.WriteLine(myr_server);
			try
			{
                using (var client = new SshClient(myr_server, myr_user, myr_password))
				{

					client.Connect();
					if (myr_command != String.Empty)
					{
						Console.Write(client.RunCommand(myr_command).Result);
					}
					client.Disconnect();

				}
			}
			catch (Exception e)
			{
				Console.WriteLine(e.Message);
			}

        }

		public void runFile(string myr_file)
		{
			
					Console.WriteLine(myr_server);
					StreamReader file = new StreamReader(myr_file);
					string line = String.Empty;
					try
					{
						using (var client = new SshClient(myr_server, myr_user, myr_password))
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
    }
}
