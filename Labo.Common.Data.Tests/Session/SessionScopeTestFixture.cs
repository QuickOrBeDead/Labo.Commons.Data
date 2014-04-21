﻿namespace Labo.Common.Data.Tests.Session
{
    using System;
    using System.Collections.Generic;

    using Labo.Common.Data.Session;

    using NSubstitute;

    using NUnit.Framework;

    // TODO: Add multi-threaded tests
    // TODO: Add session commit tests
    [TestFixture]
    public class SessionScopeTestFixture
    {
        [Test]
        public void SessionScopeCurrentMustBeSameInstanceInUsingStatement()
        {
            ISessionFactory sessionFactory = Substitute.For<ISessionFactory>();
            sessionFactory.CreateSession().Returns(Substitute.For<ISession>());

            using (SessionScope sessionScope = new SessionScope(sessionFactory))
            {
                Assert.AreEqual(sessionScope, SessionScope.Current);
                Assert.AreSame(sessionScope, SessionScope.Current);

                Assert.AreEqual(sessionScope.ScopeId, SessionScope.Current.ScopeId);
                Assert.AreEqual(sessionScope.CreateDate, SessionScope.Current.CreateDate);

                Assert.IsFalse(sessionScope.Disposed);
                Assert.IsFalse(sessionScope.Completed);
            }
        }

        [Test]
        public void SessionScopeCurrentMustBeNullWhenOutOfUsingStatement()
        {
            ISessionFactory sessionFactory = Substitute.For<ISessionFactory>();
            sessionFactory.CreateSession().Returns(Substitute.For<ISession>());

            SessionScope sessionScope = new SessionScope(sessionFactory);
            using (sessionScope)
            {
            }

            Assert.IsNull(SessionScope.Current);

            Assert.IsTrue(sessionScope.Disposed);
            Assert.IsFalse(sessionScope.Completed);
        }

        [Test]
        public void WhenOneSessionScopeIsCreatedThenSessionScopeParentMustBeNull()
        {
            ISessionFactory sessionFactory = Substitute.For<ISessionFactory>();
            sessionFactory.CreateSession().Returns(Substitute.For<ISession>());

            using (SessionScope sessionScope = new SessionScope(sessionFactory))
            {
                Assert.IsNull(sessionScope.Parent);
            }
        }

        [Test, Sequential]
        public void WhenNestedSessionScopeSessionScopeCurrentMustBeEqualToUsingSessionScope([Values(SessionScopeOption.Required, SessionScopeOption.RequiresNew)]SessionScopeOption sessionScopeOption)
        {
            ISessionFactory sessionFactory = Substitute.For<ISessionFactory>();
            sessionFactory.CreateSession().Returns(Substitute.For<ISession>());

            using (SessionScope sessionScope1 = new SessionScope(sessionFactory, sessionScopeOption))
            {
                Assert.AreEqual(sessionScope1, SessionScope.Current);
                Assert.AreSame(sessionScope1, SessionScope.Current);

                using (SessionScope sessionScope2 = new SessionScope(sessionFactory, sessionScopeOption))
                {
                    Assert.AreEqual(sessionScope2, SessionScope.Current);
                    Assert.AreSame(sessionScope2, SessionScope.Current);

                    Assert.IsFalse(sessionScope1.Disposed);
                    Assert.IsFalse(sessionScope1.Completed);

                    Assert.IsFalse(sessionScope2.Disposed);
                    Assert.IsFalse(sessionScope2.Completed);

                    Assert.AreNotEqual(sessionScope1.ScopeId, sessionScope2.ScopeId);
                }

                Assert.AreEqual(sessionScope1, SessionScope.Current);
                Assert.AreSame(sessionScope1, SessionScope.Current);

                Assert.IsFalse(sessionScope1.Disposed);
                Assert.IsFalse(sessionScope1.Completed);
            }
        }

        [Test, Sequential]
        public void WhenNestedSessionScopeChildSessionScopesParentMustBeSamaAsParentSessionScope([Values(SessionScopeOption.Required, SessionScopeOption.RequiresNew)]SessionScopeOption sessionScopeOption)
        {
            ISessionFactory sessionFactory = Substitute.For<ISessionFactory>();
            sessionFactory.CreateSession().Returns(Substitute.For<ISession>());

            using (SessionScope sessionScope1 = new SessionScope(sessionFactory, sessionScopeOption))
            {
                using (SessionScope sessionScope2 = new SessionScope(sessionFactory, sessionScopeOption))
                {
                    Assert.AreEqual(sessionScope1, sessionScope2.Parent);
                    Assert.AreSame(sessionScope1, sessionScope2.Parent);

                    Assert.AreEqual(sessionScope1, SessionScope.Current.Parent);
                    Assert.AreSame(sessionScope1, SessionScope.Current.Parent);
                }
            }
        }

        [Test, Sequential]
        public void WhenNestedSessionScopeScopeCurrentMustBeNullWhenOutOfUsingStatement([Values(SessionScopeOption.Required, SessionScopeOption.RequiresNew)]SessionScopeOption sessionScopeOption)
        {
            ISessionFactory sessionFactory = Substitute.For<ISessionFactory>();
            sessionFactory.CreateSession().Returns(Substitute.For<ISession>());

            using (new SessionScope(sessionFactory, sessionScopeOption))
            {
                using (new SessionScope(sessionFactory, sessionScopeOption))
                {
                    Assert.IsNotNull(SessionScope.Current);
                }

                Assert.IsNotNull(SessionScope.Current);
            }

            Assert.IsNull(SessionScope.Current);
        }

        [Test, Sequential]
        public void TestNNestedSessionScopes([Values(SessionScopeOption.Required, SessionScopeOption.RequiresNew)]SessionScopeOption sessionScopeOption)
        {
            ISessionFactory sessionFactory = Substitute.For<ISessionFactory>();
            sessionFactory.CreateSession().Returns(Substitute.For<ISession>());

            Func<SessionScope> createNewSessionScopeFunc = () => new SessionScope(sessionFactory, sessionScopeOption);

            Stack<SessionScope> sessionScopes = new Stack<SessionScope>();

            const int n = 10;
            for (int i = 0; i < n; i++)
            {
                SessionScope sessionScope = createNewSessionScopeFunc();

                Assert.IsNotNull(SessionScope.Current);
                Assert.AreSame(sessionScope, SessionScope.Current);

                sessionScopes.Push(sessionScope);
            }

            while (sessionScopes.Count > 0)
            {
                SessionScope currentSessionScope = sessionScopes.Pop();

                Assert.IsNotNull(SessionScope.Current);
                Assert.AreSame(currentSessionScope, SessionScope.Current);

                currentSessionScope.Dispose();
            }

            Assert.IsNull(SessionScope.Current);
        }

        [Test, ExpectedException(typeof(ObjectDisposedException))]
        public void CompleteThrowsExceptionWhenSessionScopeIsDisposed()
        {
            ISessionFactory sessionFactory = Substitute.For<ISessionFactory>();
            sessionFactory.CreateSession().Returns(Substitute.For<ISession>());

            SessionScope sessionScope = new SessionScope(sessionFactory);
            using (sessionScope)
            {
            }

            Assert.IsTrue(sessionScope.Disposed);
            
            sessionScope.Complete();
        }

        [Test, ExpectedException(typeof(InvalidOperationException))]
        public void CompleteThrowsExceptionWhenSessionScopeIsAlreadyCompleted()
        {
            ISessionFactory sessionFactory = Substitute.For<ISessionFactory>();
            sessionFactory.CreateSession().Returns(Substitute.For<ISession>());

            using (SessionScope sessionScope = new SessionScope(sessionFactory))
            {
                Assert.IsFalse(sessionScope.Completed);

                sessionScope.Complete();

                Assert.IsTrue(sessionScope.Completed);

                sessionScope.Complete();
            }
        }

        [Test]
        public void WhenSessionScopeIsCreatedThenSessionFactoryCreateSessionMustBeCalledOnce()
        {
            ISessionFactory sessionFactory = Substitute.For<ISessionFactory>();          
            sessionFactory.CreateSession().Returns(Substitute.For<ISession>());

            using (new SessionScope(sessionFactory))
            {
                sessionFactory.Received(1).CreateSession();
            }
        }

        [Test]
        public void WhenTwoNestedSessionScopeIsCreatedThenSessionFactoryCreateSessionMustBeCalledOnce()
        {
            ISessionFactory sessionFactory = Substitute.For<ISessionFactory>();
            sessionFactory.CreateSession().Returns(Substitute.For<ISession>());

            using (new SessionScope(sessionFactory, SessionScopeOption.Required))
            {
                using (new SessionScope(sessionFactory, SessionScopeOption.Required))
                {
                    sessionFactory.Received(1).CreateSession();
                }
            }
        }

        [Test]
        public void WhenTwoNestedSessionScopeIsCreatedWithRequiresNewThenSessionFactoryCreateSessionMustBeCalledTwice()
        {
            ISessionFactory sessionFactory = Substitute.For<ISessionFactory>();
            sessionFactory.CreateSession().Returns(Substitute.For<ISession>());

            using (new SessionScope(sessionFactory, SessionScopeOption.Required))
            {
                using (new SessionScope(sessionFactory, SessionScopeOption.RequiresNew))
                {
                    sessionFactory.Received(2).CreateSession();
                }
            }
        }

        [Test]
        public void WhenThreeNestedSessionScopeOneOfThemIsCreatedWithRequiresNewThenSessionFactoryCreateSessionMustBeCalledTwice()
        {
            ISessionFactory sessionFactory = Substitute.For<ISessionFactory>();
            sessionFactory.CreateSession().Returns(Substitute.For<ISession>());

            using (new SessionScope(sessionFactory, SessionScopeOption.Required))
            {
                using (new SessionScope(sessionFactory, SessionScopeOption.RequiresNew))
                {
                    using (new SessionScope(sessionFactory, SessionScopeOption.Required))
                    {
                        sessionFactory.Received(2).CreateSession();
                    }
                }
            }
        }

        [Test]
        public void WhenOutOfSessionScopeUsingStatementThenSessionScopeSessionMustBeDisposed()
        {
            ISessionFactory sessionFactory = Substitute.For<ISessionFactory>();
            bool sessionDisposed = false;
            ISession session = Substitute.For<ISession>();
            session.When(x => x.Dispose()).Do(x => sessionDisposed = true);
            sessionFactory.CreateSession().Returns(session);

            SessionScope sessionScope = new SessionScope(sessionFactory);
            using (sessionScope)
            {
            }

            session.Received(1).Dispose();

            Assert.IsTrue(sessionScope.Disposed);
            Assert.IsNull(sessionScope.Session);
            Assert.IsTrue(sessionDisposed);
        }

        [Test]
        public void WhenTwoNestedSessionScopeCreatedWithRequiredOptionThenSessionScopeSessionDisposeMustBeCalledOnce()
        {
            ISessionFactory sessionFactory = Substitute.For<ISessionFactory>();
            bool sessionDisposed = false;
            ISession session = Substitute.For<ISession>();
            session.When(x => x.Dispose()).Do(x => sessionDisposed = true);
            sessionFactory.CreateSession().Returns(session);

            using (new SessionScope(sessionFactory))
            {
                using (new SessionScope(sessionFactory, SessionScopeOption.Required))
                {
                }

                session.DidNotReceive().Dispose();
                Assert.IsFalse(sessionDisposed);
            }

            session.Received(1).Dispose();
            Assert.IsTrue(sessionDisposed);
        }

        [Test]
        public void WhenTwoNestedSessionScopeCreatedWithRequiresNewOptionThenSessionScopeSessionDisposeMustBeCalledTwice()
        {
            ISessionFactory sessionFactory = Substitute.For<ISessionFactory>();
            bool sessionDisposed = false;
            ISession session = Substitute.For<ISession>();
            session.When(x => x.Dispose()).Do(x => sessionDisposed = true);
            sessionFactory.CreateSession().Returns(session);

            using (new SessionScope(sessionFactory, SessionScopeOption.RequiresNew))
            {
                using (new SessionScope(sessionFactory, SessionScopeOption.RequiresNew))
                {
                }

                session.Received(1).Dispose();
                Assert.IsTrue(sessionDisposed);
            }

            session.Received(2).Dispose();
            Assert.IsTrue(sessionDisposed);
        }

        [Test]
        public void SessionScopeSessionMustBeNullWhenOutofUsingStatement()
        {
            ISessionFactory sessionFactory = Substitute.For<ISessionFactory>();
            sessionFactory.CreateSession().Returns(Substitute.For<ISession>());

            SessionScope sessionScope = new SessionScope(sessionFactory);
            using (sessionScope)
            {
            }

            Assert.IsNull(sessionScope.Session);
        }

        [Test]
        public void SessionScopeSessionMustBeEqualToSessionScopeCurrentSessionInTheUsingStatement()
        {
            ISessionFactory sessionFactory = Substitute.For<ISessionFactory>();
            ISession session = Substitute.For<ISession>();
            sessionFactory.CreateSession().Returns(session);

            using (SessionScope sessionScope = new SessionScope(sessionFactory))
            {
                Assert.AreSame(session, sessionScope.Session);
                Assert.AreSame(session, SessionScope.Current.Session);
            }
        }

        [Test]
        public void Test1()
        {
            ISessionFactory sessionFactory1 = Substitute.For<ISessionFactory>();
            ISession session1 = Substitute.For<ISession>();
            sessionFactory1.CreateSession().Returns(session1);

            ISessionFactory sessionFactory2 = Substitute.For<ISessionFactory>();
            ISession session2 = Substitute.For<ISession>();
            sessionFactory2.CreateSession().Returns(session2);

            using (SessionScope sessionScope = new SessionScope(sessionFactory1))
            {
                Assert.AreSame(session1, sessionScope.Session);
                Assert.AreSame(session1, SessionScope.Current.Session);

                using (SessionScope sessionScope1 = new SessionScope(sessionFactory2))
                {
                    Assert.AreSame(session1, sessionScope1.Session);
                    Assert.AreSame(session1, sessionScope.Session);
                    Assert.AreSame(session1, SessionScope.Current.Session);

                    using (SessionScope sessionScope2 = new SessionScope(sessionFactory2, SessionScopeOption.RequiresNew))
                    {
                        Assert.AreSame(session2, sessionScope2.Session);
                        Assert.AreSame(session2, SessionScope.Current.Session);
                    }
                }
            }
        }

        [Test]
        public void Test()
        {
            ISessionFactory sessionFactory = Substitute.For<ISessionFactory>();
            ISession session = Substitute.For<ISession>();
            sessionFactory.CreateSession().Returns(session);

            using (SessionScope sessionScope = new SessionScope(sessionFactory))
            {
                Assert.AreEqual(sessionScope, SessionScope.Current);
                Assert.AreSame(sessionScope, SessionScope.Current);

                Assert.AreEqual(session, SessionScope.Current.Session);
                Assert.AreSame(session, SessionScope.Current.Session);
            }
        }
    }
}