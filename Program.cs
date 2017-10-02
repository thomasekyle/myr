// //////////////////////////////////////////
// Myr: A small automation tool for configuring/command unix/linux boxes.
// @author Kyle Thomas
// @version 0.1.0
//
// ///////////////////////////////////


using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Renci.SshNet;
using Mono.Options;


namespace myr
{
    class Program
    {

        static void Main(string[] args)
        {
            int verbosity = 0; //Not implemented yet.
            string server = String.Empty;
            string user = String.Empty;
            string password = String.Empty;
            string dir = String.Empty;
            string key_location = String.Empty;
            string myr_file = String.Empty;
            string myr_command = String.Empty;
            string myr_target = String.Empty;
            string myr_scp = String.Empty;
            Boolean password_flag = false;
            Boolean help = false;


            var options = new OptionSet {
                { "s|server=", "IP/Hostname of the server you wish to connect to.", s => server = s },
                { "t|target=", "Target machines to run a command or myr file on.", t => myr_target = t },
                { "u|user=", "The user you wish to connect as.", u => user = u },
                { "p|password", "The password you wish to use. (You should be using SSH keys.)", p => password_flag = p != null },
                { "i|identity-file=", "The location of the identity file you wish to use (SSH Key)", i => key_location = i },
                { "c|myr_command=", "The command you you want to run.", c => myr_command = c },
                { "m|myr_file=", "The file you you want to run.", m => myr_file = m },
                { "S|scp=","The file(s) you wish to scp. Requires the directory flag.", S => myr_scp = S},
                { "d|dir=","The directory for the scp file upload", d => dir = d},
                { "v", "increase debug message verbosity", v => {
                if (v != null)
                    ++verbosity;
                 } },
                { "h|help", "show this message and exit", h => help = h != null },
            };

           
            //Parising options provided by Mono. Exception message is also provided if the options
            //are used incorrectly.
            List<string> extra;
            try
            {
                // parse the command line
                if (args.Length > 0)
                {
                    extra = options.Parse(args);    
                } else 
                {
					Console.WriteLine("Myr: A small tool for mass unix/linux configuration.");
					Console.WriteLine("Try `myr --help' for more information.");
                    System.Environment.Exit(0);
                }

            }
            catch (OptionException e)
            {
                // output some error message
                Console.Write("Myr: A small tool for mass unix/linux configuration.");
                Console.WriteLine(e.Message);
                Console.WriteLine("Try `myr --help' for more information.");
                return;
            }


            //If the user uses the help flag
			if (help)
			{
                myrHelp();
				options.WriteOptionDescriptions(Console.Out);
				System.Environment.Exit(0);
			}


            //Prompt the user for a password if the -p option is specified.
            if (password_flag)
            {
                password = myrPassword();
            }

            //Cannot specify option target and server at the same time.
            if (server != String.Empty && myr_target != String.Empty)
            {
                Console.WriteLine("The was an error in your command usage. Please use myr --help for usage");
                System.Environment.Exit(0);
            }


            //If server is not provided we will also aceept the value of the first arguement as the server
            if (server == String.Empty && myr_target == String.Empty  && args[0] != null) {
                server = args[0];
            } else if (args[0] == null){
                Console.WriteLine("The was an error in your command usage. Please use myr --help for usage");
                System.Environment.Exit(0);
            }

            //If the user has not specififed the user we will use their local user name
			if (user == String.Empty) {
                user = Environment.UserName;
			}
            Console.WriteLine(myr_target);
            int commands = 0;
            //Check to make sure the user didn't specify a file and a command.
            //check to make sure scp wasn't also specified.
            if (myr_command != String.Empty) commands++;
            if (myr_file != String.Empty) commands++;
            if (myr_scp != String.Empty) commands++;

            if (commands > 1) {
                myrHelp();
				options.WriteOptionDescriptions(Console.Out);
				System.Environment.Exit(0);
            } 
			else if (myr_command != String.Empty) { //Run a myr command if a command is present
                if (server != String.Empty) myrCommandS(server, user, password, myr_command);
                if (myr_target != String.Empty) myrCommandT(myr_target, user, password, myr_command);
            }
			else if (myr_file != String.Empty) { //If a myr file is provided you can run a list of commands
               if (server != String.Empty) myrFileS(server, user, password, myr_file);
                if (myr_target != String.Empty) myrFileT(myr_target, user, password, myr_file);
            } 
			//else if (myr_scp != String.Empty) {
			//	if (server != String.Empty) myrScp(server, user, password, myr_file);
			//	if (myr_target != String.Empty) myrScpT(myr_target, user, password, myr_file);    
			//} 
			else if (myr_scp != String.Empty && dir != String.Empty) {
				
				if (server != String.Empty) myrScp(server, user, password, myr_scp);
				if (myr_target != String.Empty) myrScpT(myr_target, user, password, myr_scp, dir);
			}
			else {
                Console.WriteLine("The was an error in your command usage. Please use myr --help for usage");
            }

			
		}

