using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Transactions;
using UnityEngine;
using Object = System.Object;

public class SimpleAssert
{
    public object Value;
    public bool Succeed;
    public string FailMessage;
    public string Details;
    public string PreviousAssertionName;
    public dynamic FromPreviousAssertion;

    public SimpleAssert(object value)
    {
        Value = value;
        Succeed = true;
        FailMessage = "";
    }
}

public static class Assertion
{
    public class AssertionException : Exception
    {
        public AssertionException(string msg) : base(msg)
        {
        }
    }

    public static SimpleAssert Create(object value)
    {
        return new SimpleAssert(value);
    }

    public static SimpleAssert Create(Action value)
    {
        return new SimpleAssert(value);
    }

    public static SimpleAssert AddDetails(this SimpleAssert sa, string details)
    {
        sa.Details += details + "\n";
        return sa;
    }

    public static SimpleAssert IsEqual(this SimpleAssert sa, object value)
    {
        if (!sa.Succeed)
            throw new AssertionException(sa.FailMessage);
        sa.PreviousAssertionName = MethodBase.GetCurrentMethod().Name;
        if (sa.Value.Equals(value)) return sa;
        sa.Succeed = false;
        sa.FailMessage = $"IsEqual: {sa.Value} != {value}";

        return sa;
    }

    public static SimpleAssert IsNotEqual(this SimpleAssert sa, object value)
    {
        if (!sa.Succeed)
            throw new AssertionException(sa.FailMessage);
        sa.PreviousAssertionName = MethodBase.GetCurrentMethod().Name;
        if (sa.Value.Equals(value))
        {
            sa.Succeed = false;
            sa.FailMessage = $"IsNotEqual: {sa.Value} == {value}";
        }

        return sa;
    }

    public static SimpleAssert IsGreaterThan(this SimpleAssert sa, object value)
    {
        if (!sa.Succeed)
            throw new AssertionException(sa.FailMessage);
        sa.PreviousAssertionName = MethodBase.GetCurrentMethod().Name;
        var value1 = Convert.ToDouble(sa.Value);
        var value2 = Convert.ToDouble(value);

        if (value1 > value2)
            return sa;

        sa.Succeed = false;
        sa.FailMessage = $"IsGreaterThan: {value1} > {value2}";
        return sa;
    }

    public static SimpleAssert IsGreaterThanOrEqual(this SimpleAssert sa, object value)
    {
        if (!sa.Succeed)
            throw new AssertionException(sa.FailMessage);
        sa.PreviousAssertionName = MethodBase.GetCurrentMethod().Name;
        var value1 = Convert.ToDouble(sa.Value);
        var value2 = Convert.ToDouble(value);

        if (value1 >= value2)
            return sa;

        sa.FailMessage = $"IsGreaterThanOrEqual: {sa.Value} < {value}";
        sa.Succeed = false;
        return sa;
    }

    public static SimpleAssert IsLessThan(this SimpleAssert sa, object value)
    {
        if (!sa.Succeed)
            throw new AssertionException(sa.FailMessage);
        sa.PreviousAssertionName = MethodBase.GetCurrentMethod().Name;
        var value1 = Convert.ToDouble(sa.Value);
        var value2 = Convert.ToDouble(value);

        if (value1 < value2)
            return sa;

        sa.Succeed = false;
        sa.FailMessage = $"IsLessThan: {sa.Value} > {value}";
        return sa;
    }

    public static SimpleAssert IsLessThanOrEqual(this SimpleAssert sa, object value)
    {
        if (!sa.Succeed)
            throw new AssertionException(sa.FailMessage);
        sa.PreviousAssertionName = MethodBase.GetCurrentMethod().Name;
        var value1 = Convert.ToDouble(sa.Value);
        var value2 = Convert.ToDouble(value);

        if (value1 <= value2)
            return sa;

        sa.Succeed = false;
        sa.FailMessage = $"IsLessThanOrEqual: {sa.Value} > {value}";
        return sa;
    }

