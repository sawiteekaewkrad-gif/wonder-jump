// Tiny NUnit subset with real assertion behaviour, so the pure EditMode tests can execute outside Unity.
using System;
using System.Collections;
using System.Linq;

namespace NUnit.Framework
{
    [AttributeUsage(AttributeTargets.Method)] public sealed class TestAttribute : Attribute { }
    [AttributeUsage(AttributeTargets.Method)] public sealed class SetUpAttribute : Attribute { }
    [AttributeUsage(AttributeTargets.Method)] public sealed class TearDownAttribute : Attribute { }

    public sealed class AssertionException : Exception { public AssertionException(string m) : base(m) { } }
    public sealed class IgnoreException : Exception { public IgnoreException(string m) : base(m) { } }

    public interface IResolveConstraint { bool Matches(object actual, out string description); }

    public sealed class RangeConstraint : IResolveConstraint
    {
        private readonly double lo, hi;
        public RangeConstraint(double lo, double hi) { this.lo = lo; this.hi = hi; }
        public bool Matches(object actual, out string d)
        {
            double v = Convert.ToDouble(actual);
            d = "in range [" + lo + ", " + hi + "] but was " + v;
            return v >= lo && v <= hi;
        }
    }

    public static class Is
    {
        public static RangeConstraint InRange(double lo, double hi) { return new RangeConstraint(lo, hi); }
    }

    public static class Assert
    {
        private static void Fail(string what, string msg) { throw new AssertionException(what + (msg != null ? " | " + msg : "")); }
        public static void IsTrue(bool c, string msg = null) { if (!c) Fail("Expected true", msg); }
        public static void IsFalse(bool c, string msg = null) { if (c) Fail("Expected false", msg); }
        public static void IsNull(object o, string msg = null) { if (!IsNullLike(o)) Fail("Expected null", msg); }
        public static void IsNotNull(object o, string msg = null) { if (IsNullLike(o)) Fail("Expected not null", msg); }
        public static void AreEqual(object e, object a, string msg = null) { if (!Equals(e, a) && !NumEq(e, a)) Fail("Expected " + e + " but was " + a, msg); }
        public static void AreEqual(float e, float a, float tol, string msg = null) { if (Math.Abs(e - a) > tol) Fail("Expected " + e + " ± " + tol + " but was " + a, msg); }
        public static void Greater(float a, float b, string msg = null) { if (!(a > b)) Fail(a + " not > " + b, msg); }
        public static void Greater(int a, int b, string msg = null) { if (!(a > b)) Fail(a + " not > " + b, msg); }
        public static void GreaterOrEqual(int a, int b, string msg = null) { if (!(a >= b)) Fail(a + " not >= " + b, msg); }
        public static void That(object actual, IResolveConstraint c, string msg = null) { string d; if (!c.Matches(actual, out d)) Fail("Expected " + d, msg); }
        public static void Ignore(string msg) { throw new IgnoreException(msg); }

        private static bool IsNullLike(object o)
        {
            var u = o as UnityEngine.Object;
            return o == null || (u != null && !u);
        }
        private static bool NumEq(object e, object a)
        {
            if (e == null || a == null) return false;
            if (!(e is IConvertible) || !(a is IConvertible) || e is string || a is string || e is Enum || a is Enum) return false;
            try { return Convert.ToDouble(e) == Convert.ToDouble(a); } catch { return false; }
        }
    }

    public static class CollectionAssert
    {
        public static void AreEqual(IEnumerable e, IEnumerable a, string msg = null)
        {
            if (!e.Cast<object>().SequenceEqual(a.Cast<object>())) throw new AssertionException("Collections differ " + msg);
        }
        public static void AreEquivalent(IEnumerable e, IEnumerable a, string msg = null)
        {
            var x = e.Cast<object>().OrderBy(o => o.ToString()).ToList();
            var y = a.Cast<object>().OrderBy(o => o.ToString()).ToList();
            if (!x.SequenceEqual(y)) throw new AssertionException("Collections not equivalent " + msg);
        }
        public static void Contains(IEnumerable c, object item, string msg = null)
        {
            if (!c.Cast<object>().Contains(item)) throw new AssertionException("Missing " + item + " " + msg);
        }
    }
}
