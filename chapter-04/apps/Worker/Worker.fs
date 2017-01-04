module Worker.Program

open System
open System.Threading
open RdKafka

let utf8ToString =
  System.Text.Encoding.UTF8.GetString : byte[] -> string

[<EntryPoint>]
let main argv =
  use mre = new ManualResetEventSlim(false)
  use sub = Console.CancelKeyPress.Subscribe(fun _ ->
    printfn "Shutting down"
    mre.Set())
  let config = Config(GroupId = "example-csharp-consumer")
  use consumer = new EventConsumer(config, "127.0.0.1:9092")
  use msgs = consumer.OnMessage.Subscribe (fun msg ->
    let text = utf8ToString msg.Payload
    printfn "Topic: %s Partition: %i Offset: %i – %s"
            msg.Topic
            msg.Partition
            msg.Offset
            text)
  consumer.Subscribe(ResizeArray<_> ["web-greetings"])
  consumer.Start()
  printfn "Running!"
  mre.Wait() |> ignore
  0