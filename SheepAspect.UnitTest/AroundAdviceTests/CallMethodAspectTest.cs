using System;
using System.Collections.Generic;
using FluentAssertions;
using NUnit.Framework;
using SheepAspect.AroundAdvising;
using SheepAspect.Core;
using SheepAspect.Helpers;
using SheepAspect.Pointcuts.Impl;
using SheepAspect.Runtime;
using SheepAspect.UnitTest.TestHelper;

namespace SheepAspect.UnitTest.AroundAdviceTests
{
    [TestFixture]
    public class CallMethodAspectTest : AspectTestBase
    {
        protected override Type TargetType()
        {
            return typeof(CallMethodTestTarget<int>);
        }

        protected override void SetupAspect(AspectDefinition aspect)
        {
            advice = null;
            var pointcut = CreatePointcuts<CallMethodPointcut>(aspect, "SheepPoint", 
                @"Method: ((Name: 'Substring' & InType: Name:'String') | InType:Name:'*Callee')
                & FromMethod:InType:Name:'CallMethodTestTarget*'");

            aspect.Advise(new AroundAdvice(pointcut, GetAspectMethod("MockAdvice")));

            var pointcut2 = CreatePointcuts<CallMethodPointcut>(aspect, "SheepPoint2",
                @"Method:Name:'Substring' 
                & FromMethod:Name:'FirstLetter_DoubleAdvised'");
            aspect.Advise(new AroundAdvice(pointcut2, GetAspectMethod("MockAdvice2")));
        }

        private static Func<CallMethodJointPoint, object> advice;
        public object MockAdvice(CallMethodJointPoint jp)
        {
            return advice(jp);
        }

        private static Func<CallMethodJointPoint, object> advice2;
        public object MockAdvice2(CallMethodJointPoint jp)
        {
            return advice2(jp);
        }

        [Assert]
        public void CanProceedAndIntercept()
        {
            advice = j => "advised " + j.Execute();
            Assert.AreEqual("Called advised A", target.FirstLetter("Abcde"));
        }

        [Assert]
        public void CanProceedWithAlteredArgs()
        {
            advice = j =>
            {
                j.Args[1] = 3;
                return j.Execute();
            };
            Assert.AreEqual("Called Abc", target.FirstLetter("Abcde"));
        }

        [Assert]
        public void CanProceedWithAlteredTarget()
        {
            advice = j =>
            {
                j.Target = "Mnopq";
                return j.Execute();
            };
            Assert.AreEqual("Called M", target.FirstLetter("Abcdef"));
        }

        [Assert]
        public void CanAccessGenericFromNestedClass()
        {
            advice = j =>
            {
                return "advised " + j.Execute();
            };
            Assert.AreEqual("Called advised Int32", target.CallParentGeneric());
        }
        
        [Assert]
        public void CanHandleOpenGenericMethod()
        {
            advice = j =>
            {
                j.Method.GetGenericArguments().Should().Equal(typeof(int), typeof(string));
                j.CallingMethod.GetGenericArguments().Should().Equal(typeof(int), typeof(string));
                return "advised " + j.Execute();
            };
            Assert.AreEqual("Called advised Int32,String", target.CallGeneric<int, string>());
        }

        [Assert]
        public void CanHandleNestedOpenGenericOnMethod()
        {
            advice = j =>
            {
                j.Method.GetGenericArguments().Should().Equal(typeof(IList<int>), typeof(IList<string>));
                j.CallingMethod.GetGenericArguments().Should().Equal(typeof(int), typeof(string));
                return "advised " + j.Execute();
            };
            Assert.AreEqual("Called advised IList`1,IList`1", target.CallGenericList<int, string>());
        }

        [Assert]
        public void CanHandleOpenGenericsArray()
        {
            advice = j =>
            {
                j.Method.GetGenericArguments().Should().Equal(typeof(int[]), typeof(string[]));
                j.CallingMethod.GetGenericArguments().Should().Equal(typeof(int), typeof(string));
                return "advised " + j.Execute();
            };
            Assert.AreEqual("Called advised Int32[],String[]", target.CallGenericArray<int, string>());
        }

        [Assert]
        public void CanPutMultipleAdvicesOnSameJoinPoint()
        {
            advice = j => "<" + j.Execute() + ">";
            advice2 = j => "(" + j.Execute() + ")";

            Assert.AreEqual("Called (<A>)", target.FirstLetter_DoubleAdvised("Abcde"));
        }
    }

    public class CallMethodTestTarget<TClass>
    {
        public string FirstLetter(string something)
        {
            return "Called " + something.Substring(0, 1);
        }

        public string CallGeneric<T, Y>()
        {
            return "Called " + Callee.Generic<T, Y>();
        }

        public string CallGenericList<T, Y>()
        {
            return "Called " + Callee.Generic<IList<T>, IList<Y>>();
        }

        public string CallGenericArray<T, Y>()
        {
            return "Called " + Callee.Generic<T[], Y[]>();
        }

        public string CallParentGeneric()
        {
            return "Called " + Callee.ParentGeneric();
        }

        public string FirstLetter_DoubleAdvised(string something)
        {
            return "Called " + something.Substring(0, 1);
        }

        public class Callee
        {
            public static string ParentGeneric()
            {
                return typeof(TClass).Name;
            }

            public static string Generic<T, Y>()
            {
                return "{0},{1}".FormatWith(typeof (T).Name, typeof (Y).Name);
            }
        }
    }
}