    public static SimpleAssert IsOfType(this SimpleAssert sa, Type value)
    {
        if (!sa.Succeed)
            throw new AssertionException(sa.FailMessage);
        sa.PreviousAssertionName = MethodBase.GetCurrentMethod().Name;
        if (sa.Value.GetType() == value) return sa;
        sa.Succeed = false;
        sa.FailMessage = $"IsOfType: {sa.Value.GetType()} is not {value}";

        return sa;
    }

    public static SimpleAssert InstanceOfType(this SimpleAssert sa, Type value)
    {
        if (!sa.Succeed)
            throw new AssertionException(sa.FailMessage);
        sa.PreviousAssertionName = MethodBase.GetCurrentMethod().Name;
        if (value.IsInstanceOfType(sa.Value))
            return sa;

        sa.Succeed = false;
        sa.FailMessage = $"InstanceOfType: {sa.Value.GetType()} does not inherit from {value}";

        return sa;
    }

    public static SimpleAssert IsNotOfType(this SimpleAssert sa, object value)
    {
        if (!sa.Succeed)
            throw new AssertionException(sa.FailMessage);
        sa.PreviousAssertionName = MethodBase.GetCurrentMethod().Name;
        if (sa.Value.GetType() == value.GetType())
        {
            sa.Succeed = false;
            sa.FailMessage = $"IsNotOfType: {sa.Value.GetType()} is {value.GetType()}";
        }

        return sa;
    }

    public static SimpleAssert IsReferenceEqual(this SimpleAssert sa, object value)
    {
        if (!sa.Succeed)
            throw new AssertionException(sa.FailMessage);

        sa.PreviousAssertionName = MethodBase.GetCurrentMethod().Name;
        if (!ReferenceEquals(sa.Value, value))
        {
            sa.Succeed = false;
            sa.FailMessage = $"IsReferenceEqual: {sa} is not value";
        }

        return sa;
    }

    public static SimpleAssert IsNotReferenceEqual(this SimpleAssert sa, object value)
    {
        if (!sa.Succeed)
            throw new AssertionException(sa.FailMessage);
        sa.PreviousAssertionName = MethodBase.GetCurrentMethod().Name;
        if (ReferenceEquals(sa.Value, value))
        {
            sa.Succeed = false;
            sa.FailMessage = $"IsNotReferenceEqual: {sa} is value";
        }

        return sa;
    }

    public static SimpleAssert And(this SimpleAssert sa1, SimpleAssert sa2)
    {
        sa1.PreviousAssertionName = MethodBase.GetCurrentMethod().Name;
        if (sa1.Succeed && sa2.Succeed)
            return sa1;
        sa1.FailMessage =
            $"And: One or more assertions failed. Left assertion Succeed: {sa1.Succeed} {sa1.FailMessage}, Parameter assertion Succeed: {sa2.Succeed} {sa2.FailMessage}";
        sa1.Succeed = false;
        return sa1;
    }

    public static SimpleAssert Or(this SimpleAssert sa1, SimpleAssert sa2)
    {
        if (sa1.Succeed)
        {
            sa1.PreviousAssertionName = MethodBase.GetCurrentMethod().Name;
            return sa1;
        }

        if (sa2.Succeed)
        {
            sa2.PreviousAssertionName = MethodBase.GetCurrentMethod().Name;
            return sa2;
        }

        return sa1;
    }

    public static SimpleAssert Not(this SimpleAssert sa)
    {
        if (sa.Succeed)
        {
            sa.Succeed = false;
            sa.FailMessage = $"Not: Assertion with value {sa.Value}";
        }
        else
        {
            sa.Succeed = true;
            sa.FailMessage = "";
        }

        return sa;
    }


