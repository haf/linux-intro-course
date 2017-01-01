## Chapter 4 – Leap-frogging infrastructure development

In the previous chapters, we've gone from learning what a terminal is, to being
able to set up and debug a semi-production worthy consul cluster, packaged with
`.deb`-files.

Every service you create would have to be similarly treated – pre-install,
post-install, pre-uninstall scripts, start-up scripts, `/etc/default/consul`
environment variables, firewalling, build-scripts, packaging of the above and
then modifying them all depending on your deployment environment.

In this chapter I'll show you how you can go the next step with micro-services
in F# on Linux. This is because on top of the above fiddling per-service, we
also need:

 - Logging
   * From kernel/firewall for intrusion detection and health checks
   * From auditd for commands being run
   * From systemd for start-and-stop of services, exit codes, etc
   * From your apps
   * From your load-balancer
   * From any infrastructure components you have
 - Metrics
   * From all of the above
 - Load-balancing
 - 


### Set up your own service on each node

Let's leave the consul cluster running (you can do a `vagrant suspend` and then
`vagrant up` to conserve battery power if you leave it overnight).

In your normal terminal, let's download
[Forge](https://github.com/fsharp-editing/Forge/releases) a command-line
interface (CLI) for creating and managing F# projects.

We'll create a small Suave service as a console app.

    ✗ pwd
    /Users/h/dev/linux-intro/chapter-03
    ✗ curl -o Forge.zip -L https://github.com/fsharp-editing/Forge/releases/download/1.2.1/forge.zip
    ✗ unzip Forge.zip
    ✗ chmod +x forge.sh
    ✗ ./forge.sh new project MySrv

    Forge should be run from solution/repository root. Please ensure you don't run it from folder containing other solutions

    Do You want to continue? [Y/n]

    Getting templates...
    Creating /Users/h/dev/lynx/linux-intro/chapter-03/templates
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


