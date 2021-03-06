namespace CrmDomain.CSharp

open System
open CrmDomain.Aggregate
open Fescq.Core


[<AbstractClass; Sealed>]
type ContactWorkflow private () =

   static member Create (getUtcNow:System.Func<DateTimeOffset>, store:EventStore, metaData:string, cmd:Contact.CreateContact) =
      Contact.Workflow.create (fun () -> getUtcNow.Invoke()) store metaData cmd
      |> function
         | Ok agg -> agg
         | Error msg -> failwith msg

   static member Update (getUtcNow:System.Func<DateTimeOffset>, store:EventStore, aggId:Guid, metaData:string, cmd:Fescq.Command.UpdateCommand) =
      Contact.Workflow.update (fun () -> getUtcNow.Invoke()) store aggId metaData cmd
      |> function
         | Ok x -> x
         | Error msg -> failwith msg

   static member Load (store:EventStore, aggId:Guid) =
      Contact.Workflow.load store aggId
      |> function
         | Ok agg -> agg
         | Error msg -> failwith msg

