module ScheduleTests

open System
open Akka.Actor
open Akka.FSharp
open Xunit

open Akkling.TestKit

open Scheduler

let validateResult (msg : JobCommandResult) =
    true

[<Fact>]
let ``Should deliver message with short delivery time`` () : unit = testDefault <| fun tck -> 

    let scheduler = spawn tck "scheduler" <| scheduleActor None

    let schedule = Once <| DateTimeOffset.Now.AddSeconds(1.)
    scheduler <! CreateJob (tck.TestActor, 1, schedule)
    
    expectMsgFilter tck <| validateResult |> ignore
    expectMsg tck 1 |> ignore

[<Fact>]
let ``Should not deliver message with long delivery time`` () : unit = testDefault <| fun tck -> 

    let scheduler = spawn tck "scheduler" <| scheduleActor None

    let schedule1 = Once <| DateTimeOffset.Now.AddSeconds(10.)
    scheduler <! CreateJob (tck.TestActor, 1, schedule1)

    let schedule2 = Once <| DateTimeOffset.Now.AddSeconds(1.)
    scheduler <! CreateJob (tck.TestActor, 2, schedule2)
    
    expectMsgFilter tck <| validateResult |> ignore
    expectMsgFilter tck <| validateResult |> ignore
    expectMsg tck 2 |> ignore

[<Fact>]
let ``Should deliver messages with recurrent schedule`` () : unit = testDefault <| fun tck -> 

    let scheduler = spawn tck "scheduler" <| scheduleActor None

    let schedule = RepeatWithCountAfter <| (DateTimeOffset.Now.AddSeconds(1.), TimeSpan.FromMilliseconds(500.), 2)
    scheduler <! CreateJob (tck.TestActor, 1, schedule)
    
    expectMsgFilter tck <| validateResult |> ignore
    expectMsg tck 1 |> ignore
    expectMsg tck 1 |> ignore

[<Fact>]
let ``Should not deliver message with deleted schedule`` () : unit = testDefault <| fun tck -> 

    let scheduler = spawn tck "scheduler" <| scheduleActor None

    let schedule = Once <| DateTimeOffset.Now.AddSeconds(1.)
    let (jobEvent : JobCommandResult) = scheduler <? CreateJob (tck.TestActor, 1, schedule) |> Async.RunSynchronously
    match jobEvent with
    | Success jobId -> 
        scheduler <! RemoveJob jobId
    | _ -> ()
    
    expectMsgFilter tck <| validateResult |> ignore
    expectNoMsg tck
