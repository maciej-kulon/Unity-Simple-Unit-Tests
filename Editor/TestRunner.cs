using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using SimpleTests;
using SimpleTests.Extensions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Debug = UnityEngine.Debug;
using TestCaseAttribute = SimpleTests.TestCaseAttribute;

public class TestRunner : EditorWindow
{
    private List<TestClassAttribute> _testResults = new();

    private TestCaseAttribute _detailsCase;
    private Vector2 _scrollPosition = Vector2.zero;
    private bool _runTestOnSceneOpenedEvent;
    private bool _runTestOnSceneOpenedSubscribed;

    [MenuItem("Window/MK Test runner")]
    private static void Init()
    {
        var window = (TestRunner) EditorWindow.GetWindow(typeof(TestRunner));
        window.Show();
        window.titleContent = new GUIContent("MK Simple Test Runner");
        FindTestGroups();
    }


    private static EditorSceneManager.SceneOpenedCallback EditorSceneManagerOnSceneOpened(TestRunner window)
    {
        return (scene, mode) =>
        {
            window._detailsCase = null;
            window._testResults = RunTests(FindTestGroups(), scene.name);
        };
    }


    private static List<Type> FindTestGroups()
    {
        var assembly = Assembly.Load("Assembly-CSharp");
        return assembly.ExportedTypes
            .Where(type => type.GetCustomAttribute(typeof(TestClassAttribute)) is TestClassAttribute).ToList();
    }


    private void OnGUI()
    {
        _runTestOnSceneOpenedEvent =
            EditorGUILayout.ToggleLeft("Run tests when scene is opened", _runTestOnSceneOpenedEvent);

        HandleAutoTestsRunOnSceneChanged();

        HandleRunTestsButton();

        _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
        var defaultGuiColor = GUI.color;
        foreach (var testClass in _testResults)
        {
            GUI.color = testClass.Passed ? Color.green : Color.yellow;
            var passed = testClass.TestCasesResults.Count(s => s.Passed);
            var failed = testClass.TestCasesResults.Count - passed;
            testClass.ExpandGroup = EditorGUILayout.Foldout(testClass.ExpandGroup,
                $"{testClass.Name} Passed: {passed} Failed: {failed} ({testClass.ElapsedTime}ms)", true);

            if (!testClass.ExpandGroup) continue;
            EditorGUI.indentLevel++;

            foreach (var testCase in testClass.TestCasesResults)
            {
                EditorGUILayout.BeginHorizontal();
                GUI.color = Color.white;
                if (testCase.AssertionDetails.IsNotEmpty())
                {
                    if (GUILayout.Button("Details", GUILayout.Width(50)))
                    {
                        _detailsCase = testCase;
                    }
                }
                else if (testCase.ExceptionDetails.IsNotEmpty())
                {
                    if (GUILayout.Button("Error", GUILayout.Width(50)))
                    {
                        _detailsCase = testCase;
                    }
                }


                GUI.color = testCase.Passed ? Color.green : Color.yellow;
                EditorGUILayout.LabelField("CASE " + testCase.Name + $" ({testCase.ElapsedTime}ms)");
                EditorGUILayout.EndHorizontal();

                if (ReferenceEquals(testCase, _detailsCase))
                {
                    var isError = testCase.ExceptionDetails.IsNotEmpty();
                    GUI.color = isError ? Color.yellow : Color.white;
                    EditorGUILayout.HelpBox(
                        (isError ? testCase.ExceptionDetails : testCase.AssertionDetails),
                        MessageType.Info);
                }

                if (!testCase.ErrorMessage.IsNotEmpty()) continue;
                GUI.color = Color.white;
                EditorGUILayout.BeginHorizontal();
                EditorGUI.indentLevel++;


                EditorGUILayout.LabelField(testCase.ErrorMessage);
                EditorGUI.indentLevel--;
                EditorGUILayout.EndHorizontal();
            }

            EditorGUI.indentLevel--;
            EditorGUILayout.Separator();
            EditorGUILayout.Space();
        }

        EditorGUILayout.EndScrollView();

        GUI.color = defaultGuiColor;
    }

    private void HandleRunTestsButton()
    {
        if (!GUILayout.Button("Run tests", GUILayout.MaxWidth(EditorGUIUtility.currentViewWidth - 20))) return;
        FindTestGroups();
        _detailsCase = null;
        _testResults = RunTests(FindTestGroups(), string.Empty);
    }

    private void HandleAutoTestsRunOnSceneChanged()
    {
        switch (_runTestOnSceneOpenedEvent)
        {
            case true:
            {
                if (!_runTestOnSceneOpenedSubscribed)
                {
                    EditorSceneManager.sceneOpened += EditorSceneManagerOnSceneOpened(this);
                    _runTestOnSceneOpenedSubscribed = true;
                }

                break;
            }
            case false:
            {
                if (_runTestOnSceneOpenedSubscribed)
                {
                    EditorSceneManager.sceneOpened -= EditorSceneManagerOnSceneOpened(this);
                    _runTestOnSceneOpenedSubscribed = true;
                }

                break;
            }
        }
    }

