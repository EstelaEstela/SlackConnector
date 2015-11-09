﻿using System;
using Moq;
using NUnit.Framework;
using Should;
using SlackConnector.Connections;
using SlackConnector.Connections.Handshaking;
using SlackConnector.Exceptions;
using SlackConnector.Tests.Unit.SlackConnectionTests.Setups;
using SlackConnector.Tests.Unit.Stubs;
using SpecsFor;

namespace SlackConnector.Tests.Unit.SlackConnectorTests
{
    public static class ConnectedStatusTests
    {
        public class given_valid_setup_when_connected : SpecsFor<SlackConnector>
        {
            private SlackConnectionFactoryStub FactoryStub { get; set; }
            private SlackConnectionStub Connection { get; set; }
            private ISlackConnection Result { get; set; }

            protected override void InitializeClassUnderTest()
            {
                FactoryStub = new SlackConnectionFactoryStub();
                SUT = new SlackConnector(FactoryStub);
            }
            //TODO: Conintue refactoring all this gunk out
            protected override void Given()
            {
                Connection = new SlackConnectionStub();


            }

            protected override void When()
            {
                Result = SUT.Connect("key").Result;
            }

            [Test]
            public void then_should_be_aware_of_current_state()
            {
                Result.IsConnected.ShouldBeTrue();
            }

            [Test]
            public void then_should_have_a_connected_since_date()
            {
                Result.ConnectedSince.ShouldBeGreaterThanOrEqualTo(DateTime.Now.AddSeconds(-1));
                Result.ConnectedSince.ShouldBeLessThan(DateTime.Now.AddSeconds(1));
            }

            [Test]
            public void then_should_not_contain_connected_hubs()
            {
                Result.ConnectedHubs.Count.ShouldEqual(0);
            }

            [Test]
            public void then_should_not_contain_users()
            {
                Result.UserNameCache.Count.ShouldEqual(0);
            }
        }

        public class given_handshake_was_not_ok : SlackConnectorIsSetup
        {
            private SlackHandshake HandshakeResponse { get; set; }

            protected override void Given()
            {
                GetMockFor<IConnectionFactory>()
                    .Setup(x => x.CreateHandshakeClient())
                    .Returns(GetMockFor<IHandshakeClient>().Object);

                HandshakeResponse = new SlackHandshake { Ok = false, Error = "I AM A ERROR" };
                GetMockFor<IHandshakeClient>()
                    .Setup(x => x.FirmShake(It.IsAny<string>()))
                    .ReturnsAsync(HandshakeResponse);
            }

            [Test]
            public void then_should_throw_exception()
            {
                HandshakeException exception = null;

                try
                {
                    SUT.Connect("something").Wait();
                }
                catch (AggregateException ex)
                {

                    exception = ex.InnerExceptions[0] as HandshakeException;
                }

                Assert.That(exception, Is.Not.Null);
                Assert.That(exception.Message, Is.EqualTo(HandshakeResponse.Error));
            }
        }

        public class given_empty_api_key : SlackConnectorIsSetup
        {
            protected override void Given()
            {
                GetMockFor<IConnectionFactory>()
                    .Setup(x => x.CreateHandshakeClient())
                    .Returns(GetMockFor<IHandshakeClient>().Object);

                GetMockFor<IHandshakeClient>()
                    .Setup(x => x.FirmShake(It.IsAny<string>()))
                    .ReturnsAsync(new SlackHandshake());
            }

            [Test]
            public void then_should_be_aware_of_current_state()
            {
                bool exceptionDetected = false;

                try
                {
                    SUT.Connect("").Wait();
                }
                catch (AggregateException ex)
                {
                    exceptionDetected = ex.InnerExceptions[0] is ArgumentNullException;
                }

                Assert.That(exceptionDetected, Is.True);
            }
        }
    }
}