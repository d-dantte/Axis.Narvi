using Axis.Narvi.Core;
using Axis.Narvi.Core.Notify;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.ComponentModel;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;

namespace Axis.Luna.Operation.Test
{
    [TestClass]
    public class NotifierBaseTest
    {

        [TestMethod]
        public void TestNotification()
        {
            var obj = new Notifiable();
            object control = null;
            int callCount = 0;
            obj.PropertyChanged += (x, y) =>
            {
                Console.WriteLine($"{y.PropertyName} modified");
                control = (y as NotifiedEventArgs)?.NewValue;
                callCount++;
            };

            var handle = obj.NotifyFor("FirstName", (xxx, yyy) =>
            {
                Console.WriteLine("Special Notification for First Name");
            });


            obj.FirstName = "Stanley";
            Assert.AreEqual("Stanley", obj.FirstName);
            Assert.AreEqual("Stanley", control);
            Assert.AreEqual(2, callCount);
            Console.WriteLine(obj.FullName);
            Assert.AreEqual($"Stanley", obj.FullName);

            obj.LastName = "Dantte";
            Assert.AreEqual("Dantte", obj.LastName);
            Assert.AreEqual("Stanley Dantte", control);
            Assert.AreEqual(4, callCount);
            Console.WriteLine(obj.FullName);
            Assert.AreEqual($"Stanley Dantte", obj.FullName);

            obj.FirstName = "Stanley";
            Assert.AreEqual("Stanley", obj.FirstName);
            Assert.AreEqual("Stanley Dantte", control);
            Assert.AreEqual(4, callCount);

            
            var now = DateTimeOffset.Now;
            obj.DateOfBirth = now;
            Assert.AreEqual(now, obj.DateOfBirth);
            Assert.AreEqual(now, control);
            Assert.AreEqual(5, callCount);
                       
            handle.Unsubscribe();
            obj.FirstName = "Meta";
            Assert.AreEqual("Meta", obj.FirstName);
            Assert.AreEqual("Meta Dantte", control);
            Assert.AreEqual(7, callCount);

            var child = new Notifiable();
            child.FirstName = "Child First Name";

            obj.Child = child;
            child.FirstName = "Miasma";
        }

        [TestMethod]
        public void TestNotificationSurrogate()
        {
            var obj = new Notifiable2();
            object control = null;
            int callCount = 0;
            obj.PropertyChanged += (x, y) =>
            {
                Console.WriteLine($"{y.PropertyName} modified");
                control = (y as NotifiedEventArgs)?.NewValue;
                callCount++;
            };


            obj.FirstName = "Stanley";
            Assert.AreEqual("Stanley", obj.FirstName);
            Assert.AreEqual("Stanley", control);
            Assert.AreEqual(2, callCount);
            Console.WriteLine(obj.FullName);
            Assert.AreEqual($"Stanley", obj.FullName);

            obj.LastName = "Dantte";
            Assert.AreEqual("Dantte", obj.LastName);
            Assert.AreEqual("Stanley Dantte", control);
            Assert.AreEqual(4, callCount);
            Console.WriteLine(obj.FullName);
            Assert.AreEqual($"Stanley Dantte", obj.FullName);

            obj.FirstName = "Stanley";
            Assert.AreEqual("Stanley", obj.FirstName);
            Assert.AreEqual("Stanley Dantte", control);
            Assert.AreEqual(4, callCount);

            var now = DateTimeOffset.Now;
            obj.DateOfBirth = now;
            Assert.AreEqual(now, obj.DateOfBirth);
            Assert.AreEqual(now, control);
            Assert.AreEqual(5, callCount);

            var xx = new object();
            obj.SetAttached("bleh", xx);
            Assert.AreEqual(xx, obj.GetAttached<object>("bleh"));

        }

