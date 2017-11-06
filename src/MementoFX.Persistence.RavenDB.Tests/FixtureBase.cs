using Moq;
using Raven.Client.Document;
using Raven.Client.Indexes;
using Raven.Tests.Helpers;
using MementoFX.Messaging;
using MementoFX.Persistence.RavenDB.Indexes;
using MementoFX.Persistence.RavenDB.Listeners;
using MementoFX.Persistence.RavenDB;
using System;

namespace Memento.Persistence.RavenDB.Tests
{
    public class FixtureBase : RavenTestBase, IDisposable
    {
        protected DocumentStore documentStore;
        protected DocumentStore eventDocumentStore;
        protected RavenDbEventStore MementoEventStore;

        public FixtureBase()
        {
            using (var ravenDocumentStore = new DocumentStore() { ConnectionStringName = "RavenDbInstance" })
            {
                ravenDocumentStore.Initialize();
                ravenDocumentStore.DatabaseCommands
                            .GlobalAdmin
                            .EnsureDatabaseExists("TestDocumentStore");
                ravenDocumentStore.DatabaseCommands
                            .GlobalAdmin
                            .EnsureDatabaseExists("TestEventStore");
            }

            documentStore = new DocumentStore()
            {
                ConnectionStringName = "DocumentStore"
            };
            documentStore.Initialize();

            eventDocumentStore = new DocumentStore()
            {
                ConnectionStringName = "EventStore"
            };
            eventDocumentStore.Initialize();

            eventDocumentStore.RegisterListener(new PatchDomainEventsApplyingMementoMetadata());

            new DomainEvents_Stream().Execute(eventDocumentStore);
            new RavenDocumentsByEntityName().Execute(eventDocumentStore);

            var bus = new Mock<IEventDispatcher>().Object;
            var mementoEventStore = new RavenDbEventStore(eventDocumentStore, bus);
            MementoEventStore = mementoEventStore;
        }

        void IDisposable.Dispose()
        {
            documentStore.Dispose();
            eventDocumentStore.Dispose();

            using (var ravenDocumentStore = new DocumentStore() { ConnectionStringName = "RavenDbInstance" })
            {
                ravenDocumentStore.Initialize();
                ravenDocumentStore.DatabaseCommands
                            .GlobalAdmin
                            .DeleteDatabase("TestDocumentStore");
                ravenDocumentStore.DatabaseCommands
                            .GlobalAdmin
                            .DeleteDatabase("TestEventStore");
            }
        }
    }
}
