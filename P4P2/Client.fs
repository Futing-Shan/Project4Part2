// Learn more about F# at http://docs.microsoft.com/dotnet/fsharp
open System.Text.Json.Serialization
open Newtonsoft.Json
open System
open System.Net
open System.IO

let postDocRaw (url:string) (data: string) : string =

    let request = WebRequest.Create(url)
    request.Method        <- "POST"
    request.ContentType   <- "application/json; charset=UTF-8"

    do(
    use wstream = request.GetRequestStream() 
    use sw = new StreamWriter(wstream)
    printfn("%A") (data|>JsonConvert.SerializeObject)
    data|>JsonConvert.SerializeObject|>sw.Write
    )

      // todo：json再转一步string
    let response  = request.GetResponse()
    use reader     = new StreamReader(response.GetResponseStream())
    let output = reader.ReadToEnd()

    output



let getDocRaw (url:string)  : string =

      let request = WebRequest.Create(url)
      request.Method        <- "GET"
     
      let response  = request.GetResponse()
      use reader     = new StreamReader(response.GetResponseStream())
      let output = reader.ReadToEnd()

      reader.Close()
      response.Close()
      request.Abort()

      output


let registerfun()




// printfn("response:%A" (postDocRaw "http://10.20.0.130:8080/register" "add")
// Console.ReadLine()|>ignore
