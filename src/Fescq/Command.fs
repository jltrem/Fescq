module Fescq.Command

open System
open Fescq.Core


type AggregateWithHistory<'t> = {
   Aggregate: 't
   History: seq<Event>
}

type ICommand =
   abstract member CommandId: Guid
   abstract member AggregateId: Guid

type ReadResult<'t> = {
   Id: Guid
   Result: Result<AggregateWithHistory<'t>, string>
}


[<AbstractClass>]
type CreateAggregateCommand () =

   interface ICommand with
      member val CommandId = System.Guid.NewGuid() with get
      member val AggregateId = System.Guid.NewGuid() with get


[<AbstractClass>]
type ReadAggregateCommand<'t> (aggregateId:Guid) =

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

   member val OriginalVersion = ver with get

   interface ICommand with
      member val CommandId = System.Guid.NewGuid() with get
      member val AggregateId = aggregateId with get


[<AbstractClass>]
type DetailCommand (aggregateId:Guid, detailId:Guid, originalVersion:int) =
   inherit UpdateCommand(aggregateId, originalVersion)
   member x.DetailId = detailId
   new (aggregateId:Guid, originalVersion:int) = DetailCommand(aggregateId, Guid.NewGuid(), originalVersion)