		static ConnectionInfo startConnection(string server, string user, string password) {
			ConnectionInfo ConnNfo = new ConnectionInfo(server,22,user,
				new AuthenticationMethod[]{

					// Pasword based Authentication
					new PasswordAuthenticationMethod(user, password)

					// Key Based Authentication (using keys in OpenSSH Format)
					//new PrivateKeyAuthenticationMethod("username",new PrivateKeyFile[]{ 
					//	new PrivateKeyFile(@"..\openssh.key","passphrase")
					//}),
				}
			);
			return ConnNfo;
		}


		static void myrScp(string s, string u, string p, string f) {
		
		}

		//Upload file(s) to target servers
		static void myrScpT(string target, string user, string password, string scp, string dir) {
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
							sftp.ChangeDirectory(dir);
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

        //Method for running commands on a server.
        static int myrCommandS(string server, string user, string password, string myr_command)
        {
            Console.WriteLine(server);
            try
            {
                using (var client = new SshClient(server, user, password))
                {

                    client.Connect();
                    if (myr_command != String.Empty)
                    {
                        Console.Write(client.RunCommand(myr_command).Result);
                    }
                    client.Disconnect();

                }
            } catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            
            return 0;
        }

        //Method for running commands on a multiple server using a target file config.
        static int myrCommandT(string target, string user, string password, string myr_command)
        {
            StreamReader file = new StreamReader(target);
            Console.WriteLine(target);
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
                        using (var client = new SshClient(line, user, password))
                        {

                            client.Connect();
                            if (myr_command != String.Empty)
                            {
                                Console.Write(client.RunCommand(myr_command).Result);
                            }
                            client.Disconnect();

                        }
                    } catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                    }
                    
                }
                
            }
                
            return 0;
        }

        //Method for running multiple commands on a server using a myr file.
        static int myrFileS(string server, string user, string password, string myr_file)
        {

            StreamReader file = new StreamReader(myr_file);
            string line = String.Empty;
            try
            {
                using (var client = new SshClient(server, user, password))
                {
                    Console.WriteLine(server);
                    client.Connect();
                    //!sr.EndOfStream
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
           
            return 0;
        }

        //Method for running multiple commands on a multiple servers.
        static int myrFileT(string target, string user, string password, string myr_file)
        {
            StreamReader file_target = new StreamReader(target);
            string server = String.Empty;
            //!sr.EndOfStream
            while (!file_target.EndOfStream)
            {
                server = file_target.ReadLine();
				if (!string.IsNullOrEmpty(server) && !(server.StartsWith("#")))
                {
                    Console.WriteLine(server);
                    StreamReader file = new StreamReader(myr_file);
                    string line = String.Empty;
                    try
                    {
                        using (var client = new SshClient(server, user, password))
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
                
            return 0;
        }


        //Display the help menu.
        static void myrHelp() {
            Console.WriteLine("Myr: A small tool for mass unix/linux configuration.");
            Console.WriteLine("Usage: myr [options] server [commands] ");
            Console.WriteLine();

            Console.WriteLine("Options:");
        }

        //Provides a secure way of entering a password at the command line.
        static string myrPassword()
        {
            Console.WriteLine("Please enter the password you wish to use for authentication:");
            string password = null;
            while (true)
            {
                var key = System.Console.ReadKey(true);
                if (key.Key == ConsoleKey.Enter)
                    break;
                password += key.KeyChar;
            }
            return password;
        }


        private async void createJob(string server, string user, string password)
        {
            //create the job
            //wait for the job to complete
           string result = await RunJob(server, user, password);        
        }

		private async void createJob(string server, string user, string key, string passphrase)
		{
			//create the job
			//wait for the job to complete
			string result = await RunJob(server, user, passphrase);
		}

        private async Task<string> RunJob(string server, string user, string password) 
        {
            //delay the task
            //other jobs can be started while this job is running.
            //return when it is finished
            MyrJob j = new MyrJob(user, server, password);
            await Task.Delay(1000);
            return "Finshed.";
        } 

		private async Task<string> RunJob(string server, string user, string key, string passphrase)
		{	
			//delay the task
			//other jobs can be started while this job is running.
			//return when it is finished
			MyrJob j = new MyrJob(user, server, passphrase);
			await Task.Delay(1000);
			return "Finshed.";
		} 


       
    } 
}
