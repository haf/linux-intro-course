# Chapter 5 – Kafka on Linux

In this chapter we'll dive a little deeper into Kafka, a distributed message
broker.

## Setup
### Setting up a 3-node ZK cluster
### Setting up a 3-node Kafka cluster

## Applied – Consumer and Producer
### Run your producer against the cluster
### Run your consumer against the cluster
### Discussion

## Theoretical – Kafka binaries

Kafka comes with [system tools][kafka-systools] that support operating the
broker.

 - [kafka-acls][kafka-acls] – "Principal P is [Allowed/Denied] Operation O From
   Host H On Resource R" – [KIP-11][kafka-acls-1]
 - [kafka-replay-log-producer][kafka-rlp] – Consume from one topic and replay
   those messages and produce to another topic
 - kafka-configs – 
 - kafka-replica-verification
 - kafka-console-consumer
 - kafka-run-class
 - kafka-console-producer
 - kafka-server-start
 - kafka-consumer-groups
 - kafka-server-stop
 - kafka-consumer-offset-checker
 - kafka-simple-consumer-shell
 - kafka-consumer-perf-test
 - kafka-streams-application-reset
 - kafka-mirror-maker
 - kafka-topics
 - kafka-preferred-replica-election
 - kafka-verifiable-consumer
 - kafka-producer-perf-test
 - kafka-verifiable-producer
 - kafka-reassign-partitions

## Applied – Resize partitions

Resize the number of partitions in the producer-consumer in F# example. Ensure
the consumer can read from them all (and that the producer produces to them all).

 - Can we make the number of partitions even larger?
 - What downsides are there with resizing an existing topic?

### Make a consumer group

Here are WebScale™ Technologies Inc, we want horisontal scale-out. Make it so
that you have two consumers that share the incoming messages.

 - How many consumers can we have competing?
 - Can we have competing consumers with only a single partition?
 - What kafka-tool did you use for this?

## Applied - Failover

Make your producer produce 1000 unique messages per second. Ensure your consumer
group successfully reads these messages.

Kill one Kafka node. Your message rate should not drop.

Start the node again. What happens?

## Theoretical – Aphyr's Jepsen

Read [Jepsen: Kafka][jepsen-kafka] and let's discuss.

 - How many ISRs are needed to be 'safe'?
 - What are the wake-me-up-at-night boundaries we should alert on?
 - Under what circumstances would an unclean leader election be OK for your
   system?

## Applied – HealthCheck on message rate

 - Make a Logary metric that gives the rate of sending
 - Extend your metric to log an alert when it drops below 800 messages per
   second.

## Theoretical – Idempotent producers

What would it take to have [idempotent producers][kafka-idem-prod]?

Alternatives:

 - Save unique message Id, store all Ids in B-Tree
 - Number producers, keep high-water-mark under which all messages received are
   dropped, number messages. Store producer-id and high-water-mark either per
   topic-partition, per topic or per producer.
 - Number producers, assign topic-partition to a producer to produce into
   similar to consumer groups' do with topic-partitions.

Discuss pros- and cons- of each.

### Applied – mirror-making

Let's [make a mirror][kafka-mirror] of your Kafka cluster to a *single* machine
separate cluster.

 - Is a mirror Kafka setup identical in all ways that matter?
 - `dc1` is experiencing a netsplit from your country. You can still SSH via AWS
   in London. `dc2` has a mirror Kafka. What steps do you take to failover? Can
   you do it without disrupting reads? Without disrupting writes?

 [kafka-systools]: https://cwiki.apache.org/confluence/display/KAFKA/System+Tools
 [kafka-acls]: https://cwiki.apache.org/confluence/display/KAFKA/Kafka+Authorization+Command+Line+Interface
 [kafka-acls-1]: https://cwiki.apache.org/confluence/display/KAFKA/KIP-11+-+Authorization+Interface
 [kafka-rlp]: https://cwiki.apache.org/confluence/display/KAFKA/System+Tools
 [jepsen-kafka]: https://aphyr.com/posts/293-jepsen-kafka
 [kafka-idem-prod]: https://cwiki.apache.org/confluence/display/KAFKA/Idempotent+Producer
 [kafka-mirror]: https://kafka.apache.org/documentation/#basic_ops_mirror_maker
