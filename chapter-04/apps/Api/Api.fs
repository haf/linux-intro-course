module Api.Program

open Suave
// operators gives >=>
open Suave.Operators
// gives OK
open Suave.Successful
// gives NOT_FOUND
open Suave.RequestErrors
// gives pathScan and path
open Suave.Filters
// gives the ability to set a mime type
open Suave.Writers
// the kafka library
open Kafunk

// Same as before.
let suaveConfig =
  { defaultConfig with
      bindings = [ HttpBinding.createSimple Protocol.HTTP "::" 8080 ] }

// I normally encapsule my runtime state in a record like this
type State =
  { kafka : KafkaConn
    producer : Producer }

  interface System.IDisposable with
    member x.Dispose () =
      x.kafka.Close()

// This is a helper function that aliases the longer system.text fn
let utf8 =
  System.Text.Encoding.UTF8.GetBytes : string -> byte[]

// The main work is done here. Note how it takes a `state` variable and
// returns a function from string to WebPart. This first `state` parameter
// is given in `main` below. The second parameter `message` will be given
// anew every request as the message can differ.
let publish state : string -> WebPart =
  fun (message : string) ->
    // this is how to create an Async Suave web part
    fun httpCtx ->
      async {
        // Suave/F# async fits nicely with Kafunk's API.
        let! prodRes =
          Producer.produce state.producer [| ProducerMessage.ofBytes (utf8 message) |]
        // Finally, we return this string to the requestor.
        return! CREATED "Alrighty then!" httpCtx
      }

// This function is the api composition. It should be clear and make it easy
// to find any particular path supported by the API.
let web state =
  // `choose` will go through its `WebPart list` from top to bottom to find
  // a WebPart that matches
  choose [
    POST >=> pathScan "/api/publish/%s" (publish state)
    GET >=> path "/health" >=> OK "Alive!"
    NOT_FOUND "The requested resource was not found"
  ]
  // this API always returns strings
  >=> setMimeType "text/plain; charset=utf-8"

// By separating out configuration to its own function, it's easier to see
// where side-effects, like connections to message brokers, happen.
let configure argv =
  let pcfg =
    ProducerConfig.create (
      "web-greetings",
      Partitioner.roundRobin,
      requiredAcks = RequiredAcks.Local)
  let kafka = Kafka.connHost "localhost" // side-effecting
  let producer = Producer.create kafka pcfg // side-effecting
  // Returns a State record
  { kafka = kafka; producer = producer }

[<EntryPoint>]
let main argv =
  // with `use` we can ensure that the connection is closed when the app
  // is interrupted
  use state = configure argv
  startWebServer suaveConfig (web state)
  0