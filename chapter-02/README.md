# Chapter 2 – Linux intro course

The second chapter of the course continues with the basics while starting a
foray into HTTP and TCP. At the end of this chapter should should be able to
create your own package, install it and verify that the contained service is up
and running.

## Agenda (replace for ToC)

 - Vim recap
 - Command flags – unix convention
 - More useful commands – touch, tail, less, mkdir, mv, cp, rm, rmdir
 - Folders and files – common locations
 - Linux file permissions
 - Adding/removing users from groups
 - stdin, stdout, stderr and file handles
 - Setting up consul as a cluster
 - Reading configuration from consul with Fakta
 - Creating a systemd file for your service
 - Creating a package of the above
 - Debugging a HTTP request with tcpdump
 - The TCP lifecycle – from SYN to FIN
 - Firewalling our service
 - Creating a CI pipeline that creates a package
 - Setting up kubernetes

The first chapter had a bit of a vim intro course, so we'll continue start by
doing a vim recap.

## Vim recap

Working with vim is primarily centered around remembering the *verbs*, *nouns*
and *modifiers*:

### Verbs

These are relevant for navigating around the *buffer* (i.e. the text you see in
your terminal).

 - `g` - go (`gg` to go to the top, `G` to the bottom)
 - `f` - find (`f;` to go to the semicolon)
 - `j` - down
 - `k` - up
 - `h` - left
 - `l` - right
 - `v` - visual
 - `d` - delete
 - `u` - undo
 - `y` - yank/copy

### Modifiers

The modifiers specify ranges – how large/small selection to create or what to
select.

 - `t` - to (`vt.` - visual to dot)
 - `a` - around
 - `i` - inside
 - `f` - find

### Nouns

 - `p` - paragraph
 - `w` - word (`ciw` is change inside word)
 - `b` - block

By combining the above you can become very efficient when navigating and
selecting text. You can try your new skills by adding a new binding to the Suave
example from the first chapter; add it to an IPv6 interface this time:
`IPAddress.Parse "::"`.

If you're interested in reading more about vim, [there][vim-1] [are][vim-2]
[numerous][vim-3] tutorials available online.

## Command flags