    public static SimpleAssert ShouldContains<T>(this SimpleAssert sa, object value)
        where T : IEnumerable<object>, ICollection
    {
        if (!sa.Succeed)
            throw new AssertionException(sa.FailMessage);
        var collection = ((T) sa.Value).ToList();
        sa.PreviousAssertionName = MethodBase.GetCurrentMethod().Name;
        if (collection.Contains(value)) return sa;
        sa.Succeed = false;
        sa.FailMessage = $"Contains: Collection {collection} does not contains {value}";
        return sa;
    }


    public static SimpleAssert LengthEquals(this SimpleAssert sa, object value)
    {
        if (!sa.Succeed)
            throw new AssertionException(sa.FailMessage);
        sa.PreviousAssertionName = MethodBase.GetCurrentMethod().Name;
        var l = Convert.ToInt64(value);

        switch (sa.Value)
        {
            case IEnumerable<object> enumerable:
            {
                var count = enumerable.Count();
                if (count == l) return sa;
                sa.Succeed = false;
                sa.FailMessage = $"LengthNotEqual: Collection {enumerable} length is not equal {value}";
                break;
            }
            case ICollection collection:
            {
                var count = collection.Count;
                if (count == l) return sa;
                sa.Succeed = false;
                sa.FailMessage = $"LengthNotEqual: Collection {collection} length is not equal {value}";
                break;
            }
            default:
                sa.Succeed = false;
                sa.FailMessage = $"LengthNotEqual: Parameter {sa.Value} is neither IEnumerable nor ICollection type.";
                break;
        }

        return sa;
    }

    public static SimpleAssert LengthNotEqual(this SimpleAssert sa, object value)
    {
        if (!sa.Succeed)
            throw new AssertionException(sa.FailMessage);
        sa.PreviousAssertionName = MethodBase.GetCurrentMethod().Name;
        var l = Convert.ToInt64(value);

        switch (sa.Value)
        {
            case IEnumerable<object> enumerable:
            {
                var count = enumerable.Count();
                if (count != l) return sa;
                sa.Succeed = false;
                sa.FailMessage = $"LengthNotEqual: Collection {enumerable} length is equal {value}";
                break;
            }
            case ICollection collection:
            {
                var count = collection.Count;
                if (count != l) return sa;
                sa.Succeed = false;
                sa.FailMessage = $"LengthNotEqual: Collection {collection} length is equal {value}";
                break;
            }
            default:
                sa.Succeed = false;
                sa.FailMessage = $"LengthNotEqual: Parameter {sa.Value} is neither IEnumerable nor ICollection type.";
                break;
        }

        return sa;
    }

    public static SimpleAssert LengthGreaterThan(this SimpleAssert sa, object value)
    {
        if (!sa.Succeed)
            throw new AssertionException(sa.FailMessage);
        sa.PreviousAssertionName = MethodBase.GetCurrentMethod().Name;
        var l = Convert.ToInt64(value);

        switch (sa.Value)
        {
            case IEnumerable<object> enumerable:
            {
                var count = enumerable.Count();
                if (count > l) return sa;
                sa.Succeed = false;
                sa.FailMessage = $"LengthGreaterThan: Collection {enumerable} length is not greater {value}";
                break;
            }
            case ICollection collection:
            {
                var count = collection.Count;
                if (count > l) return sa;
                sa.Succeed = false;
                sa.FailMessage = $"LengthGreaterThan: Collection {collection} length is not greater {value}";
                break;
            }
            default:
                sa.Succeed = false;
                sa.FailMessage =
                    $"LengthGreaterThan: Parameter {sa.Value} is neither IEnumerable nor ICollection type.";
                break;
        }

        return sa;
    }


