// //////////////////////////////////////////
// myr: A small automation tool for configuring/command unix/linux boxes.
// @author Kyle Thomas
// @version 0.1.0
//
// ///////////////////////////////////


using System;
using System.Collections.Generic;
using System.IO;
using Renci.SshNet;
using Mono.Options;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace myr
{
    class Program
    {
        static void Main(string[] args)
        {

            string server = String.Empty;
            string user = String.Empty;
            string password = String.Empty;
            string dir = String.Empty;
            string keyLocation = String.Empty;
            string myrFile = String.Empty;
            string myrCommand = String.Empty;
            string myrTarget = String.Empty;
            string myrSCP = String.Empty;
            string myrPassphrase = String.Empty;
            string logDir = String.Empty;
            string pbArgs = String.Empty;
            Int32 threads = Convert.ToInt32(Math.Ceiling((Environment.ProcessorCount * 0.75) * 1.0));
            string thread_option = String.Empty;
            List<string> myrTasks = new List<string>();
            Boolean becomeRoot = false;
            Boolean elevateRoot = false;
            Boolean passphraseFlag = false;
            Boolean passwordFlag = false;
            Boolean ttyFlag = false;
            Boolean help = false;


            var options = new OptionSet {
                { "s|server=", "IP/Hostname of the server you wish to connect to.", s => server = s },
                { "S|Servers=", "Target machines to run a command or command file on.", S => myrTarget = S },
                { "u|user=", "The user you wish to connect as.", l => user = l },
                { "pw|password", "The password you wish to use.", p => passwordFlag = p != null },
                { "t|tty", "Use TTY session on logon.", t => ttyFlag = t != null },
                { "b|become-root", "Become(su) root to run elevated commands. (TTY only)", b => becomeRoot = b != null },
                { "e|elevate-root", "Become(su) root to run elevated commands. (TTY only)", e => elevateRoot = e != null },
                { "pb|pbrun=", "Use pbrun to run elevated commands.", pb => pbArgs = pb },
                { "i|identity-file=", "The location of the identity file you wish to use (SSH Key)", i => keyLocation = i },
                { "P|passphrase=", "The passphrase associated with the ssh key you wish to use.", P => passphraseFlag = P != null},
                { "c|command=", "The command you you want to run.", c => myrCommand = c },
                { "C|commandfile=", "The file you you want to run.", C => myrFile = C },
                { "U|scp=","The file(s) you wish to scp. Requires the directory flag.", U => myrSCP = U},
                { "d|directory=","The directory for the scp file upload.", d => dir = d},
                { "l|log=","Log out to text file. You may specify the directory after this arguement. If you don't the current directory will be used.", l => logDir = l},
                { "T|threads=","Specify the amount of concurrency (threads) you wish to use in running commands.", T => thread_option = T},
                //{ "v", "increase debug message verbosity", v => {
                //if (v != null)
                //    ++verbosity;
                // } },
                { "h|help", "Show this message and exit", h => help = h != null },
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
                }
                else
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
            if (passwordFlag)
            {
                password = Password();
            }

            //Cannot specify option target and server at the same time.
            if (server != String.Empty && myrTarget != String.Empty)
            {
                Console.WriteLine("The was an error in your command usage. Please use myr --help for usage");
                System.Environment.Exit(0);
            }


            //If server is not provided we will also aceept the value of the first arguement as the server.
            //also check to make sure uer didn't specify server and a target
            if (server == String.Empty && myrTarget == String.Empty && args[0] != null)
            {
                server = args[0];
            }
            else if (args[0] == null)
            {
                Console.WriteLine("The was an error in your command usage. Please use myr --help for usage");
                System.Environment.Exit(0);
            }

            //If the user has not specififed the user we will use their local user name
            if (user == String.Empty)
            {
                user = Environment.UserName;
            }

            //Check to make sure a server and target weren't both specified
            if (myrTarget != String.Empty && server != String.Empty)
            {
                Console.WriteLine("The was an error in your command usage. Please use myr --help for usage");
                System.Environment.Exit(0);
            }
            else if (myrTarget != String.Empty)
            {
                Console.WriteLine("Using target: " + myrTarget);
                myrTasks = ParseText(myrTarget);

            }
            else if (server != String.Empty)
            {
                myrTasks.Add(server);
            }


            //Check to make sure the user didn't specify a file and a command.
            //check to make sure scp wasn't also specified.
            int commands = 0;
            if (myrCommand != String.Empty) commands++;
            if (myrFile != String.Empty) commands++;
            if (myrSCP != String.Empty) commands++;
            if (commands > 1)
            {
                myrHelp();
                options.WriteOptionDescriptions(Console.Out);
                System.Environment.Exit(0);
            }


            Console.WriteLine("Number of tasks to complete: " + myrTasks.Count);
            
            //Complete all Tasks for server(s). User task count to keep track of which have completed.
            int taskCount = 0;

            // If the user has specified thread amount set to that value.
            // The program will default to about a little over half of the available threads on the 
            // machine it is being run on.
            if (thread_option != String.Empty) threads = Convert.ToInt32(thread_option);
            ParallelOptions pOptions = new ParallelOptions
            {
                MaxDegreeOfParallelism = threads
            };
            Parallel.ForEach(myrTasks, pOptions, t =>
            {
                taskCount++;
                ConnectionInfo myrSession = StartConnection(t, user, password, keyLocation, passphraseFlag);
                if (myrCommand != String.Empty)
                {
                    myrCommandS(myrSession, myrCommand, t, taskCount, logDir, ttyFlag, password, pbArgs);
                }
                if (myrFile != String.Empty)
                {
                    MyrCommandFile(myrSession, myrFile, t, taskCount, logDir);
                }
                if (myrSCP != String.Empty && dir != String.Empty)
                {
                    MyrScp(myrSession, myrSCP, t, taskCount, dir);
                }
            });
        }

         /// <summary>
         /// This function establishes and scp session to upload a file to the target directory specified,
         /// </summary>
         /// <param name="session">The connection session (SCP Session)</param>
         /// <param name="scp">The file that is going to uploaded to the host</param>
         /// <param name="host">the target host that the file will be uploaded to.</param>
         /// <param name="taskID">The ID of the task if there are multiple hosts to upload the file to</param>
         /// <param name="dir">The directory on the host to upload the file to.</param>
        static void MyrScp(ConnectionInfo session, string scp, string host, int taskID, string dir)
        {
            try
            {
                using (var sftp = new SftpClient(session))
                {
                    string uploadfn = scp;
                    Console.WriteLine("[Execution] => " + DateTime.Now.ToString("h:mm:ss tt") + " on host " + host);
                    sftp.Connect();
                    sftp.ChangeDirectory(dir);
                    string basefile = Path.GetFileName(scp);
                    using (var uplfileStream = System.IO.File.OpenRead(uploadfn))
                    {
                        sftp.UploadFile(uplfileStream, basefile, true);
                        Console.WriteLine("[Success] => " + DateTime.Now.ToString("h:mm:ss tt") + " on host " + host);
                    }
                    sftp.Disconnect();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("[Failure] => " + e.Message + " " + DateTime.Now.ToString("h:mm:ss tt") + " on host " + host);
            }


        }

        /// <summary>
        /// This function will run a single command on a host specified by the program.
        /// </summary>
        /// <param name="session">The session used to connect to each host</param>
        /// <param name="myr_command">The command to run on the target host(s)</param>
        /// <param name="host">the host to run the command on.</param>
        /// <param name="taskID">The ID assigned to the specific host to a task</param>
        /// <param name="logDir">The directory that log files will be written to the log option is necessary</param>
        /// <param name="ttyFlag">This flag will allow to use tty session for ssh</param>
        /// <param name="password">The password for elevation. If not specified it will default to the auth password</param>
        /// <param name="pbArgs">Agruements for the pbrun flag. This can be used to run a command if just -c is specified.</param>
        /// <returns>        
        /// If the User provides a logPath this will create the log file(s) in that directory.
        /// otherwise this function will return output to the console.
        /// </returns>
        static int myrCommandS(ConnectionInfo session, string myr_command, string host, int taskID, string logDir, Boolean ttyFlag, string password, string pbArgs)
        {
            try
            {
                using (var client = new SshClient(session))
                {
                    client.Connect();
                    string result = String.Empty;

                    if (myr_command != String.Empty)
                    {
                        if (ttyFlag == true)
                        {
                            client.RunCommand("echo myrfinish > /tmp/myrtmp"); //Create an exitpoint for myr
                            IDictionary<Renci.SshNet.Common.TerminalModes, uint> termkvp = new Dictionary<Renci.SshNet.Common.TerminalModes, uint>();
                            termkvp.Add(Renci.SshNet.Common.TerminalModes.ECHO, 53);

                            ShellStream shellStream = client.CreateShellStream("vt320", 0, 0, 0, 0, 1024, termkvp);
                            String output = shellStream.Expect(new Regex(@"[$>]"));
                            //Console.WriteLine(output);

                            // Stuff for PBuL elevation
                            if (pbArgs != string.Empty)
                            {
                                shellStream.WriteLine("pbrun " + pbArgs);
                                System.Threading.Thread.Sleep(10000);
                                output = shellStream.Expect(new Regex(@"([$#>:])"));
                                if (output.ToLower().Contains("password") && !(output.ToLower().Contains("error")))
                                {
                                    shellStream.WriteLine(password);
                                } else
                                {
                                    throw new System.InvalidOperationException("Could not elevate with pbrun. Error occured. Please retry.");
                                }
                                
                                System.Threading.Thread.Sleep(7000);
                                output = shellStream.Expect(new Regex(@"[$#>]"));
                                shellStream.WriteLine(myr_command + " && cat /tmp/myrtmp");
                                output = shellStream.Expect(new Regex(@"(myrfinish)")); //return output on the exit point
                                result = output;
                                Console.WriteLine(output);
                            } else
                            {
                                shellStream.WriteLine(myr_command);
                                output = shellStream.Expect(new Regex(@"(myrfinish)"));
                                result = output;
                                Console.WriteLine(output);
                            }
                            
                        } else
                        {
                            result = client.RunCommand(myr_command).Result;
                        }
                        
                        if (logDir == String.Empty)
                        {
                            Console.WriteLine("[Success] => " + DateTime.Now.ToString("h:mm:ss tt") + " on host " + host + "\n" + result);
                        }
                        else
                        {
                            Console.WriteLine("[Success] => " + DateTime.Now.ToString("h:mm:ss tt") + " on host " + host);
                            WriteFile(result, host, logDir); 
                        }
                    }
                    client.Disconnect();

                }
            }
            catch (Exception e)
            {
                Console.WriteLine("[Failure] => " + e.Message + " " + DateTime.Now.ToString("h:mm:ss tt") + " on host " + host);
            }

            return 0;
        }

        /// <summary>
        /// This function will run commands in a file on a target host.
        /// </summary>
        /// <param name="session">The connection session (SSH Session)</param>
        /// <param name="File">The file you wish to run commands from</param>
        /// <param name="host">The host that the commands will be run on</param>
        /// <param name="taskID">The ID of the host that commands are running if there are multiple</param>
        /// <param name="logDir">The directory that log files will be place it from the run</param>
        /// <returns>
        /// If the User provides a logPath this will create the log file(s) in that directory.
        /// otherwise this function will return output to the console.
        /// </returns>
        static int MyrCommandFile(ConnectionInfo session, string File, string host, int taskID, string logDir)
        {

            StreamReader file = new StreamReader(File);
            string line = String.Empty;
            string result = String.Empty;

            try
            {
                using (var client = new SshClient(session))
                {
                    client.Connect();
                    Console.WriteLine("[Execution] => " + DateTime.Now.ToString("h:mm:ss tt") + " on host " + host);
                    while (!file.EndOfStream)
                    {
                        line = file.ReadLine();
                        if (!string.IsNullOrEmpty(line) && !(line.StartsWith("#")))
                        {
                            if (logDir == String.Empty)
                            {
                                Console.Write(client.RunCommand(line).Result);
                            }
                            else
                            {
                                result += client.RunCommand(line).Result;
                            }
                        }
                    }
                    if (result != string.Empty)
                    {
                        WriteFile(result, host, logDir);
                        
                    }
                    Console.WriteLine("[Success] => " + DateTime.Now.ToString("h:mm:ss tt") + " on host " + host);
                    client.Disconnect();
                    

                }
            }
            catch (Exception e)
            {
                Console.WriteLine("[Failure] => " + e.Message + " " + DateTime.Now.ToString("h:mm:ss tt") + " on host " + host);
            }

            return 0;
        }

        /// <summary>
        /// This will display the help menu and all the avaible command that you can use with Myr
        /// </summary>
        static void myrHelp()
        {
            Console.WriteLine("Myr: A small tool for mass unix/linux configuration.");
            Console.WriteLine("Usage: myr [options] server [commands] ");
            Console.WriteLine();

            Console.WriteLine("Options:");
        }

        /// <summary>
        /// This function provides a secure way of entering your password in, so it
        /// will not appear on the terminal.
        /// </summary>
        /// <returns>
        /// Return the password entered as a string.
        /// </returns>
        static string Password()
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

        /// <summary>
        /// This will return a list of strings that can be placed inside of a function
        /// to run through a parallel For Each loop.
        /// </summary>
        /// <param name="target">The file you wish to parse and create the list from</param>
        /// <returns>
        /// Return a List that can be fed into a parallel ForEach
        /// </returns>
        static List<string> ParseText(string target)
        {
            List<string> output = new List<string>();
            StreamReader file_target = new StreamReader(target);
            string server = String.Empty;
            while (!file_target.EndOfStream)
            {
                server = file_target.ReadLine();
                if (!string.IsNullOrEmpty(server) && !(server.StartsWith("#")))
                {
                    output.Add(server);
                }

            }

            return output;
        }

     

        /// <summary>
        /// This function will atempt to create a session (SSH/SCP) with the provided credentials.
        /// </summary>
        /// <param name="server">The host to create the session for</param>
        /// <param name="user">The user account to use when authenticating</param>
        /// <param name="password">The password to provide when creating the session</param>
        /// <param name="key_location">The location to the private key to use when creating the session</param>
        /// <param name="passphrase">The passphrase to use with the private key when creating the session</param>
        /// <returns>
        /// Will return the session info to start the connection.
        /// </returns>
        static ConnectionInfo StartConnection(string server, string user, string password, string key_location, Boolean passphrase)
        {
            ConnectionInfo ConnNfo = null;

            if (key_location != String.Empty)
            {
                string tempPassphrase = String.Empty;
                if (passphrase)
                {
                    tempPassphrase = Password();
                }
                ConnNfo = new ConnectionInfo(server, 22, user,
                new AuthenticationMethod[]{
                    // Key Based Authentication (using keys in OpenSSH Format)
                    new PrivateKeyAuthenticationMethod(user,new PrivateKeyFile[]{
                    new PrivateKeyFile(key_location, tempPassphrase)
                    }),
                }
            );
                return ConnNfo;
            }
            else if (password != String.Empty)
            {
                ConnNfo = new ConnectionInfo(server, 22, user,
                new AuthenticationMethod[]{
                    // Pasword based Authentication
                    new PasswordAuthenticationMethod(user, password)
                }
            );
                return ConnNfo;
            }

            return ConnNfo;
        }



        /// <summary>
        /// Writes a log file to the specified directory. This is a helper method.
        /// </summary>
        /// <param name="result"> The value of the resulting command(s).</param>
        /// <param name="host">The host the command was run on</param>
        /// <param name="logDir">The directory that the user want to write the log file to</param>
        static void WriteFile(string result, string host, string logDir)
        {
            StreamWriter sw;
            if (Directory.Exists(logDir))
            {
                sw = new StreamWriter(logDir + @"\" + host + ".log");
            }
            else
            {
                sw = new StreamWriter(@".\" + host + ".log");
            }
            sw.WriteLine(result);
            sw.Close();
            
        }


    } 
}
