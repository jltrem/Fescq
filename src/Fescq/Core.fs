module Fescq.Core

open System;

/// This marks an event data class
type IEventData = interface end

/// All IEventData classes must be marked with this attribute which specifies revision
[<AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)>]
type EventDataAttribute(name:string, version:int) =
   inherit Attribute()
   member x.Name with get() = name
   member x.Version with get() = version

type AggregateKey = {
   Name: string
   Id: Guid
   Version: int
}

type Event = {
   AggregateKey: AggregateKey
   Timestamp: DateTimeOffset
   MetaData: string
   EventData: IEventData
}

type Agg<'entity> = {
   Key: AggregateKey
   Entity: 'entity
   History: Event list
}

type EventTypeInfo = {
   Name: string
   Version: int
   DataType: Type
}

type EntityState<'entity> = {
   Version: int
   Entity: 'entity
}

module EntityState =
   let create entity = { Entity=entity; Version=1 }
   let update entity event = { Entity=entity; Version=event.AggregateKey.Version }

type ApplyAction<'entity> =
| Create of Event
| Update of EntityState<'entity> * Event

type ApplyEvent<'entity> = ApplyAction<'entity> -> EntityState<'entity>

type RegisteredEvents = {
   RevisionTypeMap: Map<string, Type>
   TypeRevisionMap: Map<string, struct (string*int)>
}

type Projection<'state, 'event> = {
   Init: 'state
   Update: 'state -> 'event -> 'state
}

type EventStore = {
   Registry: RegisteredEvents
   GetEvents: Guid -> Result<Event list, string>
   AddEvent: Event -> Result<unit, string>
   Save: unit -> Result<unit, string>
}

type AggRepository<'t> = {
   Load : Guid-> (Event list -> Result<Agg<'t>, string>) -> Result<Agg<'t>, string>
   LoadExpectedVersion : Guid -> (Event list -> Result<Agg<'t>, string>) -> int -> Result<Agg<'t>, string>
   Save : Agg<'t> -> Event list -> Result<unit, string>
}
