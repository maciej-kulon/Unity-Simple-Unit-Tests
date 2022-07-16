using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Reflection;
using _Scripts.GameManagement;
using NUnit.Framework;
using PlasticGui.WorkspaceWindow.BranchExplorer;
using SimpleTests;
using UnityEditor;
using UnityEditor.Graphs;
using UnityEditor.VersionControl;
using UnityEngine;
using UnityEngine.Purchasing.MiniJSON;
using Assert = UnityEngine.Assertions.Assert;
using Object = System.Object;
using TestCaseAttribute = SimpleTests.TestCaseAttribute;

public class TestRunner : EditorWindow
{
    private List<Type> TestClasses = new List<Type>();
    private List<TestClassAttribute> TestResults = new List<TestClassAttribute>();

    [MenuItem("Window/MK Test runner")]
    static void Init()
    {
        TestRunner window = (TestRunner) EditorWindow.GetWindow(typeof(TestRunner));
        window.Show();
        window.titleContent = new GUIContent("MK Simple Test Runner");
    }


    private void FindTestClasses()
    {
        TestClasses.Clear();
        var assembly = Assembly.Load("Assembly-CSharp");
        foreach (var type in assembly.ExportedTypes)
        {
            if (type.GetCustomAttribute(typeof(TestClassAttribute)) is TestClassAttribute)
            {
                TestClasses.Add(type);
            }
        }
    }

    string exceptionDetails = string.Empty;
    string assertionDetails = string.Empty;
    Vector2 scrollPosition = Vector2.zero;

    private void OnGUI()
    {
        if (GUILayout.Button("Run tests", GUILayout.MaxWidth(EditorGUIUtility.currentViewWidth - 20)))
        {
            FindTestClasses();
            exceptionDetails = string.Empty;
            assertionDetails = string.Empty;
            TestResults = RunTests();
        }

        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        var color = GUI.color;
        foreach (var testClass in TestResults)
        {
            GUI.color = testClass.Passed ? Color.green : Color.yellow;
            // EditorGUILayout.LabelField("TEST __" + testClass.Name + "__");
            var passed = testClass.TestCasesResults.Count(s => s.Passed);
            var failed = testClass.TestCasesResults.Count - passed;
            testClass.expandGroup = EditorGUILayout.Foldout(testClass.expandGroup,
                $"{testClass.Name} Passed: {passed} Failed: {failed}", true);
            
            if (!testClass.expandGroup) continue;
            EditorGUI.indentLevel++;
            foreach (var testCase in testClass.TestCasesResults)
            {
                EditorGUILayout.BeginHorizontal();
                if (testCase.AssertionDetails.IsNotEmpty())
                {
                    GUI.color = Color.white;
                    if (testCase.AssertionDetails.IsNotEmpty())
                    {
                        if (GUILayout.Button("Details", GUILayout.Width(50)))
                        {
                            assertionDetails = testCase.AssertionDetails;
                        }
                    }
                }

                GUI.color = testCase.Passed ? Color.green : Color.yellow;
                EditorGUILayout.LabelField("CASE __" + testCase.Name + "__");
                EditorGUILayout.EndHorizontal();

                if (!testCase.ErrorMessage.IsNotEmpty()) continue;
                GUI.color = Color.white;
                EditorGUILayout.BeginHorizontal();
                EditorGUI.indentLevel++;
                if (testCase.ExceptionDetails.IsNotEmpty())
                {
                    if (GUILayout.Button("Error", GUILayout.Width(50)))
                    {
                        exceptionDetails = testCase.ExceptionDetails;
                    }
                }

                EditorGUILayout.LabelField(testCase.ErrorMessage);
                EditorGUI.indentLevel--;
                EditorGUILayout.EndHorizontal();
            }

            EditorGUI.indentLevel--;
            EditorGUILayout.Separator();
            EditorGUILayout.Space();
        }

        if (exceptionDetails.IsNotEmpty())
        {
            GUI.color = Color.yellow;
            EditorGUILayout.HelpBox(exceptionDetails, MessageType.Info);
        }

        if (assertionDetails.IsNotEmpty())
        {
            GUI.color = Color.white;
            EditorGUILayout.HelpBox(assertionDetails, MessageType.Info);
        }

        EditorGUILayout.EndScrollView();

        GUI.color = color;
    }

    private List<TestClassAttribute> RunTests()
    {
        var results = new List<TestClassAttribute>();
        foreach (var classTest in TestClasses)
        {
            var testClassAttribute = classTest.GetCustomAttribute(typeof(TestClassAttribute)) as TestClassAttribute;

            var setupMethods =
                classTest.GetMethods().Where(s => s.IsDefined(typeof(TestSetupAttribute))).ToHashSet();

            var cleanupMethods =
                classTest.GetMethods().Where(s => s.IsDefined(typeof(TestCleanupAttribute))).ToHashSet();

            var beforeEveryTest =
                classTest.GetMethods().Where(s => s.IsDefined(typeof(BeforeEveryTestAttribute))).ToHashSet();

            var afterEveryTest =
                classTest.GetMethods().Where(s => s.IsDefined(typeof(AfterEveryTestAttribute))).ToHashSet();

            var testMethods =
                classTest.GetMethods().Where(s => s.IsDefined(typeof(TestCaseAttribute))).ToHashSet();

            var testClassInstance = Activator.CreateInstance(classTest);

            RunPreparationMethodsSet(setupMethods, testClassInstance, testClassAttribute);
            RunTestCases(beforeEveryTest, afterEveryTest, testMethods, testClassInstance, testClassAttribute);
            RunPreparationMethodsSet(cleanupMethods, testClassInstance, testClassAttribute);

            results.Add(testClassAttribute);
        }

        return results;
    }

