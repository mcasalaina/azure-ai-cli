using Microsoft.VisualStudio.TestPlatform.ObjectModel;

namespace Azure.AI.Details.Common.CLI.TestFramework
{
    public interface IYamlTestFrameworkReporter
    {
        void RecordStart(TestCase testCase);
        void RecordResult(TestResult testResult);
        void RecordEnd(TestCase testCase, TestOutcome outcome);
    }
}