You've previously seen commands like [`chmod +x file.fsx`][chmod-1], [`ls
-lah`][explain-ls] and `tree -L 2`. There are a few conventions to the unix
command line that are beneficial to know about, as a lot of the unix command
line is built to be composable similar to PowerShell or how
Iteratee/seq/IEnumerable are composable.

Command flags start with a single dash `-` and then a character `h`, like so:
`ls -h` (list directory contents, print human bits/bytes), but they can also be
merged, like so `ls -lah`, which means to print all files, including symlinks
and dotfiles, in a long listing format; as well as human readable.

Alternatively, you can juxtapose the arguments with double dashes, like so: `ls
--all --human-readable -l`.

#### Man!

To see what command flags are available for a given command, you can use `man
<command name>`, like `man ls`. This displays a buffer that is paginated with
`less`, similar to how we paginated long `tree` outputs in the previous chapter.

You can also perform a brute-force search for a string in *all* man pages, by
passing the `-K <searchstring>` argument.

![man -k](../screens/man-k.png)

### More useful commands

You can create files, but how to manipulate directories, move files and copy
them? There are commands for most things:

 - `touch <filename>` – create a new file with the passed filename
 - `tail -f <filename>` – continuously print the tail of the file; useful for
   tailing logs
 - `less` – paginate output like we've done before
 - `mkdir -p ./path/to/dir` – create a new directory recursively, if it doesn't
   already exist
 - `mv <source file> <target file>` – move the source file to the target file
   location, or alternatively to a folder. If the target doesn't already exist
   then it's assumed to be a file name. If the target is a folder, then the file
   is copied to it and if the tagret is a file, the target file is overwritten.
 - `cp <source> <target>` – copy a file or directory
 - `rm <file>` – remove a file, or alternatively `rm -rf <dir>` to recursively
   remove a directory
 - `rmdir <dir>` specifically remove a directory if it's empty

Use your newfound skills to create the following tree:

    ubuntu@ubuntu-xenial:~$ tree
    .
    ├── a
    └── b
        ├── c
        │   └── d
        │       └── flag      (an empty file)
        ├── e
        └── f
            └── runner        (an empty file)

    6 directories, 2 files

How many commands did it take you to create this tree? I did it in three
commands. Can you do the same?

When you've successfully created the above, delete it all with a single command.

## Folders and files – common locations

Unix systems have a default folder layout. While the layouts differ slightly
between distros, there's a ["filesystem hierachy standard"][fhs] in place that
looks similar to this:

    .
    ├── bin – system binaries
    ├── sbin – system binaries
    ├── etc – configuration files
    ├── home – user home folders; not much used on servers
    ├── lib – shared libraries across the system
    ├── lib64 – similarly
    ├── tmp – temporary, world-readable, world-writable folder
    ├── opt – place your service's folder here: /opt/mysrv/{bin,public}
    ├── usr – most commands, man-docs and libraries from apt-get are installed here
    │   ├── bin – user (aka. the sys-admin's) commands
    │   ├── include – data used for linking executables
    │   ├── lib – libraries
    │   ├── local – similar for /opt; symlink from /opt/xyz, into here if needed
    │   ├── sbin – more commands
    │   ├── share – data and example configs
    │   └── src – used for source code for installed s/w
    └── var – if this server is stateful, put that state in here
        ├── lock -> /run/lock
        ├── log – logs from different services
        ├── lib – databases, storage
        └── run -> /run

When writing services for Linux, it's most common to have three or four
directories written. `/opt/mysrv/{bin,public}` for the binaries, `/etc/mysrv`
for the configuration and `/var/lib/mysrv` for any potential data. Finally, if
you want file logging, those go in `/var/log/mysrv`.

## Linux file and directory authorisation

Linux is built with *users* and *groups*, where a user can belong to one or many
groups but group cannot belong to other groups. They take the form of a bitmask
of nine bits:

    rwxr-xr-x haf developers
    ^^^ user bits
       ^^^ group bits
          ^^^ others bits

Furthermore, these permissions specify that `haf` is the owner of the file
(UID), that `developers` is the owning group (GID) and that anyone/others can
read and execute the file. A real-world example is `/bin/ls`.

    -rwxr-xr-x  1 root  wheel    38K Jul  9 05:03 /bin/ls

Which shows the owner to be `root`, the group to be `wheel` and allows every
user who has an account on the system to execute the file. That way, only the
system administrator is allowed to change the file.

The permissions are checked with an algorithm similar to:

    let check (op : Read|Write|Execute) =
      uid, gid = `whoami`
      if file.owner = uid then
        allowed op file.userPerms
      elif file.group = gid then
        allowed op file.groupPerms
      else
        allowed op file.otherPerms

– so it first checks to see if the user is the owner. Then checks the user's
group's permissions, then finally if none of those match, checks the permissions
for others. [More][file-perms-1] [reading][file-perms-2].

#### Changing permissions

You can change owning user, owning group and the respective flags with `chmod`
(change mode) and `chown` (change owner). Let's create a folder structure, user
and group for your own service `mysrv`.

    $ sudo su
    # mkdir -p /opt/mysrv/bin /opt/mysrv/public /etc/mysrv
    # addgroup --system mysrv
    Adding group `mysrv' (GID 117) ...
    Done.
    # adduser --system --no-create-home --home /opt/mysrv --shell /usr/sbin/nologin --group mysrv
    Adding system user `mysrv' (UID 112) ...
    Adding new user `mysrv' (UID 112) with group `mysrv' ...
    Not creating home directory `/opt/mysrv'.

You can now verify the properties of the user and the group with the following.

    # getent passwd mysrv
    mysrv:x:112:117::/opt/mysrv:/usr/sbin/nologin
    # id mysrv
    uid=112(mysrv) gid=117(mysrv) groups=117(mysrv)

`/usr/sbin/nologin` is used as a nice way to prohibit the service from spawning a
shell, when all it should do is to make a DB connection and serve some static
files.

#### File ACLs

You can use file ACLs if your file system gives a similar output as

    $ sudo tune2fs -l /dev/sda1 | grep 'Default mount'
    Default mount options:    user_xattr acl

The ACL flag is required for the file system to support it. Remember that
utilities like `mv` and `cp` may not preserve them. ACLs are normally not used;
even less so with the pattern VM/container-per-service which has a much stronger
separation of concerns. None the less, here's [a guide][acl-guide] on how to use
them, should you wish to separate responsibilities between users on your
servers.

## stdin (0), stdout (1) and stderr (2)

Standard-in, standard-out and standard-err are the methods by which your
programs can write to the terminal when running interactively, or write to the
log that comes *systemd*.

When processes communicate over pipes (`|`) they act on stdin and stdout. `a |
b` pipes a's stdout into b's stdin. For services, stderr is normally used for
logging errors and besides that all logs should be shipped to a central log
router or collector.

You can redirect these streams for commands you run. To quelch the output of
`ls`, you could run `ls >/dev/null` which snuffs the stdout output. Similarly
use `ls /non-existent 2>/dev/null` to redirect stderr to `/dev/null`.

These are called *file descriptors* in that they are the file descriptors always
opened for the process. Any other files that you [`open(2)`][open-2] are
numbered upwards. They are opaque handles which let user programs interface the
kernel's [io devices][ioctl].

## Setting of service discovery

Now that we know a bit more about how to work with files, where to place
configuration and databases, we'll try our hand at setting up a Consul cluster
manually. By doing so, we'll learn more about how to operate non-trivial
networked software and how to debug its setup. Consul, together with
[Fakta][fakta-gh] gives us the foundations for service discovery and external
(side-cart) health checks for our own networked services. It also lets us play
with DNS (over UDP) and TCP and work more with socket bindings.

The steps we're going to take are as follows:

 1. Spawning four nodes
   - 3 for our consul/mysrv cluster
   - 1 nginx load balancer
 1. Set up consul on a single node
 1. Validate setup on a single node
 1. Set up consul server on the three nodes
 1. Validate setup on three nodes
 1. Set up consul client on the fourth node; the load balancer
 1. Validate setup on the load balancer
 1. Set up mysrv on the three nodes with systemd
 1. Set up load balancing to the three nodes
 1. Validate load balancing with health checks is working

### Spawning four nodes

In this repository there's a directory called *chapter-02*. Destroy your current
VM and start vagrant from there.

    ✗ cd chapter-02
    ✗ vagrant up
    Bringing machine 'n1' up with 'virtualbox' provider...
    Bringing machine 'n2' up with 'virtualbox' provider...
    Bringing machine 'n3' up with 'virtualbox' provider...
    Bringing machine 'lb' up with 'virtualbox' provider...
    ==> n1: Importing base box 'ubuntu/xenial64'...
    ==> n1: Matching MAC address for NAT networking...
    ...

    ✗ vagrant status
    Current machine states:

    n1                        running (virtualbox)
    n2                        running (virtualbox)
    n3                        running (virtualbox)
    lb                        running (virtualbox)

### Run consul on a single node

    ✗ vagrant ssh n1
    $ consul agent -dev
    ==> Starting Consul agent...
    ==> Starting Consul agent RPC...
    ==> Consul agent running!
               Version: 'v0.7.1'
             Node name: 'n1'
            Datacenter: 'dc1'
                Server: true (bootstrap: false)
           Client Addr: 127.0.0.1 (HTTP: 8500, HTTPS: -1, DNS: 8600, RPC: 8400)
          Cluster Addr: 127.0.0.1 (LAN: 8301, WAN: 8302)
        Gossip encrypt: false, RPC-TLS: false, TLS-Incoming: false
                 Atlas: <disabled>

    ==> Log data will now stream in as it occurs:
    ...

Seems to be working! Consul magically exists, because of a so called
*provisioning* script in the Vagrantfile that downloads and copies the `consul`
binary to /usr/bin/.

Let's SSH to validate that consul is up and running.

    ✗ vagrant ssh n1
    $ curl http://127.0.0.1:8500/ -i
    HTTP/1.1 301 Moved Permanently
    Location: /ui/
    Date: Mon, 05 Dec 2016 16:53:30 GMT
    Content-Length: 39
    Content-Type: text/html; charset=utf-8

    <a href="/ui/">Moved Permanently</a>.

Looking good. Now, let's cluster it!

### Clustering consul

Consul nodes can work both as server nodes and client nodes. The server nodes
should be an uneven number of nodes larger than three, with mutual connectivity.
The rest of your computers can run consul in client mode: aim for a ratio of,
say, 1:10 of server-to-client nodes.

We'll start by pinging `n2` and `n3` from `n1`, to verify connectivity.

    ubuntu@n1:~$ ping 172.20.20.11
    PING 172.20.20.11 (172.20.20.11) 56(84) bytes of data.
    64 bytes from 172.20.20.11: icmp_seq=1 ttl=64 time=0.542 ms
    64 bytes from 172.20.20.11: icmp_seq=2 ttl=64 time=0.329 ms
    ^C
    --- 172.20.20.11 ping statistics ---
    2 packets transmitted, 2 received, 0% packet loss, time 999ms
    rtt min/avg/max/mdev = 0.329/0.435/0.542/0.108 ms
    ubuntu@n1:~$ ping 172.20.20.12
    PING 172.20.20.12 (172.20.20.12) 56(84) bytes of data.
    64 bytes from 172.20.20.12: icmp_seq=1 ttl=64 time=0.575 ms
    ^C
    --- 172.20.20.12 ping statistics ---
    1 packets transmitted, 1 received, 0% packet loss, time 0ms
    rtt min/avg/max/mdev = 0.575/0.575/0.575/0.000 ms

That looks good. Let's shut down the dev consul instance and re-launch it like
this – we'll have a terminal up for every node in the cluster. Note that consul
deviates from the standards for command-line flags that we just learnt and only
has a single dash.

We'll also use `runuser` to execute as the consul user. Run the following **as
root**:

    mkdir -p /var/lib/consul /opt/consul /etc/consul.d
    addgroup --system consul
    adduser --system --no-create-home --home /opt/consul --shell /sbin/nologin --group consul
    chown -R consul:consul /var/lib/consul
    chown -R consul:consul /opt/consul
    runuser -l consul -c 'consul agent -server -bootstrap-expect=3 -data-dir=/var/lib/consul -bind 172.20.20.10 -config-dir /etc/consul.d'

The last line will output:

    This account is currently not available.

When testing we need to bind a shell to the execution of the service; so for
now, let's change the user's shell to point to bash, and retry.

    # usermod -s /bin/bash consul
    # runuser -l consul -c 'consul agent -server -bootstrap-expect=3 -data-dir=/var/lib/consul -bind 172.20.20.10 -config-dir /etc/consul.d'
    ==> WARNING: Expect Mode enabled, expecting 3 servers
    ==> Starting Consul agent...
    ==> Starting Consul agent RPC...
    ==> Consul agent running!
               Version: 'v0.7.1'
    ...

Log in from `n2` and `n3` and join them to the cluster by running the above
without `-boostrap-expect` but instead with `-join`:

    # runuser -l consul -c 'consul agent -server -join 172.20.20.10 -data-dir=/var/lib/consul -bind 172.20.20.11 -config-dir /etc/consul.d'

And on n3:

   # runuser -l consul -c 'consul agent -server -join 172.20.20.10 -data-dir=/var/lib/consul -bind 172.20.20.12 -config-dir /etc/consul.d'

Now you should have three terminals looking like this.

![Consul cluster](../screens/consul-cluster.png)

You can start a fourth terminal and run

    $ consul members
    Node  Address            Status  Type    Build  Protocol  DC
    n1    172.20.20.10:8301  alive   server  0.7.1  2         dc1
    n2    172.20.20.11:8301  alive   server  0.7.1  2         dc1
    n3    172.20.20.12:8301  alive   server  0.7.1  2         dc1

To ensure that all members are alive and kicking. Let's also test node discovery
from `n1`:

    $ dig +short @127.0.0.1 -p 8600 n2.node.consul
    172.20.20.11

You can also try to terminate one of the server – the cluster should remain
running and should still respond to queries. Remember to start the server again
once you're done.

### Set up consul client on the load balancer

First create the user, group and folders, like for the other consul **server**s.
However, this time, the consul agent should be run in **client** mode.

    # runuser -l consul -c 'consul agent -join 172.20.20.10 -data-dir=/var/lib/consul -bind 172.20.20.20 -config-dir /etc/consul.d'

This time around, on n1:

    $ consul members
    Node  Address            Status  Type    Build  Protocol  DC
    lb    172.20.20.20:8301  alive   client  0.7.1  2         dc1
    n1    172.20.20.10:8301  alive   server  0.7.1  2         dc1
    n2    172.20.20.11:8301  alive   server  0.7.1  2         dc1
    n3    172.20.20.12:8301  alive   server  0.7.1  2         dc1

![Consul servers and clients](../screens/consul-cluster-agent.png)

### Set up your own service on each node

Let's leave the consul cluster running (you can do a `vagrant suspend` and then
`vagrant up` to conserve battery power if you leave it overnight).

In your normal terminal, let's download
[Forge](https://github.com/fsharp-editing/Forge/releases) a command-line
interface (CLI) for creating and managing F# projects.

We'll create a small Suave service as a console app.

    ✗ pwd
    /Users/h/dev/linux-intro/chapter-02
    ✗ curl -o Forge.zip -L https://github.com/fsharp-editing/Forge/releases/download/1.2.1/forge.zip
    ✗ unzip Forge.zip
    ✗ chmod +x forge.sh
    ✗ ./forge.sh new project MySrv

    Forge should be run from solution/repository root. Please ensure you don't run it from folder containing other solutions

    Do You want to continue? [Y/n]

    Getting templates...
    Creating /Users/h/dev/lynx/linux-intro/chapter-02/templates
    git clone -b templates --single-branch https://github.com/fsharp-editing/Forge.git templates

    Enter project name:
    > MySrv
    Enter project directory (relative to working directory):
    > .
    Choose a template:
     - suave
     - suaveazurebootstrapper
     - temp

    > suave
    Generating project...
    ...
    ✗ ./build.sh

You should now have a small app MySrv in `./build`. Now, let's write some code
that lets MySrv register itself in Consul on start. This will enable us to use
Consul to steer the load balancer.

### Auto-registering in Consul

Introducing *Fakta*.

 - Poll-based (HTTP)
 - Push-based ("My status is..." to Consulvia Fakta)
 - Socket-based (TCP)

### Querying Consul for a specific service

    curl -X GET http://localhost:8600/v1/kv/services?recurse=true

### Setting up the load balancer's config with consul-template

    upstream api {
      {{ ep in eps do }}
        server {{ ep.ipv6 }};
      {{ end }}
    }

### Make each server respond with its hostname

    [lang=fsharp]
    open System
    open System.Net
    open Hopac
    open Hopac.Operators
    open Suave
    open Suave.Successful
    open Suave.ResponseErrors

    let registerChecks () =
      ()

    let app =
      choose [
        GET >=> path "/health/hostname" >=> OK (Dns.GetHostName())
        GET >=> OK "Hello world!"
        ResponseErrors.NOT_FOUND "Resource not found"
      ]

    [<EntryPoint>]
    let main argv =
      registerChecks ()
      startWebServer config app

https://github.com/kelseyhightower/consul-on-kubernetes

 [vim-1]: https://yanpritzker.com/learn-to-speak-vim-verbs-nouns-and-modifiers-d7bfed1f6b2d#.ss7enj0qq
 [vim-2]: http://www.openvim.com/
 [vim-3]: https://danielmiessler.com/study/vim/
 [chmod-1]: http://explainshell.com/explain?cmd=chmod+%2Bx+file.ext
 [explain-ls]: http://explainshell.com/explain?cmd=ls+-lah+file.ext
 [fhs]: http://www.pathname.com/fhs/pub/fhs-2.3.html
 [file-perms-1]: https://unix.stackexchange.com/questions/134332/precedence-of-user-and-group-owner-in-file-permissions?rq=1
 [file-perms-2]: https://wiki.archlinux.org/index.php/File_permissions_and_attributes
 [acl-guide]: https://wiki.archlinux.org/index.php/Access_Control_Lists
 [open-2]: http://man7.org/linux/man-pages/man2/open.2.html
 [ioctl]: http://man7.org/linux/man-pages/man2/ioctl.2.html
 [fakta-gh]: https://github.com/logibit/Fakta

