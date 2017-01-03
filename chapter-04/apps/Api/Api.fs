open Suave
open Suave.Successful
let config =
  { defaultConfig with
      bindings = [ HttpBinding.createSimple Protocol.HTTP "::" 8080 ] }
startWebServer config (OK "Hello World!")