## myr


#### What myr is?
myr is a small automation tool for running commands on multiple linux/unix boxes. It was built using C#, along with the SSH.NET.

#### Compiling
myr compiles with the frameworks below. Others are untested. Compile at your own risk.
- Mono/.NET 4.5.1

#### Usage
myr [options] [target] [commands]
- Options
  - -p, --password Prompt for a password
  - -u, user=VALUE Specify a user name. If no user name is specified, it will used the current one.
  - -P, --passphrase Prompt for a SSH key passphrase.
  - -i, --identity-file=DIR/FILE Specify a SSH key for authentication
  - -d, --directory=DIR/FILE Specify a directory for scp upload. If not directory is specified /tmp will be used.
- Target
  - -s, --server=VALUE Specify a single server
  - -t, --target=DIR/FILE Specify a group of servers from a file. Servers should be on separate line. Use # to comment out servers.
- Commands
  - -c, --command=VALUE Run a single command.
  - -m, --myrfile=DIR/FILE Run a list of commands from a file. Commands should be on separate line. Use # to comment out commands.
  - -S, --scp This will upload the requested file to the specified server(s).

##### Example
 *myr -p -u thomasekyle -s example.com -c uptime*


  You will probably want to add myr to your path, so you can access it anywhere from the command line.


  #### Binaries
  Coming Soon.

  #### Contact
  If you find any problems please open an issue. Otherwise you can contact me at thomasekyle@gmail.com