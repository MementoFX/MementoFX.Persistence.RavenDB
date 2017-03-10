using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using Moq;
using Raven.Client.Document;
using Raven.Client.Indexes;
using Raven.Tests.Helpers;
using Memento.Messaging;
using Memento.Persistence.RavenDB.Indexes;
using Memento.Persistence.RavenDB.Listeners;

namespace Memento.Persistence.RavenDB.Tests
{
    public class FixtureBase : RavenTestBase
    {
        protected DocumentStore documentStore;
        protected DocumentStore eventDocumentStore;
        protected RavenDbEventStore MementoEventStore;

        [SetUp]
        public void SetUp()
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

        [TearDown]
        public void CleanUp()
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
