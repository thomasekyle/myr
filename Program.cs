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

namespace Radiant
{
    class Program
    {

        static void Main(string[] args)
        {
            int verbosity = 0;
            string server = String.Empty;
            string user = String.Empty;
            string password = String.Empty;
            string key_location = String.Empty;
            string myr_file = String.Empty;
            string myr_command = String.Empty;
            string myr_target = String.Empty;
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
            //Check to make sure the user didn't specify a file and a command.
            if (myr_command != String.Empty && myr_file != String.Empty) {
                myrHelp();
				options.WriteOptionDescriptions(Console.Out);
				System.Environment.Exit(0);
            } else if (myr_command != String.Empty) { //Run a myr command if a command is present
                if (server != String.Empty) myrCommandS(server, user, password, myr_command);
                if (myr_target != String.Empty) myrCommandT(myr_target, user, password, myr_command);
            }
			else if (myr_file != String.Empty) { //If a myr file is provided you can run a list of commands
               if (server != String.Empty) myrFileS(server, user, password, myr_file);
                if (myr_target != String.Empty) myrFileT(myr_target, user, password, myr_file);
            } else {
                Console.WriteLine("The was an error in your command usage. Please use myr --help for usage");
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
                if (!string.IsNullOrEmpty(line))
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
                if (!string.IsNullOrEmpty(server))
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

        static void myrHelp() {
            Console.WriteLine("Myr: A small tool for mass unix/linux configuration.");
            Console.WriteLine("Usage: myr [options] server [commands] ");
            Console.WriteLine();

            Console.WriteLine("Options:");
        }

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
       
    } 
}
