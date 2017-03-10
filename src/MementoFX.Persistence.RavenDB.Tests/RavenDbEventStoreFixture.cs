using System;
using NUnit.Framework;
using SharpTestsEx;
using Moq;
using Memento.Messaging;
using Raven.Client.Document;

namespace Memento.Persistence.RavenDB.Tests
{
    [TestFixture]
    public class RavenDbEventStoreFixture
    {
        [Test]
        public void Ctor_should_throw_ArgumentNullException_on_null_documentStore_and_value_of_parameter_should_be_documentStore()
        {
            var bus = new Mock<IEventDispatcher>().Object;
            Executing.This(() => new RavenDbEventStore(null, bus))
                           .Should()
                           .Throw<ArgumentNullException>()
                           .And
                           .ValueOf
                           .ParamName
                           .Should()
                           .Be
                           .EqualTo("documentStore");
        }

        [Test]
        public void Ctor_should_throw_ArgumentNullException_on_null_bus_and_value_of_parameter_should_be_bus()
        {
            var documentStore = new Mock<DocumentStore>().Object;
            Executing.This(() => new RavenDbEventStore(documentStore, null))
                           .Should()
                           .Throw<ArgumentNullException>()
                           .And
                           .ValueOf
                           .ParamName
                           .Should()
                           .Be
                           .EqualTo("eventDispatcher");
        }

        [Ignore("Not implemented yet")]
        [Test]
        public void Ctor_should_set_DocumentStore_field()
        {
            var bus = new Mock<IEventDispatcher>().Object;
            var mock = new Mock<DocumentStore>().Object;
            var sut = new RavenDbEventStore(mock, bus);
            //Assert.AreEqual(mock, RavenDbEventStore.DocumentStore);
        }
    }
}
