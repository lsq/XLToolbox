﻿using System;
using NUnit.Framework;
using XLToolbox.WorkbookStorage;
using Microsoft.Office.Interop.Excel;

namespace XLToolbox.Test
{
    [TestFixture]
    public class TestWorkbookStorage
    {
        [Test]
        [ExpectedException(typeof(InvalidContextException))]
        public void InvalidContextCausesException()
        {
            Store storage = new Store(TestContext.Workbook);
            storage.Context = "made-up context that does not exist";
        }

        [Test]
        public void StoreInGlobalContext()
        {
            Store store = new Store(TestContext.Workbook);
            store.Context = "";
            store.Put("global setting", 1234);
            Assert.AreEqual(1234, store.Get("global setting", 0, 1, 2000));
        }

        [Test]
        [TestCase(333, 200, 400, 333)]
        [TestCase(123, 200, 400, 200)]
        [TestCase(423, 200, 400, 400)]
        public void StoreAndRetrieveIntegers(int i, int min, int max, int expect)
        {
            Store storage1 = new Store(TestContext.Workbook);
            string key = "Some property key";
            storage1.Put(key, i);
            storage1.Flush();
            Store storage2 = new Store(TestContext.Workbook);
            storage2.Context = storage1.Context;
            int j = storage2.Get(key, 0, min, max);
            Assert.AreEqual(expect, j);
        }

        [Test]
        public void StoreAndRetrieveString()
        {
            string s1 = "hello world";
            using (Store storage1 = new Store(TestContext.Workbook))
            {
                storage1.Put("my key", s1);
            }
            using (Store storage1 = new Store(TestContext.Workbook))
            {
                string s2 = storage1.Get("my key", "default");
                Assert.AreEqual(s1, s2);
            }
        }
        
        [Test]
        public void RetrieveNonExistingString()
        {
            Store store = new Store(TestContext.Workbook);
            string s = "hello world";
            Assert.AreEqual(s, store.Get("non-existing key", s));
        }

        [Test]
        [ExpectedException(typeof(EmptyKeyException))]
        public void DoNotAllowPutWithEmptyKey()
        {
            Store store = new Store(TestContext.Workbook);
            store.Put("", 123);
        }

        [Test]
        [ExpectedException(typeof(EmptyKeyException))]
        public void DoNotAllowGetWithEmptyKey()
        {
            Store store = new Store(TestContext.Workbook);
            string s = store.Get("", "not possible");
        }
    }
}
