module Api.Program

open Suave
open Suave.Operators
open Suave.Successful
open Suave.RequestErrors
open Suave.Filters
open Suave.Writers
open Kafunk

let suaveConfig =
  { defaultConfig with
      bindings = [ HttpBinding.createSimple Protocol.HTTP "::" 8080 ] }

type State =
  { kafka : KafkaConn
    producer : Producer }

  interface System.IDisposable with
    member x.Dispose () =
      x.kafka.Close()

let utf8 =
  System.Text.Encoding.UTF8.GetBytes : string -> byte[]

let publish state : string -> WebPart =
  fun (message : string) ->
    fun httpCtx ->
      async {
        let! prodRes =
          Producer.produce state.producer [| ProducerMessage.ofBytes (utf8 message) |]
        return! CREATED "Alrighty then!" httpCtx
      }

let web state =
  choose [
    POST >=> pathScan "/api/publish/%s" (publish state)
    GET >=> path "/health" >=> OK "Alive!"
    NOT_FOUND "The requested resource was not found"
  ]
  >=> setMimeType "text/plain; charset=utf-8"

let configure argv =
  let pcfg =
    ProducerConfig.create (
      "web-greetings",
      Partitioner.roundRobin,
      requiredAcks = RequiredAcks.Local)
  let kafka = Kafka.connHost "localhost"
  let producer = Producer.create kafka pcfg
  { kafka = kafka
    producer = producer }

[<EntryPoint>]
let main argv =
  use state = configure argv
  startWebServer suaveConfig (web state)
  0