    private static List<TestClassAttribute> RunTests(List<Type> testClasses, string sceneFilter)
    {
        var results = new List<TestClassAttribute>();
        foreach (var classTest in testClasses)
        {
            var testClassAttribute = classTest.GetCustomAttribute(typeof(TestClassAttribute)) as TestClassAttribute;
            if (sceneFilter.IsNotEmpty() && testClassAttribute?.SceneName != sceneFilter) continue;

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

            if (sceneFilter.IsEmpty())
                RunPreparationMethodsSet(setupMethods, testClassInstance, testClassAttribute);
            RunTestCases(beforeEveryTest, afterEveryTest, testMethods, testClassInstance, testClassAttribute);
            if (sceneFilter.IsEmpty())
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

    private static Exception GetDeepestException(Exception ex)
    {
        return ex.InnerException != null ? GetDeepestException(ex.InnerException) : ex;
    }


    private static void RunTestCases(HashSet<MethodInfo> beforeMethods, HashSet<MethodInfo> afterMethods,
        HashSet<MethodInfo> testMethods, object testClassInstance,
        TestClassAttribute testClassAttribute)
    {
        var generalStopWatch = Stopwatch.StartNew();
        foreach (var testMethod in testMethods)
        {
            var testCases = testMethod.GetCustomAttributes<TestCaseAttribute>();
            foreach (var testCase in testCases)
            {
                var testCaseStopWatch = Stopwatch.StartNew();

                try
                {
                    foreach (var mInfo in beforeMethods) mInfo.Invoke(testClassInstance, null);
                    object parameters = testCase.Parameters;

                    var methodReturnValue = testMethod.Invoke(testClassInstance, (object[]) parameters);

                    switch (methodReturnValue)
                    {
                        case IEnumerator enumerator:
                            var result = HandleCoroutineMethods(enumerator);
                            for (var i = 0; i < result.Count; i++)
                            {
                                testCase.AssertionDetails += $"Iteration [{i}] returned {result[i]}\n";
                            }

                            break;
                        case List<SimpleAssert> simpleAsserts:
                        {
                            for (var i = 0; i < simpleAsserts.Count; i++)
                                if (simpleAsserts[i].Details.IsNotEmpty())
                                    testCase.AssertionDetails += $"{(i + 1)}. {simpleAsserts[i].Details}\n";
                            break;
                        }
                        default:
                        {
                            if (methodReturnValue != testCase.ExpectedResult)
                                throw new Assertion.AssertionException(
                                    $"Expected return value is as expected. Expected: {testCase.ExpectedResult}, Returned: {methodReturnValue}");
                            break;
                        }
                    }

                    testCase.Passed = true;
                    testCase.ErrorMessage = "";
                    testClassAttribute.TestCasesResults.Add(testCase);

                    foreach (var mInfo in afterMethods) mInfo.Invoke(testClassInstance, null);
                }
                catch (Exception ex1)
                {
                    if (GetDeepestException(ex1) is Assertion.AssertionException assertionException1)
                    {
                        testCase.ErrorMessage = assertionException1.Message;
                        testClassAttribute.TestCasesResults.Add(testCase);
                        try
                        {
                            foreach (var mInfo in afterMethods) mInfo.Invoke(testClassInstance, null);
                        }
                        catch (Exception ex2)
                        {
                            var deepestException = GetDeepestException(ex2);
                            testCase.ErrorMessage = deepestException.Message;
                            testCase.ExceptionDetails = deepestException.StackTrace;
                            testClassAttribute.TestCasesResults.Add(testCase);
                        }
                    }
                    else
                    {
                        var deepestException = GetDeepestException(ex1);
                        testCase.ErrorMessage =
                            "Unknown Exception (not AssertionException) occured.";
                        testCase.ExceptionDetails =
                            "Following exception was thrown in your game code, not in Assertion methods chain.\n\n" +
                            deepestException.Message + "\n" + deepestException.StackTrace;
                        testClassAttribute.TestCasesResults.Add(testCase);
                    }
                }

                testCase.ElapsedTime = testCaseStopWatch.ElapsedMilliseconds;
            }
        }

        testClassAttribute.ElapsedTime = generalStopWatch.ElapsedMilliseconds;
        generalStopWatch.Stop();
    }

    private static List<object> HandleCoroutineMethods(IEnumerator enumerator)
    {
        var returnedValues = new List<object>();
        while (enumerator.MoveNext())
        {
            returnedValues.Add(enumerator.Current);
            if (enumerator.Current is IEnumerator subEnumerator)
                returnedValues.AddRange(HandleCoroutineMethods(subEnumerator));
        }

        return returnedValues;
    }
}