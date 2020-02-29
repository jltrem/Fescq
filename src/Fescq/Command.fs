module Fescq.Command

open System
open System.Reactive.Subjects
open System.Reactive.Linq;
open FSharpx.Control.Observable
open Fescq.Core

type AsyncValue<'t> () =
   let subject = new ReplaySubject<'t>()

   member x.Set(value:'t) =
      subject.OnNext(value)
      subject.OnCompleted()

   member x.Wait() =
      subject.LastAsync()
      |> Async.AwaitObservable
      |> Async.RunSynchronously


type AggregateWithHistory<'t> = {
   Aggregate: 't
   History: seq<Event>
}

type ICommand =
   abstract member CommandId: Guid
   abstract member AggregateId: Guid

type CommandResult = {
   Id: Guid
   Result: Result<unit, string>
}

type ReadResult<'t> = {
   Id: Guid
   Result: Result<AggregateWithHistory<'t>, string>
}



[<AbstractClass>]
type CreateAggregateCommand () =
   member x.Result = new AsyncValue<CommandResult>()
   interface ICommand with
      member val CommandId = System.Guid.NewGuid() with get
      member val AggregateId = System.Guid.NewGuid() with get

[<AbstractClass>]
type ReadAggregateCommand<'t> (aggregateId:Guid) =
   member x.Result = new AsyncValue<ReadResult<'t>>()
   interface ICommand with
      member val CommandId = System.Guid.NewGuid() with get
      member val AggregateId = aggregateId with get

[<AbstractClass>]
type UpdateCommand (aggregateId:Guid, originalVersion:int) =

   let ver =
      if originalVersion < 1 then
         failwith "originalVersion must be at least 1"
      else
         originalVersion

   member x.Result = new AsyncValue<CommandResult>()
   member val OriginalVersion = ver with get
   interface ICommand with
      member val CommandId = System.Guid.NewGuid() with get
      member val AggregateId = aggregateId with get

[<AbstractClass>]
type DetailCommand (aggregateId:Guid, detailId:Guid, originalVersion:int) =
   inherit UpdateCommand(aggregateId, originalVersion)
   member x.DetailId = detailId
   new (aggregateId:Guid, originalVersion:int) = DetailCommand(aggregateId, Guid.NewGuid(), originalVersion)
