# Chapter 4 – Leap-frogging infrastructure development

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
 - Service discovery (where is 'web', what nodes are 'web'?)
 - Horisontal scaling – bring up more nodes when needed
 - Scaling down again – when load stops peaking
 - Rolling deploys – a new version of your service should be 'eased' into load
 - Firewalling
 - Users/groups between APIs – we've only touched upon users/groups inside a
   single operating system
 - Running commands on *all* nodes in your production
 - Managing networking – need a switch? A bridge between two VLANs?
 - Managing external storage – need to add disk in a VM?
 - Failover & Health checks – when things go down and reacting on it

## Agenda

In this chapter you'll get to understand [Kubernetes][kube] and 1) how to deploy
services for a subset of the above bullet points, and 2) how to let your own
service make use of as much as possible of the above.

<blockquote>Kubernetes is an open-source system for automating deployment,
scaling, and management of containerized applications.</blockquote>

We'll deploy these services with a few commands:

 1. Hashicorp Consul & Consul UI
 1. Apache ZooKeeper
 1. Apache Kafka
 1. A F# service with Suave and Logary writing to Kafka
 1. A F# service with Suave and Logary reading from Kafka
 1. PostgreSQL
 1. EventStore
 1. Elastic's ElasticSearch & Kibana
 1. InfluxData's InfluxDB & Grafana
 1. Apache Flink

Once the initial setup is done, we'll also deploy these operators:

 1. A backup operator for ZooKeeper
 1. A backup operator for Kafka
 1. A backup operator for ElasticSearch
 1. A backup operator for InfluxDB
 1. A backup operator for PostgreSQL

## A primer on Kubernetes

Kubernetes is a cloud-orchestration platform run by the Linux Foundation, the
same organisation officially in charge of shepherding Linux, the operating
system. It solves a lot of the thorny issues of configuration management and
operations that would otherwise have to be solved by each company by itself.

### Alternative to...

If you'd not discovered Kubernetes through e.g. this guide, your path to what it
offers would probably look something like:

 1. Manually managing your server like in chapter 1-3.
 2. Discovering configuration management and,
   1. using it to *install* everything as well as,
   1. using it to *configure* one-shot configurations.
 3. Noticing you need something that *coordinates* deployments, manages service
    metadata, versions, tenancy. Create a CMDB
 4. Write custom micro-services for each use case: { rolling deployments,
    backups, service metadata-discovery, ... }
 5. Look around for something more "off the shelf" and then discovery
    Kubernetes.

### Concepts

When deploying services, you'll recall from previous chapter that a service
binds to an IP and a Port; in conjunction called an Endpoint. Ports are only
possible to bind for a single service at a time, so they are a scarce resource.

Kubernetes sets up networking so that each 'node' is run on top of an overlay
network, with a unique IP. Specifically a docker-container in the default case.
This node is called a 'pod' in Kubernetes vocabulary. Via docker(-like)
virtualisation, many 'pods' can run in a single VM/machine/node, all coordinates
via the 'kubectl' daemon.

![Kubernetes architecture](./imgs/kubernetes-architecture.png)

These pods are then linked with the overlay network software called OpenVSwitch.

![Kubernetes networking with OVS](./imgs/ovs-networking.png)

This sort of setup removes a lot of the hindrances you'd not discover until
your development organisation had grown to a certain complexity requirement.
For example, if you need to bridge on-site local GPU resources with cloud services, OVS
would give you a way out.

### Why did I just learn linux then?

You need to understand that everything in this eco-system is built around Linux,
and you need Linux skills both to administrate it and to run services on top of
it; the containers you'll run your services in, are actually small linux
distributions but running on top of the host's kernel.

Software running in containers also needs to be secured like described in the
previous chapters – a container does perform a bit of isolation, but it's not at
the same level of a VM on top of a Hypervisor. For example, by default a
Dockerfile runs your software as `root`, but that still gives you access to the
devices and some modules of the kernel which aren't namespaced. So create an
app-user.

## Consul on Kubernetes

 - Minikube

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


 [kube]: http://kubernetes.io/
