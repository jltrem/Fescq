using System;
using LanguageExt;
using static LanguageExt.FSharp;
using Fescq;
using static Fescq.EventRegistryFuncs;

namespace NetCoreWebApp.Storage
{
   public class CrmEventRegistry
   {
      public EventRegistry Registry { get; }

      public CrmEventRegistry()
      {
         var assemblies = AppDomain.CurrentDomain.GetAssemblies();
         Registry = createForAssemblies(assemblies);
      }

      public Option<Type> EventType(string name, int version) =>
         fs(eventType(Registry, name, version));

      public Option<(string Name, int Version)> EventRevision(Type dataType) =>
         fs(eventRevision(Registry, dataType));
   }
}
