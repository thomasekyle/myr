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
            Int32 threads = Convert.ToInt32(Math.Ceiling((Environment.ProcessorCount * 0.75) * 1.0));
            string thread_option = String.Empty;
            List<string> myrTasks = new List<string>();
            Boolean becomeRoot = false;
            Boolean passphraseFlag = false;
            Boolean passwordFlag = false;
            Boolean ttyFlag = false;
            Boolean help = false;
            Boolean pbflag = false;


            var options = new OptionSet {
                { "s|server=", "IP/Hostname of the server you wish to connect to.", s => server = s },
                { "S|Servers=", "Target machines to run a command or command file on.", S => myrTarget = S },
                { "u|user=", "The user you wish to connect as.", l => user = l },
                { "pw|password", "The password you wish to use.", p => passwordFlag = p != null },
                { "t|tty", "Use tty session like PuTTY and other ssh clients.", t => ttyFlag = t != null },
                { "B|become-root", "Become root to run elevated commands.", B => becomeRoot = B != null },
                { "pb|pbrun", "Use pbrun su - to run elevated commands.", pb => pbflag = pb != null },
                { "i|identity-file=", "The location of the identity file you wish to use (SSH Key)", i => keyLocation = i },
                { "P|passphrase=", "The passphrase associated with the ssh key you wish to use.", P => passphraseFlag = P != null},
                { "c|command=", "The command you you want to run.", c => myrCommand = c },
                { "C|commandfile=", "The file you you want to run.", C => myrFile = C },
                { "U|scp=","The file(s) you wish to scp. Requires the directory flag.", U => myrSCP = U},
                { "d|directory=","The directory for the scp file upload", d => dir = d},
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
                password = myrPassword();
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

            //If the user has specified thread amount set to that value.
            if (thread_option != String.Empty) threads = Convert.ToInt32(thread_option);
            ParallelOptions pOptions = new ParallelOptions
            {
                MaxDegreeOfParallelism = threads
            };
            Parallel.ForEach(myrTasks, pOptions, t =>
            {
                taskCount++;
                ConnectionInfo myrSession = startConnection(t, user, password, keyLocation, passphraseFlag);
                if (myrCommand != String.Empty)
                {
                    myrCommandS(myrSession, myrCommand, t, taskCount, logDir, ttyFlag, password, pbflag);
                }
                if (myrFile != String.Empty)
                {
                    myrFileS(myrSession, myrFile, t, taskCount, logDir);
                }
                if (myrSCP != String.Empty && dir != String.Empty)
                {
                    MyrScp(myrSession, myrSCP, t, taskCount, dir);
                }
            });

            //foreach (string t in myrTasks)
            //{

            // }

        }


        //Method for using an sftp client to moves files up to a server.
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
                    //Console.WriteLine("Sftp Client is connected: " + sftp.IsConnected);
                    using (var uplfileStream = System.IO.File.OpenRead(uploadfn))
                    {
                        sftp.UploadFile(uplfileStream, basefile, true);
                        Console.WriteLine(taskID + "[Success] => " + DateTime.Now.ToString("h:mm:ss tt") + " on host " + host);
                    }
                    sftp.Disconnect();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("[Failure] => " + e.Message + " " + DateTime.Now.ToString("h:mm:ss tt") + " on host " + host);
            }


        }

        //Method for running commands on a server.
        static int myrCommandS(ConnectionInfo session, string myr_command, string host, int taskID, string logDir, Boolean ttyFlag, string password, Boolean pbflag)
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
                            
                            IDictionary<Renci.SshNet.Common.TerminalModes, uint> termkvp = new Dictionary<Renci.SshNet.Common.TerminalModes, uint>();
                            termkvp.Add(Renci.SshNet.Common.TerminalModes.ECHO, 53);

                            ShellStream shellStream = client.CreateShellStream("vt320", 0, 0, 0, 0, 1024, termkvp);
                            String output = shellStream.Expect(new Regex(@"[$>]"));
                            Console.WriteLine(output);
                            if (pbflag == true)
                            {
                                shellStream.WriteLine("pbrun su -");
                                System.Threading.Thread.Sleep(10000);
                                output = shellStream.Expect(new Regex(@"([$#>:])"));
                                if (!(output.Contains("Error")))
                                {
                                    shellStream.WriteLine(password);
                                } else
                                {
                                    throw new System.InvalidOperationException("Could not elevate with pbrun. Error occured. Please retry.");
                                }
                                Console.WriteLine(shellStream.Read());
                                System.Threading.Thread.Sleep(7000);
                                output = shellStream.Expect(new Regex(@"[$#>]"));
                                Console.WriteLine(output);
                                shellStream.WriteLine(myr_command);
                                output = shellStream.Expect(new Regex(@"[$#>]"));
                                Console.WriteLine(output);
                                shellStream.WriteLine("echo myrfinish");
                            } else
                            {
                                shellStream.WriteLine(myr_command);
                                output = shellStream.Expect(new Regex(@"[$#>]"));
                                Console.WriteLine(output);
                                shellStream.WriteLine("echo myrfinish");
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

        //Method for running multiple commands on a server using a myr file.
        static int myrFileS(ConnectionInfo session, string myr_file, string host, int taskID, string logDir)
        {

            StreamReader file = new StreamReader(myr_file);
            string line = String.Empty;
            string result = String.Empty;

            try
            {
                using (var client = new SshClient(session))
                {
                    client.Connect();
                    //!sr.EndOfStream
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

        //Display the help menu.
        static void myrHelp()
        {
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


        //Return a list each line in a tet file
        static List<string> ParseText(string target)
        {
            List<string> output = new List<string>();
            StreamReader file_target = new StreamReader(target);
            string server = String.Empty;
            //!sr.EndOfStream
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

     

        //Creats and returns connection object base on parameters provided.
        static ConnectionInfo startConnection(string server, string user, string password, string key_location, Boolean passphrase)
        {
            ConnectionInfo ConnNfo = null;

            if (key_location != String.Empty)
            {
                string tempPassphrase = String.Empty;
                if (passphrase)
                {
                    tempPassphrase = myrPassword();
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
        //Experimental async jobs (Unfished)
        //      private async void createJob(string server, string user, string password)
        //      {
        //          //create the job
        //          //wait for the job to complete
        //         string result = await RunJob(server, user, password);        
        //      }

        //private async void createJob(string server, string user, string key, string passphrase)
        //{
        //	//create the job
        //	//wait for the job to complete
        //	string result = await RunJob(server, user, passphrase);
        //}

        //      private async Task<string> RunJob(string server, string user, string password) 
        //      {
        //          //delay the task
        //          //other jobs can be started while this job is running.
        //          //return when it is finished
        //          MyrJob j = new MyrJob(user, server, password);
        //          await Task.Delay(1000);
        //          return "Finshed.";
        //      } 

        //private async Task<string> RunJob(string server, string user, string key, string passphrase)
        //{	
        //	//delay the task
        //	//other jobs can be started while this job is running.
        //	//return when it is finished
        //	MyrJob j = new MyrJob(user, server, passphrase);
        //	await Task.Delay(1000);
        //	return "Finshed.";
        //} 



    } 
}
