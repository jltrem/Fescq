module Fescq.EventRegistry

open System;
open System.Reflection
open Core

let private revisionKey name version =
   sprintf "%s-%i" name version

let eventType (registry:RegisteredEvents) (name:string) (version:int) =
   revisionKey name version
   |> registry.RevisionTypeMap.TryFind

let eventRevision (registry:RegisteredEvents) (dataType:Type) =
   dataType.AssemblyQualifiedName
   |> registry.TypeRevisionMap.TryFind

let private toEventTypeInfo (dataType:Type) =
   dataType.GetCustomAttributes(typeof<EventDataAttribute>, false)
   |> Seq.map (fun x -> x :?> EventDataAttribute)
   |> Seq.head
   |> fun x ->
      { Name = x.Name
        Version = x.Version
        DataType = dataType }

let create (mappings:seq<EventTypeInfo>) =
   {
      RevisionTypeMap =
         mappings
         |> Seq.map (fun x ->
               let key = revisionKey x.Name x.Version
               (key, x.DataType)
            ) |> Map

      TypeRevisionMap =
         mappings
         |> Seq.map (fun x ->
               let key = x.DataType.AssemblyQualifiedName
               (key, struct (x.Name, x.Version))
            ) |> Map
   }

let createForAssemblies (assemblies:seq<Assembly>) =
   let eventDataMarker = typeof<IEventData>
   assemblies
   |> Seq.collect (fun x -> x.GetTypes())
   |> Seq.filter (fun x -> eventDataMarker.IsAssignableFrom(x) && x.IsClass)
   |> Seq.map toEventTypeInfo
   |> create