    public static SimpleAssert LengthLessThan(this SimpleAssert sa, object value)
    {
        if (!sa.Succeed)
            throw new AssertionException(sa.FailMessage);
        sa.PreviousAssertionName = MethodBase.GetCurrentMethod().Name;
        var l = Convert.ToInt64(value);

        switch (sa.Value)
        {
            case IEnumerable<object> enumerable:
            {
                var count = enumerable.Count();
                if (count < l) return sa;
                sa.Succeed = false;
                sa.FailMessage = $"LengthLessThan: Collection {enumerable} length is not less than {value}";
                break;
            }
            case ICollection collection:
            {
                var count = collection.Count;
                if (count < l) return sa;
                sa.Succeed = false;
                sa.FailMessage = $"LengthLessThan: Collection {collection} length is not less than {value}";
                break;
            }
            default:
                sa.Succeed = false;
                sa.FailMessage = $"LengthLessThan: Parameter {sa.Value} is neither IEnumerable nor ICollection type.";
                break;
        }

        return sa;
    }

    public static SimpleAssert LengthGreaterOrEqual(this SimpleAssert sa, object value)
    {
        if (!sa.Succeed)
            throw new AssertionException(sa.FailMessage);
        sa.PreviousAssertionName = MethodBase.GetCurrentMethod().Name;
        var l = Convert.ToInt64(value);

        switch (sa.Value)
        {
            case IEnumerable<object> enumerable:
            {
                var count = enumerable.Count();
                if (count >= l) return sa;
                sa.Succeed = false;
                sa.FailMessage =
                    $"LengthGreaterOrEqual: Collection {enumerable} length is neither greater nor equal {value}";
                break;
            }
            case ICollection collection:
            {
                var count = collection.Count;
                if (count >= l) return sa;
                sa.Succeed = false;
                sa.FailMessage =
                    $"LengthGreaterOrEqual: Collection {collection} length is neither greater nor equal {value}";
                break;
            }
            default:
                sa.Succeed = false;
                sa.FailMessage =
                    $"LengthGreaterOrEqual: Parameter {sa.Value} is neither IEnumerable nor ICollection type.";
                break;
        }

        return sa;
    }


    public static SimpleAssert LengthLessThanOrEqual(this SimpleAssert sa, object value)
    {
        if (!sa.Succeed)
            throw new AssertionException(sa.FailMessage);
        sa.PreviousAssertionName = MethodBase.GetCurrentMethod().Name;
        var l = Convert.ToInt64(value);

        switch (sa.Value)
        {
            case IEnumerable<object> enumerable:
            {
                var count = enumerable.Count();
                if (count <= l) return sa;
                sa.Succeed = false;
                sa.FailMessage =
                    $"LengthLessThan: Collection {enumerable} length is neither less than nor equal{value}";
                break;
            }
            case ICollection collection:
            {
                var count = collection.Count;
                if (count <= l) return sa;
                sa.Succeed = false;
                sa.FailMessage =
                    $"LengthLessThan: Collection {collection} length is neither less than nor equal {value}";
                break;
            }
            default:
                sa.Succeed = false;
                sa.FailMessage = $"LengthLessThan: Parameter {sa.Value} is neither IEnumerable nor ICollection type.";
                break;
        }

        return sa;
    }

    public static SimpleAssert StringContains(this SimpleAssert sa, object value)
    {
        if (!sa.Succeed)
            throw new AssertionException(sa.FailMessage);
        sa.PreviousAssertionName = MethodBase.GetCurrentMethod().Name;
        if (!sa.Value.ToString().Contains(value.ToString()))
        {
            sa.Succeed = false;
            sa.FailMessage = $"StringContains: String {sa.Value} does not contains string {value}";
        }

        return sa;
    }


    public static SimpleAssert ThrowsException(this SimpleAssert sa, Type ofType = null)
    {
        if (!sa.Succeed)
            throw new AssertionException(sa.FailMessage);
        sa.PreviousAssertionName = MethodBase.GetCurrentMethod().Name;
        try
        {
            if (sa.Value is Action method)
            {
                method.Invoke();
            }
            else
            {
                sa.FailMessage = $"ThrowsException: Value used in Assertion.Create() is not a method function.";
                sa.Succeed = false;
                return sa;
            }

            sa.FailMessage = $"ThrowsException: Method did not throw any exception";
            sa.Succeed = false;
            return sa;
        }
        catch (Exception ex)
        {
            if (ofType == null) return sa;
            if (ex.GetType() == ofType) return sa;
            sa.FailMessage =
                $"ThrowsException: Method thrown an exception, but of type {ex.GetType()} instead of {ofType} ";
            sa.Succeed = false;
            return sa;
        }
    }

