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

        private string line;

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
            Boolean help = false;

            var options = new OptionSet {
                { "s|server=", "IP/Hostname of the server you wish to connect to.", s => server = s },
                { "t|target=", "Target machines to run a command or myr file on.", t => myr_target = t },
                { "u|user=", "The user you wish to connect as.", u => user = u },
                { "p|password=", "The password you wish to use. (You should be using SSH keys.)", p => password = p },
                { "i|identity-file=", "The location of the identity file you wish to use (SSH Key)", i => key_location = i },
                { "c|myr_command=", "The command you you want to run.", c => myr_command = c },
                { "m|myr_file=", "The file you you want to run.", m => myr_file = m },
                { "v", "increase debug message verbosity", v => {
                if (v != null)
                    ++verbosity;
                 } },
                { "h|help", "show this message and exit", h => help = h != null },
            };

           

            List<string> extra;
            try
            {
                // parse the command line
                extra = options.Parse(args);
            }
            catch (OptionException e)
            {
                // output some error message
                Console.Write("Myr: A small tool for mass unix/linux configuration.");
                Console.WriteLine(e.Message);
                Console.WriteLine("Try `myr --help' for more information.");
                return;
            }

			if (help)
			{
				Console.WriteLine("Myr: A small tool for mass unix/linux configuration.");
				Console.WriteLine("Usage: myr [options] server [commands] ");
				Console.WriteLine();

				Console.WriteLine("Options:");
				options.WriteOptionDescriptions(Console.Out);
				System.Environment.Exit(0);
			}
			Console.WriteLine(server);

            //If server is not provided we will also aceept the value of the first arguement as the server
            if (server == null && args[0] != null) {
                server = args[0];
            } else if (args[0] == null){
                Console.WriteLine("You did not provide a server.");
                System.Environment.Exit(0);
            }

            //If the user has not specififed the user we will use their local user name
			if (user == null) {
                user = Environment.UserName;
			}

            Console.WriteLine(System.Environment.CurrentDirectory + "\\" + myr_file);

            //Check to make sure the user didn't specify a file and a command.
            if (myr_command != String.Empty && myr_file != String.Empty) {
                Console.WriteLine("Error - Cannot use a command and input file at the same time.");
				Console.WriteLine("Myr: A small tool for mass unix/linux configuration.");
				Console.WriteLine("Usage: myr [options] server [commands] ");
				Console.WriteLine();

				Console.WriteLine("Options:");
				options.WriteOptionDescriptions(Console.Out);
				System.Environment.Exit(0);
            } else if (myr_command != String.Empty) { //Run a myr command if a command is present
				using (var client = new SshClient(server, user, password))
				{

					client.Connect();
					if (myr_command != String.Empty)
					{
						Console.Write(client.RunCommand(myr_command).Result);
					}
					client.Disconnect();

				}
			}
			else if (myr_file != String.Empty) { //If a myr file is provided you can run a list of commands
                
                StreamReader file = new StreamReader(System.Environment.CurrentDirectory + "\\" + myr_file);
                string line = String.Empty;
				using (var client = new SshClient(server, user, password))
				{
					client.Connect();
                    while  ((line = file.ReadLine()) != null) {
						if (line != Environment.NewLine && !(line.StartsWith("#")) )
						{
							Console.Write(client.RunCommand(line).Result);
						} 
                    }
					client.Disconnect();
                    
				}
			} else {
                Console.WriteLine("The was an error in your command usage. Please use myr --help for usage");
            }

			
		}

       
    } 
}
