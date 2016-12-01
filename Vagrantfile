# -*- mode: ruby -*-
# vi: set ft=ruby :

# Create your own Vagrantfile with `vagrant init`.

Vagrant.configure("2") do |config|
  # https://atlas.hashicorp.com/search
  config.vm.box = "ubuntu/xenial64"
  config.vm.network "public_network"

  # Used for NAT only
  # config.vm.network "forwarded_port", guest: 8080, host: 8080

  # config.vm.synced_folder "./src", "/src"

  # config.vm.provision "shell", inline: <<-SHELL
  #   apt-get update
  #   apt-get install -y apache2
  # SHELL
end
