using System;
using HollowCreek.Core;
using NUnit.Framework;

namespace HollowCreek.Tests
{
    public class ServicesTests
    {
        class Dummy { }

        [SetUp, TearDown]
        public void Reset() => Services.Clear();

        [Test]
        public void Get_ReturnsRegisteredService()
        {
            var service = new Dummy();
            Services.Register(service);
            Assert.AreSame(service, Services.Get<Dummy>());
        }

        [Test]
        public void Get_Throws_WhenNotRegistered()
        {
            Assert.Throws<InvalidOperationException>(() => Services.Get<Dummy>());
        }

        [Test]
        public void Unregister_IgnoresOtherInstance()
        {
            var registered = new Dummy();
            Services.Register(registered);
            Services.Unregister(new Dummy());
            Assert.AreSame(registered, Services.Get<Dummy>());
        }

        [Test]
        public void Unregister_RemovesSameInstance()
        {
            var service = new Dummy();
            Services.Register(service);
            Services.Unregister(service);
            Assert.IsFalse(Services.TryGet<Dummy>(out _));
        }
    }
}