    private static void RunPreparationMethodsSet(HashSet<MethodInfo> setupMethods, object testClassInstance,
        TestClassAttribute testClassAttribute)
    {
        foreach (var setupMethod in setupMethods)
        {
            var setupAttribute = setupMethod.GetCustomAttribute(typeof(TestSetupAttribute)) as TestSetupAttribute;
            try
            {
                setupMethod.Invoke(testClassInstance, null);
            }
            catch (Exception ex)
            {
                Debug.LogError(
                    $"Something went wrong during Tests Setup: {testClassAttribute.Name} \n {(ex.InnerException != null ? ex.InnerException.Message : ex.Message)}");
            }
        }
    }

    public static Exception GetDeepestException(Exception ex)
    {
        return ex.InnerException != null ? GetDeepestException(ex.InnerException) : ex;
    }


    private static void RunTestCases(HashSet<MethodInfo> beforeMethods, HashSet<MethodInfo> afterMethods,
        HashSet<MethodInfo> testMethods, object testClassInstance,
        TestClassAttribute testClassAttribute)
    {
        foreach (var testMethod in testMethods)
        {
            var testCases = testMethod.GetCustomAttributes<TestCaseAttribute>();
            foreach (var testCaseAttribute in testCases)
            {
                try
                {
                    foreach (var mInfo in beforeMethods) mInfo.Invoke(testClassInstance, null);
                    object parameters = testCaseAttribute.Parameters;
                    if (parameters is object[] {Length: > 0} paramsArray)
                    {
                        var methodReturnValue = testMethod.Invoke(testClassInstance, paramsArray);
                        if (methodReturnValue is List<SimpleAssert> simpleAsserts)
                            for (var i = 0; i < simpleAsserts.Count; i++)
                                testCaseAttribute.AssertionDetails += $"{(i + 1)}. {simpleAsserts[i].Details}\n";
                        else if (methodReturnValue != testCaseAttribute.ExpectedResult)
                            throw new AssertionException(
                                $"Expected return value is not equal. Expected: {testCaseAttribute.ExpectedResult}, Returned: {methodReturnValue}");

                        testCaseAttribute.Passed = true;
                        testCaseAttribute.ErrorMessage = "";
                        testClassAttribute.TestCasesResults.Add(testCaseAttribute);
                    }
                    else
                    {
                        var methodReturnValue = testMethod.Invoke(testClassInstance, null);

                        if (methodReturnValue is List<SimpleAssert> simpleAsserts)
                            for (var i = 0; i < simpleAsserts.Count; i++)
                                testCaseAttribute.AssertionDetails += $"{(i + 1)}. {simpleAsserts[i].Details}\n";
                        else if (methodReturnValue != testCaseAttribute.ExpectedResult)
                            throw new AssertionException(
                                $"Expected return value is not equal. Expected: {testCaseAttribute.ExpectedResult}, Returned: {methodReturnValue}");

                        testCaseAttribute.Passed = true;
                        testCaseAttribute.ErrorMessage = "";
                        testClassAttribute.TestCasesResults.Add(testCaseAttribute);
                    }

                    foreach (var mInfo in afterMethods) mInfo.Invoke(testClassInstance, null);
                }
                catch (Exception ex1)
                {
                    if (GetDeepestException(ex1) is Assertion.AssertionException assertionException1)
                    {
                        testCaseAttribute.ErrorMessage = assertionException1.Message;
                        testClassAttribute.TestCasesResults.Add(testCaseAttribute);
                        try
                        {
                            foreach (var mInfo in afterMethods) mInfo.Invoke(testClassInstance, null);
                        }
                        catch (Exception ex2)
                        {
                            var deepestException = GetDeepestException(ex2);
                            testCaseAttribute.ErrorMessage = deepestException.Message;
                            testCaseAttribute.ExceptionDetails = deepestException.StackTrace;
                            testClassAttribute.TestCasesResults.Add(testCaseAttribute);
                        }
                    }
                    else
                    {
                        var deepestException = GetDeepestException(ex1);
                        testCaseAttribute.ErrorMessage =
                            "Unknown Exception (not AssertionException) occured.";
                        testCaseAttribute.ExceptionDetails =
                            "Following exception was thrown in your game code, not in Assertion methods chain.\n\n" +
                            deepestException.Message + "\n" + deepestException.StackTrace;
                        testClassAttribute.TestCasesResults.Add(testCaseAttribute);
                    }
                }
            }
        }
    }
}