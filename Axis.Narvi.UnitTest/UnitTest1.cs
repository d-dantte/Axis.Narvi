using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Axis.Narvi.Notify;
using System.ComponentModel;
using Axis.Narvi.Extensions;

namespace Axis.Narvi.UnitTest
{
    [TestClass]
    public class UnitTest1
    {
    
        public Notifiable[] xyz()
        {
            var n = new Notifiable();
            var n2 = new Notifiable();
            var n3 = new Notifiable();

            n.PropertyChanged += InstanceHandler;
            n.PropertyChanged += StaticHandler;

            n.PropertyChanged += new ManagedCallback<PropertyChangedEventHandler, PropertyChangedEventArgs>(InstanceHandler, _wcb => n.PropertyChanged -= _wcb.Invoke).Invoke;
            n.PropertyChanged += new ManagedCallback<PropertyChangedEventHandler, PropertyChangedEventArgs>(StaticHandler, _wcb => n.PropertyChanged -= _wcb.Invoke).Invoke;

            var shldnt = n2.NotifyFor("abc", (x, y) => Console.WriteLine("Shouldnt be called! " + y.PropertyName));

            var stuff = new Stuff();
            var should = n2.NotifyFor("Name", stuff.h1);
            var should2 = n2.NotifyFor("Name", new Stuff().h2);

            n2.Name = "John Skeet";

            should.Unsubscribe();
            Console.WriteLine("Unsubscribed");

            return new []{ n, n2};
        }


        [TestMethod]
        public void TestMethod1()
        {
            var nar = xyz();
            var n2 = nar[1];
            n2.Name = null;

            GC.Collect();
            Console.WriteLine("GC collected");

            n2.Name = "Elijah";
        }

        [TestMethod]
        public void TestMethod2()
        {
            var wr = call();
            var wr2 = new WeakReference(new object());

            Console.WriteLine("before collection, wr: "+wr.Target);
            Console.WriteLine("before collection, wr2: " + wr2.Target);

            GC.Collect();

            Console.WriteLine("after collection, wr: " + wr.Target);
            Console.WriteLine("after collection, wr2: " + wr2.Target);
        }

        public WeakReference call()
        {
            int p = 4;
            Action a = () => Console.WriteLine(p);
            Action b = () => a();
            var wr = new WeakReference(a.Target);


            GC.Collect();
            return wr;
        }

        public void InstanceHandler(object sender, PropertyChangedEventArgs arg)
        {
            Console.WriteLine("Instance handled");
        }
        public static void StaticHandler(object sender, PropertyChangedEventArgs arg)
        {
            Console.WriteLine("Statically handled");
        }
    }


    public class Stuff : NotifierBase
    {
        public void h1(object sender, PropertyChangedEventArgs arg) => Console.WriteLine("h1 " + arg.PropertyName);
        public void h2(object sender, PropertyChangedEventArgs arg) => Console.WriteLine("h2 " + arg.PropertyName);
    }
    public class Notifiable : NotifierBase
    {
        public string Name
        {
            get { return get<string>(); }
            set { set(ref value); }
        }
    }
}
