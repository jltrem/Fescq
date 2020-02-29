using System;
using LanguageExt;
using static LanguageExt.Prelude;
using static Fescq.Core;
using ES = Fescq.CSharp.Idiomatic;

namespace NetCoreWebApp.Storage
{
   public class CrmEventRegistry
   {
      public RegisteredEvents Registry { get; }

      public CrmEventRegistry()
      {
         var assemblies = AppDomain.CurrentDomain.GetAssemblies();
         Registry = ES.EventRegistry.CreateForAssemblies(assemblies);
      }

      public Option<Type> EventType(string name, int version) =>
         Try(() => ES.EventRegistry.CreateEventType(Registry, name, version))
         .ToOption();

      public Option<(string Name, int Version)> EventRevision(Type dataType) =>
         Try(() => ES.EventRegistry.CreateEventRevision(Registry, dataType))
         .ToOption();
   }
}
