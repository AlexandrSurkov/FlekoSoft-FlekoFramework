﻿using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Flekosoft.UnitTests.Plugins
{
    [TestClass]
    public class PluginTests
    {
        [TestMethod]
        public void PluginTest()
        {
            var guid = Guid.NewGuid();
            var name = "23";
            var desc = "wqe";
            var type = typeof(TestPluginInstance1);
            var version = new Version(1, 2, 3, 4);
            var tp = new TestPluginType1(guid, type, name, desc, false, version);

            Assert.AreEqual(guid, tp.Guid);
            Assert.AreEqual(name, tp.Name);
            Assert.AreEqual(desc, tp.Description);
            Assert.AreEqual(type, tp.Type);
            Assert.AreEqual(version, tp.Version);

            tp.IsEnabled = false;
            Assert.AreEqual(false, tp.IsEnabled);
            tp.IsEnabled = true;
            Assert.AreEqual(true, tp.IsEnabled);

            var instance = tp.GetInstance();
            Assert.AreEqual(tp.Type, instance.GetType());
            var p = instance as TestPluginInstance1;
            Assert.IsNotNull(p);

            Assert.IsFalse(p.IsDisposed);
            tp.Dispose();
            Assert.IsTrue(p.IsDisposed);
        }

        [TestMethod]
        public void MultipleInstanceTest()
        {
            var guid = Guid.NewGuid();
            var name = "23";
            var desc = "wqe";
            var type = typeof(TestPluginInstance1);
            var version = new Version(1, 2, 3, 4);
            var tp = new TestPluginType1(guid, type, name, desc, false, version);

            var instance1 = tp.GetInstance();
            Assert.AreEqual(tp.Type, instance1.GetType());
            var p1 = instance1 as TestPluginInstance1;
            Assert.IsNotNull(p1);

            var instance2 = tp.GetInstance();
            Assert.AreEqual(tp.Type, instance2.GetType());
            var p2 = instance2 as TestPluginInstance1;
            Assert.IsNotNull(p2);

            Assert.AreNotEqual(instance1, instance2);

            Assert.IsFalse(p1.IsDisposed);
            Assert.IsFalse(p2.IsDisposed);
            tp.Dispose();
            Assert.IsTrue(p1.IsDisposed);
            Assert.IsTrue(p2.IsDisposed);
        }


        [TestMethod]
        public void SingleInstanceTest()
        {
            var guid = Guid.NewGuid();
            var name = "23";
            var desc = "wqe";
            var type = typeof(TestPluginInstance1);
            var version = new Version(1, 2, 3, 4);
            var tp = new TestPluginType1(guid, type, name, desc, true, version);

            var instance1 = tp.GetInstance();
            Assert.AreEqual(tp.Type, instance1.GetType());
            var p1 = instance1 as TestPluginInstance1;
            Assert.IsNotNull(p1);

            var instance2 = tp.GetInstance();
            Assert.AreEqual(tp.Type, instance2.GetType());
            var p2 = instance2 as TestPluginInstance1;
            Assert.IsNotNull(p2);

            Assert.AreEqual(instance1, instance2);

            Assert.IsFalse(p1.IsDisposed);
            tp.Dispose();
            Assert.IsTrue(p1.IsDisposed);
        }
    }
}