        [TestMethod]
        public void PathNotification()
        {
            //var path = "Child.Child.FirstName";
            var notifiable = new Notifiable { Child = new Notifiable() };
            var xpath = notifiable.NotifyForPath(_n => _n.Child.Child.FirstName, (x, y, z) =>
            {
                Console.WriteLine($"Path: {y}, Value: {z.NewValue}");
            });

            notifiable.Child.FirstName = "shouldnt work";
            notifiable.Child.Child = new Notifiable(); //<-- should trigger
            notifiable.Child.Child.FirstName = "Dantte";
            var xxx = notifiable.Child.Child;
            notifiable.Child.Child = null; //should trigger

            xxx.FirstName = "Meltus"; //<-- should not trigger
        }

        [TestMethod]
        public void PathNotification2()
        {
            //var path = "Child.Child.FirstName";
            var notifiable = new Notifiable();
            var child = new Notifiable();
            child.Child = child;
            notifiable.Child = child;

            var xpath = notifiable.NotifyForPath("Child.Child.FirstName", (x, y, z) =>
            {
                Console.WriteLine($"Path: {y}, Value: {z.NewValue}");
            });

            notifiable.Child.Child.FirstName = "stuff";
        }

        [TestMethod]
        public void NotifiedBindingTest()
        {
            var obj1 = new Notifiable();
            var obj2 = new Notifiable();

            var binding = new NotifiedBinding(new BindingProfile(obj1, "FirstName"), new BindingProfile(obj2, "FirstName"));

            obj1.FirstName = "Melanin";
            Assert.AreEqual("Melanin", obj2.FirstName);

            obj2.FirstName = "Keratin";
            Assert.AreEqual("Keratin", obj1.FirstName);

            obj1.FirstName = "Soladin";
            Assert.AreEqual("Soladin", obj2.FirstName);

            obj2.FirstName = "Yasinin";
            Assert.AreEqual("Yasinin", obj1.FirstName);

            obj1.FirstName = "Plurobin";
            Assert.AreEqual("Plurobin", obj2.FirstName);

            obj2.FirstName = "Byurenin";
            Assert.AreEqual("Byurenin", obj1.FirstName);
        }
    }


    public class Notifiable: NotifiableBase
    {
        [NotifiedBy(nameof(FirstName), nameof(LastName))]
        public string FullName => $"{FirstName} {LastName}".Trim();

        public string FirstName
        {
            get => base.Get<string>();
            set => base.Set(ref value);
        }
        public string LastName
        {
            get => base.Get<string>();
            set => base.Set(ref value);
        }
        public DateTimeOffset DateOfBirth
        {
            get => base.Get<DateTimeOffset>();
            set => base.Set(ref value);
        }
        public double Weight
        {
            get => base.Get<double>();
            set => base.Set(ref value);
        }

        public Notifiable Child
        {
            get => base.Get<Notifiable>();
            set => base.Set(ref value);
        }
    }

    public class Notifiable2: INotifiable
    {
        private DelegatedNotifier _surrogate;

        public Notifiable2()
        {
            _surrogate = new DelegatedNotifier(this);
        }


        [NotifiedBy(nameof(FirstName), nameof(LastName))]
        public string FullName => $"{FirstName} {LastName}".Trim();

        public string FirstName
        {
            get => _surrogate.Get<string>();
            set => _surrogate.Set(ref value);
        }
        public string LastName
        {
            get => _surrogate.Get<string>();
            set => _surrogate.Set(ref value);
        }
        public DateTimeOffset DateOfBirth
        {
            get => _surrogate.Get<DateTimeOffset>();
            set => _surrogate.Set(ref value);
        }
        public double Weight
        {
            get => _surrogate.Get<double>();
            set => _surrogate.Set(ref value);
        }

        public void SetAttached<V>(string property, V value) => _surrogate.Set(ref value, property);
        public V GetAttached<V>(string property) => _surrogate.Get<V>(property);

        public event PropertyChangedEventHandler PropertyChanged
        {
            add => _surrogate.PropertyChanged += value;
            remove => _surrogate.PropertyChanged -= value;
        }

        public void Notify([CallerMemberName] string propertyName = null) => _surrogate.Notify(propertyName);

        public void Notify(Expression<Func<object>> exp) => _surrogate.Notify(exp);
    }
}
