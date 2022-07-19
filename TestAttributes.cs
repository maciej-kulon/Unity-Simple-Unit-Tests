using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace SimpleTests
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class TestClassAttribute : Attribute
    {
        public readonly string Name;
        public readonly string SceneName;
        public bool ExpandGroup;
        public long ElapsedTime;
        
        public bool Passed
        {
            get { return TestCasesResults.All(s => s.Passed); }
        }

        public List<TestCaseAttribute> TestCasesResults;

        public TestClassAttribute(string testGroupName, string runOnSceneLoadName = "")
        {
            Name = testGroupName;
            SceneName = runOnSceneLoadName;
            TestCasesResults = new List<TestCaseAttribute>();
        }
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class TestCaseAttribute : Attribute
    {
        public object ExpectedResult;
        public object[] Parameters;
        public bool Passed;
        public string ErrorMessage;
        public string ExceptionDetails;
        public string AssertionDetails;
        public long ElapsedTime;
        public string Name;
        public MethodInfo Method;

        public TestCaseAttribute(string name, object expected = null, params object[] parameters )
        {
            Name = name;
            Parameters = parameters;
            ExpectedResult = expected;
        }
    }




    public class TestSetupAttribute : Attribute
    {
    }

    public class TestCleanupAttribute : Attribute
    {
    }

    public class BeforeEveryTestAttribute : Attribute
    {
        public BeforeEveryTestAttribute()
        {
        }
    }

    public class AfterEveryTestAttribute : Attribute
    {
        public AfterEveryTestAttribute()
        {
        }
    }
}