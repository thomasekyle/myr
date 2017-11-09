## myr


#### What myr is?
myr is a small automation tool for running commands on multiple linux/unix boxes. It was built using C#, along with the SSH.NET. Please
see the CHANGELOG for updates.

#### Compiling
myr compiles with the frameworks below. Others are untested. Compile at your own risk.
- Mono/.NET 4.5.1

#### Usage
myr [options] [target] [commands]
- Options
  - -p, --password Prompt for a password
  - -u, --user=VALUE Specify a user name. If no user name is specified, it will used the current one.
  - -P, --passphrase Prompt for a SSH key passphrase.
  - -i, --identity-file=DIR/FILE Specify a SSH key for authentication
  - -d, --directory=DIR/FILE Specify a directory for scp upload. If not directory is specified /tmp will be used.
  - -l, --log=DIR Log output to text file. You may specify the directory after this arguement. If you don't the current directory will be used.
  - -T, --threads=VALUE Specify the amount of concurrency (threads) you wish to use in running commands.
- Target
  - -s, --server=VALUE Specify a single server
  - -t, --target=DIR/FILE Specify a group of servers from a file. Servers should be on separate line. Use # to comment out servers.
- Commands
  - -c, --command=VALUE Run a single command.
  - -C, --commandfile=DIR/FILE Run a list of commands from a file. Commands should be on separate line. Use # to comment out commands.
  - -S, --scp This will upload the requested file to the specified server(s).

##### Example
 *myr -u thomasekyle -p -s example.com -c uptime*


 *myr -u thomasekyle -i ~/Keys/privatekey -P -t serverlist -C commandfile* -l /path/to/log/dir


 *myr -u uploaduser -p -t serverlist --scp myfile*



 *note if you use the -p or -P flags you will be prompted to enter the Passphrase/Password*


  You will probably want to add myr to your path, so you can access it anywhere from the command line.


  #### Binaries
  - Windows - [Download](https://github.com/thomasekyle/myr/blob/release/myr_Win.zip?raw=true)
  - Linux - [Download](https://github.com/thomasekyle/myr/blob/release/myr_Linux.tar?raw=true)
  - macOS - [Download](https://github.com/thomasekyle/myr/blob/release/myr_macOS.tar?raw=true)

  ### Note
  Renci.SshNet.dll is required for all Windows versions, it will need to be in the same directory as the executable.

  #### Contact
  If you find any problems please open an issue. Otherwise you can contact me at thomasekyle@gmail.com
