open System
open System.Text
open System.Security.Cryptography
open System.Collections.Generic
open Akka.Actor
open Akka.FSharp
open System.Diagnostics
open Akka.Remote


let mutable username = "add"
let mutable logoutFlag = false

let mutable portnumber = "2552"

printfn("Please input your port number:")
portnumber<-Console.ReadLine()
//printfn("portnumber:%s") portnumber
let tempString = @"akka {
            actor {
                provider = ""Akka.Remote.RemoteActorRefProvider, Akka.Remote""
            }
            remote {
                helios.tcp {
                    port = "+portnumber+"
                    hostname = 10.20.0.154
                }
            }
        }"

//


//printfn("%A") tempString


//own configuration
let configuration =
    Configuration.parse(tempString)

let akkasystem = System.create "Server" configuration
let serverIPwithPort="akka.tcp://Server@10.20.0.209:2552/user/"

let viewLiveTwitter()=
    printfn("View Live Twitter. (Press Enter to escape)")
    let view_send = select (serverIPwithPort+"viewactor")  akkasystem
    let msg = "view@"+username
    view_send<!msg
    while(Console.ReadKey().KeyChar.ToString().Equals("\n")) do
        0|>ignore
    let logoutmsg = "logoutview@"+username
    view_send<!logoutmsg    
    printfn("Exit Live Twitter")

let sendTwitter()=
    printfn("Please enter your twitter. (#tag @mention,don't miss the space!)")
    let twitter_msg = Console.ReadLine() 
    let twitter_send = select (serverIPwithPort+"tweetactor")  akkasystem
    let msg = username+"@"+twitter_msg
    printfn("twitterMsg:%s") msg
    twitter_send<! msg

let reTweet()=
    printfn("Please enter the NO. of the twitter want to retweet.")
    let twitter_no = Console.ReadLine()
    let retweet_send = select (serverIPwithPort+"retweetactor")  akkasystem
    let msg = username+"@"+twitter_no
    printfn("retweetMsg:%s") msg
    retweet_send<!msg 
    0|>ignore

let queryTwitter()=
    printfn("Please do the selection.")
    printfn("===================================")
    printfn("1.Tag")
    printfn("2.My Mention")
    printfn("3.My subscription")
    printfn("===================================")
    let mutable choice = ""
    let query_send = select (serverIPwithPort+"queryactor")  akkasystem
    let selection = Console.ReadLine()
    if selection.Equals("1") then
        printfn("Please enter the tag.")
        choice<-Console.ReadLine()
        let msg = username+"#"+choice
        printfn("QueryMsg:%s") msg
        query_send<!msg
    elif selection.Equals("2") then
        let msg = "@"+username
        printfn("QueryMsg:%s") msg
        query_send<!msg
    elif selection.Equals("3") then
        let msg = "&"+username
        printfn("QueryMsg:%s") msg 
        query_send<!msg
    else 
        printfn("Please enter the right number.")
    0|>ignore
 
let subscribeUser()=
    printfn("Please enter the username you want to subscribe.")
    let subscribe_name = Console.ReadLine()
    let subscribe_send = select (serverIPwithPort+"subscribeactor")  akkasystem
    let msg = username+"@"+subscribe_name
    subscribe_send<!msg
    printfn("subscribeMsg:%s") msg 
    0|>ignore

let logOut()=
    let logout_send = select (serverIPwithPort+"loginserver")  akkasystem
    let msg = "logout@"+username
    printfn("logoutmsg:%s") msg
    logout_send<! msg
    logoutFlag <- true
    //printf "flag changed" 
    printfn("ALready Logout, Thank you!")


let mutable selection="1"
let secondPage()=
    printfn("===================================")
    printfn("0.view live twitter")
    printfn("1.send twitter")
    printfn("2.re-tweet")
    printfn("3.query twitter")
    printfn("4.subscribe user")
    printfn("5.logout")
    printfn("===================================")
    printfn("Please enter your selection.")
    selection <- Console.ReadLine()
    if(selection.Equals("0")) then
        viewLiveTwitter()
        0
    elif(selection.Equals("1")) then
        sendTwitter()
        0
    elif(selection.Equals("2")) then
        reTweet()
        0 
    elif(selection.Equals("3")) then
        queryTwitter()
        0
    elif(selection.Equals("4")) then
        subscribeUser()
        0
    elif(selection.Equals("5")) then
        logOut()
        
        0
    else 
        printfn("Please enter the right selection.")
        0




let Regist()=
    printfn("===================================")
    printfn("Login/Regist")
    printfn("Please enter your username.")
    let input = Console.ReadLine()
    username<-input
    let actorname ="user"+username
    printfn("actorename:%s") actorname
    let clientaActor = spawn akkasystem actorname (fun mailbox ->   
            let rec loop() = actor {
                let! msg = mailbox.Receive()
                if (msg.ToString()="successlogin") then 
                    while (logoutFlag<>true) do
                        secondPage()
                else
                    printfn("%s") (msg.ToString())
                return! loop() 
            }
            loop())
    let regist_send = select (serverIPwithPort+"loginserver")  akkasystem
    let msg = "regist@"+input+"@"+portnumber
    regist_send<! msg
    0|>ignore

Regist()
//secondPage()

let actorname ="msg"+username
printfn("msg actor name:%s") actorname
let msgActor = spawn akkasystem actorname (fun mailbox ->   
            let rec loop() = actor {
                let! msg = mailbox.Receive()
                if(msg.ToString().StartsWith("end")) then
                    printfn("====================Query End==========================")
                else
                    printfn("%A") (msg.ToString())
                return! loop() 
            }
            loop())


while(logoutFlag<>true) do
    0

//secondPage()