    public static SimpleAssert HasMember(this SimpleAssert sa, string name)
    {
        if (!sa.Succeed)
            throw new AssertionException(sa.FailMessage);
        sa.PreviousAssertionName = MethodBase.GetCurrentMethod().Name;

        var memberInfos = sa.Value.GetType()
            .GetMembers(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

        var member = memberInfos.ToList().Find(s => s.Name == name);

        if (member != null)
        {
            sa.FromPreviousAssertion = member;
            return sa;
        }

        sa.FailMessage = $"Object {sa.Value} does not have member with name {name}";
        sa.Succeed = false;
        return sa;
    }

    public static SimpleAssert IsMethod(this SimpleAssert sa)
    {
        if (!sa.Succeed)
            throw new AssertionException(sa.FailMessage);

        var member = (MemberInfo) sa.FromPreviousAssertion;
        if (member != null)
        {
            if (member.MemberType != MemberTypes.Method)
            {
                sa.FailMessage = $"Member {member} from object {sa.Value} is not a method.";
                sa.Succeed = false;
                sa.PreviousAssertionName = MethodBase.GetCurrentMethod().Name;
                return sa;
            }

            sa.FromPreviousAssertion = sa.Value.GetType().GetMethod(member.Name);
        }
        else
        {
            if (sa.Value.GetType().MemberType != MemberTypes.Field)
            {
                sa.FailMessage = $"{sa.Value} is not a method.";
                sa.Succeed = false;
                sa.PreviousAssertionName = MethodBase.GetCurrentMethod().Name;
                return sa;
            }

            sa.FromPreviousAssertion = sa.Value;
        }

        sa.PreviousAssertionName = MethodBase.GetCurrentMethod().Name;
        return sa;
    }

    public static SimpleAssert IsProperty(this SimpleAssert sa)
    {
        if (!sa.Succeed)
            throw new AssertionException(sa.FailMessage);
        var member = (MemberInfo) sa.FromPreviousAssertion;
        if (member != null)
        {
            if (member.MemberType != MemberTypes.Property)
            {
                sa.FailMessage = $"Member {member} from object {sa.Value} is not a property.";
                sa.Succeed = false;
                sa.PreviousAssertionName = MethodBase.GetCurrentMethod().Name;
                return sa;
            }

            sa.FromPreviousAssertion = sa.Value.GetType().GetProperty(member.Name);
        }
        else
        {
            if (sa.Value.GetType().MemberType != MemberTypes.Property)
            {
                sa.FailMessage = $"{sa.Value} is not a property.";
                sa.Succeed = false;
                sa.PreviousAssertionName = MethodBase.GetCurrentMethod().Name;
                return sa;
            }

            sa.FromPreviousAssertion = sa.Value;
        }

        sa.PreviousAssertionName = MethodBase.GetCurrentMethod().Name;
        return sa;
    }

    public static SimpleAssert IsField(this SimpleAssert sa)
    {
        if (!sa.Succeed)
            throw new AssertionException(sa.FailMessage);

        var member = (MemberInfo) sa.FromPreviousAssertion;
        if (member != null)
        {
            if (member.MemberType != MemberTypes.Field)
            {
                sa.FailMessage = $"Member {member} from object {sa.Value} is not a field.";
                sa.Succeed = false;
                sa.PreviousAssertionName = MethodBase.GetCurrentMethod().Name;
                return sa;
            }

            sa.FromPreviousAssertion = sa.Value.GetType().GetField(member.Name);
        }
        else
        {
            if (sa.Value.GetType().MemberType != MemberTypes.Field)
            {
                sa.FailMessage = $"{sa.Value} is not a field.";
                sa.Succeed = false;
                sa.PreviousAssertionName = MethodBase.GetCurrentMethod().Name;
                return sa;
            }

            sa.FromPreviousAssertion = sa.Value;
        }

        sa.PreviousAssertionName = MethodBase.GetCurrentMethod().Name;
        return sa;
    }


    public static SimpleAssert OfType(this SimpleAssert sa, Type ofType)
    {
        if (!sa.Succeed)
            throw new AssertionException(sa.FailMessage);

        var allowedAssertions = new List<string>() {nameof(IsField), nameof(IsProperty)};
        var currentMethodName = MethodBase.GetCurrentMethod().Name;

        if (!allowedAssertions.Contains(sa.PreviousAssertionName))
        {
            sa.Succeed = false;
            sa.Details = $"{currentMethodName} has to be after one of the following " +
                         $"assertions: {string.Join(", ", allowedAssertions)}, was {sa.PreviousAssertionName}";
            sa.FailMessage = $"Wrong assertions order. {sa.Details}";
        }
        else
        {
            switch (sa.FromPreviousAssertion)
            {
                case FieldInfo fieldInfo:
                    if (fieldInfo.FieldType != ofType)
                    {
                        sa.FailMessage = $"Field {fieldInfo.Name} is {fieldInfo.FieldType}, expected: {ofType}";
                        sa.Succeed = false;
                        return sa;
                    }

                    break;
                case PropertyInfo propertyInfo:
                    if (propertyInfo.PropertyType != ofType)
                    {
                        sa.FailMessage = $"Property {propertyInfo.Name} is {propertyInfo.PropertyType} expected of {ofType}";
                        sa.Succeed = false;
                        return sa;
                    }

                    break;
            }
        }

        sa.PreviousAssertionName = MethodBase.GetCurrentMethod().Name;
        return sa;
    }

    public static SimpleAssert ReturnsType(this SimpleAssert sa, Type returnType)
    {
        if (!sa.Succeed)
            throw new AssertionException(sa.FailMessage);

        var allowedAssertions = new List<string>() {nameof(IsMethod), nameof(IsProperty), nameof(IsField)};
        var currentMethodName = MethodBase.GetCurrentMethod().Name;

        if (!allowedAssertions.Contains(sa.PreviousAssertionName))
        {
            sa.Succeed = false;
            sa.Details = $"{currentMethodName} has to be after one of the following " +
                         $"assertions: {string.Join(", ", allowedAssertions)}, was {sa.PreviousAssertionName}";
            sa.FailMessage = $"Wrong assertions order. {sa.Details}";
        }
        else
        {
            switch (sa.FromPreviousAssertion)
            {
                case MethodInfo methodInfo:
                    if (methodInfo.ReturnType != returnType)
                    {
                        sa.FailMessage =
                            $"Method {methodInfo.Name} returns {methodInfo.ReturnType} instead of {returnType}";
                        sa.Succeed = false;
                        return sa;
                    }

                    break;
                case PropertyInfo propertyInfo:
                    if (propertyInfo.PropertyType != returnType)
                    {
                        sa.FailMessage =
                            $"Property {propertyInfo.Name} returns {propertyInfo.PropertyType} instead of {returnType}";
                        sa.Succeed = false;
                        return sa;
                    }

                    break;
                case FieldInfo fieldInfo:
                    if (fieldInfo.FieldType != returnType)
                    {
                        sa.FailMessage =
                            $"Field {fieldInfo.Name} returns {fieldInfo.FieldType} instead of {returnType}";
                        sa.Succeed = false;
                        return sa;
                    }

                    break;
            }
        }

        sa.PreviousAssertionName = MethodBase.GetCurrentMethod().Name;
        return sa;
    }

    public static SimpleAssert End(this SimpleAssert sa, string additionalErrorMessage = "")
    {
        if (!sa.Succeed)
            throw new AssertionException(additionalErrorMessage + " :: " + sa.FailMessage);
        return sa;
    }
}