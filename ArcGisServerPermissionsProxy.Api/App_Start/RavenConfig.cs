using System;
using Raven.Client;
using Raven.Client.Indexes;

namespace ArcGisServerPermissionsProxy
{
    public class RavenConfig
    {
        public static void Register(Type type, IDocumentStore documentStore)
        {
            RegisterIndexes(type, documentStore);
        }

        private static void RegisterIndexes(Type type, IDocumentStore documentStore)
        {
            IndexCreation.CreateIndexes(type.Assembly, documentStore);
        }
    }
}